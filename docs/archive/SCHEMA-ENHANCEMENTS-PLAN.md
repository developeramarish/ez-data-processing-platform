# Schema Definition Enhancements Plan

## Overview
Enhance JSON Schema definitions and example JSON generation to use JSON Schema Draft 2020-12 features more effectively, with smart example generation and validation.

## Current State Analysis

### Frontend (SchemaBuilderNew.tsx)
**Strengths:**
- Uses jsonjoy-builder for visual schema editing
- Has basic example JSON generation
- Supports Hebrew localization

**Gaps:**
1. Example generation is simplistic
2. No validation of generated examples
3. Limited use of JSON Schema features:
   - No `minLength`/`maxLength` handling
   - No `multipleOf` for numbers
   - No support for `const`, `allOf`, `anyOf`, `oneOf`
   - No `if/then/else` conditionals
   - Limited pattern matching
   - No `dependentRequired`/`dependentSchemas`

### Backend (SchemaValidationService.cs)
**Gaps:**
1. Only basic JSON parsing validation
2. No actual JSON Schema validation library
3. TODOs indicate missing functionality

## Implementation Plan

### Phase 1: Frontend - Add Validation Library ✓
**Install AJV (Another JSON Validator)**
```bash
npm install ajv ajv-formats ajv-errors
```

**Why AJV:**
- Fastest JSON Schema validator
- Full JSON Schema Draft 2020-12 support
- Excellent format support
- Active maintenance
- Free and open-source

### Phase 2: Frontend - Smart Example Generator
**Create New Utility: `src/Frontend/src/utils/schemaExampleGenerator.ts`**

Features to implement:
1. **String constraints:**
   - Respect `minLength`/`maxLength`
   - Generate data matching `pattern` regex
   - Support all `format` types (email, date, uri, uuid, etc.)
   - Handle `enum` values
   - Use `const` values
   - Smart defaults based on field names/descriptions

2. **Number constraints:**
   - Respect `minimum`/`maximum`
   - Handle `exclusiveMinimum`/`exclusiveMaximum`
   - Support `multipleOf`
   - Use `enum` and `const`

3. **Array constraints:**
   - Respect `minItems`/`maxItems`
   - Handle `uniqueItems`
   - Support `prefixItems` and `items`
   - Generate proper `contains` examples

4. **Object constraints:**
   - Handle `required` fields
   - Support `minProperties`/`maxProperties`
   - Respect `additionalProperties`
   - Handle `patternProperties`
   - Support `dependentRequired`/`dependentSchemas`

5. **Advanced features:**
   - Handle `allOf`, `anyOf`, `oneOf` combinators
   - Support `if/then/else` conditionals
   - Generate `$ref` references
   - Respect `default` values

### Phase 3: Frontend - Validation Integration
**Enhance SchemaBuilderNew.tsx:**
1. Add AJV validator instance
2. Validate generated examples before display
3. Show validation errors if example is invalid
4. Auto-fix common validation issues
5. Add "Validate & Fix" button

### Phase 4: Backend - Add NJsonSchema
**Install NJsonSchema NuGet package**
```xml
<PackageReference Include="NJsonSchema" Version="11.0.0" />
```

**Why NJsonSchema:**
- Full JSON Schema Draft 2020-12 support
- Free and open-source (MIT license)
- Excellent C# integration
- Can generate examples from schemas
- Active maintenance

**Update SchemaValidationService.cs:**
1. Implement proper schema validation
2. Add data validation against schema
3. Return detailed validation errors
4. Support all JSON Schema features

### Phase 5: Enhanced Features
1. **Schema Templates:**
   - Common field patterns (email, phone, address)
   - Business entities (customer, order, invoice)
   - Israeli-specific fields (ID number, postal code)

2. **Smart Suggestions:**
   - Suggest constraints based on field names
   - Recommend formats for common patterns
   - Auto-detect validation rules

3. **Validation Feedback:**
   - Real-time validation as users type
   - Clear error messages
   - Suggestions for fixing issues

