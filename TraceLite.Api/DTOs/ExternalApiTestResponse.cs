namespace TraceLite.Api.DTOs
{
    public class ExternalApiTestResponse
    {
        // 0 when no response was received; see ErrorType
        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; } = string.Empty;

        public long DurationMs { get; set; }

        // Time until the response headers arrived
        public long? TimeToFirstByteMs { get; set; }

        public string ResponseBody { get; set; } = string.Empty;

        public bool ResponseBodyTruncated { get; set; }

        public long ResponseBytes { get; set; }

        public string? ContentType { get; set; }

        public string? HttpVersion { get; set; }

        // Set when redirects were followed
        public string? FinalUrl { get; set; }

        public Dictionary<string, string> Headers { get; set; } = [];

        public string? ErrorType { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
