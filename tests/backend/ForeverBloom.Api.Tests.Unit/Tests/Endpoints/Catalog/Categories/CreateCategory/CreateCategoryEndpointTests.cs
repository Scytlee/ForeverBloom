// using FluentAssertions;
// using ForeverBloom.Api.Contracts.Catalog.Categories.CreateCategory;
// using ForeverBloom.Api.Endpoints.Catalog.Categories.CreateCategory;
// using ForeverBloom.Domain.Shared.Validation;
// using ForeverBloom.Persistence.Abstractions;
// using ForeverBloom.Persistence.Abstractions.Repositories;
// using ForeverBloom.Persistence.Entities;
// using ForeverBloom.Testing.Common.BaseTestClasses;
// using ForeverBloom.Testing.Common.Helpers.Validation;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.EntityFrameworkCore;
// using NSubstitute;
// using NSubstitute.ExceptionExtensions;
//
// namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Categories.CreateCategory;
//
// public sealed class CreateCategoryEndpointTests : TestClassBase
// {
//     private readonly IUnitOfWork _unitOfWork;
//     private readonly ICreateCategoryEndpointQueryProvider _queryProvider;
//     private readonly ICategoryRepository _categoryRepository;
//
//     public CreateCategoryEndpointTests()
//     {
//         _unitOfWork = Substitute.For<IUnitOfWork>();
//         _queryProvider = Substitute.For<ICreateCategoryEndpointQueryProvider>();
//         _categoryRepository = Substitute.For<ICategoryRepository>();
//     }
//
//     private static CreateCategoryRequest CreateValidRequest()
//     {
//         return new CreateCategoryRequest
//         {
//             Name = "Test Category",
//             Description = "Test category description",
//             Slug = "test-category",
//             ParentCategoryId = 1,
//             DisplayOrder = 0,
//             IsActive = true,
//             ImagePath = "test-image.jpg"
//         };
//     }
//
//     [Fact]
//     public async Task HandleAsync_ShouldRethrowException_WhenCheckingSlugFails()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>())
//             .ThrowsAsync<DbUpdateException>();
//         
//         var act = async () => await CreateCategoryEndpoint.HandleAsync(
//             request, _unitOfWork, _categoryRepository, _queryProvider, TestContext.Current.CancellationToken);
//         
//         await act.Should().ThrowAsync<DbUpdateException>();
//     }
//
//     [Fact]
//     public async Task HandleAsync_ShouldReturnValidationProblem_WhenCategorySlugAlreadyExists()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(true);
//
//         var response = await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository,
//             _queryProvider, TestContext.Current.CancellationToken);
//         
//         await _queryProvider.Received(1).CategoryExistsBySlugAsync(request.Slug, TestContext.Current.CancellationToken);
//         response.Result.Should().BeOfType<ValidationProblem>();
//         var validationProblem = response.Result.As<ValidationProblem>();
//         validationProblem.ProblemDetails.Errors.ShouldContainErrorForProperty(nameof(request.Slug), CategoryValidation.ErrorCodes.SlugAlreadyExists);
//     }
//
//     [Fact]
//     public async Task HandleAsync_ShouldRethrowException_WhenCheckingParentCategoryFails()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).ThrowsAsync<DbUpdateException>();
//
//         var act = async () => await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository,
//             _queryProvider, TestContext.Current.CancellationToken);
//         
//         await act.Should().ThrowAsync<DbUpdateException>();
//     }
//
//     [Fact]
//     public async Task HandleAsync_ShouldReturnValidationProblem_WhenParentCategoryDoesNotExist()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).Returns(false);
//         
//         var response = await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository,
//             _queryProvider, TestContext.Current.CancellationToken);
//         
//         await _queryProvider.Received(1).CategoryExistsBySlugAsync(request.Slug, TestContext.Current.CancellationToken);
//         await _queryProvider.Received(1).CategoryExistsByIdAsync(request.ParentCategoryId.Value, TestContext.Current.CancellationToken);
//         response.Result.Should().BeOfType<ValidationProblem>();
//         var validationProblem = response.Result.As<ValidationProblem>();
//         validationProblem.ProblemDetails.Errors.ShouldContainErrorForProperty(nameof(request.ParentCategoryId), CategoryValidation.ErrorCodes.ParentCategoryNotFound);
//     }
//
//     [Fact]
//     public async Task HandleAsync_ShouldCorrectlyMapRequestToCategoryEntity()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).Returns(true);
//         Category? insertedCategory = null;
//         _categoryRepository.When(x => x.InsertCategory(Arg.Any<Category>())).Do(callInfo =>
//         {
//             insertedCategory = callInfo.Arg<Category>();
//         });
//         
//         await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository, _queryProvider, TestContext.Current.CancellationToken);
//         
//         insertedCategory.Should().NotBeNull();
//         insertedCategory!.Name.Should().Be(request.Name);
//         insertedCategory.Description.Should().Be(request.Description);
//         insertedCategory.Slug.Should().Be(request.Slug);
//         insertedCategory.ParentCategoryId.Should().Be(request.ParentCategoryId);
//         insertedCategory.DisplayOrder.Should().Be(request.DisplayOrder);
//         insertedCategory.IsActive.Should().Be(request.IsActive);
//         insertedCategory.ImagePath.Should().Be(request.ImagePath);
//     }
//     
//     [Fact]
//     public async Task HandleAsync_ShouldRethrowException_WhenInsertingCategoryFails()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).Returns(true);
//         _categoryRepository.When(x => x.InsertCategory(Arg.Any<Category>())).Throws<DbUpdateException>();
//         
//         var act = async () => await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository, _queryProvider, TestContext.Current.CancellationToken);
//         
//         await act.Should().ThrowAsync<DbUpdateException>();
//     }
//     
//     [Fact]
//     public async Task HandleAsync_ShouldRethrowException_WhenSavingChangesFails()
//     {
//         var request = CreateValidRequest();
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).Returns(true);
//         _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws<DbUpdateException>();
//         
//         var act = async () => await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository, _queryProvider, TestContext.Current.CancellationToken);
//         
//         await act.Should().ThrowAsync<DbUpdateException>();
//     }
//     
//     [Fact]
//     public async Task HandleAsync_ShouldPropagateCancellationToken()
//     {
//         var request = CreateValidRequest();
//         var cancellationToken = TestContext.Current.CancellationToken;
//         _queryProvider.CategoryExistsBySlugAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);
//         _queryProvider.CategoryExistsByIdAsync(request.ParentCategoryId!.Value, Arg.Any<CancellationToken>()).Returns(true);
//         
//         await CreateCategoryEndpoint.HandleAsync(request, _unitOfWork, _categoryRepository, _queryProvider, cancellationToken);
//         
//         await _queryProvider.Received(1).CategoryExistsBySlugAsync(request.Slug, cancellationToken);
//         await _queryProvider.Received(1).CategoryExistsByIdAsync(request.ParentCategoryId.Value, cancellationToken);
//         await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
//     }
// }
