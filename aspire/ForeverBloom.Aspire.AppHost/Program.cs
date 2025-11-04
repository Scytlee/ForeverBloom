var builder = DistributedApplication.CreateBuilder(args);

const int postgresPort = 65432;

var foreverBloomDatabaseName = builder.AddParameter("foreverBloomDatabaseName", secret: true);
var foreverBloomMigratorUsername = builder.AddParameter("foreverBloomMigratorUsername", secret: true);
var foreverBloomMigratorPassword = builder.AddParameter("foreverBloomMigratorPassword", secret: true);
var foreverBloomUserUsername = builder.AddParameter("foreverBloomUserUsername", secret: true);
var foreverBloomUserPassword = builder.AddParameter("foreverBloomUserPassword", secret: true);

var db = builder.AddPostgres("db", port: postgresPort)
    .WithImage("postgres:17.5-alpine")
    .WithInitBindMount("../../deploy/postgres-init")
    .WithEnvironment("FOREVERBLOOM_DATABASE_NAME", foreverBloomDatabaseName)
    .WithEnvironment("FOREVERBLOOM_MIGRATOR_USERNAME", foreverBloomMigratorUsername)
    .WithEnvironment("FOREVERBLOOM_MIGRATOR_PASSWORD", foreverBloomMigratorPassword)
    .WithEnvironment("FOREVERBLOOM_USER_USERNAME", foreverBloomUserUsername)
    .WithEnvironment("FOREVERBLOOM_USER_PASSWORD", foreverBloomUserPassword);

var foreverBloomMigratorConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={foreverBloomDatabaseName.Resource.Value};Username={foreverBloomMigratorUsername.Resource.Value};Password={foreverBloomMigratorPassword.Resource.Value}";
var dbMigrator = builder.AddProject<Projects.ForeverBloom_Tools_DatabaseMigrator>("dbmigrator")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomMigratorConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(db);

var foreverBloomUserConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={foreverBloomDatabaseName.Resource.Value};Username={foreverBloomUserUsername.Resource.Value};Password={foreverBloomUserPassword.Resource.Value}";
var dbSeeder = builder.AddProject<Projects.ForeverBloom_Tools_DatabaseSeeder>("dbseeder")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomUserConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(db)
    .WaitForCompletion(dbMigrator);

var backend = builder.AddProject<Projects.ForeverBloom_WebApi>("backend")
    .WithHttpsHealthCheck("/alive")
    .WithEnvironment("ConnectionStrings__Postgres", foreverBloomUserConnectionString)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitForCompletion(dbSeeder);

// builder.AddProject<Projects.ForeverBloom_Frontend_RazorPages>("frontend")
//   .WithExternalHttpEndpoints()
//   .WithHttpsHealthCheck("/health")
//   .WaitFor(backend);

builder.Build().Run();
