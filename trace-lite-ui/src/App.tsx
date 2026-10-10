import { useEffect, useMemo, useState } from "react";
import { getPerformanceRecords } from "./api/performanceApi";

import Icon, { type IconName } from "./components/Icon";
import MetricCard from "./components/MetricCard";
import PageHeader from "./components/PageHeader";
import RequestDetails from "./components/RequestDetails";
import RequestTable from "./components/RequestTable";
import ApiTester from "./components/ApiTester";
import ExternalApis from "./components/ExternalApis";
import PerformanceCharts from "./components/PerformanceCharts";

import type { PerformanceRecord } from "./types/performance";
import { formatDuration } from "./utils/format";

import "./App.css";

type Page =
  | "dashboard"
  | "external-apis"
  | "api-tester"
  | "performance-analyzer";

const NAV_ITEMS: {
  page: Page;
  label: string;
  shortLabel: string;
  icon: IconName;
}[] = [
  {
    page: "dashboard",
    label: "Dashboard",
    shortLabel: "Dashboard",
    icon: "dashboard",
  },
  {
    page: "external-apis",
    label: "External APIs",
    shortLabel: "External",
    icon: "globe",
  },
  {
    page: "api-tester",
    label: "API Tester",
    shortLabel: "Tester",
    icon: "send",
  },
  {
    page: "performance-analyzer",
    label: "Performance Analyzer",
    shortLabel: "Analyzer",
    icon: "chart",
  },
];

