import { useEffect, useState } from "react";
import {
  getHttpForRequest,
  getSqlForRequest,
} from "../api/performanceApi";
import type {
  HttpPerformanceRecord,
  PerformanceRecord,
  SqlPerformanceRecord,
} from "../types/performance";
import { formatDateTime, formatDuration } from "../utils/format";
import { MethodBadge, StatusBadge } from "./Badges";
import Icon from "./Icon";
import TraceTimeline from "./TraceTimeline";

interface RequestDetailsProps {
  record: PerformanceRecord;
  onBack: () => void;
}

function sumDurations(items: { durationMs: number }[]): number {
  return items.reduce((total, item) => total + item.durationMs, 0);
}

export default function RequestDetails({
  record,
  onBack,
}: RequestDetailsProps) {
  const [sqlRecords, setSqlRecords] = useState<
    SqlPerformanceRecord[]
  >([]);

  const [httpRecords, setHttpRecords] = useState<
    HttpPerformanceRecord[]
  >([]);

  const [loading, setLoading] = useState(true);

  const [sqlError, setSqlError] = useState<string | null>(null);

  const [httpError, setHttpError] = useState<string | null>(null);

  const [copied, setCopied] = useState(false);

  useEffect(() => {
    async function loadSpans() {
      setLoading(true);
      setSqlError(null);
      setHttpError(null);

      // Load both independently so one failing still shows the other
      const [sqlResult, httpResult] = await Promise.allSettled([
        getSqlForRequest(record.id),
        getHttpForRequest(record.id),
      ]);

      if (sqlResult.status === "fulfilled") {
        setSqlRecords(sqlResult.value);
      } else {
        console.error("Failed to load SQL records:", sqlResult.reason);
        setSqlError("Failed to load SQL queries.");
      }

      if (httpResult.status === "fulfilled") {
        setHttpRecords(httpResult.value);
      } else {
        console.error("Failed to load HTTP records:", httpResult.reason);
        setHttpError("Failed to load external calls.");
      }

      setLoading(false);
    }

    loadSpans();
  }, [record.id]);

  async function copyTraceId() {
    try {
      await navigator.clipboard.writeText(record.traceId);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch (error) {
      console.error("Failed to copy trace ID:", error);
    }
  }

  const failedHttpCount = httpRecords.filter(
    (http) => http.errorType || http.statusCode >= 400
  ).length;

  const failedSqlCount = sqlRecords.filter(
    (sql) => sql.errorType
  ).length;

  const hasSpans = sqlRecords.length > 0 || httpRecords.length > 0;

  return (
    <>
      <button
        type="button"
        className="back-link"
        onClick={onBack}
      >
        <Icon name="arrow-left" size={16} />
        Back to requests
      </button>

      <section className="panel">
        <header className="details-summary">
          <div className="details-title">
            <MethodBadge method={record.method} />
            <h1>{record.path}</h1>
          </div>

          <div className="details-badges">
            <StatusBadge statusCode={record.statusCode} />
            <span className="badge badge-neutral">
              <Icon name="clock" size={12} />
              {formatDuration(record.durationMs)}
            </span>
          </div>
        </header>

        <dl className="detail-grid">
          <div>
            <dt>Time in SQL</dt>
            <dd>
              {loading
                ? "—"
                : `${formatDuration(sumDurations(sqlRecords))} · ${sqlRecords.length} ${sqlRecords.length === 1 ? "query" : "queries"}`}
            </dd>
          </div>

          <div>
            <dt>Time in external calls</dt>
            <dd>
              {loading
                ? "—"
                : `${formatDuration(sumDurations(httpRecords))} · ${httpRecords.length} ${httpRecords.length === 1 ? "call" : "calls"}`}
            </dd>
          </div>

          <div>
            <dt>Created</dt>
            <dd>{formatDateTime(record.createdAtUtc)}</dd>
          </div>

          <div className="detail-wide">
            <dt>Trace ID</dt>
            <dd className="trace-id">
              <code>{record.traceId}</code>

              <button
                type="button"
                className="button button-ghost"
                onClick={copyTraceId}
              >
                {copied ? "Copied" : "Copy"}
              </button>
            </dd>
          </div>
        </dl>
      </section>

      {/* Timeline */}
      {!loading && hasSpans && (
        <section className="panel">
          <div className="panel-header">
            <div>
              <h2 className="panel-title">
                <Icon name="activity" size={16} />
                Timeline
              </h2>
              <p className="panel-subtitle">
                When each SQL query and external call ran during the request
              </p>
            </div>

            <ul className="chart-legend">
              <li><span className="legend-swatch bar-request" />Request</li>
              <li><span className="legend-swatch bar-sql" />SQL</li>
              <li><span className="legend-swatch bar-http" />External call</li>
              <li><span className="legend-swatch bar-failed" />Failed</li>
            </ul>
          </div>

          <div className="panel-body">
            <TraceTimeline
              record={record}
              sqlRecords={sqlRecords}
              httpRecords={httpRecords}
            />
          </div>
        </section>
      )}

      {/* External calls */}
      <section className="panel">
        <div className="panel-header">
          <h2 className="panel-title">
            <Icon name="send" size={16} />
            External calls
            <span className="count-pill">{httpRecords.length}</span>
            {failedHttpCount > 0 && (
              <span className="badge badge-danger">
                {failedHttpCount} failed
              </span>
            )}
          </h2>
        </div>

        {loading && (
          <p className="state-message">Loading external calls…</p>
        )}

        {httpError && (
          <div className="panel-body">
            <div className="alert" role="alert">{httpError}</div>
          </div>
        )}

        {!loading && !httpError && httpRecords.length === 0 && (
          <p className="state-message">
            No outbound HTTP calls were made during this request.
          </p>
        )}

        {!loading && !httpError && httpRecords.length > 0 && (
          <ol className="sql-list">
            {httpRecords.map((http) => (
              <li className="sql-item" key={http.id}>
                <div className="sql-item-header">
                  <span className="sql-item-title">
                    <MethodBadge method={http.method} />

                    {http.errorType ? (
                      <span className="badge badge-danger">
                        <span className="badge-dot" aria-hidden="true" />
                        {http.errorType}
                      </span>
                    ) : (
                      <StatusBadge statusCode={http.statusCode} />
                    )}
                  </span>

                  <span className="cell-numeric">
                    {formatDuration(http.durationMs)}
                  </span>
                </div>

                <code className="call-url">{http.url}</code>

                {http.errorMessage && (
                  <p className="span-error">{http.errorMessage}</p>
                )}
              </li>
            ))}
          </ol>
        )}
      </section>

      {/* SQL queries */}
      <section className="panel">
        <div className="panel-header">
          <h2 className="panel-title">
            <Icon name="database" size={16} />
            SQL queries
            <span className="count-pill">{sqlRecords.length}</span>
            {failedSqlCount > 0 && (
              <span className="badge badge-danger">
                {failedSqlCount} failed
              </span>
            )}
          </h2>
        </div>

        {loading && (
          <p className="state-message">Loading SQL queries…</p>
        )}

        {sqlError && (
          <div className="panel-body">
            <div className="alert" role="alert">{sqlError}</div>
          </div>
        )}

        {!loading && !sqlError && sqlRecords.length === 0 && (
          <p className="state-message">
            No SQL queries were recorded for this request.
          </p>
        )}

        {!loading && !sqlError && sqlRecords.length > 0 && (
          <ol className="sql-list">
            {sqlRecords.map((sqlRecord, index) => (
              <li className="sql-item" key={sqlRecord.id}>
                <div className="sql-item-header">
                  <span className="sql-item-title">
                    Query {index + 1}
                    <span className="badge badge-neutral">
                      {sqlRecord.commandType}
                    </span>
                    {sqlRecord.errorType && (
                      <span className="badge badge-danger">
                        <span className="badge-dot" aria-hidden="true" />
                        {sqlRecord.errorType}
                      </span>
                    )}
                  </span>

                  <span className="cell-numeric">
                    {formatDuration(sqlRecord.durationMs)}
                  </span>
                </div>

                <pre className="code-block">{sqlRecord.commandText}</pre>

                {sqlRecord.errorMessage && (
                  <p className="span-error">{sqlRecord.errorMessage}</p>
                )}
              </li>
            ))}
          </ol>
        )}
      </section>
    </>
  );
}
