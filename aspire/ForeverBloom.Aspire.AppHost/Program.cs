var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "foreverbloom";
const int postgresPort = 65432;

var dbMigrationUserName = builder.AddParameter("dbMigrationUserName", secret: true);
var dbMigrationUserPassword = builder.AddParameter("dbMigrationUserPassword", secret: true);
var dbAppRoleName = builder.AddParameter("dbAppRoleName", secret: true);
var dbAppUserName = builder.AddParameter("dbAppUserName", secret: true);
var dbAppUserPassword = builder.AddParameter("dbAppUserPassword", secret: true);

var db = builder.AddPostgres("db", port: postgresPort)
  .WithImage("postgres:17.5-alpine")
  .WithInitBindMount("../../deploy/postgres-init")
  .WithEnvironment("POSTGRES_DB", databaseName)
  .WithEnvironment("MIGRATION_USER", dbMigrationUserName)
  .WithEnvironment("MIGRATION_PASSWORD", dbMigrationUserPassword)
  .AddDatabase(databaseName);

var dbManagerPostgresConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={databaseName};Username={dbMigrationUserName.Resource.Value};Password={dbMigrationUserPassword.Resource.Value}";
var dbManager = builder.AddProject<Projects.ForeverBloom_DatabaseManager>("dbmanager")
  .WithEnvironment("ConnectionStrings__Postgres", dbManagerPostgresConnectionString)
  .WithEnvironment("DatabaseManager__AppRole", dbAppRoleName)
  .WithEnvironment("DatabaseManager__AppUserName", dbAppUserName)
  .WithEnvironment("DatabaseManager__AppUserPassword", dbAppUserPassword)
  .WaitFor(db);

var backendPostgresConnectionString = $"Host=localhost;Port={postgresPort.ToString()};Database={databaseName};Username={dbAppUserName.Resource.Value};Password={dbAppUserPassword.Resource.Value}";
var backend = builder.AddProject<Projects.ForeverBloom_Api>("backend")
  .WithHttpsHealthCheck("/health")
  .WithEnvironment("ConnectionStrings__Postgres", backendPostgresConnectionString)
  .WaitForCompletion(dbManager);

builder.AddProject<Projects.ForeverBloom_Frontend_RazorPages>("frontend")
  .WithExternalHttpEndpoints()
  .WithHttpsHealthCheck("/health")
  .WaitFor(backend);

builder.Build().Run();
