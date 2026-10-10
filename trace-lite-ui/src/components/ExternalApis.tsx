import { useEffect, useMemo, useState } from "react";
import { getExternalCalls } from "../api/performanceApi";
import type {
  ExternalCall,
  PerformanceRecord,
} from "../types/performance";
import {
  formatDateTime,
  formatDuration,
  getHost,
} from "../utils/format";
import { MethodBadge, StatusBadge } from "./Badges";
import Icon from "./Icon";
import MetricCard from "./MetricCard";
import PageHeader from "./PageHeader";

interface ExternalApisProps {
  onSelectRequest: (record: PerformanceRecord) => void;
}

interface HostSummary {
  host: string;
  calls: number;
  failed: number;
  averageMs: number;
  slowestMs: number;
  lastSeen: string;
}

function isFailed(call: ExternalCall): boolean {
  return Boolean(call.errorType) || call.statusCode >= 400;
}

export default function ExternalApis({
  onSelectRequest,
}: ExternalApisProps) {
  const [calls, setCalls] = useState<ExternalCall[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);
  const [search, setSearch] = useState("");
  const [hostFilter, setHostFilter] = useState<string | null>(null);

  useEffect(() => {
    async function loadCalls() {
      try {
        setLoading(true);
        setError("");
        setCalls(await getExternalCalls());
      } catch (loadError) {
        console.error(loadError);
        setError(
          "Unable to load external calls. Check that the API is running."
        );
      } finally {
        setLoading(false);
      }
    }

    loadCalls();
  }, [reloadKey]);

  const hosts = useMemo(() => {
    const byHost = new Map<string, ExternalCall[]>();

    for (const call of calls) {
      const host = getHost(call.url);
      byHost.set(host, [...(byHost.get(host) ?? []), call]);
    }

    return [...byHost.entries()]
      .map(([host, hostCalls]): HostSummary => ({
        host,
        calls: hostCalls.length,
        failed: hostCalls.filter(isFailed).length,
        averageMs:
          hostCalls.reduce((total, call) => total + call.durationMs, 0) /
          hostCalls.length,
        slowestMs: Math.max(...hostCalls.map((call) => call.durationMs)),
        // Calls arrive newest first
        lastSeen: hostCalls[0].createdAtUtc,
      }))
      .sort((a, b) => b.calls - a.calls);
  }, [calls]);

  const filteredCalls = useMemo(() => {
    const query = search.trim().toLowerCase();

    return calls.filter(
      (call) =>
        (!hostFilter || getHost(call.url) === hostFilter) &&
        (!query ||
          call.url.toLowerCase().includes(query) ||
          call.method.toLowerCase().includes(query) ||
          String(call.statusCode).includes(query) ||
          (call.errorType ?? "").toLowerCase().includes(query))
    );
  }, [calls, search, hostFilter]);

  const failedCount = calls.filter(isFailed).length;

  const averageMs =
    calls.length === 0
      ? 0
      : calls.reduce((total, call) => total + call.durationMs, 0) /
        calls.length;

  function openRequest(call: ExternalCall) {
    onSelectRequest({
      id: call.requestId,
      traceId: call.traceId,
      method: call.requestMethod,
      path: call.requestPath,
      statusCode: call.requestStatusCode,
      durationMs: call.requestDurationMs,
      createdAtUtc: call.requestCreatedAtUtc,
    });
  }

  return (
    <>
      <PageHeader
        title="External APIs"
        description="Every outbound call your API made, with the real endpoint it reached."
        actions={
          <button
            type="button"
            className="button button-secondary"
            onClick={() => setReloadKey((key) => key + 1)}
            disabled={loading}
          >
            <Icon name="refresh" size={16} />
            {loading ? "Refreshing…" : "Refresh"}
          </button>
        }
      />

      {error && (
        <div className="alert" role="alert">
          <Icon name="alert" size={18} />
          {error}
        </div>
      )}

      <section className="metrics-grid">
        <MetricCard
          title="External calls"
          value={calls.length}
          subtitle="Most recent 200"
          icon="globe"
          accent="blue"
        />

        <MetricCard
          title="Hosts"
          value={hosts.length}
          subtitle="Distinct APIs called"
          icon="activity"
          accent="violet"
        />

        <MetricCard
          title="Average duration"
          value={formatDuration(averageMs)}
          subtitle="Across all calls"
          icon="clock"
          accent="green"
        />

        <MetricCard
          title="Failed"
          value={failedCount}
          subtitle="Errors and 4xx/5xx responses"
          icon="alert"
          accent="red"
          tone={failedCount > 0 ? "danger" : "default"}
        />
      </section>

      {/* By host */}
      <section className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              By host
              <span className="count-pill">{hosts.length}</span>
            </h2>
            <p className="panel-subtitle">
              Select a host to filter the calls below.
            </p>
          </div>
        </div>

        {loading ? (
          <p className="state-message">Loading external calls…</p>
        ) : hosts.length === 0 ? (
          <p className="state-message">
            No external calls recorded yet. Send one from the API Tester.
          </p>
        ) : (
          <div className="table-scroll">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Host</th>
                  <th className="align-right">Calls</th>
                  <th className="align-right">Failed</th>
                  <th className="align-right">Average</th>
                  <th className="align-right">Slowest</th>
                  <th>Last seen</th>
                </tr>
              </thead>

              <tbody>
                {hosts.map((host) => (
                  <tr
                    key={host.host}
                    className={`clickable-row${hostFilter === host.host ? " row-selected" : ""}`}
                    tabIndex={0}
                    aria-pressed={hostFilter === host.host}
                    onClick={() =>
                      setHostFilter((current) =>
                        current === host.host ? null : host.host
                      )
                    }
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        setHostFilter((current) =>
                          current === host.host ? null : host.host
                        );
                      }
                    }}
                  >
                    <td className="cell-host">
                      <Icon name="globe" size={14} />
                      {host.host}
                    </td>
                    <td className="align-right cell-numeric">{host.calls}</td>
                    <td className="align-right cell-numeric">
                      {host.failed > 0 ? (
                        <span className="badge badge-danger">{host.failed}</span>
                      ) : (
                        0
                      )}
                    </td>
                    <td className="align-right cell-numeric">
                      {formatDuration(host.averageMs)}
                    </td>
                    <td className="align-right cell-numeric">
                      {formatDuration(host.slowestMs)}
                    </td>
                    <td className="cell-muted">
                      {formatDateTime(host.lastSeen)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* All calls */}
      <section className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              Calls
              <span className="count-pill">{filteredCalls.length}</span>
              {hostFilter && (
                <button
                  type="button"
                  className="filter-chip"
                  onClick={() => setHostFilter(null)}
                  aria-label={`Clear host filter ${hostFilter}`}
                >
                  {hostFilter} ×
                </button>
              )}
            </h2>
            <p className="panel-subtitle">
              Select a call to open the request that made it.
            </p>
          </div>

          <div className="search-field">
            <Icon name="search" size={16} />

            <input
              type="search"
              className="input search-input"
              placeholder="Filter by URL, method, status or error"
              aria-label="Filter external calls"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>
        </div>

        {loading ? (
          <p className="state-message">Loading external calls…</p>
        ) : filteredCalls.length === 0 ? (
          <p className="state-message">
            {calls.length === 0
              ? "No external calls recorded yet."
              : "No calls match your filter."}
          </p>
        ) : (
          <div className="table-scroll">
            <table className="data-table stack-on-mobile">
              <thead>
                <tr>
                  <th>Method</th>
                  <th>Endpoint</th>
                  <th>Status</th>
                  <th className="align-right">Duration</th>
                  <th>Time</th>
                </tr>
              </thead>

              <tbody>
                {filteredCalls.map((call) => (
                  <tr
                    key={call.id}
                    className="clickable-row"
                    tabIndex={0}
                    onClick={() => openRequest(call)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        openRequest(call);
                      }
                    }}
                  >
                    <td className="cell-method">
                      <MethodBadge method={call.method} />
                    </td>

                    <td className="cell-path" title={call.url}>
                      {call.url}
                      <span className="call-target call-target-more">
                        from {call.requestMethod} {call.requestPath}
                      </span>
                    </td>

                    <td className="cell-status">
                      {call.errorType ? (
                        <span className="badge badge-danger" title={call.errorMessage ?? undefined}>
                          <span className="badge-dot" aria-hidden="true" />
                          {call.errorType}
                        </span>
                      ) : (
                        <StatusBadge statusCode={call.statusCode} />
                      )}
                    </td>

                    <td className="cell-duration align-right cell-numeric">
                      {formatDuration(call.durationMs)}
                    </td>

                    <td className="cell-time cell-muted">
                      {formatDateTime(call.createdAtUtc)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
