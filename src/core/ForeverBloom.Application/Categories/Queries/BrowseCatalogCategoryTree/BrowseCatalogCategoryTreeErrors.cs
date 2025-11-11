using ForeverBloom.SharedKernel.Result;

namespace ForeverBloom.Application.Categories.Queries.BrowseCatalogCategoryTree;

internal static class BrowseCatalogCategoryTreeErrors
{
    internal sealed record DepthOutOfRange(int AttemptedDepth) : IError
    {
        public string Code => "BrowseCatalogCategoryTree.DepthOutOfRange";
        public string Message => "Depth must be greater than or equal to 0.";
    }
}
