using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Commands.RestoreProduct;

public sealed record RestoreProductCommand(
    long ProductId,
    uint RowVersion) : ICommand<RestoreProductResult>;
