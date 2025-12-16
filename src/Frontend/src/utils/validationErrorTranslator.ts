/**
 * Validation Error Translator - Translates Corvus.Json.Validator error messages to Hebrew
 * with detailed explanations of what went wrong and what's expected
 */

export interface TranslatedError {
  fieldName: string;
  shortMessage: string;      // Short Hebrew message for display
  detailedMessage: string;   // Detailed Hebrew message with expected format
  validationType: string;    // pattern, enum, minLength, etc.
  expectedValue?: string;    // What the value should be/look like
  actualValue?: string;      // What was actually received
  isValid: boolean;          // false if this is an unknown/unparseable error
}

// Hebrew messages with placeholders for details
const validationMessages: Record<string, {
  short: (field: string) => string;
  detailed: (field: string, expected?: string, actual?: string) => string;
}> = {
  'pattern': {
    short: (field) => `${field}: לא בפורמט תקין`,
    detailed: (field, expected) => expected
      ? `השדה ${field} לא תואם לתבנית הנדרשת: ${expected}`
      : `השדה ${field} לא בפורמט הנדרש`,
  },
  'minlength': {
    short: (field) => `${field}: קצר מדי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להכיל לפחות ${expected} תווים`
      : `השדה ${field} קצר מדי`,
  },
  'maxlength': {
    short: (field) => `${field}: ארוך מדי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להכיל לכל היותר ${expected} תווים`
      : `השדה ${field} ארוך מדי`,
  },
  'minimum': {
    short: (field) => `${field}: ערך קטן מדי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להיות לפחות ${expected}`
      : `השדה ${field} מכיל ערך קטן מהמינימום המותר`,
  },
  'maximum': {
    short: (field) => `${field}: ערך גדול מדי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להיות לכל היותר ${expected}`
      : `השדה ${field} מכיל ערך גדול מהמקסימום המותר`,
  },
  'enum': {
    short: (field) => `${field}: ערך לא חוקי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להיות אחד מ: ${expected}`
      : `השדה ${field} מכיל ערך שאינו מהרשימה המותרת`,
  },
  'format': {
    short: (field) => `${field}: פורמט שגוי`,
    detailed: (field, expected) => expected
      ? `השדה ${field} חייב להיות בפורמט ${expected}`
      : `השדה ${field} אינו בפורמט הנדרש`,
  },
  'type': {
    short: (field) => `${field}: סוג נתונים שגוי`,
    detailed: (field, expected, actual) => {
      if (expected && actual) {
        return `השדה ${field} צריך להיות מסוג ${expected}, אבל הוא מסוג ${actual}`;
      }
      return expected
        ? `השדה ${field} חייב להיות מסוג ${expected}`
        : `השדה ${field} מכיל סוג נתונים שגוי`;
    },
  },
  'required': {
    short: (field) => `${field}: שדה חובה חסר`,
    detailed: (field) => `השדה ${field} הוא שדה חובה ולא ניתן להשאירו ריק`,
  },
  'properties': {
    short: (field) => `${field}: שדה חובה חסר`,
    detailed: (field) => `השדה ${field} הוא שדה חובה וחסר מהרשומה. יש להוסיף את השדה הזה.`,
  },
};

// Hebrew labels for validation types (for filter dropdown)
export const validationTypeLabels: Record<string, string> = {
  'pattern': 'תבנית לא תקינה',
  'minlength': 'קצר מדי',
  'maxlength': 'ארוך מדי',
  'minimum': 'מתחת למינימום',
  'maximum': 'מעל למקסימום',
  'enum': 'ערך לא ברשימה',
  'format': 'פורמט שגוי',
  'type': 'סוג נתונים שגוי',
  'required': 'שדה חובה חסר',
  'properties': 'שדה חובה חסר',
};

/**
 * Parse Corvus.Json.Validator error message to extract details
 * Format: "#/FieldName Invalid Validation type - details" or similar
 */
