using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.CreateProduct;

internal sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISlugRegistrationService slugRegistrationService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _slugRegistrationService = slugRegistrationService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateProductResult>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Map command to value objects
        var valueObjectsResult = command.AssembleValueObjects();
        if (valueObjectsResult.IsFailure)
        {
            return Result<CreateProductResult>.Failure(valueObjectsResult.Error);
        }

        var valueObjects = valueObjectsResult.Value;

        // Slug must be available - cannot already be registered by any entity
        var slugAvailable = await _slugRegistrationService.IsSlugAvailableAsync(
            valueObjects.Slug,
            cancellationToken);

        if (!slugAvailable)
        {
            return Result<CreateProductResult>.Failure(
                new ProductErrors.SlugNotAvailable(valueObjects.Slug.Value));
        }

        // Category must exist
        var categoryExists = await _categoryRepository.ExistsAsync(
            command.CategoryId,
            cancellationToken);

        if (!categoryExists)
        {
            return Result<CreateProductResult>.Failure(
                new ProductErrors.CategoryNotFound(command.CategoryId));
        }

        var productResult = Product.Create(
            name: valueObjects.Name,
            seoTitle: valueObjects.SeoTitle,
            fullDescription: valueObjects.FullDescription,
            metaDescription: valueObjects.MetaDescription,
            slug: valueObjects.Slug,
            categoryId: command.CategoryId,
            price: valueObjects.Price,
            isFeatured: command.IsFeatured,
            availabilityStatus: command.AvailabilityStatus,
            timestamp: _timeProvider.UtcNow,
            images: valueObjects.Images);

        if (productResult.IsFailure)
        {
            return Result<CreateProductResult>.Failure(productResult.Error);
        }

        _productRepository.Add(productResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _slugRegistrationService.RegisterSlugAsync(
            EntityType.Product,
            productResult.Value.Id,
            productResult.Value.CurrentSlug,
            cancellationToken);

        return Result<CreateProductResult>.Success(
            new CreateProductResult(productResult.Value.Id));
    }
}
