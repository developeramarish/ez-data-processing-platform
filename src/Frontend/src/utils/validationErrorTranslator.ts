/**
 * Validation Error Translator - Translates Corvus.Json.Validator error messages to simple Hebrew
 */

interface TranslatedError {
  fieldName: string;
  hebrewMessage: string;
  shortMessage: string;  // Very short version for display
  validationType: string;
}

// Short Hebrew messages for validation types
const shortMessages: Record<string, (field: string) => string> = {
  'pattern': (field) => `${field}: לא בפורמט תקין`,
  'minlength': (field) => `${field}: קצר מדי`,
  'maxlength': (field) => `${field}: ארוך מדי`,
  'minimum': (field) => `${field}: ערך קטן מדי`,
  'maximum': (field) => `${field}: ערך גדול מדי`,
  'enum': (field) => `${field}: ערך לא חוקי`,
  'format': (field) => `${field}: פורמט שגוי`,
  'type': (field) => `${field}: סוג נתונים שגוי`,
  'required': (field) => `${field}: שדה חובה חסר`,
};

/**
 * Parse Corvus.Json.Validator error message to extract field name and type
 * Format: "#/FieldName Invalid Validation type - details"
 */
function parseCorvusError(errorMessage: string): { fieldName: string; validationType: string } {
  // Extract field name from "#/FieldName"
  const fieldMatch = errorMessage.match(/^#\/([^\s]+)/);
  const fieldPath = fieldMatch ? fieldMatch[1] : '';
  const fieldName = fieldPath.split('/').pop() || 'unknown';

  // Extract validation type
  const typeMatch = errorMessage.match(/Invalid Validation\s+(\w+)/i);
  let validationType = typeMatch ? typeMatch[1].toLowerCase() : 'unknown';

  // Fallback: detect type from common keywords
  if (validationType === 'unknown') {
    const message = errorMessage.toLowerCase();
    if (message.includes('pattern') || message.includes('did not match')) validationType = 'pattern';
    else if (message.includes('minlength')) validationType = 'minlength';
    else if (message.includes('maxlength')) validationType = 'maxlength';
    else if (message.includes('minimum')) validationType = 'minimum';
    else if (message.includes('maximum')) validationType = 'maximum';
    else if (message.includes('enum')) validationType = 'enum';
    else if (message.includes('format')) validationType = 'format';
    else if (message.includes('type')) validationType = 'type';
    else if (message.includes('required')) validationType = 'required';
  }

  return { fieldName, validationType };
}

/**
 * Translate a single validation error to simple Hebrew
 */
export function translateValidationError(errorMessage: string): TranslatedError {
  const { fieldName, validationType } = parseCorvusError(errorMessage);

  const template = shortMessages[validationType];
  const shortMessage = template ? template(fieldName) : `${fieldName}: שגיאת אימות`;

  return {
    fieldName,
    hebrewMessage: shortMessage,
    shortMessage,
    validationType,
  };
}

/**
 * Extract errored field names from validation errors
 */
export function extractErroredFields(errors: Array<{ message: string }>): Set<string> {
  const fields = new Set<string>();
  for (const error of errors) {
    const { fieldName } = parseCorvusError(error.message);
    if (fieldName && fieldName !== 'unknown') {
      fields.add(fieldName);
    }
  }
  return fields;
}

export default { translateValidationError, extractErroredFields };
