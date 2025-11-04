using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler
    : ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITimeProvider _timeProvider;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UpdateProductResult>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var valueObjectsResult = command.AssembleValueObjects();
        if (valueObjectsResult.IsFailure)
        {
            return Result<UpdateProductResult>.Failure(valueObjectsResult.Error);
        }

        var valueObjects = valueObjectsResult.Value;

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<UpdateProductResult>.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        if (product.RowVersion != command.RowVersion)
        {
            return Result<UpdateProductResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // Validate category exists if CategoryId is being updated
        if (command.CategoryId.IsSet)
        {
            var categoryExists = await _categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                return Result<UpdateProductResult>.Failure(
                    new ProductErrors.CategoryNotFound(command.CategoryId.Value));
            }
        }

        var updateResult = product.Update(
            valueObjects.Name,
            valueObjects.SeoTitle,
            valueObjects.FullDescription,
            valueObjects.MetaDescription,
            command.CategoryId,
            valueObjects.Price,
            command.DisplayOrder,
            command.IsFeatured,
            command.Availability,
            command.PublishStatus,
            _timeProvider.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result<UpdateProductResult>.Failure(updateResult.Error);
        }

        // Only persist if changes were made
        if (updateResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new UpdateProductResult(
            product.Name.Value,
            product.SeoTitle?.Value,
            product.FullDescription?.Value,
            product.MetaDescription?.Value,
            product.CategoryId,
            product.Price?.Value,
            product.DisplayOrder,
            product.IsFeatured,
            product.Availability,
            product.PublishStatus,
            product.UpdatedAt,
            product.RowVersion);

        return Result<UpdateProductResult>.Success(result);
    }
}
