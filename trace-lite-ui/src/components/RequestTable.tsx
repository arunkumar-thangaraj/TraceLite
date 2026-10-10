import type { PerformanceRecord } from "../types/performance";
import {
  formatDateTime,
  formatDuration,
  formatEndpoint,
} from "../utils/format";
import { MethodBadge, StatusBadge } from "./Badges";

interface RequestTableProps {
  records: PerformanceRecord[];
  onSelect: (record: PerformanceRecord) => void;
  emptyMessage?: string;
}

export default function RequestTable({
  records,
  onSelect,
  emptyMessage = "No performance records found.",
}: RequestTableProps) {
  if (records.length === 0) {
    return <p className="state-message">{emptyMessage}</p>;
  }

  return (
    <div className="table-scroll">
      <table className="data-table stack-on-mobile">
        <thead>
          <tr>
            <th>Method</th>
            <th>Path</th>
            <th>Status</th>
            <th className="align-right">Duration</th>
            <th>Time</th>
          </tr>
        </thead>

        <tbody>
          {records.map((record) => (
            <tr
              key={record.id}
              className="clickable-row"
              onClick={() => onSelect(record)}
              tabIndex={0}
              onKeyDown={(event) => {
                if (
                  event.key === "Enter" ||
                  event.key === " "
                ) {
                  event.preventDefault();
                  onSelect(record);
                }
              }}
            >
              <td className="cell-method">
                <MethodBadge method={record.method} />
              </td>

              <td className="cell-path" title={record.path}>
                {record.path}

                {record.externalCalls?.map((call, index) => (
                  <span
                    key={index}
                    className={`call-target${call.errorType || call.statusCode >= 400 ? " call-target-failed" : ""}`}
                    title={call.url}
                  >
                    → {call.method} {formatEndpoint(call.url)}
                  </span>
                ))}

                {(record.externalCallCount ?? 0) >
                  (record.externalCalls?.length ?? 0) && (
                  <span className="call-target call-target-more">
                    + {(record.externalCallCount ?? 0) - (record.externalCalls?.length ?? 0)} more external calls
                  </span>
                )}
              </td>

              <td className="cell-status">
                <StatusBadge statusCode={record.statusCode} />
              </td>

              <td className="cell-duration align-right cell-numeric">
                {formatDuration(record.durationMs)}
              </td>

              <td className="cell-time cell-muted">
                {formatDateTime(record.createdAtUtc)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
