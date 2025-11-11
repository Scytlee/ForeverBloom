using ForeverBloom.Application.Products.Commands.ArchiveProduct;

namespace ForeverBloom.WebApi.Endpoints.Products.ArchiveProduct;

internal sealed record ArchiveProductResponse(
    DateTimeOffset DeletedAt,
    uint RowVersion)
{
    internal static ArchiveProductResponse FromResult(ArchiveProductResult result) =>
        new(result.DeletedAt, result.RowVersion);
}
