using System.Threading.Channels;

namespace TraceLite.Tracing
{
    internal class TraceQueue
    {
        private const int Capacity = 10_000;

        // Wait mode makes TryWrite return false when full,
        // so the caller can log the dropped trace instead of blocking the request.
        private readonly Channel<TraceBatch> _channel =
            Channel.CreateBounded<TraceBatch>(
                new BoundedChannelOptions(Capacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true
                });

        public ChannelReader<TraceBatch> Reader => _channel.Reader;

        public bool TryEnqueue(TraceBatch batch)
        {
            return _channel.Writer.TryWrite(batch);
        }
    }
}
