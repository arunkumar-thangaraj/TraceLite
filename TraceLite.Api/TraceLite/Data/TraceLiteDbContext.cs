using Microsoft.EntityFrameworkCore;
using TraceLite.Entities;

namespace TraceLite.Data
{
    public class TraceLiteDbContext : DbContext
    {
        public TraceLiteDbContext(
       DbContextOptions<TraceLiteDbContext> options)
       : base(options)
        {
        }

        public DbSet<PerformanceRecord> PerformanceRecords =>
            Set<PerformanceRecord>();

        public DbSet<SqlPerformanceRecord> SqlPerformanceRecords =>
    Set<SqlPerformanceRecord>();

        public DbSet<HttpPerformanceRecord>
    HttpPerformanceRecords
        { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PerformanceRecord>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.TraceId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Method)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(x => x.Path)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.StatusCode)
                    .IsRequired();

                entity.Property(x => x.DurationMs)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.HasIndex(x => x.CreatedAtUtc);

                entity.HasIndex(x => x.Path);

                entity.HasIndex(x => x.TraceId);
            });

            modelBuilder.Entity<SqlPerformanceRecord>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.TraceId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.CommandType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CommandText)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.Property(x => x.DurationMs)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.ErrorType)
                    .HasMaxLength(100);

                entity.Property(x => x.ErrorMessage)
                    .HasMaxLength(1000);

                entity.HasIndex(x => x.CreatedAtUtc);

                entity.HasIndex(x => x.TraceId);
            });


            modelBuilder.Entity<HttpPerformanceRecord>(
    entity =>
    {
        entity.Property(x => x.TraceId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Method)
            .HasMaxLength(10)
            .IsRequired();

        entity.Property(x => x.Url)
            .HasMaxLength(2000)
            .IsRequired();

        entity.Property(x => x.StatusCode)
            .IsRequired();

        entity.Property(x => x.DurationMs)
            .IsRequired();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();

        entity.Property(x => x.ErrorType)
            .HasMaxLength(100);

        entity.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        entity.HasIndex(x => x.TraceId);

        entity.HasIndex(x => x.CreatedAtUtc);
    });
        }
    }
}
