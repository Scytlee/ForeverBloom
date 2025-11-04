using System.Data;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Requests;

namespace ForeverBloom.Application.Categories.Commands.ReslugCategory;

public sealed record ReslugCategoryCommand(
    long CategoryId,
    string NewSlug,
    uint RowVersion) : ICommand<ReslugCategoryResult>, IWithTransactionOverrides
{
    public TransactionSettings TransactionSettings => new()
    {
        Isolation = IsolationLevel.Serializable,
        LockTimeout = TimeSpan.FromSeconds(2),
        StatementTimeout = TimeSpan.FromSeconds(30)
    };
}
