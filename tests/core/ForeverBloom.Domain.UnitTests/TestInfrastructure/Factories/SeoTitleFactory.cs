using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class SeoTitleFactory
{
    public static SeoTitle Create(string value = "Test SEO title")
    {
        var seoTitleResult = SeoTitle.Create(value);
        seoTitleResult.Should().BeSuccess();
        return seoTitleResult.Value!;
    }
}
