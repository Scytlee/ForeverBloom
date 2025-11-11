using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class MetaDescriptionFactory
{
    public static MetaDescription Create(string value = "Test meta description")
    {
        var metaDescriptionResult = MetaDescription.Create(value);
        metaDescriptionResult.Should().BeSuccess();
        return metaDescriptionResult.Value!;
    }
}
