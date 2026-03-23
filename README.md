## LambdaTale v3 - TryOut

On this branch I am currently trying out if it will be easier to run scenarios with a "custom" TestFramework all the way
down.

This code is based on the "ObservationExample" in [xunit/samples.xunit](https://github.com/xunit/samples.xunit/)

This is a lot more code and work than to do it using all the `IXunitXYZ` interfaces, but maybe the better way. Time will tell.

__*Use this branch at your own peril*__

Current state of the implementation:
- LambdaTale Scenarios are run using a completely separate Execution-Pipeline
  - Running of Testassemblies is 'unified' using a custom wrapper to run discovery once for ""normal"" XUnit Tests and for Scenarios
  - After the discovery phase the tests are dispatched to the LambdaTale and XUnit execution pipeline sequentially
- To use LambdaTale it is required to add `[assembly: TestFramework(typeof(CombinedTestFramework))]` to the test assembly
- Test project currently has a bit of a weird requirement for xunit.v3 packages -> due to missing nuspec and usage of extensionability

### Features that should probably be there for a "MVP" scope
- [x] Specify a Scenario test using `[Scenario]`
- [x] Discover execution steps for each scenario method using the `.x(() => {})` extension method
- [x] Execute each discovered step as a test
- [x] Allow for mixing `[Scenario]` with `[Fact]` in single source file
- [x] Share context between steps inside a `[Scenario]` method
- [x] Allow setting up state before executing the first step
- [x] Allow specifying variables as function parameters
- [x] Use class member as data in specs
- [x] Support both `Action` and `Func<Task>` as steps
- [x] Try out the new version in a actual project
- [x] Support `[InlineData]`, `[MemberData]`, and `[ClassData]` to supply testdata
- [x] Show tests correctly in the testrunners
- [x] Support skipping scenarios with `Skip` parameter
- [x] Support `ContinueOnError` and `StopOnError` step behavior
- [x] Reintroduce way for `[Background]` and `[Teardown]` function
- [ ] After a first step clean up the implementation
- [ ] Nuget package and infrastructure around that