function App() {
  const [records, setRecords] = useState<PerformanceRecord[]>([]);
  const [selectedRecord, setSelectedRecord] =
    useState<PerformanceRecord | null>(null);

  const [currentPage, setCurrentPage] =
    useState<Page>("dashboard");

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  async function loadRecords() {
    try {
      setLoading(true);
      setError("");

      const data = await getPerformanceRecords();
      setRecords(data);
      setLastUpdated(new Date());
    } catch (error) {
      console.error(error);

      setError(
        "Unable to load performance records. Check that the API is running."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadRecords();
  }, []);

  const stats = useMemo(() => {
    const total = records.length;

    const successful = records.filter(
      (record) =>
        record.statusCode >= 200 &&
        record.statusCode < 300
    ).length;

    const errors = records.filter(
      (record) => record.statusCode >= 400
    ).length;

    const averageDuration =
      total === 0
        ? 0
        : records.reduce(
            (sum, record) => sum + record.durationMs,
            0
          ) / total;

    return {
      total,
      successful,
      errors,
      averageDuration,
      successRate: total === 0 ? 0 : (successful / total) * 100,
    };
  }, [records]);

  const filteredRecords = useMemo(() => {
    const query = search.trim().toLowerCase();

    if (!query) {
      return records;
    }

    return records.filter(
      (record) =>
        record.path.toLowerCase().includes(query) ||
        record.method.toLowerCase().includes(query) ||
        String(record.statusCode).includes(query) ||
        (record.externalCalls ?? []).some((call) =>
          call.url.toLowerCase().includes(query)
        )
    );
  }, [records, search]);

  function navigateTo(page: Page) {
    setCurrentPage(page);
    setSelectedRecord(null);
  }

  const currentNavItem = NAV_ITEMS.find(
    (item) => item.page === currentPage
  );

  const apiStatus = error
    ? { tone: "danger", label: "API unreachable" }
    : loading && !lastUpdated
      ? { tone: "neutral", label: "Connecting…" }
      : { tone: "success", label: "API connected" };

  const refreshButton = (
    <button
      type="button"
      className="button button-secondary"
      onClick={loadRecords}
      disabled={loading}
    >
      <Icon name="refresh" size={16} />
      {loading ? "Refreshing…" : "Refresh"}
    </button>
  );

  const errorAlert = error && (
    <div className="alert" role="alert">
      <Icon name="alert" size={18} />
      {error}
    </div>
  );

  return (
    <div className="app-shell">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-mark" aria-hidden="true">
            <Icon name="activity" size={18} />
          </span>

          <div>
            <strong className="brand-name">TraceLite</strong>
            <small className="brand-tagline">
              API Performance Analyzer
            </small>
          </div>
        </div>

        <nav className="sidebar-nav" aria-label="Main">
          <p className="sidebar-section">Monitoring</p>

          {NAV_ITEMS.map((item) => (
            <button
              key={item.page}
              type="button"
              className="sidebar-link"
              aria-current={
                currentPage === item.page ? "page" : undefined
              }
              aria-label={item.label}
              title={item.label}
              onClick={() => navigateTo(item.page)}
            >
              <Icon name={item.icon} size={20} />
              <span className="nav-label">{item.label}</span>
              <span className="nav-label-short" aria-hidden="true">
                {item.shortLabel}
              </span>
            </button>
          ))}
        </nav>

        <div className="sidebar-footer" title={apiStatus.label}>
          <span
            className={`status-dot status-dot-${apiStatus.tone}`}
            aria-hidden="true"
          />
          <span className="sidebar-footer-label">
            {apiStatus.label}
          </span>
        </div>
      </aside>

      <div className="main-area">
        {/* Top bar */}
        <header className="topbar">
          <span className="brand-mark topbar-brand" aria-hidden="true">
            <Icon name="activity" size={16} />
          </span>

          <nav className="breadcrumb" aria-label="Breadcrumb">
            <span className="breadcrumb-root">TraceLite</span>
            <span className="breadcrumb-separator breadcrumb-root">/</span>

            {selectedRecord ? (
              <>
                <button
                  type="button"
                  className="breadcrumb-link"
                  onClick={() => setSelectedRecord(null)}
                >
                  {currentNavItem?.label}
                </button>
                <span className="breadcrumb-separator">/</span>
                <span className="breadcrumb-current">
                  Request #{selectedRecord.id}
                </span>
              </>
            ) : (
              <span className="breadcrumb-current">
                {currentNavItem?.label}
              </span>
            )}
          </nav>

          <span
            className={`status-dot status-dot-${apiStatus.tone} topbar-status`}
            role="img"
            aria-label={apiStatus.label}
            title={apiStatus.label}
          />

          {lastUpdated && (
            <span className="topbar-meta">
              <Icon name="clock" size={14} />
              Updated {lastUpdated.toLocaleTimeString()}
            </span>
          )}
        </header>

        {/* Page content */}
        <main className="app-content">
          <div className="page">
            {selectedRecord !== null ? (
              <RequestDetails
                record={selectedRecord}
                onBack={() => setSelectedRecord(null)}
              />
            ) : (
              <>
                {/* Dashboard Page */}
                {currentPage === "dashboard" && (
                  <>
                    <PageHeader
                      title="Dashboard"
                      description="Monitor API request performance and SQL activity."
                      actions={refreshButton}
                    />

                    {errorAlert}

                    <section className="metrics-grid">
                      <MetricCard
                        title="Total requests"
                        value={stats.total}
                        subtitle="Captured by TraceLite"
                        icon="activity"
                        accent="blue"
                      />

                      <MetricCard
                        title="Average duration"
                        value={formatDuration(stats.averageDuration)}
                        subtitle="Across all requests"
                        icon="clock"
                        accent="violet"
                      />

                      <MetricCard
                        title="Success rate"
                        value={`${stats.successRate.toFixed(1)}%`}
                        subtitle={`${stats.successful} of ${stats.total} returned 2xx`}
                        icon="check"
                        accent="green"
                      />

                      <MetricCard
                        title="Errors"
                        value={stats.errors}
                        subtitle="4xx and 5xx responses"
                        icon="alert"
                        accent="red"
                        tone={stats.errors > 0 ? "danger" : "default"}
                      />
                    </section>

                    <section className="panel">
                      <div className="panel-header">
                        <div>
                          <h2 className="panel-title">
                            Recent requests
                            <span className="count-pill">
                              {filteredRecords.length}
                            </span>
                          </h2>

                          <p className="panel-subtitle">
                            Select a request to view its details and SQL queries.
                          </p>
                        </div>

                        <div className="search-field">
                          <Icon name="search" size={16} />

                          <input
                            type="search"
                            className="input search-input"
                            placeholder="Filter by path, endpoint, method or status"
                            aria-label="Filter requests"
                            value={search}
                            onChange={(event) =>
                              setSearch(event.target.value)
                            }
                          />
                        </div>
                      </div>

                      {loading ? (
                        <p className="state-message">
                          Loading performance data…
                        </p>
                      ) : (
                        <RequestTable
                          records={filteredRecords}
                          onSelect={setSelectedRecord}
                          emptyMessage={
                            search.trim()
                              ? "No requests match your filter."
                              : "No performance records yet."
                          }
                        />
                      )}
                    </section>
                  </>
                )}

                {/* External APIs Page */}
                {currentPage === "external-apis" && (
                  <ExternalApis onSelectRequest={setSelectedRecord} />
                )}

                {/* API Tester Page */}
                {currentPage === "api-tester" && (
                  <>
                    <PageHeader
                      title="API Tester"
                      description="Send a request to any API and measure its response time."
                    />

                    <ApiTester />
                  </>
                )}

                {/* Performance Analyzer Page */}
                {currentPage === "performance-analyzer" && (
                  <>
                    <PageHeader
                      title="Performance Analyzer"
                      description="Analyze request durations and HTTP status patterns."
                      actions={refreshButton}
                    />

                    {errorAlert}

                    <PerformanceCharts records={records} />
                  </>
                )}
              </>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}

export default App;
