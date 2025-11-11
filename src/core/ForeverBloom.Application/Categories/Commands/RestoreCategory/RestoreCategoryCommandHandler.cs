using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.RestoreCategory;

internal sealed class RestoreCategoryCommandHandler
    : ICommandHandler<RestoreCategoryCommand, RestoreCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryHierarchyService _hierarchyService;

    public RestoreCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICategoryRepository categoryRepository,
        CategoryHierarchyService hierarchyService)
    {
        _unitOfWork = unitOfWork;
        _categoryRepository = categoryRepository;
        _hierarchyService = hierarchyService;
    }

    public async Task<Result<RestoreCategoryResult>> Handle(
        RestoreCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve category by ID, including archived
        var category = await _categoryRepository.GetByIdIncludingArchivedAsync(
            command.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<RestoreCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        // Check row version for optimistic concurrency
        if (category.RowVersion != command.RowVersion)
        {
            return Result<RestoreCategoryResult>.Failure(
                new ApplicationErrors.ConcurrencyConflict());
        }

        // Early no-op: if already restored, return immediately without fetching descendants
        if (category.DeletedAt is null)
        {
            return Result<RestoreCategoryResult>.Success(
                new RestoreCategoryResult(
                    category.DeletedAt,
                    category.RowVersion));
        }

        // Check for archived ancestors
        var hasArchivedAncestors = await _categoryRepository.HasArchivedAncestorsAsync(
            category.Path,
            category.Id,
            cancellationToken);

        if (hasArchivedAncestors)
        {
            return Result<RestoreCategoryResult>.Failure(
                new CategoryErrors.HasArchivedAncestors(command.CategoryId));
        }

        // Fetch descendants up to the maximum + 1 to ensure we don't exceed the limit
        var descendants = await _categoryRepository.GetDescendantsAsync(
            category.Path,
            category.Id,
            Category.DescendantLimitOnUpdate,
            cancellationToken);

        if (descendants.Count > Category.DescendantLimitOnUpdate)
        {
            return Result<RestoreCategoryResult>.Failure(
                new CategoryErrors.TooManyDescendants(
                    command.CategoryId));
        }

        // Domain service handles restoring category + descendants
        var restoreResult = _hierarchyService.RestoreCategoryAndDescendants(
            category,
            descendants);

        if (restoreResult.IsFailure)
        {
            return Result<RestoreCategoryResult>.Failure(restoreResult.Error);
        }

        // Persist if changes were made
        if (restoreResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new RestoreCategoryResult(
            category.DeletedAt,
            category.RowVersion);

        return Result<RestoreCategoryResult>.Success(result);
    }
}
