using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ReparentCategory;

internal sealed class ReparentCategoryCommandHandler
    : ICommandHandler<ReparentCategoryCommand, ReparentCategoryResult>
{
    private const int MaxDescendantsToMove = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryHierarchyService _hierarchyService;
    private readonly ITimeProvider _timeProvider;

    public ReparentCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICategoryRepository categoryRepository,
        CategoryHierarchyService hierarchyService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _categoryRepository = categoryRepository;
        _hierarchyService = hierarchyService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReparentCategoryResult>> Handle(
        ReparentCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        if (category.RowVersion != command.RowVersion)
        {
            return Result<ReparentCategoryResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // No-op: Subject category already has the provided category ID as parent
        if (command.NewParentCategoryId == category.ParentCategoryId)
        {
            return Result<ReparentCategoryResult>.Success(
                new ReparentCategoryResult(
                    category.Path.Value,
                    category.ParentCategoryId,
                    category.UpdatedAt,
                    category.RowVersion));
        }

        // New parent category must exist if provided
        HierarchicalPath? newParentPath = null;
        if (command.NewParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(
                command.NewParentCategoryId.Value,
                cancellationToken);

            if (parent is null)
            {
                return Result<ReparentCategoryResult>.Failure(
                    new CategoryErrors.ParentNotFound(command.NewParentCategoryId.Value));
            }

            newParentPath = parent.Path;
        }

        // Subject category's name must be unique within immediate children of the new parent
        var nameExists = await _categoryRepository.NameExistsWithinParentAsync(
            category.Name,
            command.NewParentCategoryId,
            cancellationToken);
        if (nameExists)
        {
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.NameNotUniqueWithinParent(
                    category.Name.Value,
                    command.NewParentCategoryId));
        }

        // Fetching descendants up to the maximum + 1 to ensure we don't exceed the limit
        var descendants = await _categoryRepository.GetDescendantsAsync(
            category.Path,
            category.Id,
            MaxDescendantsToMove,
            cancellationToken);

        if (descendants.Count > MaxDescendantsToMove)
        {
            return Result<ReparentCategoryResult>.Failure(
                new CategoryErrors.TooManyDescendantsToMove(
                    command.CategoryId,
                    MaxDescendantsToMove));
        }

        // Domain service handles everything, including validation
        var reparentResult = _hierarchyService.ReparentCategoryAndRebaseDescendants(
            category,
            command.NewParentCategoryId,
            newParentPath,
            descendants,
            _timeProvider.UtcNow);

        if (reparentResult.IsFailure)
        {
            return Result<ReparentCategoryResult>.Failure(reparentResult.Error);
        }

        // Persist if changes were made
        if (reparentResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new ReparentCategoryResult(
            category.Path.Value,
            category.ParentCategoryId,
            category.UpdatedAt,
            category.RowVersion);

        return Result<ReparentCategoryResult>.Success(result);
    }
}
