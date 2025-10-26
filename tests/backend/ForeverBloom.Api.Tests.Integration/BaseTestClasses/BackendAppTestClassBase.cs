using System.Diagnostics.CodeAnalysis;
using ForeverBloom.Api.Tests.Fixtures.App;
using ForeverBloom.Testing.Common.BaseTestClasses;
using ForeverBloom.Testing.Common.Fixtures.Database;

namespace ForeverBloom.Api.Tests.BaseTestClasses;

public abstract class BackendAppTestClassBase : DatabaseTestClassBase, IAsyncLifetime
{
    protected BackendApp? _app;

    protected BackendAppTestClassBase(DatabaseFixture postgres) : base(postgres)
    {
    }

    public new async ValueTask DisposeAsync()
    {
        try
        {
            if (_app is not null)
            {
                await _app.DisposeAsync();
            }
        }
        finally
        {
            _app = null;
            await base.DisposeAsync();
        }
    }

    [MemberNotNull(nameof(_app))]
    protected void BuildApp(Action<BackendAppBuilder>? configureAction = null)
    {
        var builder = new BackendAppBuilder()
          .UsePostgres(DatabaseConnectionString);

        configureAction?.Invoke(builder);

        _app = builder.Build();
    }
}
