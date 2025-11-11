using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class UrlPathFactory
{
    public static UrlPath Create(string value = "/images/products")
    {
        var urlPathResult = UrlPath.Create(value);
        urlPathResult.Should().BeSuccess();
        return urlPathResult.Value!;
    }
}
