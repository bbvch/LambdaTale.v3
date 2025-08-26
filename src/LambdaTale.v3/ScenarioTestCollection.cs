using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace LambdaTale.v3;

public sealed class ScenarioTestCollection(
    ScenarioTestAssembly testAssembly,
    string displayName) : ITestCollection
{
    public ScenarioTestAssembly ScenarioTestAssembly { get; } = Guard.ArgumentNotNull(testAssembly);

    public string? TestCollectionClassName => null;

    public string TestCollectionDisplayName => Guard.ArgumentNotNull(displayName);

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits { get; } =
        ExtensibilityPointFactory.GetCollectionTraits(testCollectionDefinition: null, testAssembly.Traits);

    public string UniqueID { get; } =
        UniqueIDGenerator.ForTestCollection(testAssembly.UniqueID, displayName, collectionDefinitionClassName: null);

    ITestAssembly ITestCollection.TestAssembly => this.ScenarioTestAssembly;
}
