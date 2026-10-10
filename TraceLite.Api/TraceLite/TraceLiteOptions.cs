namespace TraceLite;

public sealed class TraceLiteOptions
{
    // Requests whose path starts with one of these segments are not traced
    // (health probes, docs and the dashboard's own API are just noise).
    public IList<string> ExcludedPaths { get; } = new List<string>
    {
        "/health",
        "/swagger"
    };
}
