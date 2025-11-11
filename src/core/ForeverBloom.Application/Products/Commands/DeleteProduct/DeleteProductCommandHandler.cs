using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.DeleteProduct;

internal sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public DeleteProductCommandHandler(
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

    public async Task<Result> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve product by ID, including archived
        var product = await _productRepository.GetByIdIncludingArchivedAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        // Check row version for optimistic concurrency
        if (product.RowVersion != command.RowVersion)
        {
            return Result.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // Check if product is archived
        if (!product.DeletedAt.HasValue)
        {
            return Result.Failure(
                new ProductErrors.CannotDeleteNotArchived(command.ProductId));
        }

        // Check if deletion grace period has elapsed
        var eligibleAt = product.DeletedAt.Value.AddHours(Product.DeletionGracePeriodInHours);
        if (_timeProvider.UtcNow < eligibleAt)
        {
            return Result.Failure(
                new ProductErrors.CannotDeleteTooSoon(
                    command.ProductId,
                    product.DeletedAt.Value,
                    eligibleAt));
        }

        // Permanently delete the product
        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Clean up all slug registrations for the product
        await _slugRegistrationService.UnregisterAllSlugsOfEntityAsync(
            EntityType.Product,
            command.ProductId,
            cancellationToken);

        return Result.Success();
    }
}
