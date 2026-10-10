using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TraceLite.Data;

namespace TraceLite.Api.Data
{
    public class TraceLitePostgresDbContextFactory
        : IDesignTimeDbContextFactory<TraceLiteDbContext>
    {
        public TraceLiteDbContext CreateDbContext(string[] args)
        {
            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets<TraceLitePostgresDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("NEON_MIGRATION_CONNECTION_STRING")
                ?? configuration.GetConnectionString("NEON_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No connection string found. Set either " +
                    "ConnectionStrings:NEON_MIGRATION_CONNECTION_STRING or " +
                    "ConnectionStrings:NEON_CONNECTION_STRING in user secrets or environment variables.");
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<TraceLiteDbContext>();

            optionsBuilder.UseNpgsql(connectionString);

            return new TraceLiteDbContext(optionsBuilder.Options);
        }
    }
}