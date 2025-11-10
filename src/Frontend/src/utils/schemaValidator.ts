/**
 * JSON Schema Validator with AJV
 * Validates JSON data against JSON Schema Draft 2020-12 definitions
 */

import Ajv, { ErrorObject } from 'ajv';
import { type JSONSchema } from 'jsonjoy-builder';

/**
 * Fix RTL-corrupted regex patterns before validation
 * RTL text direction can reverse patterns like ^[0-9]{4}$ to ${4}[0-9]^
 */
function unreverseRTLPattern(pattern: string): string {
  if (!pattern) return pattern;
  
  const looksReversed = 
    (pattern.endsWith('^') && !pattern.startsWith('^')) ||
    (pattern.startsWith('$') && !pattern.endsWith('$')) ||
    pattern.includes('}{') ||
    (pattern.startsWith('}') || pattern.startsWith(']')) ||
    pattern.match(/\]-\d-\d\[/) ||
    pattern.endsWith('[');
  
  if (looksReversed) {
    return pattern.split('').reverse().join('');
  }
  
  return pattern;
}

/**
 * Recursively fix RTL-corrupted patterns in schema
 */
function fixRTLPatterns(schema: any): any {
  if (!schema || typeof schema !== 'object') return schema;
  
  const fixed = { ...schema };
  
  // Fix pattern in current level
  if (fixed.pattern && typeof fixed.pattern === 'string') {
    fixed.pattern = unreverseRTLPattern(fixed.pattern);
  }
  
  // Recursively fix nested schemas
  if (fixed.properties) {
    fixed.properties = Object.keys(fixed.properties).reduce((acc, key) => {
      acc[key] = fixRTLPatterns(fixed.properties[key]);
      return acc;
    }, {} as any);
  }
  
  if (fixed.items) {
    fixed.items = fixRTLPatterns(fixed.items);
  }
  
  if (fixed.allOf) {
    fixed.allOf = fixed.allOf.map(fixRTLPatterns);
  }
  
  if (fixed.anyOf) {
    fixed.anyOf = fixed.anyOf.map(fixRTLPatterns);
  }
  
  if (fixed.oneOf) {
    fixed.oneOf = fixed.oneOf.map(fixRTLPatterns);
  }
  
  return fixed;
}

// Create AJV instance with Draft 2020-12 support
const ajv = new Ajv({
  allErrors: true, // Collect all errors
  verbose: true, // Include schema and data in errors
  strict: false, // Don't be too strict about schema keywords
  validateFormats: false, // Disable format validation to avoid dependency issues
  strictSchema: false, // Allow unknown keywords
  validateSchema: false, // Don't validate the schema itself
  allowUnionTypes: true // Allow union types
});

/**
 * Validation error with Hebrew support
 */
export interface ValidationError {
  field: string;
  message: string;
  messageHebrew: string;
  keyword?: string;
  schemaPath?: string;
  params?: any;
}

/**
 * Validation result
 */
export interface ValidationResult {
  valid: boolean;
  errors: ValidationError[];
  errorCount: number;
}

/**
 * Translate AJV error keywords to Hebrew
 */
function getHebrewErrorMessage(error: ErrorObject): string {
  const keyword = error.keyword;
  const params = error.params;

  switch (keyword) {
    case 'required':
      return `שדה חובה: ${params.missingProperty}`;
    case 'type':
      return `סוג לא תקין: צריך להיות ${params.type}`;
    case 'format':
      return `פורמט לא תקין: ${params.format}`;
    case 'pattern':
      return `תבנית לא תקינה: לא תואם ${params.pattern}`;
    case 'minLength':
      return `אורך מינימלי: ${params.limit} תווים`;
    case 'maxLength':
      return `אורך מקסימלי: ${params.limit} תווים`;
    case 'minimum':
      return `ערך מינימלי: ${params.limit}`;
    case 'maximum':
      return `ערך מקסימלי: ${params.limit}`;
    case 'exclusiveMinimum':
      return `ערך צריך להיות גדול מ-${params.limit}`;
    case 'exclusiveMaximum':
      return `ערך צריך להיות קטן מ-${params.limit}`;
    case 'multipleOf':
      return `צריך להיות כפולה של ${params.multipleOf}`;
    case 'minItems':
      return `מספר מינימלי של פריטים: ${params.limit}`;
    case 'maxItems':
      return `מספר מקסימלי של פריטים: ${params.limit}`;
    case 'uniqueItems':
      return `פריטים חייבים להיות ייחודיים`;
    case 'minProperties':
      return `מספר מינימלי של מאפיינים: ${params.limit}`;
    case 'maxProperties':
      return `מספר מקסימלי של מאפיינים: ${params.limit}`;
    case 'enum':
      return `ערך חייב להיות אחד מהרשימה: ${params.allowedValues?.join(', ')}`;
    case 'const':
      return `ערך חייב להיות: ${params.allowedValue}`;
    case 'additionalProperties':
      return `מאפיין נוסף לא מורשה: ${params.additionalProperty}`;
    case 'dependentRequired':
      return `שדה ${params.missingProperty} נדרש כאשר ${params.property} קיים`;
    case 'allOf':
      return 'לא עומד בכל התנאים (allOf)';
    case 'anyOf':
      return 'לא עומד באף אחד מהתנאים (anyOf)';
    case 'oneOf':
      return 'חייב לעמוד בדיוק בתנאי אחד (oneOf)';
    case 'not':
      return 'לא צריך לעמוד בתנאי (not)';
    case 'if':
      return 'תנאי if/then/else לא מתקיים';
    default:
      return error.message || 'שגיאת אימות';
  }
}

