using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const int postgresPort = 65432;

var foreverBloomDatabaseNameResourceBuilder = builder.AddParameter("foreverBloomDatabaseName", secret: true);
var foreverBloomMigratorUsernameResourceBuilder = builder.AddParameter("foreverBloomMigratorUsername", secret: true);
var foreverBloomMigratorPasswordResourceBuilder = builder.AddParameter("foreverBloomMigratorPassword", secret: true);
var foreverBloomUserUsernameResourceBuilder = builder.AddParameter("foreverBloomUserUsername", secret: true);
var foreverBloomUserPasswordResourceBuilder = builder.AddParameter("foreverBloomUserPassword", secret: true);

var foreverBloomDatabaseName = await foreverBloomDatabaseNameResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
var foreverBloomMigratorUsername = await foreverBloomMigratorUsernameResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
var foreverBloomMigratorPassword = await foreverBloomMigratorPasswordResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
var foreverBloomUserUsername = await foreverBloomUserUsernameResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
var foreverBloomUserPassword = await foreverBloomUserPasswordResourceBuilder.Resource.GetValueAsync(CancellationToken.None);

var db = builder.AddPostgres("db", port: postgresPort)
    .WithImage("postgres:17.5-alpine")
    .WithInitFiles("../../../deploy/postgres-init")
    .WithEnvironment("FOREVERBLOOM_DATABASE_NAME", foreverBloomDatabaseNameResourceBuilder)
    .WithEnvironment("FOREVERBLOOM_MIGRATOR_USERNAME", foreverBloomMigratorUsernameResourceBuilder)
    .WithEnvironment("FOREVERBLOOM_MIGRATOR_PASSWORD", foreverBloomMigratorPasswordResourceBuilder)
    .WithEnvironment("FOREVERBLOOM_USER_USERNAME", foreverBloomUserUsernameResourceBuilder)
    .WithEnvironment("FOREVERBLOOM_USER_PASSWORD", foreverBloomUserPasswordResourceBuilder);

var foreverBloomMigratorConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={foreverBloomDatabaseName};Username={foreverBloomMigratorUsername};Password={foreverBloomMigratorPassword}";
var dbMigrator = builder.AddProject<ForeverBloom_Tools_DatabaseMigrator>("dbmigrator")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomMigratorConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(db);

var foreverBloomUserConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={foreverBloomDatabaseName};Username={foreverBloomUserUsername};Password={foreverBloomUserPassword}";
var dbSeeder = builder.AddProject<ForeverBloom_Tools_DatabaseSeeder>("dbseeder")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomUserConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(db)
    .WaitForCompletion(dbMigrator);

var backend = builder.AddProject<ForeverBloom_WebApi>("backend")
    .WithHttpHealthCheck("/alive")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomUserConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitForCompletion(dbSeeder);

builder.AddProject<ForeverBloom_WebUI_RazorPages>("frontend")
  .WithExternalHttpEndpoints()
  .WithHttpHealthCheck("/health")
  .WaitFor(backend);

builder.Build().Run();
