using TraceLite.Data;

namespace TraceLite.Tracing
{
    // Persists traces off the request path, in batches,
    // using its own DbContext so request changes are never saved by accident.
    internal class TraceWriterService : BackgroundService
    {
        private const int MaxBatchSize = 100;

        private readonly TraceQueue _traceQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TraceWriterService> _logger;

        public TraceWriterService(
            TraceQueue traceQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<TraceWriterService> logger)
        {
            _traceQueue = traceQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                while (await _traceQueue.Reader.WaitToReadAsync(stoppingToken))
                {
                    await SaveQueuedAsync();
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
            }

            // Flush whatever is still queued when the host stops.
            await SaveQueuedAsync();
        }

        private async Task SaveQueuedAsync()
        {
            var batches = new List<TraceBatch>(MaxBatchSize);

            while (_traceQueue.Reader.TryRead(out var batch))
            {
                batches.Add(batch);

                if (batches.Count == MaxBatchSize)
                {
                    await SaveAsync(batches);

                    batches.Clear();
                }
            }

            if (batches.Count > 0)
            {
                await SaveAsync(batches);
            }
        }

        private async Task SaveAsync(List<TraceBatch> batches)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<TraceLiteDbContext>();

                foreach (var batch in batches)
                {
                    dbContext.PerformanceRecords.Add(batch.Request);

                    dbContext.SqlPerformanceRecords.AddRange(batch.SqlRecords);

                    dbContext.HttpPerformanceRecords.AddRange(batch.HttpRecords);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to save {Count} TraceLite traces",
                    batches.Count);
            }
        }
    }
}
