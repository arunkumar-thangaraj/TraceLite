using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using TraceLite.Data;
using TraceLite.Handlers;
using TraceLite.Interceptors;
using TraceLite.Middleware;
using TraceLite.Tracing;

namespace TraceLite;

public static class TraceLiteExtensions
{
    // Registers tracing and the PostgreSQL store the traces are written to.
    // Every HttpClient created by IHttpClientFactory is traced automatically.
    public static IServiceCollection AddTraceLite(
        this IServiceCollection services,
        string connectionString,
        Action<TraceLiteOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = services.AddOptions<TraceLiteOptions>();

        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddHttpContextAccessor();

        services.AddSingleton<SqlPerformanceInterceptor>();

        services.AddTransient<HttpPerformanceHandler>();

        services.ConfigureHttpClientDefaults(http =>
            http.AddHttpMessageHandler<HttpPerformanceHandler>());

        services.AddDbContext<TraceLiteDbContext>(dbOptions =>
            dbOptions.UseNpgsql(connectionString));

        // Traces are queued per request and saved in the background
        services.AddSingleton<TraceQueue>();

        services.AddHostedService<TraceWriterService>();

        return services;
    }

    // Records the queries of your own DbContext:
    // AddDbContext<AppDbContext>((sp, o) => o.UseSqlServer(...).AddTraceLiteInterceptor(sp))
    public static DbContextOptionsBuilder AddTraceLiteInterceptor(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        return builder.AddInterceptors(
            serviceProvider.GetRequiredService<SqlPerformanceInterceptor>());
    }

    // Add early in the pipeline so the measured time covers the whole request.
    public static IApplicationBuilder UseTraceLite(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceTrackingMiddleware>();
    }

    // Creates or updates the TraceLite tables.
    public static async Task MigrateTraceLiteDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TraceLiteDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
