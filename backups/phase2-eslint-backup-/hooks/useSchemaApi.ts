import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { schemaApiClient } from '../services/schema-api-client';
import {
  SchemaListParams,
  CreateSchemaRequest,
  UpdateSchemaRequest,
  DuplicateSchemaRequest,
  ValidateJsonSchemaRequest,
  TestRegexRequest
} from '../types/schema-api';

// Query Keys
export const SCHEMA_QUERY_KEYS = {
  all: ['schemas'] as const,
  lists: () => [...SCHEMA_QUERY_KEYS.all, 'list'] as const,
  list: (params: SchemaListParams) => [...SCHEMA_QUERY_KEYS.lists(), params] as const,
  details: () => [...SCHEMA_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...SCHEMA_QUERY_KEYS.details(), id] as const,
  templates: () => [...SCHEMA_QUERY_KEYS.all, 'templates'] as const,
  usage: (id: string) => [...SCHEMA_QUERY_KEYS.all, 'usage', id] as const,
};

// Hook for getting schemas list
export function useSchemas(params: SchemaListParams = {}) {
  return useQuery({
    queryKey: SCHEMA_QUERY_KEYS.list(params),
    queryFn: () => schemaApiClient.getSchemas(params),
  });
}

// Hook for getting single schema
export function useSchema(id: string | undefined) {
  return useQuery({
    queryKey: SCHEMA_QUERY_KEYS.detail(id || ''),
    queryFn: () => schemaApiClient.getSchema(id!),
    enabled: !!id,
  });
}

// Hook for getting templates
export function useSchemaTemplates() {
  return useQuery({
    queryKey: SCHEMA_QUERY_KEYS.templates(),
    queryFn: () => schemaApiClient.getTemplates(),
  });
}

// Hook for getting usage statistics
export function useSchemaUsageStatistics(id: string | undefined) {
  return useQuery({
    queryKey: SCHEMA_QUERY_KEYS.usage(id || ''),
    queryFn: () => schemaApiClient.getUsageStatistics(id!),
    enabled: !!id,
  });
}

// Hook for creating schema
export function useCreateSchema() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateSchemaRequest) => schemaApiClient.createSchema(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.lists() });
      message.success('Schema נוצר בהצלחה');
    },
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה ביצירת Schema');
    },
  });
}

// Hook for updating schema
export function useUpdateSchema() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateSchemaRequest }) => 
      schemaApiClient.updateSchema(id, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.detail(data.id) });
      message.success('Schema עודכן בהצלחה');
    },
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה בעדכון Schema');
    },
  });
}

// Hook for deleting schema
export function useDeleteSchema() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, deletedBy }: { id: string; deletedBy?: string }) => 
      schemaApiClient.deleteSchema(id, deletedBy),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.lists() });
      message.success('Schema נמחק בהצלחה');
    },
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה במחיקת Schema');
    },
  });
}

// Hook for publishing schema
export function usePublishSchema() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => schemaApiClient.publishSchema(id),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.detail(data.id) });
      message.success('Schema פורסם בהצלחה');
    },
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה בפרסום Schema');
    },
  });
}

// Hook for duplicating schema
export function useDuplicateSchema() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: DuplicateSchemaRequest }) => 
      schemaApiClient.duplicateSchema(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SCHEMA_QUERY_KEYS.lists() });
      message.success('Schema שוכפל בהצלחה');
    },
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה בשכפול Schema');
    },
  });
}

// Hook for validating sample data
export function useValidateSampleData() {
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => 
      schemaApiClient.validateSampleData(id, data),
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה באימות נתונים');
    },
  });
}

// Hook for validating JSON Schema
export function useValidateJsonSchema() {
  return useMutation({
    mutationFn: (request: ValidateJsonSchemaRequest) => 
      schemaApiClient.validateJsonSchema(request),
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה באימות JSON Schema');
    },
  });
}

// Hook for testing regex
export function useTestRegex() {
  return useMutation({
    mutationFn: (request: TestRegexRequest) => schemaApiClient.testRegex(request),
    onError: (error: Error) => {
      message.error(error.message || 'שגיאה בבדיקת תבנית Regex');
    },
  });
}
