import { useMemo, useState, type FormEvent } from "react";
import { testExternalApi } from "../api/performanceApi";
import type {
  ExternalApiTestResponse,
} from "../types/performance";
import { formatBytes, formatDuration } from "../utils/format";
import { StatusBadge } from "./Badges";

const METHODS = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"];

const METHODS_WITHOUT_BODY = ["GET", "HEAD"];

function formatResponseBody(body: string): string {
  try {
    return JSON.stringify(JSON.parse(body), null, 2);
  } catch {
    return body;
  }
}

export default function ApiTester() {
  const [method, setMethod] = useState("GET");

  const [url, setUrl] = useState(
    "https://jsonplaceholder.typicode.com/posts/1"
  );

  const [headers, setHeaders] = useState("");

  const [body, setBody] = useState("");

  const [result, setResult] =
    useState<ExternalApiTestResponse | null>(null);

  const [loading, setLoading] = useState(false);

  const [error, setError] = useState<string | null>(null);

  const [activeTab, setActiveTab] = useState<"body" | "headers">("body");

  const hasBody = !METHODS_WITHOUT_BODY.includes(method);

  const formattedResponse = useMemo(
    () => (result ? formatResponseBody(result.responseBody) : ""),
    [result]
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    let parsedHeaders: Record<string, string> = {};

    if (headers.trim()) {
      try {
        parsedHeaders = JSON.parse(headers);
      } catch {
        setError(
          'Headers must be a valid JSON object, e.g. {"Accept": "application/json"}.'
        );
        return;
      }
    }

    try {
      setLoading(true);
      setError(null);
      setResult(null);

      const response = await testExternalApi({
        method,
        url,
        headers: parsedHeaders,
        body: hasBody && body ? body : undefined,
      });

      setResult(response);
    } catch (err) {
      console.error(err);

      // Network failures come back as a result with errorType,
      // so reaching here means the TraceLite API itself rejected the call.
      setError(
        "TraceLite could not run the request. Check the URL format and that the API is running."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <form className="panel api-tester" onSubmit={handleSubmit}>
        <div className="request-bar">
          <select
            className="input"
            aria-label="HTTP method"
            value={method}
            onChange={(event) =>
              setMethod(event.target.value)
            }
          >
            {METHODS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>

          <input
            type="url"
            className="input"
            aria-label="Request URL"
            value={url}
            onChange={(event) =>
              setUrl(event.target.value)
            }
            placeholder="https://api.example.com/resource"
            required
          />

          <button
            type="submit"
            className="button button-primary"
            disabled={loading || !url.trim()}
          >
            {loading ? "Sending…" : "Send"}
          </button>
        </div>

        <div className="field-grid">
          <div className="field">
            <label htmlFor="api-headers">
              Headers
              <span className="field-hint">JSON object</span>
            </label>

            <textarea
              id="api-headers"
              className="input textarea"
              value={headers}
              onChange={(event) =>
                setHeaders(event.target.value)
              }
              placeholder='{"Accept": "application/json"}'
              spellCheck={false}
            />
          </div>

          {hasBody && (
            <div className="field">
              <label htmlFor="api-body">
                Request body
              </label>

              <textarea
                id="api-body"
                className="input textarea"
                value={body}
                onChange={(event) =>
                  setBody(event.target.value)
                }
                placeholder='{"name": "TraceLite"}'
                spellCheck={false}
              />
            </div>
          )}
        </div>

        {error && (
          <div className="alert" role="alert">
            {error}
          </div>
        )}
      </form>

      {result && result.errorType && (
        <section className="panel response-panel">
          <div className="panel-header">
            <h2 className="panel-title">Response</h2>

            <div className="response-meta">
              <span className="badge badge-danger">
                <span className="badge-dot" aria-hidden="true" />
                {result.errorType}
              </span>

              <span className="badge badge-neutral">
                {formatDuration(result.durationMs)}
              </span>
            </div>
          </div>

          <div className="panel-body">
            <div className="alert" role="alert">
              {result.errorMessage ?? "The request failed before a response was received."}
            </div>
          </div>
        </section>
      )}

      {result && !result.errorType && (
        <section className="panel response-panel">
          <div className="panel-header">
            <h2 className="panel-title">Response</h2>

            <div className="response-meta">
              <StatusBadge
                statusCode={result.statusCode}
                label={result.reasonPhrase}
              />

              <span className="badge badge-neutral">
                {formatDuration(result.durationMs)}
              </span>
            </div>
          </div>

          <dl className="response-stats">
            <div>
              <dt>Total time</dt>
              <dd>{formatDuration(result.durationMs)}</dd>
            </div>

            <div>
              <dt>Time to first byte</dt>
              <dd>
                {result.timeToFirstByteMs == null
                  ? "—"
                  : formatDuration(result.timeToFirstByteMs)}
              </dd>
            </div>

            <div>
              <dt>Size</dt>
              <dd>{formatBytes(result.responseBytes)}</dd>
            </div>

            <div>
              <dt>Protocol</dt>
              <dd>{result.httpVersion ? `HTTP/${result.httpVersion}` : "—"}</dd>
            </div>

            <div>
              <dt>Content type</dt>
              <dd>{result.contentType ?? "—"}</dd>
            </div>
          </dl>

          {result.finalUrl && (
            <p className="response-note">
              Redirected to <code>{result.finalUrl}</code>
            </p>
          )}

          <div className="tabs" role="tablist" aria-label="Response">
            <button
              type="button"
              role="tab"
              className="tab"
              aria-selected={activeTab === "body"}
              onClick={() => setActiveTab("body")}
            >
              Body
            </button>

            <button
              type="button"
              role="tab"
              className="tab"
              aria-selected={activeTab === "headers"}
              onClick={() => setActiveTab("headers")}
            >
              Headers
              <span className="count-pill">
                {Object.keys(result.headers).length}
              </span>
            </button>
          </div>

          {activeTab === "body" ? (
            <>
              {result.responseBodyTruncated && (
                <p className="response-note">
                  The body is large, so only the beginning is shown.
                </p>
              )}

              <pre className="code-block">
                {formattedResponse || "(empty response body)"}
              </pre>
            </>
          ) : (
            <table className="kv-table">
              <tbody>
                {Object.entries(result.headers).map(([name, value]) => (
                  <tr key={name}>
                    <th scope="row">{name}</th>
                    <td>{value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}
    </>
  );
}
