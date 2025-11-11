using ForeverBloom.Application.Abstractions;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public DeleteCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICategoryRepository categoryRepository,
        ISlugRegistrationService slugRegistrationService,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _categoryRepository = categoryRepository;
        _slugRegistrationService = slugRegistrationService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Retrieve category by ID, including archived
        var category = await _categoryRepository.GetByIdIncludingArchivedAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(
                new CategoryErrors.NotFoundById(command.CategoryId));
        }

        // Check row version for optimistic concurrency
        if (category.RowVersion != command.RowVersion)
        {
            return Result.Failure(new ApplicationErrors.ConcurrencyConflict());
        }

        // Check if category is archived
        if (!category.DeletedAt.HasValue)
        {
            return Result.Failure(
                new CategoryErrors.CannotDeleteNotArchived(command.CategoryId));
        }

        // Check if deletion grace period has elapsed
        var eligibleAt = category.DeletedAt.Value.AddHours(Category.DeletionGracePeriodInHours);
        if (_timeProvider.UtcNow < eligibleAt)
        {
            return Result.Failure(
                new CategoryErrors.CannotDeleteTooSoon(
                    command.CategoryId,
                    category.DeletedAt.Value,
                    eligibleAt));
        }

        // Check if category has child categories (including archived)
        var hasChildren = await _categoryRepository.HasChildCategoriesAsync(command.CategoryId, cancellationToken);
        if (hasChildren)
        {
            return Result.Failure(
                new CategoryErrors.CannotDeleteHasChildren(command.CategoryId));
        }

        // Check if any products reference this category (including archived)
        var hasProducts = await _categoryRepository.HasProductsAsync(command.CategoryId, cancellationToken);
        if (hasProducts)
        {
            return Result.Failure(
                new CategoryErrors.CannotDeleteHasProducts(command.CategoryId));
        }

        // Permanently delete the category
        _categoryRepository.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Clean up all slug registrations for the category
        await _slugRegistrationService.UnregisterAllSlugsOfEntityAsync(
            EntityType.Category,
            command.CategoryId,
            cancellationToken);

        return Result.Success();
    }
}
