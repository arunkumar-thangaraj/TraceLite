using TraceLite;
using TraceLite.Api.Controllers;
using TraceLite.Data;
using TraceLite.Security;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database (PostgreSQL). The connection string is never stored in
// appsettings: use `dotnet user-secrets` locally and the
// ConnectionStrings__SUPABASE_CONNECTION_STRING environment variable
// in deployed environments.
var connectionString =
    builder.Configuration.GetConnectionString("SUPABASE_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'SUPABASE_CONNECTION_STRING' was not found. " +
        "Set it with `dotnet user-secrets set \"ConnectionStrings:SUPABASE_CONNECTION_STRING\" \"<value>\"` " +
        "or the ConnectionStrings__SUPABASE_CONNECTION_STRING environment variable.");
}

// TraceLite tracing and trace store. This API also serves the dashboard,
// so its own read endpoints are not traced.
builder.Services.AddTraceLite(connectionString, options =>
    options.ExcludedPaths.Add("/api/performance"));

builder.Services.AddHttpClient("TraceLiteClient");

// The API Tester calls user-supplied URLs, so its connections are limited
// to public addresses (no localhost, private ranges or cloud metadata).
// The proxy is disabled because the guard would only see the proxy address.
builder.Services
    .AddHttpClient(ExternalApiController.ClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectCallback = DestinationGuard.ConnectAsync,
        UseProxy = false,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    });

// Health checks
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<TraceLiteDbContext>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS
app.UseCors();

app.UseHttpsRedirection();

// Performance tracking
app.UseTraceLite();

app.UseAuthorization();

// Controllers
app.MapControllers();

// Health
app.MapHealthChecks("/health");

app.Run();