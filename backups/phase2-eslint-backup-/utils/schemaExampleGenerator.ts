/**
 * Smart JSON Schema Example Generator
 * Generates realistic example JSON data from JSON Schema Draft 2020-12 definitions
 */

import { type JSONSchema } from 'jsonjoy-builder';

interface GeneratorOptions {
  includeOptional?: boolean; // Generate optional fields
  arrayMinItems?: number; // Minimum items for arrays (default: 1)
  arrayMaxItems?: number; // Maximum items for arrays (default: 3)
  useDefaults?: boolean; // Use default values if specified
  useExamples?: boolean; // Use examples if specified
}

const defaultOptions: GeneratorOptions = {
  includeOptional: true, // Changed to true - generate ALL fields by default
  arrayMinItems: 1,
  arrayMaxItems: 3,
  useDefaults: true,
  useExamples: true
};

/**
 * Helper: Reverse RTL-corrupted pattern back to correct LTR form
 * RTL causes patterns like ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ to become ${2}[0-9]-{2}[0-9]-{4}[0-9]-${
 */
function unreverseRTLPattern(pattern: string): string {
  // Check if pattern looks reversed (starts with $ or } instead of ^)
  if (pattern.startsWith('$') || pattern.startsWith('}')) {
    // Reverse the entire string back
    return pattern.split('').reverse().join('');
  }
  return pattern;
}

/**
 * Generate realistic string based on pattern, format, or semantic hints
 */
