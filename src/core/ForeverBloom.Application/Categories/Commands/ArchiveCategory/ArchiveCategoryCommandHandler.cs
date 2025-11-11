using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.ArchiveCategory;

internal sealed class ArchiveCategoryCommandHandler
    : ICommandHandler<ArchiveCategoryCommand, ArchiveCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryHierarchyService _hierarchyService;
    private readonly ITimeProvider _timeProvider;

    public ArchiveCategoryCommandHandler(
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

    public async Task<Result<ArchiveCategoryResult>> Handle(
        ArchiveCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve category by ID, including archived
        var category = await _categoryRepository.GetByIdIncludingArchivedAsync(
            command.CategoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<ArchiveCategoryResult>.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        // Check row version for optimistic concurrency
        if (category.RowVersion != command.RowVersion)
        {
            return Result<ArchiveCategoryResult>.Failure(
                new ApplicationErrors.ConcurrencyConflict());
        }

        // Early no-op: if already archived, return immediately without fetching descendants
        if (category.DeletedAt is not null)
        {
            return Result<ArchiveCategoryResult>.Success(
                new ArchiveCategoryResult(
                    category.DeletedAt.Value,
                    category.RowVersion));
        }

        // Fetch descendants up to the maximum + 1 to ensure we don't exceed the limit
        var descendants = await _categoryRepository.GetDescendantsAsync(
            category.Path,
            category.Id,
            Category.DescendantLimitOnUpdate,
            cancellationToken);

        if (descendants.Count > Category.DescendantLimitOnUpdate)
        {
            return Result<ArchiveCategoryResult>.Failure(
                new CategoryErrors.TooManyDescendants(
                    command.CategoryId));
        }

        // Domain service handles archiving category + descendants
        var archiveResult = _hierarchyService.ArchiveCategoryAndDescendants(
            category,
            descendants,
            _timeProvider.UtcNow);

        if (archiveResult.IsFailure)
        {
            return Result<ArchiveCategoryResult>.Failure(archiveResult.Error);
        }

        // Persist if changes were made
        if (archiveResult.Value)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new ArchiveCategoryResult(
            category.DeletedAt!.Value,
            category.RowVersion);

        return Result<ArchiveCategoryResult>.Success(result);
    }
}
