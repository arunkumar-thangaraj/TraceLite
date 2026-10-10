namespace TraceLite.Tracing
{
    internal class SqlTraceEntry
    {
        public string TraceId { get; init; } = string.Empty;
        public string CommandType { get; init; } = string.Empty;

        public string CommandText { get; init; } = string.Empty;

        public long DurationMs { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public string? ErrorType { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
