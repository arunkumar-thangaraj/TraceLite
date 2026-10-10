namespace TraceLite.Api.DTOs
{
    public class ExternalApiTestRequest
    {
        public string Method { get; set; } = "GET";

        public string Url { get; set; } = string.Empty;

        public Dictionary<string, string>? Headers { get; set; }

        public string? Body { get; set; }
    }
}
