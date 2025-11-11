using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class ImageFactory
{
    public static Image Create(
        string source = "/images/test.jpg",
        string? altText = "Test image")
    {
        var imageResult = Image.Create(source, altText);
        imageResult.Should().BeSuccess();
        return imageResult.Value!;
    }
}
