namespace TraceLite.Entities
{
    public class PerformanceRecord
    {

        public long Id { get; set; }
        public string TraceId { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public long DurationMs { get; set; }

        public DateTime CreatedAtUtc { get; set; }


    }
}
