namespace TraceLite.Api.DTOs
{
    // An outbound call together with the inbound request that made it
    public class ExternalCallResponse : HttpPerformanceRecordResponse
    {
        public long RequestId { get; set; }

        public string RequestMethod { get; set; } = string.Empty;

        public string RequestPath { get; set; } = string.Empty;

        public int RequestStatusCode { get; set; }

        public long RequestDurationMs { get; set; }

        public DateTime RequestCreatedAtUtc { get; set; }
    }
}
