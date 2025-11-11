using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class HierarchicalPathFactory
{
    public static HierarchicalPath Create(string value)
    {
        var pathResult = HierarchicalPath.FromString(value);
        pathResult.Should().BeSuccess();
        return pathResult.Value!;
    }
}