function generateString(schema: any, fieldName: string = ''): string {
  // 1. Check for const value
  if (schema.const !== undefined) return String(schema.const);
  
  // 2. Check for enum
  if (schema.enum && schema.enum.length > 0) return String(schema.enum[0]);
  
  // 3. Check for format (standard JSON Schema formats)
  if (schema.format) {
    switch (schema.format) {
      case 'email': return 'user@example.com';
      case 'date': return '2025-01-15'; // ISO 8601 date format
      case 'date-time': return '2025-01-15T12:00:00Z';
      case 'time': return '12:00:00';
      case 'uri': return 'https://example.com';
      case 'uri-reference': return '/path/to/resource';
      case 'uri-template': return '/api/{resource}/{id}';
      case 'url': return 'https://example.com/page';
      case 'uuid': return '123e4567-e89b-12d3-a456-426614174000';
      case 'ipv4': return '192.168.1.1';
      case 'ipv6': return '2001:0db8:85a3:0000:0000:8a2e:0370:7334';
      case 'hostname': return 'example.com';
      case 'regex': return '^[a-z]+$';
      case 'json-pointer': return '/path/to/field';
      default: break;
    }
  }
  
  // 4. Generate from pattern (smart pattern detection)
  if (schema.pattern) {
    // CRITICAL FIX: Unreverse RTL-corrupted patterns
    const pattern = unreverseRTLPattern(schema.pattern);
    
    // Date patterns - parse to determine format
    // YYYY-MM-DD pattern: ^[0-9]{4}-[0-9]{2}-[0-9]{2}$
    if (pattern.match(/^\^?\[0-9\]\{4\}-\[0-9\]\{2\}-\[0-9\]\{2\}\$?$/)) {
      return '2025-01-15'; // YYYY-MM-DD
    }
    // DD-MM-YYYY pattern: ^[0-9]{2}-[0-9]{2}-[0-9]{4}$
    if (pattern.match(/^\^?\[0-9\]\{2\}-\[0-9\]\{2\}-\[0-9\]\{4\}\$?$/)) {
      return '15-01-2025'; // DD-MM-YYYY
    }
    // MM/DD/YYYY pattern
    if (pattern.match(/^\^?\[0-9\]\{2\}\/\[0-9\]\{2\}\/\[0-9\]\{4\}\$?$/)) {
      return '01/15/2025'; // MM/DD/YYYY
    }
    // DD/MM/YYYY pattern
    if (pattern.match(/^\^?\[0-9\]\{2\}\/\[0-9\]\{2\}\/\[0-9\]\{4\}\$?$/) && 
        (fieldName.toLowerCase().includes('date') || fieldName.toLowerCase().includes('תאריך'))) {
      return '15/01/2025'; // DD/MM/YYYY
    }
    
    // Phone patterns
    if (pattern.match(/05\[0-9\]/) || pattern.includes('phone') || pattern.includes('טלפון')) {
      return '050-1234567';
    }
    
    // Israeli ID - 9 digits
    if (pattern.match(/^\^?\[0-9\]\{9\}\$?$/)) {
      return '123456789';
    }
    
    // Postal/ZIP code
    if (pattern.match(/^\^?\[0-9\]\{5,7\}\$?$/)) {
      return '12345';
    }
    
    // Credit card
    if (pattern.match(/\[0-9\]\{13,19\}/)) {
      return '4111111111111111';
    }
    
    // Generic digit pattern \d{n} or [0-9]{n}
    const digitMatch = pattern.match(/\[0-9\]\{(\d+)\}|\\d\{(\d+)\}/);
    if (digitMatch) {
      const length = parseInt(digitMatch[1] || digitMatch[2]);
      // Generate appropriate digits based on context
      if (length === 4) return '2025'; // Likely year
      if (length === 2) return '15'; // Likely day/month
      return '1'.repeat(Math.min(length, 15));
    }
    
    // Alpha pattern [a-z]{n} or [a-zA-Z]{n}
    const alphaMatch = pattern.match(/\[a-zA-Z\]\{(\d+)\}|\[a-z\]\{(\d+)\}/i);
    if (alphaMatch) {
      const length = parseInt(alphaMatch[1] || alphaMatch[2]);
      return 'example'.substring(0, Math.min(length, 20)).padEnd(Math.min(length, 20), 'x');
    }
  }
  
  // 6. Use semantic hints from title/description and field name
  const hint = (schema.title || schema.description || fieldName || '').toLowerCase();
  
  // Transaction IDs
  if (hint.includes('transaction') && hint.includes('id')) {
    return `TXN-${Date.now().toString().slice(-8)}`;
  }
  if (hint.includes('order') && hint.includes('id')) {
    return `ORD-${Date.now().toString().slice(-8)}`;
  }
  if (hint.includes('invoice') && (hint.includes('id') || hint.includes('number'))) {
    return `INV-${Date.now().toString().slice(-8)}`;
  }
  
  if (hint.includes('email') || hint.includes('דוא') || hint.includes('מייל')) {
    return 'user@example.com';
  }
  if (hint.includes('phone') || hint.includes('טלפון')) {
    return '050-1234567';
  }
  if (hint.includes('name') || hint.includes('שם')) {
    return 'ישראל ישראלי';
  }
  if (hint.includes('first name') || hint.includes('שם פרטי')) {
    return 'ישראל';
  }
  if (hint.includes('last name') || hint.includes('שם משפחה')) {
    return 'ישראלי';
  }
  if (hint.includes('address') || hint.includes('כתובת')) {
    return 'רחוב הרצל 1, תל אביב';
  }
  if (hint.includes('city') || hint.includes('עיר')) {
    return 'תל אביב';
  }
  if (hint.includes('street')) {
    return 'רחוב הרצל 1';
  }
  if (hint.includes('country') || hint.includes('מדינה')) {
    return 'ישראל';
  }
  if (hint.includes('company') || hint.includes('חברה')) {
    return 'חברת דוגמה בע"מ';
  }
  if (hint.includes('id') || hint.includes('תעודת זהות')) {
    return '123456789';
  }
  if (hint.includes('description') || hint.includes('תיאור')) {
    return 'תיאור מפורט של הפריט';
  }
  if (hint.includes('notes') || hint.includes('הערות')) {
    return 'הערות נוספות';
  }
  if (hint.includes('status')) {
    return 'active';
  }
  if (hint.includes('category') || hint.includes('קטגוריה')) {
    return 'כללי';
  }
  
  // 7. Respect length constraints
  let result = 'example text';
  if (schema.minLength !== undefined && result.length < schema.minLength) {
    result = result.padEnd(schema.minLength, ' example');
  }
  if (schema.maxLength !== undefined && result.length > schema.maxLength) {
    result = result.substring(0, schema.maxLength);
  }
  
  return result;
}

