using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Testing.Integration.Fixtures;

namespace ForeverBloom.Testing.Integration.Seeding;

public static class ApplicationFixtureSeeds
{
    public static async Task<CreateCategoryResult> GivenCategoryAsync(
        this ApplicationTestFixture fixture,
        string? name = null,
        string? description = null,
        string? slug = null,
        string? imagePath = null,
        string? imageAltText = null,
        long? parentCategoryId = null,
        int displayOrder = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var token = Guid.NewGuid().ToString("N");

        var generatedName = name ?? $"Category-{token}";
        var generatedSlug = slug ?? $"cat-{token}";

        var truncatedName = Truncate(generatedName, SeoTitle.MaxLength);
        var truncatedSlug = Truncate(generatedSlug, Slug.MaxLength);

        var command = new CreateCategoryCommand(
            Name: truncatedName,
            Description: description,
            Slug: truncatedSlug,
            ImagePath: imagePath,
            ImageAltText: imageAltText,
            ParentCategoryId: parentCategoryId,
            DisplayOrder: displayOrder);

        var result = await fixture.SendAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            var errorMessage = result.Error is null
                ? "Unknown error."
                : $"{result.Error.Code}: {result.Error.Message}";
            throw new InvalidOperationException(
                $"Failed to seed category via {nameof(CreateCategoryCommand)}. {errorMessage}");
        }

        return result.Value;
    }
    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
