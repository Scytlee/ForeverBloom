using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.Domain.Shared;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ReslugCategory;

internal sealed class ReslugCategoryCommandHandler
    : ICommandHandler<ReslugCategoryCommand, ReslugCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly CategoryHierarchyService _hierarchyService;
    private readonly ITimeProvider _timeProvider;

    public ReslugCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICategoryRepository categoryRepository,
        ISlugRegistrationService slugRegistrationService,
        CategoryHierarchyService hierarchyService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _categoryRepository = categoryRepository;
        _slugRegistrationService = slugRegistrationService;
        _hierarchyService = hierarchyService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReslugCategoryResult>> Handle(
        ReslugCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var slugResult = Slug.Create(command.NewSlug);
        if (slugResult.IsFailure)
        {
            return Result<ReslugCategoryResult>.Failure(slugResult.Error);
        }

        var newSlug = slugResult.Value;

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<ReslugCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        if (category.RowVersion != command.RowVersion)
        {
            return Result<ReslugCategoryResult>.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // No-op: Subject category already has the provided slug
        if (category.CurrentSlug == newSlug)
        {
            return Result<ReslugCategoryResult>.Success(
                new ReslugCategoryResult(
                    category.CurrentSlug.Value,
                    category.Path.Value,
                    category.UpdatedAt,
                    category.RowVersion));
        }

        // Slug must be available for this category to use
        // (either unregistered, or registered to this category)
        var isAvailable = await _slugRegistrationService.IsSlugAvailableForEntityAsync(
            newSlug,
            EntityType.Category,
            category.Id,
            cancellationToken);

        if (!isAvailable)
        {
            return Result<ReslugCategoryResult>.Failure(
                new CategoryErrors.SlugNotAvailable(newSlug.Value));
        }

        // Fetch descendants up to the maximum + 1 to ensure we don't exceed the limit
        var descendants = await _categoryRepository.GetDescendantsAsync(
            category.Path,
            category.Id,
            Category.DescendantLimitOnUpdate,
            cancellationToken);

        if (descendants.Count > Category.DescendantLimitOnUpdate)
        {
            return Result<ReslugCategoryResult>.Failure(
                new CategoryErrors.TooManyDescendants(
                    command.CategoryId));
        }

        // Domain service handles slug change and descendant rebasing
        var changeSlugResult = _hierarchyService.ChangeCategorySlugAndRebaseDescendants(
            category,
            newSlug,
            descendants,
            _timeProvider.UtcNow);

        if (changeSlugResult.IsFailure)
        {
            return Result<ReslugCategoryResult>.Failure(changeSlugResult.Error);
        }

        // Only persist if changes were made
        if (changeSlugResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Register the new slug (deactivate old, reactivate or create new entry)
            await _slugRegistrationService.RegisterSlugAsync(
                EntityType.Category,
                category.Id,
                newSlug,
                cancellationToken);
        }

        var result = new ReslugCategoryResult(
            category.CurrentSlug.Value,
            category.Path.Value,
            category.UpdatedAt,
            category.RowVersion);

        return Result<ReslugCategoryResult>.Success(result);
    }
}
