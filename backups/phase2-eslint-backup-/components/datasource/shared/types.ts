// TypeScript interfaces for DataSource forms and details

export interface DataSource {
  ID: string;
  Name: string;
  SupplierName: string;
  Category: string;
  Description?: string;
  IsActive: boolean;
  FilePath: string;
  FilePattern: string;
  AdditionalConfiguration?: {
    ConfigurationSettings?: string;
    ValidationRules?: string;
    Metadata?: string;
    RetentionDays?: number;
  };
  CreatedAt: string;
  UpdatedAt: string;
  LastProcessedAt?: string;
  TotalFilesProcessed: number;
  TotalErrorRecords: number;
  SchemaVersion: number;
  PollingRate: string;
  JsonSchema: any;
}

export interface ApiResponse<T> {
  CorrelationId: string;
  Data: T;
  Error: any;
  IsSuccess: boolean;
}

export interface ConnectionConfig {
  type: 'SFTP' | 'FTP' | 'HTTP' | 'Local' | 'Kafka';
  host?: string;
  port?: number;
  username?: string;
  password?: string;
  path?: string;
  url?: string;
  // Kafka-specific fields
  brokers?: string;
  topic?: string;
  consumerGroup?: string;
  securityProtocol?: string;
  offsetReset?: string;
}

export interface FileConfig {
  type: 'CSV' | 'Excel' | 'JSON' | 'XML';
  delimiter?: string;
  hasHeaders?: boolean;
  sheetName?: string;
  encoding?: string;
}

export interface ScheduleConfig {
  frequency: string;
  cronExpression?: string;
  enabled: boolean;
}

export interface ValidationRules {
  skipInvalidRecords: boolean;
  maxErrorsAllowed?: number;
}

export interface NotificationSettings {
  onSuccess: boolean;
  onFailure: boolean;
  recipients: string[];
}

export interface ParsedConfig {
  connectionConfig?: ConnectionConfig;
  fileConfig?: FileConfig;
  schedule?: ScheduleConfig;
  validationRules?: ValidationRules;
  notificationSettings?: NotificationSettings;
}
