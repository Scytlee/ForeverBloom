using FluentAssertions;
using ForeverBloom.Application.Abstractions.Errors;
using ForeverBloom.Application.Categories.Commands.DeleteCategory;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;
using ForeverBloom.Testing.Result;

namespace ForeverBloom.Application.UnitTests.Categories;

public sealed class DeleteCategoryCommandTests
{
    private static Result<DeleteCategoryCommand> CreateCommandWith(
        long categoryId = 1,
        uint rowVersion = 1)
    {
        return DeleteCategoryCommand.Create(categoryId, rowVersion);
    }

    [Fact]
    public void Create_ShouldCorrectlyConstructCommand_WhenAllInputIsValid()
    {
        const long categoryId = 1;
        const uint rowVersion = 1;

        var createResult = DeleteCategoryCommand.Create(categoryId, rowVersion);

        createResult.Should().BeSuccess();
        var command = createResult.Value!;
        command.CategoryId.Should().Be(categoryId);
        command.RowVersion.Should().Be(rowVersion);
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
    public void Create_ShouldReturnFailureWithMultipleErrors_WhenMultipleParametersAreInvalid()
    {
        const long invalidCategoryId = 0;
        const uint invalidRowVersion = 0;

        var createResult = CreateCommandWith(
            categoryId: invalidCategoryId,
            rowVersion: invalidRowVersion);

        createResult.Should().BeFailure();
        createResult.Should().HaveError<CategoryErrors.CategoryIdInvalid>();
        createResult.Should().HaveError<ApplicationErrors.RowVersionInvalid>();
    }
}
