/**
 * Real-time Schema Validation Hook
 * Validates schema as it changes with debouncing
 */

import { useState, useEffect, useCallback } from 'react';
import { type JSONSchema } from 'jsonjoy-builder';
import { validateSchemaDefinition, ValidationResult } from '../utils/schemaValidator';

interface UseRealtimeSchemaValidationOptions {
  debounceMs?: number;
  enabled?: boolean;
}

interface RealtimeValidationResult {
  isValidating: boolean;
  validationResult: ValidationResult | null;
  lastValidated: Date | null;
}

export function useRealtimeSchemaValidation(
  schema: JSONSchema,
  options: UseRealtimeSchemaValidationOptions = {}
): RealtimeValidationResult {
  const { debounceMs = 500, enabled = true } = options;
  
  const [isValidating, setIsValidating] = useState(false);
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);
  const [lastValidated, setLastValidated] = useState<Date | null>(null);

  const validateSchema = useCallback(() => {
    if (!enabled) return;
    
    setIsValidating(true);
    
    try {
      const result = validateSchemaDefinition(schema);
      setValidationResult(result);
      setLastValidated(new Date());
    } catch (error) {
      setValidationResult({
        valid: false,
        errors: [{
          field: 'schema',
          message: 'Validation error',
          messageHebrew: 'שגיאה באימות'
        }],
        errorCount: 1
      });
    } finally {
      setIsValidating(false);
    }
  }, [schema, enabled]);

  useEffect(() => {
    if (!enabled) return;

    const timeoutId = setTimeout(() => {
      validateSchema();
    }, debounceMs);

    return () => clearTimeout(timeoutId);
  }, [schema, debounceMs, enabled, validateSchema]);

  return {
    isValidating,
    validationResult,
    lastValidated
  };
}
