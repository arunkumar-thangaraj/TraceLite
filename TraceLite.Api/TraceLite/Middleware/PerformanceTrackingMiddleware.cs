using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using TraceLite.Entities;
using TraceLite.Tracing;

namespace TraceLite.Middleware;

internal class PerformanceTrackingMiddleware
{
    private const int MaxPathLength = 500;

    private readonly RequestDelegate _next;
    private readonly TraceQueue _traceQueue;
    private readonly PathString[] _excludedPaths;
    private readonly ILogger<PerformanceTrackingMiddleware> _logger;

    public PerformanceTrackingMiddleware(
        RequestDelegate next,
        TraceQueue traceQueue,
        IOptions<TraceLiteOptions> options,
        ILogger<PerformanceTrackingMiddleware> logger)
    {
        _next = next;
        _traceQueue = traceQueue;
        _excludedPaths = options.Value.ExcludedPaths
            .Select(path => new PathString(path))
            .ToArray();
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_excludedPaths.Any(path =>
                context.Request.Path.StartsWithSegments(path)))
        {
            await _next(context);
            return;
        }

        // TraceIdentifier is a per-connection counter that can repeat
        // after a restart, so use a GUID to keep SQL/HTTP rows joined
        // to the right request.
        var traceId = Guid.NewGuid().ToString("N");

        context.Items[TraceContextKeys.TraceId] = traceId;

        // Concurrent queues because SQL/HTTP calls can run in parallel.
        var sqlEntries = new ConcurrentQueue<SqlTraceEntry>();

        context.Items[TraceContextKeys.SqlEntries] = sqlEntries;

        var httpEntries = new ConcurrentQueue<HttpTraceEntry>();

        context.Items[TraceContextKeys.HttpEntries] = httpEntries;

        var failed = false;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch
        {
            // The exception handler sets 500 further up the pipeline,
            // so the response still reports 200 at this point.
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var path = context.Request.Path.ToString();

            if (path.Length > MaxPathLength)
            {
                path = path[..MaxPathLength];
            }

            var batch = new TraceBatch
            {
                Request = new PerformanceRecord
                {
                    TraceId = traceId,
                    Method = context.Request.Method,
                    Path = path,
                    StatusCode = failed
                        ? StatusCodes.Status500InternalServerError
                        : context.Response.StatusCode,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    CreatedAtUtc = DateTime.UtcNow
                },

                SqlRecords = sqlEntries
                    .Select(sqlEntry => new SqlPerformanceRecord
                    {
                        TraceId = sqlEntry.TraceId,
                        CommandType = sqlEntry.CommandType,
                        CommandText = sqlEntry.CommandText,
                        DurationMs = sqlEntry.DurationMs,
                        CreatedAtUtc = sqlEntry.CreatedAtUtc,
                        ErrorType = sqlEntry.ErrorType,
                        ErrorMessage = sqlEntry.ErrorMessage
                    })
                    .ToList(),

                HttpRecords = httpEntries
                    .Select(httpEntry => new HttpPerformanceRecord
                    {
                        TraceId = httpEntry.TraceId,
                        Method = httpEntry.Method,
                        Url = httpEntry.Url,
                        StatusCode = httpEntry.StatusCode,
                        DurationMs = httpEntry.DurationMs,
                        CreatedAtUtc = httpEntry.CreatedAtUtc,
                        ErrorType = httpEntry.ErrorType,
                        ErrorMessage = httpEntry.ErrorMessage
                    })
                    .ToList()
            };

            if (_traceQueue.TryEnqueue(batch))
            {
                _logger.LogInformation(
                    "Request {Method} {Path} completed in {DurationMs} ms with {SqlCount} SQL queries and {HttpCount} HTTP calls",
                    batch.Request.Method,
                    batch.Request.Path,
                    batch.Request.DurationMs,
                    batch.SqlRecords.Count,
                    batch.HttpRecords.Count);
            }
            else
            {
                _logger.LogWarning(
                    "TraceLite queue is full; dropped trace for {Method} {Path}",
                    batch.Request.Method,
                    batch.Request.Path);
            }
        }
    }
}
