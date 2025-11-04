using ForeverBloom.Application.Abstractions.Data;

namespace ForeverBloom.Application.Abstractions.Requests;

/// <summary>
/// Marker interface for commands that require custom transaction settings.
/// Commands implementing this interface can override default transaction behavior
/// such as isolation level, lock timeout, and statement timeout.
/// </summary>
public interface IWithTransactionOverrides
{
    /// <summary>
    /// Gets the transaction settings for this command.
    /// </summary>
    TransactionSettings TransactionSettings { get; }
}
