namespace ForeverBloom.Domain.Shared.Validation;

public static class SlugValidation
{
    public static class Constants
    {
        public const int MaxLength = 255;
        public const string Regex = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
    }
}
