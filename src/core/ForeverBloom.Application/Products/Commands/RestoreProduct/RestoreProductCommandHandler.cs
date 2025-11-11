using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.RestoreProduct;

internal sealed class RestoreProductCommandHandler
    : ICommandHandler<RestoreProductCommand, RestoreProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;

    public RestoreProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
    }

    public async Task<Result<RestoreProductResult>> Handle(
        RestoreProductCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve product by ID, including archived products
        var product = await _productRepository.GetByIdIncludingArchivedAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<RestoreProductResult>.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        // Check row version for optimistic concurrency
        if (product.RowVersion != command.RowVersion)
        {
            return Result<RestoreProductResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        var updateResult = product.Restore();
        if (updateResult.IsFailure)
        {
            return Result<RestoreProductResult>.Failure(updateResult.Error);
        }

        // Only persist if changes were made
        if (updateResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new RestoreProductResult(
            product.DeletedAt,
            product.RowVersion);

        return Result<RestoreProductResult>.Success(result);
    }
}
