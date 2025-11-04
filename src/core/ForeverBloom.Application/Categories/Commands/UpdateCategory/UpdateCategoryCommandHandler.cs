using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITimeProvider _timeProvider;

    public UpdateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICategoryRepository categoryRepository,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _categoryRepository = categoryRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UpdateCategoryResult>> Handle(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var valueObjectsResult = command.AssembleValueObjects();
        if (valueObjectsResult.IsFailure)
        {
            return Result<UpdateCategoryResult>.Failure(valueObjectsResult.Error);
        }

        var valueObjects = valueObjectsResult.Value;

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<UpdateCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        if (category.RowVersion != command.RowVersion)
        {
            return Result<UpdateCategoryResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        var updateResult = category.Update(
            valueObjects.Name,
            valueObjects.Description,
            valueObjects.Image,
            command.DisplayOrder,
            command.PublishStatus,
            _timeProvider.UtcNow);

        if (updateResult.IsFailure)
        {
            return Result<UpdateCategoryResult>.Failure(updateResult.Error);
        }

        // Only persist if changes were made
        if (updateResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new UpdateCategoryResult(
            category.Name.Value,
            category.Description?.Value,
            category.Image?.Source.Value,
            category.Image?.AltText,
            category.DisplayOrder,
            category.PublishStatus,
            category.UpdatedAt,
            category.RowVersion);

        return Result<UpdateCategoryResult>.Success(result);
    }
}
