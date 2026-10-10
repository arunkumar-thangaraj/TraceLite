namespace TraceLite.Tracing;

// Masks credentials in URLs before they are stored, so API keys passed
// in query strings (or user:password@host) never reach the trace tables.
internal static class UrlRedactor
{
    private const string Mask = "REDACTED";

    private static readonly string[] SensitiveNames =
    [
        "key",
        "token",
        "secret",
        "password",
        "pwd",
        "signature",
        "sig",
        "auth",
        "credential",
        "session",
        "code"
    ];

    public static string Redact(Uri? uri)
    {
        if (uri is null)
        {
            return string.Empty;
        }

        if (!uri.IsAbsoluteUri)
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(uri);

        if (!string.IsNullOrEmpty(builder.UserName) ||
            !string.IsNullOrEmpty(builder.Password))
        {
            builder.UserName = Mask;
            builder.Password = string.Empty;
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            var parameters = uri.Query
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(RedactParameter);

            builder.Query = string.Join('&', parameters);
        }

        return builder.Uri.AbsoluteUri;
    }

    private static string RedactParameter(string parameter)
    {
        var separator = parameter.IndexOf('=');

        if (separator < 0)
        {
            return parameter;
        }

        var name = Uri.UnescapeDataString(parameter[..separator]);

        var isSensitive = SensitiveNames.Any(sensitive =>
            name.Contains(sensitive, StringComparison.OrdinalIgnoreCase));

        return isSensitive
            ? $"{parameter[..separator]}={Mask}"
            : parameter;
    }
}
