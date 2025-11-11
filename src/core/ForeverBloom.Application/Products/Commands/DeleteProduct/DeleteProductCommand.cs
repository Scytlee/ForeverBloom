using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(
    long ProductId,
    uint RowVersion) : ICommand;
