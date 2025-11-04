using System.Data;

namespace ForeverBloom.Application.Abstractions.Data;

/// <summary>
/// Configuration settings for database transactions.
/// Commands can specify these settings to override default transaction behavior.
/// </summary>
public sealed record TransactionSettings
{
    /// <summary>
    /// The transaction isolation level.
    /// Default: <see cref="IsolationLevel.ReadCommitted"/>.
    /// </summary>
    public IsolationLevel Isolation { get; init; } = IsolationLevel.ReadCommitted;

    /// <summary>
    /// Maximum time to wait for locks before failing.
    /// Uses PostgreSQL's lock_timeout setting (SET LOCAL lock_timeout).
    /// Default: null (no timeout, waits indefinitely).
    /// </summary>
    public TimeSpan? LockTimeout { get; init; }

    /// <summary>
    /// Maximum time for any statement to execute before being canceled.
    /// Uses PostgreSQL's statement_timeout setting (SET LOCAL statement_timeout).
    /// Default: null (no timeout).
    /// </summary>
    public TimeSpan? StatementTimeout { get; init; }

    /// <summary>
    /// Default transaction settings: READ COMMITTED with no timeouts.
    /// </summary>
    public static readonly TransactionSettings Default = new();
}
