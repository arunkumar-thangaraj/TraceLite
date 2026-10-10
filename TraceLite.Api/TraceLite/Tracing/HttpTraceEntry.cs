namespace TraceLite.Tracing
{
    internal class HttpTraceEntry
    {
        public string TraceId { get; init; } = string.Empty;

        public string Method { get; init; } = string.Empty;

        public string Url { get; init; } = string.Empty;

        public int StatusCode { get; init; }

        public long DurationMs { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string? ErrorType { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
