namespace ForeverBloom.Domain.Shared.Validation;

public static class ProductImageValidation
{
    public static class Constants
    {
        public const int ImagePathMaxLength = 500;
        public const int AltTextMaxLength = 200;
    }

    public static class ErrorCodes
    {
        public const string ImagePathRequired = "ProductImage_ImagePathRequired";
        public const string ImagePathTooLong = "ProductImage_ImagePathTooLong";
        public const string AltTextTooLong = "ProductImage_AltTextTooLong";
        public const string InvalidDisplayOrder = "ProductImage_InvalidDisplayOrder";
    }
}
