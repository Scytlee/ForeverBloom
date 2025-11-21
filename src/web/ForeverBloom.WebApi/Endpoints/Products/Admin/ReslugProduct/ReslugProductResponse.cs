using ForeverBloom.Application.Products.Commands.ReslugProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.Admin.ReslugProduct;

internal sealed record ReslugProductResponse(
    string CurrentSlug,
    DateTimeOffset UpdatedAt,
    uint RowVersion)
{
    internal static ReslugProductResponse FromResult(ReslugProductResult result) =>
        new(result.CurrentSlug, result.UpdatedAt, result.RowVersion);
}
