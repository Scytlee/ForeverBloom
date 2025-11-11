using ForeverBloom.Domain.Catalog;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;

internal static class CategoryFactory
{
    public static Category Create(
        DateTimeOffset timestamp,
        string name = "Test Category",
        string slug = "test-category",
        string? description = null,
        string? imagePath = null,
        string path = "test-category",
        long? parentCategoryId = null,
        int displayOrder = 1,
        PublishStatus? publishStatus = null,
        long? id = null)
    {
        var categoryResult = Category.Create(
            SeoTitleFactory.Create(name),
            description is null ? null : MetaDescriptionFactory.Create(description),
            SlugFactory.Create(slug),
            imagePath is null ? null : ImageFactory.Create(imagePath),
            HierarchicalPathFactory.Create(path),
            parentCategoryId,
            displayOrder,
            timestamp);

        categoryResult.Should().BeSuccess();
        var category = categoryResult.Value!;

        if (id > 0)
        {
            category.GetType().GetProperty(nameof(Category.Id))!.SetValue(category, id.Value);
        }

        if (publishStatus is not null && publishStatus != PublishStatus.Draft)
        {
            var updateResult = category.Update(
                name: default,
                description: default,
                image: default,
                displayOrder: default,
                publishStatus: publishStatus,
                timestamp: timestamp);
            updateResult.Should().BeSuccess();
        }

        return category;
    }
}
