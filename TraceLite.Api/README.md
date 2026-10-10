# TraceLite

When an ASP.NET Core endpoint is slow, TraceLite shows where the time went. For every request it records:

- the total duration
- each EF Core query (failed ones too)
- each outbound `HttpClient` call

Traces are stored in PostgreSQL and shown on a request timeline in the TraceLite dashboard.

This repository contains:

| Folder | What it is |
|---|---|
| `TraceLite/` | The library you add to your app |
| `samples/TraceLite.Sample/` | A minimal API using the library, with a deliberate N+1 query |
| `/` (`TraceLite.Api`) | The dashboard backend. It reads the traces and also hosts the API Tester |

The dashboard UI is in the separate [`TraceLite`](https://github.com/arunkumarja/TraceLite) repository.

## Requirements

- .NET 8
- A PostgreSQL database for the traces. Your app's own database can use any EF Core provider.

## Add TraceLite to your app

Reference the library. It isn't on nuget.org yet, so add a project reference to `TraceLite/TraceLite.csproj`, or build a package with `dotnet pack TraceLite/TraceLite.csproj`.

Then, in `Program.cs`:

```csharp
using TraceLite;

// 1. Register tracing and the PostgreSQL trace store.
//    Every HttpClient created by IHttpClientFactory is traced automatically.
builder.Services.AddTraceLite(
    builder.Configuration.GetConnectionString("TraceLite")!);

// 2. Record your own DbContext's queries.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options
        .UseSqlServer(connectionString)        // any provider
        .AddTraceLiteInterceptor(serviceProvider));

var app = builder.Build();

// 3. Trace every request. Add it early so the timing covers the whole pipeline.
app.UseTraceLite();

// Creates the TraceLite tables on first run.
await app.Services.MigrateTraceLiteDatabaseAsync();
```

Requests to `/health` and `/swagger` are not traced. To skip other paths:

```csharp
builder.Services.AddTraceLite(connectionString, options =>
    options.ExcludedPaths.Add("/metrics"));
```

## See the traces

1. Run `TraceLite.Api` against the same database:
   `ConnectionStrings__SUPABASE_CONNECTION_STRING=<your trace database>`
2. Point the dashboard UI at it with `VITE_API_BASE_URL`.
3. Open a request to see its SQL queries, its outbound calls and the timeline.

## Try the sample

1. Set `ConnectionStrings:TraceLite` in `samples/TraceLite.Sample/appsettings.json`. It defaults to a local PostgreSQL with user `postgres` and password `postgres`.
2. Run the sample:

   ```bash
   dotnet run --project samples/TraceLite.Sample
   ```

3. Call these endpoints and compare them in the dashboard:

| Endpoint | What you'll see |
|---|---|
| `GET http://localhost:5180/orders` | 11 SQL queries: an N+1, one customer lookup per order |
| `GET http://localhost:5180/orders/fast` | The same data in 1 query |
| `GET http://localhost:5180/customers/2/profile` | 1 SQL query, then 1 outbound HTTP call |

## What is captured

- **Requests:** method, path, status code, duration.
- **SQL:** command text (first 4,000 characters), duration, error type and message. Parameter values are not stored.
- **HTTP calls:** method, URL, status code, duration, and an error type (`Timeout`, `DnsFailure`, `TlsError`, ...).
  - Credentials in query strings and `user:password@` are replaced with `REDACTED`.
  - Secrets inside the URL path are **not** redacted.

Traces are queued in memory and written in batches by a background service, so requests don't wait for the database. If the queue fills (10,000 requests), new traces are dropped and a warning is logged.

## Limitations

- **PostgreSQL only.** The trace store supports only PostgreSQL. The tables (`PerformanceRecords`, `SqlPerformanceRecords`, `HttpPerformanceRecords`) are created in the default schema.
  - Use a separate database if those names clash with your own tables.
- **No retention.** Old traces are never deleted.
- **No authentication.** The dashboard API (`TraceLite.Api`) has no authentication. Don't expose it publicly with real trace data.
- **Overhead not measured.** Overhead has not been measured yet.

## Developing

```bash
dotnet build TraceLite.Api.csproj

# Add a migration for the trace store (the design-time factory in TraceLite.Api reads user-secrets)
dotnet ef migrations add <Name> --project TraceLite/TraceLite.csproj --startup-project TraceLite.Api.csproj
```
