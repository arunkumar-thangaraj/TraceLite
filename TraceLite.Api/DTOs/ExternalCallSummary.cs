namespace TraceLite.Api.DTOs
{
    // Short form of an outbound call, shown under its request in lists
    public class ExternalCallSummary
    {
        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public string? ErrorType { get; set; }
    }
}
