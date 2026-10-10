using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TraceLite.Api.DTOs;
using TraceLite.Tracing;

namespace TraceLite.Api.Controllers;

[ApiController]
[Route("api/external")]
public class ExternalApiController : ControllerBase
{
    public const string ClientName = "ExternalApiTester";

    // Stop downloading after this many bytes; the size is still reported
    // from Content-Length when the server sends it.
    private const int MaxBodyBytes = 1_000_000;

    private const int MaxBodyCharacters = 200_000;

    private static readonly string[] AllowedMethods =
    [
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "HEAD"
    ];

    private static readonly string[] TextualMediaTypes =
    [
        "json",
        "xml",
        "html",
        "javascript",
        "x-www-form-urlencoded",
        "yaml",
        "csv"
    ];

    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalApiController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestApi(
        [FromBody] ExternalApiTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(
                request.Url,
                UriKind.Absolute,
                out var uri))
        {
            return BadRequest("Invalid URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return BadRequest(
                "Only HTTP and HTTPS URLs are allowed.");
        }

        var method = request.Method.ToUpperInvariant();

        if (!AllowedMethods.Contains(method))
        {
            return BadRequest("Unsupported HTTP method.");
        }

        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(method),
            uri);

        if (!string.IsNullOrWhiteSpace(request.Body) &&
            method != "GET" &&
            method != "HEAD")
        {
            httpRequest.Content = new StringContent(
                request.Body);

            httpRequest.Content.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/json");
        }

        if (request.Headers is not null)
        {
            foreach (var header in request.Headers)
            {
                if (header.Key.Equals(
                        "Host",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Content headers such as Content-Type are rejected on the
                // request itself and belong on the body instead.
                if (!httpRequest.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value) &&
                    httpRequest.Content is not null)
                {
                    httpRequest.Content.Headers.Remove(header.Key);

                    httpRequest.Content.Headers.TryAddWithoutValidation(
                        header.Key,
                        header.Value);
                }
            }
        }

        var client = _httpClientFactory
            .CreateClient(ClientName);

        var stopwatch = Stopwatch.StartNew();

        long? timeToFirstByteMs = null;

        try
        {
            using var response = await client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            timeToFirstByteMs = stopwatch.ElapsedMilliseconds;

            var (bodyBytes, reachedLimit) =
                await ReadBodyAsync(response, cancellationToken);

            stopwatch.Stop();

            var contentType =
                response.Content.Headers.ContentType?.ToString();

            var (body, bodyTruncated) = DecodeBody(
                bodyBytes,
                response.Content.Headers.ContentType);

            var finalUri = response.RequestMessage?.RequestUri;

            return Ok(new ExternalApiTestResponse
            {
                StatusCode = (int)response.StatusCode,

                ReasonPhrase = response.ReasonPhrase ?? string.Empty,

                DurationMs = stopwatch.ElapsedMilliseconds,

                TimeToFirstByteMs = timeToFirstByteMs,

                ResponseBody = body,

                ResponseBodyTruncated = reachedLimit || bodyTruncated,

                ResponseBytes =
                    response.Content.Headers.ContentLength
                    ?? bodyBytes.Length,

                ContentType = contentType,

                HttpVersion = response.Version.ToString(),

                FinalUrl = finalUri is not null && finalUri != uri
                    ? finalUri.AbsoluteUri
                    : null,

                Headers = GetHeaders(response)
            });
        }
        catch (Exception exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            var (errorType, errorMessage) =
                HttpErrorClassifier.Classify(
                    exception,
                    callerCancelled: false);

            return Ok(new ExternalApiTestResponse
            {
                StatusCode = 0,

                DurationMs = stopwatch.ElapsedMilliseconds,

                TimeToFirstByteMs = timeToFirstByteMs,

                ErrorType = errorType,

                ErrorMessage = errorMessage
            });
        }
    }

    private static async Task<(byte[] Bytes, bool ReachedLimit)> ReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        using var buffer = new MemoryStream();

        var chunk = new byte[81_920];

        int read;

        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            var remaining = MaxBodyBytes - (int)buffer.Length;

            buffer.Write(chunk, 0, Math.Min(read, remaining));

            if (read >= remaining)
            {
                return (buffer.ToArray(), true);
            }
        }

        return (buffer.ToArray(), false);
    }

    private static (string Body, bool Truncated) DecodeBody(
        byte[] bytes,
        MediaTypeHeaderValue? contentType)
    {
        if (bytes.Length == 0)
        {
            return (string.Empty, false);
        }

        var mediaType = contentType?.MediaType ?? string.Empty;

        var isText =
            mediaType.Length == 0 ||
            mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            TextualMediaTypes.Any(textual =>
                mediaType.Contains(textual, StringComparison.OrdinalIgnoreCase));

        if (!isText)
        {
            return ($"[{mediaType} content, {bytes.Length:N0} bytes, not shown]", false);
        }

        var body = GetEncoding(contentType?.CharSet).GetString(bytes);

        return body.Length > MaxBodyCharacters
            ? (body[..MaxBodyCharacters], true)
            : (body, false);
    }

    private static Encoding GetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static Dictionary<string, string> GetHeaders(
        HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return headers;
    }
}
