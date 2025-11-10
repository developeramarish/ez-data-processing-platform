// Metrics Configuration API Client
// Connects to MetricsConfigurationService on port 5002

const BASE_URL = 'http://localhost:5002/api/v1/metrics';

export interface MetricConfiguration {
  id: string;
  name: string;
  displayName: string;
  description: string;
  category: string;
  scope: 'global' | 'datasource-specific';
  dataSourceId: string | null;
  dataSourceName?: string;
  prometheusType?: string; // counter, gauge, histogram, summary
  formula: string;
  fieldPath?: string;
  labels?: string[];
  retention?: string;
  status: number; // 0=Draft, 1=Active, 2=Inactive, 3=Error
  lastValue?: number;
  lastCalculated?: string;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface CreateMetricRequest {
  name: string;
  displayName: string;
  description: string;
  category: string;
  scope: string;
  dataSourceId?: string | null;
  dataSourceName?: string | null;
  prometheusType?: string;
  formula: string;
  fieldPath?: string | null;
  labels?: string[];
  retention?: string | null;
  status: number;
  createdBy: string;
}

export interface UpdateMetricRequest {
  displayName: string;
  description: string;
  category: string;
  scope: string;
  dataSourceId?: string | null;
  dataSourceName?: string | null;
  prometheusType?: string;
  formula: string;
  formulaType?: number; // 0=Simple, 1=PromQL, 2=Recording
  fieldPath?: string | null;
  labelNames?: string | null;
  labelsExpression?: string | null;
  labels?: string[];
  alertRules?: any[]; // AlertRule[]
  retention?: string | null;
  status: number;
  updatedBy: string;
}

export interface DuplicateMetricRequest {
  name: string;
  displayName: string;
  description: string;
  createdBy: string;
}

export interface ApiResponse<T> {
  isSuccess: boolean;
  data?: T;
  message?: string;
  error?: {
    message: string;
  };
}

class MetricsApiClient {
  async getAll(): Promise<MetricConfiguration[]> {
    const response = await fetch(BASE_URL);
    const result: ApiResponse<MetricConfiguration[]> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to fetch metrics');
    }
    
    return result.data || [];
  }

  async getById(id: string): Promise<MetricConfiguration> {
    const response = await fetch(`${BASE_URL}/${id}`);
    const result: ApiResponse<MetricConfiguration> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to fetch metric');
    }
    
    return result.data!;
  }

  async create(request: CreateMetricRequest): Promise<MetricConfiguration> {
    const response = await fetch(BASE_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify(request),
    });
    
    const result: ApiResponse<MetricConfiguration> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to create metric');
    }
    
    return result.data!;
  }

  async update(id: string, request: UpdateMetricRequest): Promise<MetricConfiguration> {
    const response = await fetch(`${BASE_URL}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify(request),
    });
    
    const result: ApiResponse<MetricConfiguration> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to update metric');
    }
    
    return result.data!;
  }

  async delete(id: string): Promise<void> {
    const response = await fetch(`${BASE_URL}/${id}`, {
      method: 'DELETE',
    });
    
    const result: ApiResponse<void> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to delete metric');
    }
  }

  async duplicate(id: string, request: DuplicateMetricRequest): Promise<MetricConfiguration> {
    const response = await fetch(`${BASE_URL}/${id}/duplicate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify(request),
    });
    
    const result: ApiResponse<MetricConfiguration> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to duplicate metric');
    }
    
    return result.data!;
  }

  async getByDataSource(dataSourceId: string): Promise<MetricConfiguration[]> {
    const response = await fetch(`${BASE_URL}/datasource/${dataSourceId}`);
    const result: ApiResponse<MetricConfiguration[]> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to fetch metrics for data source');
    }
    
    return result.data || [];
  }

  async getGlobal(): Promise<MetricConfiguration[]> {
    const response = await fetch(`${BASE_URL}/global`);
    const result: ApiResponse<MetricConfiguration[]> = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.error?.message || 'Failed to fetch global metrics');
    }
    
    return result.data || [];
  }
}

export const metricsApi = new MetricsApiClient();
export default metricsApi;
