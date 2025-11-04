using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.ReslugProduct;

internal sealed class ReslugProductCommandHandler
    : ICommandHandler<ReslugProductCommand, ReslugProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public ReslugProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ISlugRegistrationService slugRegistrationService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _slugRegistrationService = slugRegistrationService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReslugProductResult>> Handle(
        ReslugProductCommand command,
        CancellationToken cancellationToken)
    {
        // Convert slug string to value object
        var slugResult = Slug.Create(command.NewSlug);
        if (slugResult.IsFailure)
        {
            return Result<ReslugProductResult>.Failure(slugResult.Error);
        }

        var newSlug = slugResult.Value;

        // Retrieve product by ID
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<ReslugProductResult>.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        // Check row version for optimistic concurrency
        if (product.RowVersion != command.RowVersion)
        {
            return Result<ReslugProductResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // Check if slug is available for this product to use
        // (allows reuse of product's own historical slugs)
        var isAvailable = await _slugRegistrationService.IsSlugAvailableForEntityAsync(
            newSlug,
            EntityType.Product,
            product.Id,
            cancellationToken);

        if (!isAvailable)
        {
            return Result<ReslugProductResult>.Failure(
                new ProductErrors.SlugNotAvailable(newSlug.Value));
        }

        // Change the product's slug
        var updateResult = product.ChangeSlug(newSlug, _timeProvider.UtcNow);
        if (updateResult.IsFailure)
        {
            return Result<ReslugProductResult>.Failure(updateResult.Error);
        }

        // Only persist if changes were made
        if (updateResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Register the new slug (deactivate old, reactivate or create new entry)
            await _slugRegistrationService.RegisterSlugAsync(
                EntityType.Product,
                product.Id,
                newSlug,
                cancellationToken);
        }

        var result = new ReslugProductResult(
            product.CurrentSlug.Value,
            product.UpdatedAt,
            product.RowVersion);

        return Result<ReslugProductResult>.Success(result);
    }
}
