using FluentAssertions;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.Domain.UnitTests.TestInfrastructure.Factories;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Domain.UnitTests.Catalog;

public sealed class CategoryTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldSucceed_WithMinimalRequiredFields()
    {
        var name = SeoTitleFactory.Create("Test Category");
        var slug = SlugFactory.Create("test-category");
        var path = HierarchicalPathFactory.Create("test-category");

        var result = Category.Create(
            name,
            description: null,
            slug,
            image: null,
            path,
            parentCategoryId: null,
            displayOrder: 0,
            TestTimestamp);

        result.Should().BeSuccess();
        var category = result.Value!;
        category.Name.Should().Be(name);
        category.Description.Should().BeNull();
        category.CurrentSlug.Should().Be(slug);
        category.Image.Should().BeNull();
        category.Path.Should().Be(path);
        category.ParentCategoryId.Should().BeNull();
        category.DisplayOrder.Should().Be(0);
        category.PublishStatus.Should().Be(PublishStatus.Draft);
        category.IsDeleted.Should().BeFalse();
        category.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSucceed_WithAllFieldsPopulated()
    {
        var name = SeoTitleFactory.Create("Test Category");
        var description = MetaDescriptionFactory.Create("Test description");
        var slug = SlugFactory.Create("test-category");
        var image = ImageFactory.Create("/images/category.jpg");
        var path = HierarchicalPathFactory.Create("parent-category.test-category");
        const long parentCategoryId = 5;
        const int displayOrder = 10;

        var result = Category.Create(
            name,
            description,
            slug,
            image,
            path,
            parentCategoryId,
            displayOrder,
            TestTimestamp);

        result.Should().BeSuccess();
        var category = result.Value!;
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.CurrentSlug.Should().Be(slug);
        category.Image.Should().Be(image);
        category.Path.Should().Be(path);
        category.ParentCategoryId.Should().Be(parentCategoryId);
        category.DisplayOrder.Should().Be(displayOrder);
        category.PublishStatus.Should().Be(PublishStatus.Draft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldFail_ForInvalidParentCategoryId(long parentCategoryId)
    {
        var name = SeoTitleFactory.Create();
        var slug = SlugFactory.Create();
        var path = HierarchicalPathFactory.Create("test-category");

        var result = Category.Create(
            name,
            description: null,
            slug,
            image: null,
            path,
            parentCategoryId,
            displayOrder: 0,
            TestTimestamp);

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<CategoryErrors.ParentCategoryIdInvalid>();
        error.AttemptedId.Should().Be(parentCategoryId);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingName()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newName = SeoTitleFactory.Create("Updated Category");

        var result = category.Update(
            name: newName,
            description: default,
            image: default,
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Name.Should().Be(newName);
        category.UpdatedAt.Should().Be(TestTimestamp.AddHours(1));
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingDescription()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newDescription = MetaDescriptionFactory.Create("Updated description");

        var result = category.Update(
            name: default,
            description: newDescription,
            image: default,
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Description.Should().Be(newDescription);
    }

    [Fact]
    public void Update_ShouldSucceed_ClearingOptionalDescription()
    {
        var category = CategoryFactory.Create(TestTimestamp, description: "Test description");

        var result = category.Update(
            name: default,
            description: Optional<MetaDescription?>.FromValue(null),
            image: default,
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Description.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingImage()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newImage = ImageFactory.Create("/images/new-category.jpg");

        var result = category.Update(
            name: default,
            description: default,
            image: newImage,
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Image.Should().Be(newImage);
    }

    [Fact]
    public void Update_ShouldSucceed_ClearingOptionalImage()
    {
        var category = CategoryFactory.Create(TestTimestamp, imagePath: "/images/category.jpg");

        var result = category.Update(
            name: default,
            description: default,
            image: Optional<Image?>.FromValue(null),
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Image.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingDisplayOrder()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        const int newDisplayOrder = 42;

        var result = category.Update(
            name: default,
            description: default,
            image: default,
            displayOrder: newDisplayOrder,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingPublishStatus()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newStatus = PublishStatus.Published;

        var result = category.Update(
            name: default,
            description: default,
            image: default,
            displayOrder: default,
            publishStatus: newStatus,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.PublishStatus.Should().Be(newStatus);
    }

    [Fact]
    public void Update_ShouldSucceed_UpdatingMultipleFields()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newName = SeoTitleFactory.Create("Updated Category");
        var newDescription = MetaDescriptionFactory.Create("Updated description");
        const int newDisplayOrder = 5;

        var result = category.Update(
            name: newName,
            description: newDescription,
            image: default,
            displayOrder: newDisplayOrder,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Name.Should().Be(newName);
        category.Description.Should().Be(newDescription);
        category.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenNoFieldsSet()
    {
        var category = CategoryFactory.Create(TestTimestamp);

        var result = category.Update(
            name: default,
            description: default,
            image: default,
            displayOrder: default,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldReturnFalse_WhenFieldsUnchanged()
    {
        var category = CategoryFactory.Create(TestTimestamp);

        var result = category.Update(
            name: category.Name,
            description: default,
            image: default,
            displayOrder: category.DisplayOrder,
            publishStatus: default,
            TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldFail_ForInvalidPublishStatusTransition()
    {
        var currentStatus = PublishStatus.Published;
        var attemptedStatus = PublishStatus.Draft;
        var category = CategoryFactory.Create(TestTimestamp, publishStatus: currentStatus);

        var result = category.Update(
            name: default,
            description: default,
            image: default,
            displayOrder: default,
            publishStatus: attemptedStatus,
            TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<CategoryErrors.PublishStatusTransitionNotAllowed>();
        error.CurrentStatus.Should().Be(currentStatus);
        error.AttemptedStatus.Should().Be(attemptedStatus);
    }

    [Fact]
    public void ChangeSlug_ShouldSucceed_AndReturnTrue()
    {
        var category = CategoryFactory.Create(TestTimestamp, slug: "old-slug", path: "old-slug");
        var newSlug = SlugFactory.Create("new-slug");

        var result = category.ChangeSlug(newSlug, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.CurrentSlug.Should().Be(newSlug);
        category.Path.Value.Should().Be("new-slug");
        category.UpdatedAt.Should().Be(TestTimestamp.AddHours(1));
    }

    [Fact]
    public void ChangeSlug_ShouldReturnFalse_WhenSlugUnchanged()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var sameSlug = category.CurrentSlug;

        var result = category.ChangeSlug(sameSlug, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void ChangeSlug_ShouldUpdatePath_ForNestedCategory()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "child-slug",
            path: "parent.child-slug");
        var newSlug = SlugFactory.Create("new-child-slug");

        var result = category.ChangeSlug(newSlug, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.CurrentSlug.Should().Be(newSlug);
        category.Path.Value.Should().Be("parent.new-child-slug");
    }

    [Fact]
    public void Reparent_ShouldSucceed_ChangingParent()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "test-category",
            path: "old-parent.test-category",
            parentCategoryId: 1);
        var newParentPath = HierarchicalPathFactory.Create("new-parent");

        var result = category.Reparent(2, newParentPath, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.ParentCategoryId.Should().Be(2);
        category.Path.Value.Should().Be("new-parent.test-category");
        category.UpdatedAt.Should().Be(TestTimestamp.AddHours(1));
    }

    [Fact]
    public void Reparent_ShouldSucceed_SettingToRoot()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "test-category",
            path: "parent.test-category",
            parentCategoryId: 1);

        var result = category.Reparent(null, null, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.ParentCategoryId.Should().BeNull();
        category.Path.Value.Should().Be("test-category");
    }

    [Fact]
    public void Reparent_ShouldReturnFalse_WhenParentUnchanged()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            slug: "test-category",
            path: "parent.test-category",
            parentCategoryId: 1);
        var sameParentPath = HierarchicalPathFactory.Create("parent");

        var result = category.Reparent(1, sameParentPath, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Reparent_ShouldFail_ForInvalidParentCategoryId(long parentCategoryId)
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var newParentPath = HierarchicalPathFactory.Create("parent");

        var result = category.Reparent(parentCategoryId, newParentPath, TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<CategoryErrors.ParentCategoryIdInvalid>();
        error.AttemptedId.Should().Be(parentCategoryId);
    }

    [Fact]
    public void Reparent_ShouldFail_WhenCategoryIsOwnParent()
    {
        var category = CategoryFactory.Create(TestTimestamp, id: 5);
        var parentPath = HierarchicalPathFactory.Create("some-path");

        var result = category.Reparent(5, parentPath, TestTimestamp.AddHours(1));

        result.Should().BeFailure();
        var error = result.Should().HaveSingleError<CategoryErrors.CannotBeOwnParent>();
        error.CategoryId.Should().Be(5);
    }

    [Fact]
    public void RebasePath_ShouldSucceed_AndReturnTrue()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            path: "old-parent.child.grandchild");
        var oldBase = HierarchicalPathFactory.Create("old-parent");
        var newBase = HierarchicalPathFactory.Create("new-parent");

        var result = category.RebasePath(oldBase, newBase, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.Path.Value.Should().Be("new-parent.child.grandchild");
        category.UpdatedAt.Should().Be(TestTimestamp.AddHours(1));
    }

    [Fact]
    public void RebasePath_ShouldReturnFalse_WhenPathUnchanged()
    {
        var category = CategoryFactory.Create(
            TestTimestamp,
            path: "parent.child.grandchild");
        var sameBase = HierarchicalPathFactory.Create("parent");

        var result = category.RebasePath(sameBase, sameBase, TestTimestamp.AddHours(1));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        category.Path.Value.Should().Be("parent.child.grandchild");
    }

    [Fact]
    public void Archive_ShouldSucceed_AndSetDeletedAt()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        var archiveTime = TestTimestamp.AddHours(1);

        var result = category.Archive(archiveTime);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().Be(archiveTime);
        category.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Archive_ShouldReturnFalse_WhenAlreadyArchived()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        category.Archive(TestTimestamp.AddHours(1));

        var result = category.Archive(TestTimestamp.AddHours(2));

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        category.DeletedAt.Should().Be(TestTimestamp.AddHours(1)); // Unchanged
    }

    [Fact]
    public void Restore_ShouldSucceed_AndClearDeletedAt()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        category.Archive(TestTimestamp.AddHours(1));

        var result = category.Restore();

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        category.DeletedAt.Should().BeNull();
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldReturnFalse_WhenNotArchived()
    {
        var category = CategoryFactory.Create(TestTimestamp);

        var result = category.Restore();

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        category.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsDeleted_ShouldBeFalse_WhenDeletedAtIsNull()
    {
        var category = CategoryFactory.Create(TestTimestamp);

        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_ShouldBeTrue_WhenDeletedAtHasValue()
    {
        var category = CategoryFactory.Create(TestTimestamp);
        category.Archive(TestTimestamp.AddHours(1));

        category.IsDeleted.Should().BeTrue();
    }
}
