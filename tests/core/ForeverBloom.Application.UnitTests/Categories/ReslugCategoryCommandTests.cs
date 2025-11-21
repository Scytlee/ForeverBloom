using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.ReslugCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;
using ForeverBloom.Testing.ValueObjectAssertions;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class ReslugCategoryCommandTests
{
    private static Result<ReslugCategoryCommand> CreateCommandWith(
        long categoryId = 1,
        uint rowVersion = 1,
        string newSlug = "test-category")
    {
        return ReslugCategoryCommand.Create(categoryId, rowVersion, newSlug);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValid()
    {
        const long categoryId = 1;
        const uint rowVersion = 1;
        const string newSlug = "test-category";

        var createResult = ReslugCategoryCommand.Create(categoryId, rowVersion, newSlug);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
        command.NewSlug.Should().HaveValue(newSlug);
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

    [Fact]
    public void Create_ShouldReturnFailure_WhenNewSlugIsInvalid()
    {
        const string invalidNewSlug = "Obviously.Invalid_Slug";

        var createResult = CreateCommandWith(newSlug: invalidNewSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }

    [Fact]
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidCategoryId = 0;
        const uint invalidRowVersion = 0;
        const string invalidNewSlug = "Obviously.Invalid_Slug";

        var createResult = CreateCommandWith(
            categoryId: invalidCategoryId,
            rowVersion: invalidRowVersion,
            newSlug: invalidNewSlug);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
        createResult.Should().HaveError<SlugErrors.InvalidFormat>();
    }
}
