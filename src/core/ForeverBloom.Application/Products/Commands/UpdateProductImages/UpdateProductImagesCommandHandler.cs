using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Products.Commands.UpdateProductImages;

internal sealed class UpdateProductImagesCommandHandler
    : ICommandHandler<UpdateProductImagesCommand, UpdateProductImagesResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ITimeProvider _timeProvider;

    public UpdateProductImagesCommandHandler(
        IUnitOfWork unitOfWork,
        IProductRepository productRepository,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UpdateProductImagesResult>> Handle(
        UpdateProductImagesCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result<UpdateProductImagesResult>.Failure(
                new ProductErrors.NotFoundById(command.ProductId));
        }

        if (product.RowVersion != command.RowVersion)
        {
            return Result<UpdateProductImagesResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        var existingImageIds = product.Images.Select(image => image.Id).ToArray();

        var missingIds = command.ImagesToDelete
            .Concat(command.ImagesToUpdate.Select(x => x.Id))
            .Where(id => !existingImageIds.Contains(id))
            .Distinct()
            .ToArray();

        if (missingIds.Length > 0)
        {
            var compositeError = new CompositeError(missingIds.Select(id => new ProductErrors.ImageNotFound(id)));
            return Result<UpdateProductImagesResult>.Failure(compositeError);
        }

        var newImages = CreateNewImageCollection(command, product);
        if (newImages.IsFailure)
        {
            return Result<UpdateProductImagesResult>.Failure(newImages.Error);
        }

        var applyResult = product.UpdateImages(newImages.Value, _timeProvider.UtcNow);
        if (applyResult.IsFailure)
        {
            return Result<UpdateProductImagesResult>.Failure(applyResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var orderedImages = product.Images
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new UpdateProductImagesResultImage(
                image.Id,
                image.Image.Source.Value,
                image.Image.AltText,
                image.IsPrimary,
                image.DisplayOrder))
            .ToArray();

        var result = new UpdateProductImagesResult(
            orderedImages,
            product.RowVersion);

        return Result<UpdateProductImagesResult>.Success(result);
    }

    private static Result<ICollection<ProductImage>> CreateNewImageCollection(UpdateProductImagesCommand command, Product product)
    {
        var existingImagesById = product.Images.ToDictionary(image => image.Id);

        var errors = new List<IError>();

        var newImageCollection = product.Images
            .Where(image => !command.ImagesToDelete.Contains(image.Id))
            .ToList();

        foreach (var update in command.ImagesToUpdate)
        {
            var image = existingImagesById[update.Id];
            var updateResult = image.Update(update.AltText, update.IsPrimary, update.DisplayOrder);
            if (updateResult.IsFailure)
            {
                errors.Add(updateResult.Error);
            }
        }

        foreach (var create in command.ImagesToCreate)
        {
            var imageResult = Image.Create(create.Source, create.AltText);
            if (imageResult.IsFailure)
            {
                errors.Add(imageResult.Error);
                continue;
            }

            var productImage = ProductImage.Create(
                imageResult.Value,
                create.IsPrimary,
                create.DisplayOrder);

            newImageCollection.Add(productImage);
        }

        return Result<ICollection<ProductImage>>.FromValidation(
            errors,
            () => newImageCollection);
    }
}
