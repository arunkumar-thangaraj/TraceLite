import axios from "axios";
import type { ExternalApiTestRequest, ExternalApiTestResponse, ExternalCall, HttpPerformanceRecord, PerformanceRecord, SqlPerformanceRecord } from "../types/performance";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export async function getPerformanceRecords(): Promise<
  PerformanceRecord[]
> {
  const response = await apiClient.get<PerformanceRecord[]>(
    "/api/Performance"
  );

  return response.data;
}

export async function getSqlForRequest(
    requestId: number
  ): Promise<SqlPerformanceRecord[]> {
    const response = await apiClient.get<SqlPerformanceRecord[]>(
      `/api/performance/${requestId}/sql`
    );
  
    return response.data;
  }


  export async function getHttpForRequest(
    requestId: number
  ): Promise<HttpPerformanceRecord[]> {
    const response = await apiClient.get<HttpPerformanceRecord[]>(
      `/api/performance/${requestId}/http`
    );

    return response.data;
  }

  export async function getExternalCalls(): Promise<ExternalCall[]> {
    const response = await apiClient.get<ExternalCall[]>(
      "/api/performance/http"
    );

    return response.data;
  }

  export async function testExternalApi(
    request: ExternalApiTestRequest
  ): Promise<ExternalApiTestResponse> {
    const response = await apiClient.post<ExternalApiTestResponse>(
      "/api/external/test",
      request
    );
  
    return response.data;
  }