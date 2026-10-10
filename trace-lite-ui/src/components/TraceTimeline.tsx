import type {
  HttpPerformanceRecord,
  PerformanceRecord,
  SqlPerformanceRecord,
} from "../types/performance";
import { formatDuration } from "../utils/format";

interface TraceTimelineProps {
  record: PerformanceRecord;
  sqlRecords: SqlPerformanceRecord[];
  httpRecords: HttpPerformanceRecord[];
}

interface Span {
  key: string;
  kind: "sql" | "http";
  label: string;
  offsetMs: number;
  durationMs: number;
  failed: boolean;
}

const TICKS = [0, 0.25, 0.5, 0.75, 1];

/* Minimum bar width so sub-millisecond spans stay visible */
const MIN_WIDTH_PERCENT = 0.8;

function describeUrl(method: string, url: string): string {
  try {
    const parsed = new URL(url);
    return `${method} ${parsed.host}${parsed.pathname}`;
  } catch {
    return `${method} ${url}`;
  }
}

function describeSql(commandText: string): string {
  const compact = commandText.replace(/\s+/g, " ").trim();
  return compact.length > 80 ? `${compact.slice(0, 80)}…` : compact;
}

/*
  Rows are stored with their end time (createdAtUtc) and duration,
  so each span's start is end − duration, relative to the request start.
*/
export default function TraceTimeline({
  record,
  sqlRecords,
  httpRecords,
}: TraceTimelineProps) {
  const totalMs = Math.max(record.durationMs, 1);

  const requestStart =
    Date.parse(record.createdAtUtc) - record.durationMs;

  function toOffset(createdAtUtc: string, durationMs: number) {
    const start = Date.parse(createdAtUtc) - durationMs;
    return Math.min(Math.max(start - requestStart, 0), totalMs);
  }

  const spans: Span[] = [
    ...sqlRecords.map((sql) => ({
      key: `sql-${sql.id}`,
      kind: "sql" as const,
      label: describeSql(sql.commandText),
      offsetMs: toOffset(sql.createdAtUtc, sql.durationMs),
      durationMs: sql.durationMs,
      failed: Boolean(sql.errorType),
    })),
    ...httpRecords.map((http) => ({
      key: `http-${http.id}`,
      kind: "http" as const,
      label: describeUrl(http.method, http.url),
      offsetMs: toOffset(http.createdAtUtc, http.durationMs),
      durationMs: http.durationMs,
      failed: Boolean(http.errorType) || http.statusCode >= 500,
    })),
  ].sort((a, b) => a.offsetMs - b.offsetMs);

  function barStyle(offsetMs: number, durationMs: number) {
    const width = Math.max(
      (durationMs / totalMs) * 100,
      MIN_WIDTH_PERCENT
    );

    const left = Math.min((offsetMs / totalMs) * 100, 100 - width);

    return { left: `${left}%`, width: `${width}%` };
  }

  return (
    <div className="timeline">
      <div className="timeline-row timeline-axis" aria-hidden="true">
        <span />

        <div className="timeline-track">
          {TICKS.map((tick) => (
            <span
              key={tick}
              className="timeline-tick"
              style={{ left: `${tick * 100}%` }}
            >
              {formatDuration(totalMs * tick)}
            </span>
          ))}
        </div>
      </div>

      <div className="timeline-row">
        <span className="timeline-label">
          <span className="timeline-kind kind-request">REQ</span>
          {record.method} {record.path}
        </span>

        <div className="timeline-track">
          <span
            className="timeline-bar bar-request"
            style={barStyle(0, record.durationMs)}
            title={formatDuration(record.durationMs)}
          />
        </div>
      </div>

      {spans.map((span) => (
        <div className="timeline-row" key={span.key}>
          <span className="timeline-label" title={span.label}>
            <span className={`timeline-kind kind-${span.kind}`}>
              {span.kind === "sql" ? "SQL" : "HTTP"}
            </span>
            {span.label}
          </span>

          <div className="timeline-track">
            <span
              className={`timeline-bar bar-${span.failed ? "failed" : span.kind}`}
              style={barStyle(span.offsetMs, span.durationMs)}
              title={`${formatDuration(span.durationMs)} · starts at ${formatDuration(span.offsetMs)}`}
            />
          </div>
        </div>
      ))}
    </div>
  );
}
