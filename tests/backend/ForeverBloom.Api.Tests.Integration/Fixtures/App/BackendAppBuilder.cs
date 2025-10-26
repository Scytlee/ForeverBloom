namespace ForeverBloom.Api.Tests.Fixtures.App;

public sealed class BackendAppBuilder
{
    private string? _postgresConnectionString;
    private bool _built;

    public BackendAppBuilder UsePostgres(string connectionString)
    {
        EnsureMutable();
        _postgresConnectionString = connectionString;
        return this;
    }

    public BackendApp Build()
    {
        EnsureMutable();

        if (_postgresConnectionString is null)
        {
            throw new InvalidOperationException("The PostgreSQL connection string is not set.");
        }

        _built = true;

        return BackendApp.Create(new BackendAppConfiguration(
            PostgresConnectionString: _postgresConnectionString));
    }

    private void EnsureMutable()
    {
        if (_built)
        {
            throw new InvalidOperationException("BackendAppBuilder instances can only build once.");
        }
    }
}