/**
 * Format field path from JSON pointer
 */
function formatFieldPath(instancePath: string): string {
  if (!instancePath) return 'root';
  return instancePath.replace(/^\//, '').replace(/\//g, '.');
}

/**
 * Convert AJV errors to our ValidationError format
 */
function convertAjvErrors(errors: ErrorObject[] | null | undefined): ValidationError[] {
  if (!errors || errors.length === 0) return [];

  return errors.map(error => ({
    field: formatFieldPath(error.instancePath),
    message: error.message || 'Validation error',
    messageHebrew: getHebrewErrorMessage(error),
    keyword: error.keyword,
    schemaPath: error.schemaPath,
    params: error.params
  }));
}

/**
 * Validate JSON data against a schema
 */
export function validateJsonAgainstSchema(
  schema: JSONSchema,
  data: any
): ValidationResult {
  try {
    // Fix any RTL-corrupted patterns in the schema before validation
    const fixedSchema = fixRTLPatterns(schema);
    
    // Compile schema (cached by AJV)
    const validate = ajv.compile(fixedSchema);
    
    // Validate data
    const valid = validate(data);
    
    // Convert errors
    const errors = convertAjvErrors(validate.errors);
    
    return {
      valid: valid === true,
      errors,
      errorCount: errors.length
    };
  } catch (error) {
    // Schema compilation error
    return {
      valid: false,
      errors: [{
        field: 'schema',
        message: error instanceof Error ? error.message : 'Schema compilation error',
        messageHebrew: 'שגיאה בקומפילציה של Schema'
      }],
      errorCount: 1
    };
  }
}

/**
 * Validate a schema definition itself
 */
export function validateSchemaDefinition(schema: any): ValidationResult {
  try {
    // Try to compile the schema
    ajv.compile(schema);
    
    return {
      valid: true,
      errors: [],
      errorCount: 0
    };
  } catch (error) {
    return {
      valid: false,
      errors: [{
        field: 'schema',
        message: error instanceof Error ? error.message : 'Invalid schema definition',
        messageHebrew: 'הגדרת Schema לא תקינה'
      }],
      errorCount: 1
    };
  }
}

/**
 * Check if data matches schema type
 */
export function checkType(schema: JSONSchema, data: any): boolean {
  if (typeof schema === 'boolean') return schema;
  if (!schema.type) return true;

  const type = schema.type;
  const dataType = Array.isArray(data) ? 'array' : data === null ? 'null' : typeof data;

  if (type === 'integer') {
    return typeof data === 'number' && Number.isInteger(data);
  }

  return type === dataType;
}

/**
 * Get validation error summary in Hebrew
 */
export function getErrorSummary(result: ValidationResult): string {
  if (result.valid) {
    return 'האימות עבר בהצלחה';
  }

  if (result.errorCount === 1) {
    return `נמצאה שגיאת אימות אחת`;
  }

  return `נמצאו ${result.errorCount} שגיאות אימות`;
}

/**
 * Format validation errors for display
 */
export function formatValidationErrors(errors: ValidationError[]): string {
  return errors.map((error, index) => 
    `${index + 1}. ${error.field}: ${error.messageHebrew}`
  ).join('\n');
}

/**
 * Quick validation check (returns boolean only)
 */
export function isValid(schema: JSONSchema, data: any): boolean {
  const result = validateJsonAgainstSchema(schema, data);
  return result.valid;
}

/**
 * Validate and return first error only
 */
export function validateWithFirstError(
  schema: JSONSchema,
  data: any
): { valid: boolean; error?: ValidationError } {
  const result = validateJsonAgainstSchema(schema, data);
  
  return {
    valid: result.valid,
    error: result.errors.length > 0 ? result.errors[0] : undefined
  };
}
