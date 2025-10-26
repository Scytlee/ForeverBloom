namespace ForeverBloom.Domain.Shared.Validation;

public static class CategoryValidation
{
    public static class Constants
    {
        public const int NameMaxLength = 40;
        public const int DescriptionMaxLength = 150;
        public const int ImagePathMaxLength = 500;
        public const int MaxHierarchyDepth = 10;
    }

    public static class ErrorCodes
    {
        public const string NameRequired = "Category_NameRequired";
        public const string NameTooLong = "Category_NameTooLong";

        public const string DescriptionTooLong = "Category_DescriptionTooLong";

        public const string ImagePathTooLong = "Category_ImagePathTooLong";

        public const string SlugRequired = "Category_SlugRequired";
        public const string SlugTooLong = "Category_SlugTooLong";
        public const string SlugInvalidFormat = "Category_SlugInvalidFormat";

        public const string ParentCategoryIdInvalid = "Category_ParentCategoryIdInvalid";
        public const string ParentCategoryNotFound = "Category_ParentCategoryNotFound";
        public const string ParentCategorySelfReference = "Category_ParentCategorySelfReference";
        public const string ParentCategoryCircularReference = "Category_ParentCategoryCircularReference";

        public const string SlugIsNotAvailable = "Category_SlugIsNotAvailable";
        public const string CannotArchiveCategoryWithChildren = "Category_CannotDeleteCategoryWithChildren";

        public const string HierarchyChangeNotAllowed = "Category_HierarchyChangeNotAllowed";

        public const string RowVersionRequired = "Category_RowVersionRequired";
        public const string InvalidSortParameters = "Category_InvalidSortParameters";
        public const string MaxHierarchyDepthExceeded = "Category_MaxHierarchyDepthExceeded";
        public const string NameNotUniqueWithinParent = "Category_NameNotUniqueWithinParent";

        public const string UpdateConcurrencyConflict = "Category_UpdateConcurrencyConflict";
        public const string ArchiveConcurrencyConflict = "Category_ArchiveConcurrencyConflict";
        public const string RestoreConcurrencyConflict = "Category_RestoreConcurrencyConflict";
        public const string DeleteConcurrencyConflict = "Category_DeleteConcurrencyConflict";
        public const string CategoryNotArchived = "Category_CategoryNotArchived";
        public const string CategoryHasChildCategories = "Category_CategoryHasChildCategories";
        public const string CategoryHasProducts = "Category_CategoryHasProducts";
        public const string IdInvalid = "Category_IdInvalid";
    }
}
