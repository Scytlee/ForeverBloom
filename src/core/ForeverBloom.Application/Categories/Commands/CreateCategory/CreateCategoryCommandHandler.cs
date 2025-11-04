using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Abstractions.Requests;
using ForeverBloom.Application.Abstractions.SlugRegistry;
using ForeverBloom.Application.Abstractions.Time;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISlugRegistrationService _slugRegistrationService;
    private readonly ITimeProvider _timeProvider;

    public CreateCategoryCommandHandler(
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

    public async Task<Result<CreateCategoryResult>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var valueObjectsResult = command.AssembleValueObjects();
        if (valueObjectsResult.IsFailure)
        {
            return Result<CreateCategoryResult>.Failure(valueObjectsResult.Error);
        }

        var valueObjects = valueObjectsResult.Value;

        HierarchicalPath? parentPath = null;
        if (command.ParentCategoryId.HasValue)
        {
            parentPath = await _categoryRepository.GetCategoryPathAsync(command.ParentCategoryId.Value, cancellationToken);
            if (parentPath is null)
            {
                return Result<CreateCategoryResult>.Failure(new CategoryErrors.ParentNotFound(command.ParentCategoryId.Value));
            }
        }

        var pathResult = parentPath is null
                ? HierarchicalPath.FromString(valueObjects.Slug)
                : HierarchicalPath.FromParent(parentPath, valueObjects.Slug);
        if (pathResult.IsFailure)
        {
            return Result<CreateCategoryResult>.Failure(pathResult.Error);
        }

        var slugAvailable = await _slugRegistrationService.IsSlugAvailableAsync(
            valueObjects.Slug,
            cancellationToken);

        if (!slugAvailable)
        {
            return Result<CreateCategoryResult>.Failure(
                new CategoryErrors.SlugNotAvailable(valueObjects.Slug.Value));
        }

        var siblingExists = await _categoryRepository.NameExistsWithinParentAsync(
            valueObjects.Name,
            command.ParentCategoryId,
            cancellationToken);

        if (siblingExists)
        {
            return Result<CreateCategoryResult>.Failure(
                new CategoryErrors.NameNotUniqueWithinParent(valueObjects.Name.Value, command.ParentCategoryId));
        }

        var categoryResult = Category.Create(
            name: valueObjects.Name,
            description: valueObjects.Description,
            slug: valueObjects.Slug,
            image: valueObjects.Image,
            path: pathResult.Value,
            parentCategoryId: command.ParentCategoryId,
            displayOrder: command.DisplayOrder,
            timestamp: _timeProvider.UtcNow);

        if (categoryResult.IsFailure)
        {
            return Result<CreateCategoryResult>.Failure(categoryResult.Error);
        }

        var category = categoryResult.Value!;

        _categoryRepository.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _slugRegistrationService.RegisterSlugAsync(
            EntityType.Category,
            category.Id,
            category.CurrentSlug,
            cancellationToken);

        return Result<CreateCategoryResult>.Success(new CreateCategoryResult(category.Id));
    }
}
