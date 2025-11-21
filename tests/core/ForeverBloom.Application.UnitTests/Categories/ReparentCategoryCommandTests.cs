using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.ReparentCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class ReparentCategoryCommandTests
{
    private static readonly Optional<long?> DefaultParentCategoryId = Optional<long?>.FromValue(2L);

    private static Result<ReparentCategoryCommand> CreateCommandWith(
        long categoryId = 1,
        uint rowVersion = 1,
        Optional<long?> newParentCategoryId = default)
    {
        var parentCategoryId = newParentCategoryId.IsSet ? newParentCategoryId : DefaultParentCategoryId;

        return ReparentCategoryCommand.Create(categoryId, rowVersion, parentCategoryId);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValidAndNewParentIsProvided()
    {
        const long categoryId = 5;
        const uint rowVersion = 3;
        const long newParentCategoryId = 2;

        var createResult = ReparentCategoryCommand.Create(categoryId, rowVersion, newParentCategoryId);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
        command.NewParentCategoryId.Should().Be(newParentCategoryId);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenNewParentIsExplicitlySetToNull()
    {
        const long categoryId = 4;
        const uint rowVersion = 2;

        var createResult = ReparentCategoryCommand.Create(categoryId, rowVersion, (long?)null);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.NewParentCategoryId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnFailure_WhenCategoryIdIsInvalid(long invalidCategoryId)
    {
        var createResult = CreateCommandWith(categoryId: invalidCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenRowVersionIsInvalid()
    {
        const uint invalidRowVersion = 0;

        var createResult = CreateCommandWith(rowVersion: invalidRowVersion);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_ShouldReturnFailure_WhenNewParentCategoryIdIsInvalid(long invalidParentCategoryId)
    {
        var createResult = CreateCommandWith(newParentCategoryId: invalidParentCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.ParentCategoryIdInvalid>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenNewParentCategoryIdIsNotProvided()
    {
        var createResult = ReparentCategoryCommand.Create(
            categoryId: 1,
            rowVersion: 1,
            newParentCategoryId: Optional<long?>.Unset);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<ApplicationErrors.RequiredFieldMissing>();
    }

    [Fact]
    public void Create_ShouldReturnFailure_WhenNewParentCategoryIdEqualsCategoryId()
    {
        const long categoryId = 3;

        var createResult = CreateCommandWith(
            categoryId: categoryId,
            newParentCategoryId: categoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CannotBeOwnParent>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long categoryId = 0;
        const uint rowVersion = 0;
        const long invalidParentCategoryId = 0;

        var createResult = ReparentCategoryCommand.Create(categoryId, rowVersion, invalidParentCategoryId);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
        createResult.Should().HaveError<CategoryErrors.ParentCategoryIdInvalid>();
    }
}