## Implementation Steps

### Step 1: Install Frontend Dependencies
```bash
cd src/Frontend
npm install ajv ajv-formats ajv-errors
```

### Step 2: Create Schema Example Generator
File: `src/Frontend/src/utils/schemaExampleGenerator.ts`
- Comprehensive example generation
- Support all JSON Schema features
- Realistic data generation

### Step 3: Create Schema Validator Utility
File: `src/Frontend/src/utils/schemaValidator.ts`
- AJV integration
- Validation error formatting
- Hebrew error messages

### Step 4: Update SchemaBuilderNew Component
- Integrate validator
- Add validation display
- Show validation results
- Add fix suggestions

### Step 5: Backend NJsonSchema Integration
- Add NuGet package
- Update SchemaValidationService
- Add comprehensive validation
- Update API responses

### Step 6: Testing
- Unit tests for example generator
- Unit tests for validator
- Integration tests for API
- E2E tests for UI

## Success Criteria

1. ✅ Generated examples always pass schema validation
2. ✅ Support for all JSON Schema Draft 2020-12 features
3. ✅ Realistic example data (not just "example text")
4. ✅ Clear validation error messages in Hebrew
5. ✅ Backend properly validates data against schemas
6. ✅ No breaking changes to existing functionality

## Technical Details

### JSON Schema Draft 2020-12 Features to Support

**Core Keywords:**
- `$schema`, `$id`, `$ref`, `$defs`
- `$comment`, `title`, `description`
- `default`, `examples`

**Type Keywords:**
- `type`, `enum`, `const`

**Numeric Keywords:**
- `multipleOf`, `minimum`, `maximum`
- `exclusiveMinimum`, `exclusiveMaximum`

**String Keywords:**
- `minLength`, `maxLength`
- `pattern`, `format`

**Array Keywords:**
- `items`, `prefixItems`, `unevaluatedItems`
- `contains`, `minContains`, `maxContains`
- `minItems`, `maxItems`, `uniqueItems`

**Object Keywords:**
- `properties`, `patternProperties`, `additionalProperties`
- `required`, `minProperties`, `maxProperties`
- `dependentRequired`, `dependentSchemas`

**Combinators:**
- `allOf`, `anyOf`, `oneOf`, `not`

**Conditionals:**
- `if`, `then`, `else`

**Formats (via ajv-formats):**
- `date-time`, `date`, `time`
- `email`, `hostname`, `ipv4`, `ipv6`
- `uri`, `uri-reference`, `uri-template`
- `uuid`, `regex`, `json-pointer`

### Example Generation Strategies

**For Strings:**
1. Check for `const` → use it
2. Check for `enum` → use first value
3. Check for `format` → generate format-specific data
4. Check for `pattern` → generate matching string
5. Check field name/description → use semantic generation
6. Fallback to generic example

**For Numbers:**
1. Check for `const` → use it
2. Check for `enum` → use first value
3. Respect `minimum`/`maximum` and `multipleOf`
4. Use semantic defaults based on field name
5. Fallback to reasonable number

**For Arrays:**
1. Respect `minItems`/`maxItems`
2. Generate items using item schema
3. Ensure `uniqueItems` if required
4. Handle `prefixItems` for tuple validation

**For Objects:**
1. Generate all `required` fields
2. Optionally generate some optional fields
3. Respect `dependentRequired` relationships
4. Follow `if/then/else` logic

## Timeline

- **Day 1 (Today):**
  - Install dependencies
  - Create example generator utility
  - Create validator utility
  
- **Day 2:**
  - Integrate into SchemaBuilderNew
  - Add validation UI
  - Test frontend changes
  
- **Day 3:**
  - Backend NJsonSchema integration
  - Update validation service
  - API testing
  
- **Day 4:**
  - End-to-end testing
  - Documentation
  - Bug fixes

## Notes

- Maintain backward compatibility
- Keep Hebrew localization working
- Don't break existing schemas
- Follow project coding standards
- Add comprehensive tests
