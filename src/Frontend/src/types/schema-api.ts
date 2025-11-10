// Schema API Types - Frontend interfaces matching backend DTOs

export interface SchemaListParams {
  page?: number;
  size?: number;
  search?: string;
  status?: SchemaStatus;
  dataSourceId?: string;
}

export interface SchemaListResponse {
  isSuccess: boolean;
  data: Schema[];
  total: number;
  page: number;
  size: number;
  totalPages: number;
}

export interface Schema {
  id: string;
  name: string;
  displayName: string;
  description: string;
  dataSourceId?: string;
  version: number;
  status: SchemaStatus;
  jsonSchemaContent: string;
  fields: SchemaField[];
  tags: string[];
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  usageCount: number;
  createdBy: string;
  updatedBy?: string;
}

export interface SchemaField {
  name: string;
  displayName: string;
  type: string;
  description: string;
  isRequired: boolean;
  validation?: FieldValidation;
}

export interface FieldValidation {
  minLength?: number;
  maxLength?: number;
  minimum?: number;
  maximum?: number;
  pattern?: string;
  format?: string;
  enum?: string[];
}

export enum SchemaStatus {
  Draft = 'Draft',
  Active = 'Active',
  Inactive = 'Inactive',
  Archived = 'Archived'
}

export interface CreateSchemaRequest {
  name: string;
  displayName: string;
  description: string;
  dataSourceId?: string;
  jsonSchemaContent: string;
  tags: string[];
  status: SchemaStatus;
  version: number;
  createdBy: string;
}

export interface UpdateSchemaRequest {
  displayName: string;
  description: string;
  dataSourceId?: string;
  jsonSchemaContent: string;
  tags: string[];
  status: SchemaStatus;
  updatedBy: string;
  incrementVersion?: boolean;
}

export interface DuplicateSchemaRequest {
  name: string;
  displayName: string;
  description: string;
  createdBy: string;
  copyTags: boolean;
  additionalTags: string[];
}

export interface ValidateJsonSchemaRequest {
  jsonSchemaContent: string;
  schemaVersion?: string;
  strictMode: boolean;
}

export interface TestRegexRequest {
  pattern: string;
  testStrings: string[];
  options: RegexTestOptions;
  description?: string;
}

export interface RegexTestOptions {
  ignoreCase: boolean;
  multiline: boolean;
  singleline: boolean;
  timeoutMs: number;
}

export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
  warnings: string[];
  message?: string;
}

export interface ValidationError {
  message: string;
  messageEnglish: string;
  path?: string;
  lineNumber?: number;
  columnNumber?: number;
  errorCode?: string;
}

export interface DataValidationResult {
  isValid: boolean;
  errors: DataValidationError[];
  warnings: string[];
  message?: string;
  schemaId?: string;
  schemaName?: string;
  validatedAt: string;
  recordsValidated: number;
  validRecords: number;
  invalidRecords: number;
}

export interface DataValidationError {
  message: string;
  messageEnglish: string;
  fieldPath?: string;
  fieldName?: string;
  actualValue?: any;
  expectedConstraint?: string;
  ruleName?: string;
  recordIndex?: number;
}

export interface RegexTestResult {
  isValidPattern: boolean;
  pattern: string;
  testResults: RegexStringTestResult[];
  patternError?: string;
  patternErrorEnglish?: string;
  summary: RegexTestSummary;
}

export interface RegexStringTestResult {
  testString: string;
  isMatch: boolean;
  matchedGroups: string[];
  matchedText?: string;
  matchPosition?: number;
  matchLength?: number;
}

export interface RegexTestSummary {
  totalTests: number;
  matchCount: number;
  noMatchCount: number;
  successRate: number;
}

export interface SchemaTemplate {
  id: string;
  name: string;
  nameHebrew: string;
  description: string;
  descriptionHebrew: string;
  category: string;
  fieldsCount: number;
  complexity: string;
  jsonSchema: object;
  sampleData: object[];
  documentation: string;
  tags: string[];
  createdAt: string;
  version: string;
  isBuiltIn: boolean;
  usageCount: number;
  rating: number;
  author: string;
}

export interface SchemaUsageStatistics {
  schemaId: string;
  schemaName: string;
  displayName: string;
  currentUsageCount: number;
  dataSources: DataSourceUsage[];
  totalRecordsProcessed: number;
  totalValidationAttempts: number;
  successfulValidations: number;
  failedValidations: number;
  successRate: number;
  averageValidationTimeMs: number;
  createdAt: string;
  lastValidationAt?: string;
  collectedAt: string;
  usageTrend: UsageTrendPoint[];
}

export interface DataSourceUsage {
  dataSourceId: string;
  dataSourceName: string;
  recordsProcessed: number;
  lastActivityAt?: string;
  isActive: boolean;
}

export interface UsageTrendPoint {
  date: string;
  validationCount: number;
  recordsCount: number;
  successRate: number;
}

export interface SchemaResponse {
  isSuccess: boolean;
  data: Schema;
}

export interface ApiError {
  message: string;
  messageEnglish: string;
  validationErrors?: ValidationError[];
  usageCount?: number;
}

export interface ApiResponse<T> {
  isSuccess: boolean;
  data?: T;
  error?: ApiError;
}
