namespace TraceLite.Entities
{
    public class SqlPerformanceRecord
    {
        public long Id { get; set; }

        public string TraceId { get; set; } = string.Empty;

        public string CommandType { get; set; } = string.Empty;

        public string CommandText { get; set; } = string.Empty;

        public long DurationMs { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        // Null when the command succeeded
        public string? ErrorType { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
