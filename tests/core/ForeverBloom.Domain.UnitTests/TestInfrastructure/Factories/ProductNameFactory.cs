using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class ProductNameFactory
{
    public static ProductName Create(string value = "Test product")
    {
        var productNameResult = ProductName.Create(value);
        productNameResult.Should().BeSuccess();
        return productNameResult.Value!;
    }
}