function parseCorvusError(errorMessage: string): {
  fieldName: string;
  validationType: string;
  expectedValue?: string;
  actualValue?: string;
} {
  // Extract field name from "#/FieldName" at start of message
  const fieldMatch = errorMessage.match(/^#\/([^\s]+)/);
  const fieldPath = fieldMatch ? fieldMatch[1] : '';
  const fieldName = fieldPath.split('/').pop() || '';

  // Try to extract validation type from message
  let validationType = 'unknown';
  let expectedValue: string | undefined;
  let actualValue: string | undefined;

  const message = errorMessage.toLowerCase();

  // Pattern validation - try to extract the pattern
  if (message.includes('pattern') || message.includes('did not match')) {
    validationType = 'pattern';
    const patternMatch = errorMessage.match(/pattern[:\s]+['"]?([^'"]+)['"]?/i);
    if (patternMatch) expectedValue = patternMatch[1];
  }
  // Enum validation - try to extract allowed values
  else if (message.includes('enum') || message.includes('one of')) {
    validationType = 'enum';
    const enumMatch = errorMessage.match(/\[([^\]]+)\]/);
    if (enumMatch) expectedValue = enumMatch[1];
  }
  // MinLength validation
  else if (message.includes('minlength') || message.includes('minimum length')) {
    validationType = 'minlength';
    const lengthMatch = errorMessage.match(/(\d+)/);
    if (lengthMatch) expectedValue = lengthMatch[1];
  }
  // MaxLength validation
  else if (message.includes('maxlength') || message.includes('maximum length')) {
    validationType = 'maxlength';
    const lengthMatch = errorMessage.match(/(\d+)/);
    if (lengthMatch) expectedValue = lengthMatch[1];
  }
  // Minimum value validation
  else if (message.includes('minimum') && !message.includes('length')) {
    validationType = 'minimum';
    const valueMatch = errorMessage.match(/minimum[:\s]+(\d+(?:\.\d+)?)/i);
    if (valueMatch) expectedValue = valueMatch[1];
  }
  // Maximum value validation
  else if (message.includes('maximum') && !message.includes('length')) {
    validationType = 'maximum';
    const valueMatch = errorMessage.match(/maximum[:\s]+(\d+(?:\.\d+)?)/i);
    if (valueMatch) expectedValue = valueMatch[1];
  }
  // Format validation (date, email, uri, etc.)
  else if (message.includes('format')) {
    validationType = 'format';
    const formatMatch = errorMessage.match(/format[:\s]+['"]?(\w+)['"]?/i);
    if (formatMatch) expectedValue = formatMatch[1];
  }
  // Type validation
  else if (message.includes('type')) {
    validationType = 'type';
    const typeMatch = errorMessage.match(/expected[:\s]+['"]?(\w+)['"]?/i);
    if (typeMatch) expectedValue = typeMatch[1];
  }
  // Required field validation (via "properties" - required property was not present)
  else if (message.includes('properties') && message.includes('required property')) {
    validationType = 'properties';
    // Extract field name from "the required property 'FieldName' was not present"
    const requiredMatch = errorMessage.match(/required property ['"]?([^'"]+)['"]? was not present/i);
    if (requiredMatch && !fieldName) {
      return { fieldName: requiredMatch[1], validationType, expectedValue: 'required', actualValue: '(missing)' };
    }
  }
  // Required field validation (other formats)
  else if (message.includes('required')) {
    validationType = 'required';
  }

  return { fieldName, validationType, expectedValue, actualValue };
}

/**
 * Translate a single validation error to Hebrew with details
 */
export function translateValidationError(errorMessage: string): TranslatedError {
  const { fieldName, validationType, expectedValue, actualValue } = parseCorvusError(errorMessage);

  // Check if this is a valid, parseable error
  const isValid = fieldName !== '' && fieldName !== 'unknown' && validationType !== 'unknown';

  const messageConfig = validationMessages[validationType];

  let shortMessage: string;
  let detailedMessage: string;

  if (messageConfig && fieldName) {
    shortMessage = messageConfig.short(fieldName);
    detailedMessage = messageConfig.detailed(fieldName, expectedValue, actualValue);
  } else if (fieldName && fieldName !== 'unknown') {
    // Known field but unknown validation type
    shortMessage = `${fieldName}: שגיאת אימות`;
    detailedMessage = `השדה ${fieldName} מכיל שגיאת אימות`;
  } else {
    // Completely unknown error - mark as invalid
    shortMessage = 'שגיאת אימות כללית';
    detailedMessage = errorMessage; // Show original message
  }

  return {
    fieldName: fieldName || 'unknown',
    shortMessage,
    detailedMessage,
    validationType,
    expectedValue,
    actualValue,
    isValid,
  };
}

/**
 * Translate all errors to Hebrew.
 * Note: Previously this filtered out unparseable errors, but that caused
 * records to appear with no errors when error messages didn't match expected format.
 * Now ALL errors are shown - parsed errors get nice messages, unparseable errors
 * show a generic message with the original error text.
 */
export function translateErrors(errors: Array<{ message: string }>): TranslatedError[] {
  return errors.map(error => translateValidationError(error.message));
}

/**
 * Extract errored field names from validation errors (excludes unknown fields)
 */
export function extractErroredFields(errors: Array<{ message: string }>): Set<string> {
  const fields = new Set<string>();
  for (const error of errors) {
    const { fieldName, validationType } = parseCorvusError(error.message);
    // Only add fields that were successfully parsed
    if (fieldName && fieldName !== 'unknown' && validationType !== 'unknown') {
      fields.add(fieldName);
    }
  }
  return fields;
}

/**
 * Get unique validation types from errors (for filter dropdown)
 */
export function getUniqueValidationTypes(errors: Array<{ message: string }>): string[] {
  const types = new Set<string>();
  for (const error of errors) {
    const translated = translateValidationError(error.message);
    if (translated.isValid && translated.validationType !== 'unknown') {
      types.add(translated.validationType);
    }
  }
  return Array.from(types);
}

export default {
  translateValidationError,
  translateErrors,
  extractErroredFields,
  getUniqueValidationTypes,
  validationTypeLabels,
};
