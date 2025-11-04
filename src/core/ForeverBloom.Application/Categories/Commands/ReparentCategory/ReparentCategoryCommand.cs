using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.ReparentCategory;

public sealed record ReparentCategoryCommand(
    long CategoryId,
    long? NewParentCategoryId,
    uint RowVersion) : ICommand<ReparentCategoryResult>, IWithTransactionOverrides
{
    public TransactionSettings TransactionSettings => new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };
}
