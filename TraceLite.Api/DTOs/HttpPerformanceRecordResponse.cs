namespace TraceLite.Api.DTOs
{
    public class HttpPerformanceRecordResponse
    {
        public long Id { get; set; }

        public string TraceId { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public long DurationMs { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        // Null when a response was received; an HTTP error status is not an exception
        public string? ErrorType { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
