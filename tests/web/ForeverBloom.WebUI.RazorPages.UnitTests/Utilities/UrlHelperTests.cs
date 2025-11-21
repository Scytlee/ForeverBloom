using FluentAssertions;
using ForeverBloom.WebUI.RazorPages.Utilities;

namespace ForeverBloom.WebUI.RazorPages.UnitTests.Utilities;

public sealed class UrlHelperTests
{
    [Fact]
    public void ExtractSlugFromUrl_ShouldReturnLastSegment_WhenUrlHasMultipleSegments()
    {
        var result = UrlHelper.ExtractSlugFromUrl("/api/v1/categories/flower-bouquets");

        result.Should().Be("flower-bouquets");
    }

    [Fact]
    public void ExtractSlugFromUrl_ShouldReturnSlug_WhenUrlHasSingleSegment()
    {
        var result = UrlHelper.ExtractSlugFromUrl("flower-bouquets");

        result.Should().Be("flower-bouquets");
    }

    [Fact]
    public void ExtractSlugFromUrl_ShouldReturnLastSegment_WhenUrlHasTrailingSlash()
    {
        var result = UrlHelper.ExtractSlugFromUrl("/api/v1/categories/flower-bouquets/");

        result.Should().Be("flower-bouquets");
    }

    [Fact]
    public void ExtractSlugFromUrl_ShouldReturnNull_WhenUrlIsEmpty()
    {
        var result = UrlHelper.ExtractSlugFromUrl("");

        result.Should().BeNull();
    }
}
