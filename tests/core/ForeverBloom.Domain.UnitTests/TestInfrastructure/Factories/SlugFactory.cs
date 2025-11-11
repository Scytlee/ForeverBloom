using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class SlugFactory
{
    public static Slug Create(string value = "test-slug")
    {
        var slugResult = Slug.Create(value);
        slugResult.Should().BeSuccess();
        return slugResult.Value!;
    }
}
