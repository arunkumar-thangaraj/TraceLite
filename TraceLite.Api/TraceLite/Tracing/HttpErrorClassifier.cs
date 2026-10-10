using TraceLite.Security;

namespace TraceLite.Tracing;

// Turns HttpClient exceptions into a short, filterable error type
// instead of recording every failure as status code 0.
public static class HttpErrorClassifier
{
    public const int MaxMessageLength = 1000;

    public static (string ErrorType, string ErrorMessage) Classify(
        Exception exception,
        bool callerCancelled)
    {
        var blocked = FindInChain<BlockedDestinationException>(exception);

        if (blocked is not null)
        {
            return ("Blocked", Truncate(blocked.Message));
        }

        var errorType = exception switch
        {
            // HttpClient.Timeout surfaces as a cancellation wrapping a TimeoutException
            OperationCanceledException { InnerException: TimeoutException } => "Timeout",
            OperationCanceledException when callerCancelled => "Cancelled",
            OperationCanceledException => "Timeout",

            HttpRequestException httpException => httpException.HttpRequestError switch
            {
                HttpRequestError.NameResolutionError => "DnsFailure",
                HttpRequestError.ConnectionError => "ConnectionFailed",
                HttpRequestError.SecureConnectionError => "TlsError",
                HttpRequestError.ResponseEnded => "ResponseEnded",
                HttpRequestError.InvalidResponse => "InvalidResponse",
                _ => "NetworkError"
            },

            _ => exception.GetType().Name
        };

        return (errorType, Truncate(exception.Message));
    }

    public static string Truncate(string message)
    {
        return message.Length > MaxMessageLength
            ? message[..MaxMessageLength]
            : message;
    }

    private static T? FindInChain<T>(Exception? exception)
        where T : Exception
    {
        while (exception is not null)
        {
            if (exception is T match)
            {
                return match;
            }

            exception = exception.InnerException;
        }

        return null;
    }
}
