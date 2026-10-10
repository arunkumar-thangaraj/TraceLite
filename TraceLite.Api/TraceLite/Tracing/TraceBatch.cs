using TraceLite.Entities;

namespace TraceLite.Tracing
{
    internal class TraceBatch
    {
        public required PerformanceRecord Request { get; init; }

        public IReadOnlyList<SqlPerformanceRecord> SqlRecords { get; init; } = [];

        public IReadOnlyList<HttpPerformanceRecord> HttpRecords { get; init; } = [];
    }
}
