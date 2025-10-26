namespace ForeverBloom.Domain.Shared.Validation;

public static class ProductValidation
{
    public static class Constants
    {
        public const int NameMaxLength = 100;
        public const int SeoTitleMaxLength = 40;
        public const int FullDescriptionMaxLength = 4000;
        public const int MetaDescriptionMaxLength = 150;
        public const int ImagePathMaxLength = 500;
        public const int AltTextMaxLength = 200;
        public const decimal MinPrice = 0.01m;
        public const decimal MaxPrice = 999999.99m;
    }

    public static class ErrorCodes
    {
        public const string IdInvalid = "Product_IdInvalid";

        public const string NameRequired = "Product_NameRequired";
        public const string NameTooLong = "Product_NameTooLong";

        public const string SeoTitleTooLong = "Product_SeoTitleTooLong";

        public const string FullDescriptionTooLong = "Product_FullDescriptionTooLong";

        public const string MetaDescriptionTooLong = "Product_MetaDescriptionTooLong";

        public const string SlugRequired = "Product_SlugRequired";
        public const string SlugTooLong = "Product_SlugTooLong";
        public const string SlugInvalidFormat = "Product_SlugInvalidFormat";
        public const string SlugIsNotAvailable = "Product_SlugIsNotAvailable";

        public const string PriceOutOfRange = "Product_PriceOutOfRange";

        public const string CategoryIdRequired = "Product_CategoryIdRequired";
        public const string CategoryNotFound = "Product_CategoryNotFound";
        public const string CategoryInactive = "Product_CategoryInactive";

        public const string PublishStatusInvalid = "Product_PublishStatusInvalid";
        public const string AvailabilityStatusInvalid = "Product_AvailabilityStatusInvalid";

        public const string ImagePathTooLong = "Product_ImagePathTooLong";
        public const string AltTextTooLong = "Product_AltTextTooLong";
        public const string ThumbnailImageRequired = "Product_ThumbnailImageRequired";
        public const string DuplicateImageDisplayOrder = "Product_DuplicateImageDisplayOrder";
        public const string ExactlyOnePrimaryImage = "Product_ExactlyOnePrimaryImage";
        public const string NoImageCollectionProvided = "Product_NoImageCollectionProvided";

        public const string RowVersionRequired = "Product_RowVersionRequired";
        public const string InvalidSortParameters = "Product_InvalidSortParameters";

        public const string UpdateConcurrencyConflict = "Product_UpdateConcurrencyConflict";
        public const string ArchiveConcurrencyConflict = "Product_ArchiveConcurrencyConflict";
        public const string RestoreConcurrencyConflict = "Product_RestoreConcurrencyConflict";
        public const string DeleteConcurrencyConflict = "Product_DeleteConcurrencyConflict";
        public const string ProductNotArchived = "Product_ProductNotArchived";
    }
}
