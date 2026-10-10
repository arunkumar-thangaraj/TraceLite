using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TraceLite.Tracing;

namespace TraceLite.Interceptors;

internal class SqlPerformanceInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SqlPerformanceInterceptor> _logger;

    public SqlPerformanceInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SqlPerformanceInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        Record(command, eventData.Duration);

        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Record(command, eventData.Duration);

        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Record(command, eventData.Duration);

        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration);

        return ValueTask.FromResult(result);
    }

    // Failed commands (timeouts, errors) are often the slowest ones.
    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        Record(command, eventData.Duration, eventData.Exception);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration, eventData.Exception);

        return Task.CompletedTask;
    }

    private void Record(
        DbCommand command,
        TimeSpan duration,
        Exception? exception = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // No active HTTP request (this also skips TraceLite's own
        // INSERTs, which run in TraceWriterService)
        if (httpContext is null)
        {
            return;
        }

        // Get the SQL trace collection
        if (!httpContext.Items.TryGetValue(
                TraceContextKeys.SqlEntries,
                out var sqlEntriesValue))
        {
            return;
        }

        if (sqlEntriesValue is not ConcurrentQueue<SqlTraceEntry> entries)
        {
            return;
        }

        // Get the request TraceId
        if (!httpContext.Items.TryGetValue(
                TraceContextKeys.TraceId,
                out var traceIdValue))
        {
            return;
        }

        if (traceIdValue is not string traceId)
        {
            return;
        }

        // Limit SQL command length
        var commandText = command.CommandText;

        if (commandText.Length > 4000)
        {
            commandText = commandText[..4000];
        }

        // Create SQL trace entry
        var entry = new SqlTraceEntry
        {
            TraceId = traceId,

            CommandType = command.CommandType.ToString(),

            CommandText = commandText,

            DurationMs = Math.Max(
                0,
                (long)duration.TotalMilliseconds),

            CreatedAtUtc = DateTime.UtcNow,

            ErrorType = exception?.GetType().Name,

            ErrorMessage = exception is null
                ? null
                : HttpErrorClassifier.Truncate(exception.Message)
        };

        // Add SQL entry to the current request
        entries.Enqueue(entry);

        _logger.LogInformation(
            "TraceLite SQL: {DurationMs} ms | TraceId: {TraceId}",
            entry.DurationMs,
            entry.TraceId);
    }
}