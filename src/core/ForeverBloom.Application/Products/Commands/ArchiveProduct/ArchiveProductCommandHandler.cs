using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.ArchiveProduct;

internal sealed class ArchiveProductCommandHandler
    : ICommandHandler<ArchiveProductCommand, ArchiveProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ITimeProvider _timeProvider;

    public ArchiveProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ArchiveProductResult>> Handle(
        ArchiveProductCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve product by ID, including archived
        var product = await _productRepository.GetByIdIncludingArchivedAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<ArchiveProductResult>.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        // Check row version for optimistic concurrency
        if (product.RowVersion != command.RowVersion)
        {
            return Result<ArchiveProductResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        var updateResult = product.Archive(_timeProvider.UtcNow);
        if (updateResult.IsFailure)
        {
            return Result<ArchiveProductResult>.Failure(updateResult.Error);
        }

        // Only persist if changes were made
        if (updateResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new ArchiveProductResult(
            product.DeletedAt!.Value,
            product.RowVersion);

        return Result<ArchiveProductResult>.Success(result);
    }
}
