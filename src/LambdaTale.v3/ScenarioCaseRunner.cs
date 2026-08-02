using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

// Stateless execution engine for a single test case: instantiates the test class,
// drives the step loop, and then runs disposal hooks. All inputs are passed explicitly
// so the output depends purely on (testCase, emitter, arguments) with no shared instance state.
internal static class ScenarioCaseRunner
{
    public static async ValueTask<RunSummary> RunDelayEnumerated(
        ScenarioTestCase testCase,
        ScenarioMessageEmitter emitter,
        object?[] constructorArguments)
    {
        var summary = new RunSummary();
        await using var disposalTracker = new DisposalTracker();

        foreach (var dataAttr in testCase.TestMethod.DataAttributes)
        {
            var rows = await dataAttr.GetData(testCase.TestMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                var args = row.GetData();
                summary.Aggregate(await RunWithArguments(testCase, emitter, constructorArguments, args));
            }
        }

        return summary;
    }

    public static async ValueTask<RunSummary> RunWithArguments(
        ScenarioTestCase testCase,
        ScenarioMessageEmitter emitter,
        object?[] constructorArguments,
        object?[]? methodArguments)
    {
        var summary = new RunSummary();

        // One helper per case, re-initialized per step in RunStepLoop to attribute output correctly.
        var outputHelper = new TestOutputHelper();
        object? testClassInstance = null;

        var mainStepCount = 0;
        var constructorFailed = false;
        try
        {
            using var ctx = Scenario.Acquire();

            var ctorStart = Stopwatch.StartNew();
            Exception? ctorFailure = null;
            try
            {
                testClassInstance = CreateTestClassInstance(testCase.TestClass.Class, constructorArguments, outputHelper);
            }
            catch (Exception ex)
            {
                ctorFailure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
            }
            finally
            {
                ctorStart.Stop();
            }

            if (ctorFailure is not null)
            {
                var ctorElapsed = (decimal)ctorStart.Elapsed.TotalSeconds;
                summary.Time += ctorElapsed;
                summary.Total++;

                if (IsSkipException(testCase, ctorFailure))
                {
                    await emitter.EmitSynthetic(
                        emitter.TestUniqueId(0), "(Constructor)", testCase.Traits, elapsed: 0m, new StepOutcome.Skipped(ctorFailure.Message));
                    summary.Skipped++;
                }
                else
                {
                    await emitter.ReportSyntheticFailure("(Constructor)", stepIndex: 0, ctorFailure, ctorElapsed);
                    summary.Failed++;
                }

                constructorFailed = true;
            }

            if (!constructorFailed)
            {
                var invocationArguments = methodArguments;
                var parameters = testCase.MethodParameters;
                var providedCount = invocationArguments?.Length ?? 0;
                if (providedCount < parameters.Length)
                {
                    invocationArguments =
                    [
                        .. invocationArguments ?? [],
                        .. parameters.Skip(providedCount)
                            .Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null),
                    ];
                }

                var scenarioResult = testCase.TestMethod.Method.Invoke(testClassInstance, invocationArguments);
                if (scenarioResult is Task scenarioTask)
                {
                    await scenarioTask;
                }

                var mainSteps = Scenario.TestDefinitions.ToList();
                mainStepCount = mainSteps.Count;
                summary.Aggregate(await RunStepLoop(testCase, emitter, mainSteps, stepIndexOffset: 0, methodArguments, outputHelper));
            }
        }
        finally
        {
            if (testClassInstance is not null)
            {
                var disposeOffset = constructorFailed ? 1 : mainStepCount;
                using var teardownCtx = Scenario.Acquire();

                try
                {
                    var (disposeFailure, disposeElapsed) = await DisposeTestClassInstance(testClassInstance);
                    if (disposeFailure != null)
                    {
                        summary.Time += disposeElapsed;
                        summary.Total++;
                        if (IsSkipException(testCase, disposeFailure))
                        {
                            await emitter.EmitSynthetic(
                                emitter.TestUniqueId(disposeOffset), "(Dispose)", testCase.Traits, elapsed: 0m,
                                new StepOutcome.Skipped(disposeFailure.Message));
                            summary.Skipped++;
                        }
                        else
                        {
                            await emitter.ReportSyntheticFailure("(Dispose)", disposeOffset, disposeFailure, disposeElapsed);
                            summary.Failed++;
                        }
                        // fall through — do NOT return (would suppress in-flight exception)
                    }
                    else
                    {
                        var tdSteps = Scenario.TestDefinitions.ToList();
                        summary.Aggregate(await RunStepLoop(testCase, emitter, tdSteps, stepIndexOffset: disposeOffset, methodArguments, outputHelper));
                    }
                }
                catch (Exception tdEx)
                {
                    // Dispose processing threw unexpectedly (e.g. from RunStepLoop or message bus). Record to summary
                    // but do not re-throw — this is a finally block, re-throwing would swallow any
                    // in-flight exception from the try block.
                    await emitter.ReportSyntheticFailure("(Dispose)", disposeOffset, tdEx, elapsed: 0m);
                    summary.Failed++;
                    summary.Total++;
                }
            }
        }