/**
 * Generate realistic number based on constraints
 */
function generateNumber(schema: any, isInteger: boolean = false): number {
  // 1. Check for const value
  if (schema.const !== undefined) return Number(schema.const);
  
  // 2. Check for enum
  if (schema.enum && schema.enum.length > 0) return Number(schema.enum[0]);
  
  // 3. Respect minimum/maximum constraints
  let value: number;
  
  if (schema.minimum !== undefined) {
    value = schema.exclusiveMinimum ? schema.minimum + 1 : schema.minimum;
  } else if (schema.maximum !== undefined) {
    value = schema.exclusiveMaximum 
      ? schema.maximum - 1 
      : Math.floor(schema.maximum / 2);
  } else {
    // Use semantic hints
    const hint = (schema.title || schema.description || '').toLowerCase();
    if (hint.includes('age') || hint.includes('גיל')) {
      value = 30;
    } else if (hint.includes('price') || hint.includes('מחיר') || hint.includes('amount') || hint.includes('סכום')) {
      value = 100.00;
    } else if (hint.includes('quantity') || hint.includes('כמות') || hint.includes('count')) {
      value = 10;
    } else if (hint.includes('percentage') || hint.includes('אחוז')) {
      value = 50;
    } else {
      value = isInteger ? 42 : 3.14;
    }
  }
  
  // 4. Apply multipleOf constraint
  if (schema.multipleOf !== undefined) {
    value = Math.round(value / schema.multipleOf) * schema.multipleOf;
  }
  
  // 5. Ensure within bounds
  if (schema.minimum !== undefined) {
    if (schema.exclusiveMinimum && value <= schema.minimum) {
      value = schema.minimum + (schema.multipleOf || 1);
    } else if (!schema.exclusiveMinimum && value < schema.minimum) {
      value = schema.minimum;
    }
  }
  
  if (schema.maximum !== undefined) {
    if (schema.exclusiveMaximum && value >= schema.maximum) {
      value = schema.maximum - (schema.multipleOf || 1);
    } else if (!schema.exclusiveMaximum && value > schema.maximum) {
      value = schema.maximum;
    }
  }
  
  return isInteger ? Math.floor(value) : value;
}

/**
 * Generate array with realistic items
 */
function generateArray(schema: any, options: GeneratorOptions): any[] {
  const itemSchema = schema.items;
  if (!itemSchema) return [];
  
  // Determine array length
  let length = options.arrayMinItems!;
  if (schema.minItems !== undefined) {
    length = Math.max(length, schema.minItems);
  }
  if (schema.maxItems !== undefined) {
    length = Math.min(length, schema.maxItems);
  }
  if (schema.maxItems !== undefined && schema.minItems !== undefined) {
    length = schema.minItems;
  }
  
  // Generate items
  const items: any[] = [];
  for (let i = 0; i < length; i++) {
    const item = generateFromSchema(itemSchema, options);
    
    // Handle uniqueItems constraint
    if (schema.uniqueItems && items.some(existing => 
      JSON.stringify(existing) === JSON.stringify(item)
    )) {
      // Try to modify the item slightly to make it unique
      if (typeof item === 'string') {
        items.push(`${item}_${i}`);
      } else if (typeof item === 'number') {
        items.push(item + i);
      } else if (typeof item === 'object' && item !== null) {
        items.push({ ...item, _uniqueId: i });
      } else {
        items.push(item);
      }
    } else {
      items.push(item);
    }
  }
  
  return items;
}

/**
 * Generate object with realistic properties
 */
