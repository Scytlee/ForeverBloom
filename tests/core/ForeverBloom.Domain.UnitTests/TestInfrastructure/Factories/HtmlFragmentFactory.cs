using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class HtmlFragmentFactory
{
    public static HtmlFragment Create(string value = "<p>Test</p>")
    {
        var htmlFragmentResult = HtmlFragment.Create(value);
        htmlFragmentResult.Should().BeSuccess();
        return htmlFragmentResult.Value!;
    }
}
