import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  PieChart,
  Pie,
  Cell,
} from "recharts";

import type { PerformanceRecord } from "../types/performance";
import {
  STATUS_TONE_COLORS,
  formatDuration,
  getStatusTone,
  percentile,
} from "../utils/format";
import MetricCard from "./MetricCard";

interface PerformanceChartsProps {
  records: PerformanceRecord[];
}

/* =========================================================
   CHART STYLE
   ========================================================= */

const AXIS_TICK = { fill: "#64748b", fontSize: 12 };

const GRID_COLOR = "#e2e8f0";

/* Bars stay neutral blue; only errors get a warning color */
const BAR_COLOR = "#3b82f6";

const STATUS_GROUP_LABELS: Record<string, string> = {
  "1xx": "Informational",
  "2xx": "Success",
  "3xx": "Redirect",
  "4xx": "Client error",
  "5xx": "Server error",
};

function getBarColor(statusCode: number): string {
  const tone = getStatusTone(statusCode);

  return tone === "warning" || tone === "danger"
    ? STATUS_TONE_COLORS[tone]
    : BAR_COLOR;
}

interface DurationPoint {
  name: string;
  duration: number;
  path: string;
  statusCode: number;
  color: string;
}

interface DurationTooltipProps {
  active?: boolean;
  payload?: ReadonlyArray<{ payload?: DurationPoint }>;
}

function DurationTooltip({ active, payload }: DurationTooltipProps) {
  const point = payload?.[0]?.payload;

  if (!active || !point) {
    return null;
  }

  return (
    <div className="chart-tooltip">
      <p className="chart-tooltip-title">{point.path}</p>

      <p className="chart-tooltip-row">
        Request <strong>{point.name}</strong>
      </p>

      <p className="chart-tooltip-row">
        Status <strong>{point.statusCode}</strong>
      </p>

      <p className="chart-tooltip-row">
        Duration <strong>{formatDuration(point.duration)}</strong>
      </p>
    </div>
  );
}

