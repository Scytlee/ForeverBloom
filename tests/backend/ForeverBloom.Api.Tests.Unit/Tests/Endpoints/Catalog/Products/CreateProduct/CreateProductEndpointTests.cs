using FluentAssertions;
using ForeverBloom.Api.Contracts.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Endpoints.Catalog.Products.Admin.CreateProduct;
using ForeverBloom.Api.Results;
using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Domain.Shared.Validation;
using ForeverBloom.Persistence.Abstractions;
using ForeverBloom.Persistence.Abstractions.Repositories;
using ForeverBloom.Persistence.Entities;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Helpers.Assertion;
using ForeverBloom.Testing.Common.Helpers.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ForeverBloom.Api.Tests.Endpoints.Catalog.Products.CreateProduct;

public sealed class CreateProductEndpointTests : TestClassBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ISlugRegistryRepository _slugRegistryRepository;
    private readonly ICreateProductEndpointQueryProvider _queryProvider;

    public CreateProductEndpointTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _productRepository = Substitute.For<IProductRepository>();
        _slugRegistryRepository = Substitute.For<ISlugRegistryRepository>();
        _queryProvider = Substitute.For<ICreateProductEndpointQueryProvider>();
    }

    private static CreateProductRequest CreateValidRequest()
    {
        return new CreateProductRequest
        {
            Name = "Test Product",
            SeoTitle = "Test Product SEO",
            FullDescription = "This is a full description of the test product",
            MetaDescription = "Test product meta description",
            Slug = "test-product",
            Price = 99.99m,
            DisplayOrder = 1,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.Available,
            CategoryId = 1
        };
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationProblem_WhenSlugIsNotAvailable()
    {
        var request = CreateValidRequest();
        _queryProvider.IsSlugAvailableAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(false);

        var response = await CreateProductEndpoint.HandleAsync(request, _unitOfWork, _productRepository,
            _slugRegistryRepository, _queryProvider, NullLogger.Instance, TestContext.Current.CancellationToken);

        await _queryProvider.Received(1).IsSlugAvailableAsync(request.Slug, TestContext.Current.CancellationToken);
        response.Result.Should().BeOfType<ValidationProblemResult>();
        var validationProblem = response.Result.As<ValidationProblemResult>();
        validationProblem.Value.Errors.ShouldContainErrorForProperty(nameof(request.Slug), ProductValidation.ErrorCodes.SlugIsNotAvailable);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationProblem_WhenCategoryDoesNotExist()
    {
        var request = CreateValidRequest();
        _queryProvider.IsSlugAvailableAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(true);
        _queryProvider.CategoryExistsAsync(request.CategoryId, Arg.Any<CancellationToken>()).Returns(false);

        var response = await CreateProductEndpoint.HandleAsync(request, _unitOfWork, _productRepository,
            _slugRegistryRepository, _queryProvider, NullLogger.Instance, TestContext.Current.CancellationToken);

        await _queryProvider.Received(1).IsSlugAvailableAsync(request.Slug, TestContext.Current.CancellationToken);
        await _queryProvider.Received(1).CategoryExistsAsync(request.CategoryId, TestContext.Current.CancellationToken);
        response.Result.Should().BeOfType<ValidationProblemResult>();
        var validationProblem = response.Result.As<ValidationProblemResult>();
        validationProblem.Value.Errors.ShouldContainErrorForProperty(nameof(request.CategoryId), ProductValidation.ErrorCodes.CategoryNotFound);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoriesInCorrectOrder()
    {
        var request = CreateValidRequest();
        _queryProvider.IsSlugAvailableAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(true);
        _queryProvider.CategoryExistsAsync(request.CategoryId, Arg.Any<CancellationToken>()).Returns(true);

        await CreateProductEndpoint.HandleAsync(request, _unitOfWork, _productRepository, _slugRegistryRepository,
            _queryProvider, NullLogger.Instance, TestContext.Current.CancellationToken);

        Received.InOrder(() =>
        {
            _productRepository.InsertProduct(Arg.Any<Product>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _slugRegistryRepository.InsertSlugRegistryEntry(Arg.Any<SlugRegistryEntry>());
        });
    }

    [Fact]
    public async Task HandleAsync_ShouldCorrectlyHandleSuccessfulProductCreation()
    {
        var request = CreateValidRequest();
        _queryProvider.IsSlugAvailableAsync(request.Slug, Arg.Any<CancellationToken>()).Returns(true);
        _queryProvider.CategoryExistsAsync(request.CategoryId, Arg.Any<CancellationToken>()).Returns(true);

        const int assignedId = 123;

        Product? insertedProduct = null;
        _productRepository.When(x => x.InsertProduct(Arg.Any<Product>())).Do(callInfo =>
        {
            insertedProduct = callInfo.Arg<Product>();
            insertedProduct!.Id = assignedId;
        });

        SlugRegistryEntry? insertedSlugEntry = null;
        _slugRegistryRepository.When(x => x.InsertSlugRegistryEntry(Arg.Any<SlugRegistryEntry>())).Do(callInfo =>
        {
            insertedSlugEntry = callInfo.Arg<SlugRegistryEntry>();
        });

        var result = await CreateProductEndpoint.HandleAsync(request, _unitOfWork, _productRepository,
            _slugRegistryRepository, _queryProvider, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Verify product creation
        insertedProduct.Should().NotBeNull();
        AssertionHelpers.AssertAllPropertiesAreMapped(
            sourceObject: request,
            destinationObject: insertedProduct,
            overridesMap: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(request.Slug) , nameof(insertedProduct.CurrentSlug) }
            });

        // Verify slug registry entry creation
        insertedSlugEntry.Should().NotBeNull();
        insertedSlugEntry.Slug.Should().Be(request.Slug);
        insertedSlugEntry.EntityType.Should().Be(EntityType.Product);
        insertedSlugEntry.EntityId.Should().Be(insertedProduct.Id);
        insertedSlugEntry.IsActive.Should().BeTrue();

        // Verify response
        result.Result.Should().BeOfType<CreatedResult<CreateProductResponse>>();
        var createdResponse = result.Result.As<CreatedResult<CreateProductResponse>>();
        createdResponse.Location.Should().Be($"/api/v1/admin/products/{assignedId}");
    }
}
