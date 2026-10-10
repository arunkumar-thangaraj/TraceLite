export interface PerformanceRecord {
  id: number;
  traceId: string;
  method: string;
  path: string;
  statusCode: number;
  durationMs: number;
  createdAtUtc: string;
  /* Outbound calls made by this request (first few only, in list views) */
  externalCallCount?: number;
  externalCalls?: ExternalCallSummary[];
}

export interface ExternalCallSummary {
  method: string;
  url: string;
  statusCode: number;
  errorType?: string | null;
}

export interface SqlPerformanceRecord {
  id: number;
  traceId: string;
  commandType: string;
  commandText: string;
  durationMs: number;
  createdAtUtc: string;
  errorType?: string | null;
  errorMessage?: string | null;
}

/* An outbound HttpClient call made while handling a request */
export interface HttpPerformanceRecord {
  id: number;
  traceId: string;
  method: string;
  url: string;
  /* 0 when no response was received; see errorType */
  statusCode: number;
  durationMs: number;
  createdAtUtc: string;
  errorType?: string | null;
  errorMessage?: string | null;
}

/* An outbound call together with the inbound request that made it */
export interface ExternalCall extends HttpPerformanceRecord {
  requestId: number;
  requestMethod: string;
  requestPath: string;
  requestStatusCode: number;
  requestDurationMs: number;
  requestCreatedAtUtc: string;
}

export interface ExternalApiTestRequest {
  method: string;
  url: string;
  headers?: Record<string, string>;
  body?: string;
}

export interface ExternalApiTestResponse {
  /* 0 when no response was received; see errorType */
  statusCode: number;
  reasonPhrase: string;
  durationMs: number;
  timeToFirstByteMs?: number | null;
  responseBody: string;
  responseBodyTruncated: boolean;
  responseBytes: number;
  contentType?: string | null;
  httpVersion?: string | null;
  finalUrl?: string | null;
  headers: Record<string, string>;
  errorType?: string | null;
  errorMessage?: string | null;
}