        return summary;
    }

    private static async ValueTask<RunSummary> RunStepLoop(
        ScenarioTestCase testCase,
        ScenarioMessageEmitter emitter,
        List<ScenarioTestDefinition> steps,
        int stepIndexOffset,
        object?[]? rowArgs,
        TestOutputHelper outputHelper)
    {
        var summary = new RunSummary();
        var stopped = false;

        // Row arguments are identical for every step, so serialize them once per row rather than
        // re-serializing inside each step's UniqueID.
        var serializedRowArgs = rowArgs is { Length: > 0 }
            ? Array.ConvertAll(rowArgs, static arg => ScenarioTestCase.SerializeArgForId(arg))
            : null;

        for (var i = 0; i < steps.Count; i++)
        {
            var td = steps[i];
            var step = new ScenarioStep(testCase, stepIndexOffset + i, td.Tale, rowArgs, serializedRowArgs);
            var testUniqueId = step.UniqueID;
            summary.Total++;

            if (stopped)
            {
                summary.Skipped++;
                await emitter.EmitSynthetic(testUniqueId, step.TestDisplayName, step.Traits, elapsed: 0m, new StepOutcome.Skipped("Previous step failed"));
                continue;
            }

            var start = DateTimeOffset.UtcNow;
            await emitter.EmitStarting(testUniqueId, step.TestDisplayName, step.Traits, start);

            Exception? failure = null;
            var sw = Stopwatch.StartNew();

            outputHelper.Initialize(emitter.MessageBus, step);
            TestContext.SetForTest(step, TestEngineStatus.Running, emitter.CancellationToken, testOutputHelper: outputHelper);
            try
            {
                switch (td.Lambda)
                {
                    case TaleBody.SynchronousTaleBody sync:
                        sync.Body.Invoke();
                        break;
                    case TaleBody.AsynchronousTaleBody asyncBody:
                        await asyncBody.Body.Invoke();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown lambda type: {td.Lambda.GetType()}");
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            sw.Stop();
            var output = outputHelper.Output;
            outputHelper.Uninitialize();
            var elapsed = (decimal)sw.Elapsed.TotalSeconds;
            var finish = DateTimeOffset.UtcNow;

            StepOutcome outcome;
            if (failure is null)
            {
                outcome = new StepOutcome.Passed();
            }
            else if (IsSkipException(testCase, failure))
            {
                summary.Skipped++;
                outcome = new StepOutcome.Skipped(failure.Message);
            }
            else
            {
                summary.Failed++;
                if (td.OnError == OnError.Stop)
                {
                    stopped = true;
                }

                outcome = new StepOutcome.Failed(failure);
            }

            summary.Time += elapsed;

            await emitter.EmitOutcome(testUniqueId, finish, elapsed, outcome, output);
        }

        return summary;
    }

    private static bool IsSkipException(ScenarioTestCase testCase, Exception ex) =>
        testCase.SkipExceptions is { } types && types.Any(t => t.IsInstanceOfType(ex));

    // Mirrors xUnit's constructor-argument resolution: ITestOutputHelper params get the managed
    // helper, deferred Func<T> placeholders are invoked, and the rest come from xUnit's fixtures.
    private static object CreateTestClassInstance(Type testClass, object?[] constructorArguments, ITestOutputHelper outputHelper)
    {
        var ctor = testClass.GetConstructors().Single(c => !c.IsStatic && c.IsPublic);
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType == typeof(ITestOutputHelper))
            {
                args[i] = outputHelper;
                continue;
            }

            var provided = i < constructorArguments.Length ? constructorArguments[i] : null;
            args[i] = provided is not null && provided.GetType() == typeof(Func<>).MakeGenericType(parameterType)
                ? provided.GetType().GetMethod("Invoke", Type.EmptyTypes)!.Invoke(provided, null)
                : provided;
        }

        return TypeActivator.Current.CreateInstance(
            ctor,
            args,
            static (_, missing) =>
                $"The following constructor parameters did not have matching fixture data: {string.Join(", ", missing.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
    }

    private static async ValueTask<(Exception? failure, decimal elapsedSeconds)> DisposeTestClassInstance(object instance)
    {
        var sw = Stopwatch.StartNew();
        Exception? failure = null;
        try
        {
            if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            failure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
        }

        sw.Stop();
        return (failure, (decimal)sw.Elapsed.TotalSeconds);
    }
}
