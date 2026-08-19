using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

// Stateless execution engine for a single test case: instantiates the test class,
// drives the step loop, and then runs disposal hooks. All inputs are passed explicitly
// so the output depends purely on (context, arguments) with no shared instance state.
internal static class ScenarioCaseRunner
{
    public static async ValueTask<RunSummary> RunDelayEnumerated(ScenarioTestCaseRunnerContext ctxt)
    {
        var summary = new RunSummary();
        await using var disposalTracker = new DisposalTracker();

        foreach (var dataAttr in ctxt.TestCase.TestMethod.DataAttributes)
        {
            var rows = await dataAttr.GetData(ctxt.TestCase.TestMethod.Method, disposalTracker);
            foreach (var row in rows)
            {
                summary.Aggregate(await RunWithArguments(ctxt, row.GetData()));
            }
        }

        return summary;
    }

    public static async ValueTask<RunSummary> RunWithArguments(
        ScenarioTestCaseRunnerContext ctxt,
        object?[]? methodArguments)
    {
        var testCase = ctxt.TestCase;
        var summary = new RunSummary();

        // One helper per case, re-initialized per step by ScenarioStepRunner to attribute output correctly.
        var outputHelper = new TestOutputHelper();
        object? testClassInstance = null;

        var mainStepCount = 0;
        var constructorFailed = false;
        try
        {
            using var ctx = Scenario.Acquire();

            Exception? ctorFailure = null;
            var ctorElapsed = await ExecutionTimer.MeasureAsync(async () =>
            {
                try
                {
                    // Assigned before initializing so a failing InitializeAsync still gets disposed.
                    testClassInstance = CreateTestClassInstance(testCase.TestClass.Class, ctxt.ConstructorArguments, outputHelper);

                    if (testClassInstance is IAsyncLifetime asyncLifetime)
                    {
                        await asyncLifetime.InitializeAsync();
                    }
                }
                catch (Exception ex)
                {
                    // The activator wraps whatever the constructor threw.
                    ctorFailure = ex is TargetInvocationException tie ? tie.InnerException ?? tie : ex;
                }
            });

            if (ctorFailure is not null)
            {
                summary.Aggregate(await RunSyntheticStep(ctxt, "(Constructor)", ctorFailure, ctorElapsed));
                constructorFailed = true;
            }

            if (!constructorFailed)
            {
                var invocationArguments = methodArguments;
                var parameters = testCase.TestMethod.Parameters;
                var providedCount = invocationArguments?.Length ?? 0;
                if (providedCount < parameters.Count)
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
                summary.Aggregate(await RunStepLoop(ctxt, mainSteps, stepIndexOffset: 0, methodArguments, outputHelper));
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
                        summary.Aggregate(await RunSyntheticStep(ctxt, "(Dispose)", disposeFailure, disposeElapsed));
                        // fall through — do NOT return (would suppress in-flight exception)
                    }
                    else
                    {
                        var tdSteps = Scenario.TestDefinitions.ToList();
                        summary.Aggregate(await RunStepLoop(ctxt, tdSteps, stepIndexOffset: disposeOffset, methodArguments, outputHelper));
                    }
                }
                catch (Exception tdEx)
                {
                    // Dispose processing threw unexpectedly (e.g. from RunStepLoop or message bus). Record to summary
                    // but do not re-throw — this is a finally block, re-throwing would swallow any
                    // in-flight exception from the try block.
                    summary.Aggregate(await RunSyntheticStep(ctxt, "(Dispose)", tdEx, TimeSpan.Zero));
                }
            }
        }

        return summary;
    }

    public static ValueTask<RunSummary> RunSkippedCase(ScenarioTestCaseRunnerContext ctxt, string skipReason) =>
        ScenarioStepRunner.Instance.RunStep(new ScenarioStepRunnerContext(
            SyntheticStep(ctxt, ctxt.TestCase.TestCaseDisplayName),
            ctxt,
            new TestOutputHelper(),
            static () => default,
            skipReason));

    // Reports a failure that happened outside any step (construction, disposal, timeout) as a
    // pseudo-step, so it still surfaces as a test result. Returning a faulted task rather than
    // throwing keeps the original exception's stack trace intact.
    public static ValueTask<RunSummary> RunSyntheticStep(
        ScenarioTestCaseRunnerContext ctxt,
        string displayName,
        Exception failure,
        TimeSpan elapsed) =>
        ScenarioStepRunner.Instance.RunStep(new ScenarioStepRunnerContext(
            SyntheticStep(ctxt, QualifiedStepName(ctxt, displayName)),
            ctxt,
            new TestOutputHelper(),
            () => ValueTask.FromException(failure),
            elapsedOverride: elapsed));

    private static async ValueTask<RunSummary> RunStepLoop(
        ScenarioTestCaseRunnerContext ctxt,
        List<ScenarioTestDefinition> steps,
        int stepIndexOffset,
        object?[]? rowArgs,
        TestOutputHelper outputHelper)
    {
        var summary = new RunSummary();
        var stopped = false;

        for (var i = 0; i < steps.Count; i++)
        {
            var td = steps[i];
            var step = Step(ctxt, stepIndexOffset + i, td.Tale, rowArgs);
            var stepSummary = await ScenarioStepRunner.Instance.RunStep(new ScenarioStepRunnerContext(
                step,
                ctxt,
                outputHelper,
                () => InvokeTale(td.Lambda),
                skipReason: stopped ? "Previous step failed" : null));

            summary.Aggregate(stepSummary);

            if (stepSummary.Failed > 0 && td.OnError == OnError.Stop)
            {
                stopped = true;
            }
        }

        return summary;
    }

    private static XunitTest Step(
        ScenarioTestCaseRunnerContext ctxt,
        int displayIndex,
        string tale,
        object?[]? rowArgs)
    {
        var stepName = ctxt.TestCase.IsDelayEnumerated && rowArgs is { Length: > 0 }
            ? $"({string.Join(", ", rowArgs.Select(FormatArg))}) [{displayIndex}] {tale}"
            : $"[{displayIndex}] {tale}";
        var displayName = QualifiedStepName(ctxt, stepName);

        // Folds the row's own arguments into the ID (not just its position) so a delay-enumerated
        // scenario's step IDs stay stable across runs even if the data source reorders or resizes.
        var uniqueId = ComputeStepUniqueID(ctxt.TestCase, displayIndex, rowArgs);
        return NewStep(ctxt, displayName, uniqueId, rowArgs);
    }

    private static string QualifiedStepName(ScenarioTestCaseRunnerContext ctxt, string stepName) =>
        $"{ctxt.TestCase.TestCaseDisplayName}: {stepName}";

    private static XunitTest SyntheticStep(ScenarioTestCaseRunnerContext ctxt, string displayName) =>
        NewStep(ctxt, displayName, UniqueIDGenerator.ForTest(ctxt.TestCase.UniqueID, ctxt.NextTestIndex()), rowArgs: null);

    private static XunitTest NewStep(
        ScenarioTestCaseRunnerContext ctxt,
        string displayName,
        string uniqueId,
        object?[]? rowArgs) =>
        new(ctxt.TestCase,
            ctxt.TestCase.TestMethod,
            ctxt.TestCase.DisableParallelization,
            @explicit: null,
            skipReason: null,
            skipType: null,
            skipUnless: null,
            skipWhen: null,
            displayName,
            testLabel: null,
            uniqueId,
            ctxt.Traits,
            timeout: null,
            rowArgs ?? []);

    private static string ComputeStepUniqueID(ScenarioTestCase testCase, int displayIndex, object?[]? rowArgs)
    {
        using var g = new UniqueIDGenerator();
        g.Add(testCase.UniqueID);
        g.Add(displayIndex.ToString());
        foreach (var arg in rowArgs ?? [])
        {
            g.Add(ScenarioTestCase.SerializeArgForId(arg));
        }

        return g.Compute();
    }

    private static string FormatArg(object? arg) => arg switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => arg.ToString() ?? "null",
    };

    private static ValueTask InvokeTale(TaleBody lambda)
    {
        switch (lambda)
        {
            case TaleBody.SynchronousTaleBody sync:
                sync.Body.Invoke();
                return default;
            case TaleBody.AsynchronousTaleBody asyncBody:
                return new ValueTask(asyncBody.Body.Invoke());
            default:
                throw new NotSupportedException($"Unknown lambda type: {lambda.GetType()}");
        }
    }

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

    private static async ValueTask<(Exception? failure, TimeSpan elapsed)> DisposeTestClassInstance(object instance)
    {
        var tracker = new DisposalTracker();
        tracker.Add(instance);

        var aggregator = new ExceptionAggregator();
        var elapsed = await ExecutionTimer.MeasureAsync(() => aggregator.RunAsync(tracker.DisposeAsync));

        return (aggregator.ToException(), elapsed);
    }
}
