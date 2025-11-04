using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Commands.ArchiveProduct;

public sealed record ArchiveProductCommand(
    long ProductId,
    uint RowVersion) : ICommand<ArchiveProductResult>;
