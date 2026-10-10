using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using TraceLite.Tracing;

namespace TraceLite.Handlers;

internal class HttpPerformanceHandler : DelegatingHandler
{
    private const int MaxUrlLength = 2000;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpPerformanceHandler> _logger;

    public HttpPerformanceHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpPerformanceHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(
                request,
                cancellationToken);

            stopwatch.Stop();

            Record(
                request,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                errorType: null,
                errorMessage: null);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            // The token here already includes HttpClient's timeout, so use
            // the inbound request's abort signal to tell a caller cancel
            // apart from a timeout.
            var callerCancelled =
                _httpContextAccessor.HttpContext?.RequestAborted
                    .IsCancellationRequested == true;

            var (errorType, errorMessage) =
                HttpErrorClassifier.Classify(exception, callerCancelled);

            // StatusCode 0 means no response was received.
            Record(
                request,
                0,
                stopwatch.ElapsedMilliseconds,
                errorType,
                errorMessage);

            throw;
        }
    }

    private void Record(
        HttpRequestMessage request,
        int statusCode,
        long durationMs,
        string? errorType,
        string? errorMessage)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return;
        }

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

        if (!httpContext.Items.TryGetValue(
                TraceContextKeys.HttpEntries,
                out var entriesValue))
        {
            return;
        }

        if (entriesValue is not ConcurrentQueue<HttpTraceEntry> entries)
        {
            return;
        }

        // Query-string credentials are masked before anything is stored.
        var url = UrlRedactor.Redact(request.RequestUri);

        if (url.Length > MaxUrlLength)
        {
            url = url[..MaxUrlLength];
        }

        var entry = new HttpTraceEntry
        {
            TraceId = traceId,

            Method = request.Method.Method,

            Url = url,

            StatusCode = statusCode,

            DurationMs = Math.Max(
                0,
                durationMs),

            CreatedAtUtc = DateTime.UtcNow,

            ErrorType = errorType,

            ErrorMessage = errorMessage
        };

        entries.Enqueue(entry);

        if (errorType is null)
        {
            _logger.LogInformation(
                "TraceLite HTTP: {Method} {Url} - {StatusCode} in {DurationMs} ms",
                entry.Method,
                entry.Url,
                entry.StatusCode,
                entry.DurationMs);
        }
        else
        {
            _logger.LogWarning(
                "TraceLite HTTP: {Method} {Url} failed with {ErrorType} after {DurationMs} ms",
                entry.Method,
                entry.Url,
                errorType,
                entry.DurationMs);
        }
    }
}