function generateObject(schema: any, options: GeneratorOptions): any {
  const result: any = {};
  
  // 1. Generate required properties
  if (schema.required && Array.isArray(schema.required)) {
    for (const key of schema.required) {
      if (schema.properties && schema.properties[key]) {
        result[key] = generateFromSchema(schema.properties[key], options, key);
      }
    }
  }
  
  // 2. Generate non-required properties if includeOptional is true
  if (options.includeOptional && schema.properties) {
    for (const key of Object.keys(schema.properties)) {
      if (!result.hasOwnProperty(key)) {
        // Include ALL optional fields when includeOptional is true
        result[key] = generateFromSchema(schema.properties[key], options, key);
      }
    }
  }
  
  // 3. Handle dependentRequired
  if (schema.dependentRequired) {
    for (const [key, deps] of Object.entries(schema.dependentRequired)) {
      if (result.hasOwnProperty(key) && Array.isArray(deps)) {
        for (const depKey of deps) {
          if (!result.hasOwnProperty(depKey) && schema.properties && schema.properties[depKey]) {
            result[depKey] = generateFromSchema(schema.properties[depKey], options, depKey);
          }
        }
      }
    }
  }
  
  return result;
}

/**
 * Main schema generation function with combinator support
 */
function generateFromSchema(schema: any, options: GeneratorOptions = defaultOptions, fieldName: string = ''): any {
  if (typeof schema === 'boolean') {
    return schema ? {} : undefined;
  }
  
  // Use default value if specified
  if (options.useDefaults && schema.default !== undefined) {
    return schema.default;
  }
  
  // Use examples if specified
  if (options.useExamples && schema.examples && schema.examples.length > 0) {
    return schema.examples[0];
  }
  
  // Handle combinators
  if (schema.allOf) {
    // Merge all schemas
    let merged = {};
    for (const subSchema of schema.allOf) {
      const generated = generateFromSchema(subSchema, options);
      if (typeof generated === 'object' && generated !== null) {
        merged = { ...merged, ...generated };
      }
    }
    return merged;
  }
  
  if (schema.anyOf && schema.anyOf.length > 0) {
    // Use first schema
    return generateFromSchema(schema.anyOf[0], options);
  }
  
  if (schema.oneOf && schema.oneOf.length > 0) {
    // Use first schema
    return generateFromSchema(schema.oneOf[0], options);
  }
  
  // Handle type-specific generation
  const type = schema.type;
  
  if (type === 'null' || schema.const === null) {
    return null;
  }
  
  if (type === 'boolean') {
    if (schema.const !== undefined) return Boolean(schema.const);
    if (schema.enum && schema.enum.length > 0) return Boolean(schema.enum[0]);
    return true;
  }
  
  if (type === 'integer') {
    return generateNumber(schema, true);
  }
  
  if (type === 'number') {
    return generateNumber(schema, false);
  }
  
  if (type === 'string') {
    return generateString(schema, fieldName);
  }
  
  if (type === 'array') {
    return generateArray(schema, options);
  }
  
  if (type === 'object') {
    return generateObject(schema, options);
  }
  
  // If no type specified but has properties, treat as object
  if (schema.properties) {
    return generateObject(schema, options);
  }
  
  // Default fallback
  return null;
}

/**
 * Public API: Generate example JSON from schema
 */
export function generateExample(
  schema: JSONSchema,
  options: Partial<GeneratorOptions> = {}
): any {
  const fullOptions = { ...defaultOptions, ...options };
  return generateFromSchema(schema, fullOptions);
}

/**
 * Generate multiple example variations
 */
export function generateExamples(
  schema: JSONSchema,
  count: number = 3,
  options: Partial<GeneratorOptions> = {}
): any[] {
  const examples: any[] = [];
  const baseOptions = { ...defaultOptions, ...options };
  
  for (let i = 0; i < count; i++) {
    // Vary includeOptional for different examples
    const variedOptions = {
      ...baseOptions,
      includeOptional: i % 2 === 1
    };
    examples.push(generateFromSchema(schema, variedOptions));
  }
  
  return examples;
}
