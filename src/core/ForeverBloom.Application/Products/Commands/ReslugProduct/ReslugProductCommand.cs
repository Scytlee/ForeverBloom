using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Products.Commands.ReslugProduct;

public sealed record ReslugProductCommand(
    long ProductId,
    uint RowVersion,
    string NewSlug) : ICommand<ReslugProductResult>;
