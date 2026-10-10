namespace TraceLite.Api.DTOs
{
    public class PerformanceRecordResponse
    {
        public long Id { get; set; }

        public string TraceId { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public long DurationMs { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        // Outbound calls made while handling this request (list views only)
        public int ExternalCallCount { get; set; }

        public List<ExternalCallSummary> ExternalCalls { get; set; } = [];
    }
}
