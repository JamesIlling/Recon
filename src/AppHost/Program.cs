using Aspire.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with a named database.
IResourceBuilder<SqlServerServerResource> sqlServer = builder.AddSqlServer("sqlserver");
IResourceBuilder<SqlServerDatabaseResource> sqlDb = sqlServer.AddDatabase("locationmanagement");

// ASP.NET Core Web API — receives the SQL Server connection string via WithReference.
IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.LocationManagement_Api>("api")
    .WithReference(sqlDb)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Vite frontend dev server — AddViteApp registers the HTTP endpoint automatically
// and sets the PORT environment variable so Vite binds to the correct port.
builder
    .AddViteApp("frontend", "../src/client")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));

await builder.Build().RunAsync();