export default function PerformanceCharts({
  records,
}: PerformanceChartsProps) {
  /* =========================================================
     REQUEST DURATION DATA
     ========================================================= */

  const chartRecords: DurationPoint[] = [...records]
    .reverse()
    .slice(-10)
    .map((record) => ({
      name: `#${record.id}`,
      duration: record.durationMs,
      path: record.path,
      statusCode: record.statusCode,
      color: getBarColor(record.statusCode),
    }));

  const hasClientErrors = chartRecords.some(
    (point) => getStatusTone(point.statusCode) === "warning"
  );

  const hasServerErrors = chartRecords.some(
    (point) => getStatusTone(point.statusCode) === "danger"
  );

  /* =========================================================
     STATUS DATA
     ========================================================= */

  const statusCounts = records.reduce<Record<string, number>>(
    (counts, record) => {
      const statusGroup = `${Math.floor(
        record.statusCode / 100
      )}xx`;

      counts[statusGroup] =
        (counts[statusGroup] ?? 0) + 1;

      return counts;
    },
    {}
  );

  const statusData = Object.entries(statusCounts)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([name, value]) => ({
      name,
      value,
      label: STATUS_GROUP_LABELS[name] ?? "Other",
      percent: (value / records.length) * 100,
      color:
        STATUS_TONE_COLORS[
          getStatusTone(Number(name[0]) * 100)
        ],
    }));

  /* =========================================================
     PERFORMANCE SUMMARY
     ========================================================= */

  const durations = records.map((record) => record.durationMs);

  const averageDuration =
    durations.length > 0
      ? durations.reduce((total, value) => total + value, 0) /
        durations.length
      : 0;

  const fastestRequest =
    durations.length > 0 ? Math.min(...durations) : 0;

  const slowestRequest =
    durations.length > 0 ? Math.max(...durations) : 0;

  /* =========================================================
     RENDER
     ========================================================= */

  return (
    <>
      <section className="metrics-grid">
        <MetricCard
          title="Average"
          value={formatDuration(averageDuration)}
          subtitle={`Across ${records.length} requests`}
          icon="clock"
          accent="blue"
        />

        <MetricCard
          title="95th percentile"
          value={formatDuration(percentile(durations, 95))}
          subtitle="95% of requests are faster"
          icon="gauge"
          accent="violet"
        />

        <MetricCard
          title="Fastest"
          value={formatDuration(fastestRequest)}
          subtitle="Quickest response"
          icon="zap"
          accent="green"
        />

        <MetricCard
          title="Slowest"
          value={formatDuration(slowestRequest)}
          subtitle="Longest response"
          icon="hourglass"
          accent="amber"
        />
      </section>

      <div className="charts-grid">
        {/* ===================================================
            REQUEST DURATION
            =================================================== */}

        <section className="panel">
          <div className="panel-header">
            <div>
              <h2 className="panel-title">Request duration</h2>
              <p className="panel-subtitle">
                Last {chartRecords.length} requests
              </p>
            </div>

            {chartRecords.length > 0 && (
              <ul className="chart-legend">
                <li>
                  <span
                    className="legend-swatch"
                    style={{ background: BAR_COLOR }}
                  />
                  Duration
                </li>

                {hasClientErrors && (
                  <li>
                    <span
                      className="legend-swatch"
                      style={{ background: STATUS_TONE_COLORS.warning }}
                    />
                    4xx
                  </li>
                )}

                {hasServerErrors && (
                  <li>
                    <span
                      className="legend-swatch"
                      style={{ background: STATUS_TONE_COLORS.danger }}
                    />
                    5xx
                  </li>
                )}

                <li>
                  <span className="legend-line" />
                  Average
                </li>
              </ul>
            )}
          </div>

          {chartRecords.length === 0 ? (
            <p className="state-message">
              No request data available.
            </p>
          ) : (
            <div className="chart-body chart-canvas">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart
                  data={chartRecords}
                  margin={{ top: 16, right: 8, left: 0, bottom: 0 }}
                  accessibilityLayer={false}
                >
                  <CartesianGrid
                    stroke={GRID_COLOR}
                    strokeDasharray="4 4"
                    vertical={false}
                  />

                  <XAxis
                    dataKey="name"
                    tick={AXIS_TICK}
                    axisLine={false}
                    tickLine={false}
                    tickMargin={8}
                  />

                  <YAxis
                    tick={AXIS_TICK}
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={(value: number) =>
                      formatDuration(value).replace(" ", "")
                    }
                    width={56}
                  />

                  <Tooltip
                    cursor={{ fill: "#f1f5f9", radius: 6 }}
                    content={<DurationTooltip />}
                  />

                  <ReferenceLine
                    y={averageDuration}
                    stroke="#94a3b8"
                    strokeDasharray="5 5"
                    ifOverflow="extendDomain"
                  />

                  <Bar
                    dataKey="duration"
                    radius={[6, 6, 0, 0]}
                    maxBarSize={36}
                  >
                    {chartRecords.map((entry) => (
                      <Cell
                        key={entry.name}
                        fill={entry.color}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </section>

        {/* ===================================================
            STATUS DISTRIBUTION
            =================================================== */}

        <section className="panel">
          <div className="panel-header">
            <div>
              <h2 className="panel-title">Status distribution</h2>
              <p className="panel-subtitle">
                All requests by status class
              </p>
            </div>
          </div>

          {statusData.length === 0 ? (
            <p className="state-message">
              No status data available.
            </p>
          ) : (
            <div className="donut-layout">
              <div className="donut-chart">
                <PieChart width={180} height={180}>
                  <Pie
                    data={statusData}
                    dataKey="value"
                    nameKey="name"
                    cx="50%"
                    cy="50%"
                    innerRadius={66}
                    outerRadius={88}
                    startAngle={90}
                    endAngle={-270}
                    paddingAngle={statusData.length > 1 ? 3 : 0}
                    cornerRadius={statusData.length > 1 ? 4 : 0}
                    stroke="none"
                    rootTabIndex={-1}
                  >
                    {statusData.map((entry) => (
                      <Cell
                        key={entry.name}
                        fill={entry.color}
                      />
                    ))}
                  </Pie>
                </PieChart>

                <div className="donut-center">
                  <span className="donut-total">
                    {records.length}
                  </span>
                  <span className="donut-caption">requests</span>
                </div>
              </div>

              <ul className="status-list">
                {statusData.map((entry) => (
                  <li key={entry.name}>
                    <span
                      className="legend-swatch"
                      style={{ background: entry.color }}
                    />

                    <span className="status-name">
                      <strong>{entry.name}</strong>
                      <span>{entry.label}</span>
                    </span>

                    <span className="status-count">
                      {entry.value}
                      <span>{entry.percent.toFixed(0)}%</span>
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>
      </div>
    </>
  );
}
