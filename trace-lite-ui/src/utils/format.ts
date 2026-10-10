export type StatusTone =
  | "success"
  | "info"
  | "warning"
  | "danger"
  | "neutral";

/* Chart fills — mid-tone versions of the badge colors in index.css */
export const STATUS_TONE_COLORS: Record<StatusTone, string> = {
  success: "#12b76a",
  info: "#2e90fa",
  warning: "#f79009",
  danger: "#f04438",
  neutral: "#98a2b3",
};

export function getStatusTone(statusCode: number): StatusTone {
  if (statusCode >= 500) return "danger";
  if (statusCode >= 400) return "warning";
  if (statusCode >= 300) return "info";
  if (statusCode >= 200) return "success";
  return "neutral";
}

export function formatDuration(ms: number): string {
  if (ms >= 1000) {
    return `${(ms / 1000).toFixed(2)} s`;
  }

  return `${Math.round(ms)} ms`;
}

/* "https://api.example.com/v1/items?id=5" → "api.example.com/v1/items?id=5" */
export function formatEndpoint(url: string): string {
  try {
    const parsed = new URL(url);
    return `${parsed.host}${parsed.pathname}${parsed.search}`;
  } catch {
    return url;
  }
}

export function getHost(url: string): string {
  try {
    return new URL(url).host;
  } catch {
    return url;
  }
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}

const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

export function formatDateTime(value: string): string {
  return dateTimeFormatter.format(new Date(value));
}

export function percentile(values: number[], p: number): number {
  if (values.length === 0) {
    return 0;
  }

  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.ceil((p / 100) * sorted.length) - 1;

  return sorted[Math.min(Math.max(index, 0), sorted.length - 1)];
}
