# Schema Enhancements - Step-by-Step Testing Plan

Based on docs/SCHEMA-ENHANCEMENTS-PLAN.md

## Testing Approach
- Test one feature at a time
- Fix any issues found
- Get user approval before proceeding
- No final reports until all features verified

## Current Issues to Fix First

### Issue 1: Smart Generator Not Using All Fields
**Problem:** Generated example only shows required fields, not all schema properties
**Expected:** Should generate examples for ALL properties in schema
**Test:** Create schema with optional fields and verify they appear in example

### Issue 2: Validation Error on reportDate
**Problem:** Pattern `${2}[0-9]-{2}[0-9]-{4}[0-9]` fails validation
**Expected:** Generated date should match pattern or pattern should be standard regex
**Test:** Verify date generation matches actual schema pattern

### Issue 3: JSON Schema Features Not Visible in UI
**Problem:** User can't see features like minLength, maxLength, pattern, format in jsonjoy-builder
**Expected:** jsonjoy-builder should show all constraint fields
**Test:** Verify jsonjoy-builder exposes all JSON Schema Draft 2020-12 features

## Test Plan by Feature Category

### Category 1: String Constraints

#### Test 1.1: minLength / maxLength
- [ ] Create string field with minLength: 5, maxLength: 20
- [ ] Generate example
- [ ] Verify length is between 5-20 characters
- [ ] Validate example passes
- [ ] **Status:** Not tested

#### Test 1.2: Pattern Validation
- [ ] Create string with pattern for phone: `^05\\d{8}$`
- [ ] Generate example
- [ ] Verify matches pattern (e.g., 050-1234567 or 051234567)
- [ ] Validate example passes
- [ ] **Status:** Not tested

#### Test 1.3: Format Support
Test each format:
- [ ] email - should generate valid email
- [ ] date - should generate valid date (YYYY-MM-DD)
- [ ] date-time - should generate valid ISO datetime
- [ ] uri - should generate valid URI
- [ ] uuid - should generate valid UUID
- [ ] ipv4 - should generate valid IPv4
- [ ] ipv6 - should generate valid IPv6
- [ ] **Status:** Not tested

#### Test 1.4: Enum Values
- [ ] Create string with enum: ["option1", "option2", "option3"]
- [ ] Generate example
- [ ] Verify uses first enum value
- [ ] **Status:** Not tested

#### Test 1.5: Const Value
- [ ] Create string with const: "fixed-value"
- [ ] Generate example
- [ ] Verify uses exact const value
- [ ] **Status:** Not tested

### Category 2: Number Constraints

#### Test 2.1: Minimum / Maximum
- [ ] Create number with minimum: 10, maximum: 100
- [ ] Generate example
- [ ] Verify value is between 10-100
- [ ] **Status:** Not tested

#### Test 2.2: Exclusive Minimum / Maximum
- [ ] Create number with exclusiveMinimum: 0, exclusiveMaximum: 100
- [ ] Generate example
- [ ] Verify value is > 0 and < 100 (not equal)
- [ ] **Status:** Not tested

#### Test 2.3: MultipleOf
- [ ] Create number with multipleOf: 5
- [ ] Generate example
- [ ] Verify value is multiple of 5
- [ ] **Status:** Not tested

#### Test 2.4: Integer Type
- [ ] Create integer field
- [ ] Generate example
- [ ] Verify value is whole number (no decimals)
- [ ] **Status:** Not tested

### Category 3: Array Constraints

#### Test 3.1: minItems / maxItems
- [ ] Create array with minItems: 2, maxItems: 5
- [ ] Generate example
- [ ] Verify array has 2-5 items
- [ ] **Status:** Not tested

#### Test 3.2: uniqueItems
- [ ] Create array with uniqueItems: true
- [ ] Generate example
- [ ] Verify all items are unique
- [ ] **Status:** Not tested

#### Test 3.3: Item Schema
- [ ] Create array with typed items (e.g., array of strings)
- [ ] Generate example
- [ ] Verify all items match type
- [ ] **Status:** Not tested

### Category 4: Object Constraints

#### Test 4.1: Required Fields
- [ ] Create object with required: ["field1", "field2"]
- [ ] Generate example
- [ ] Verify all required fields present
- [ ] **Status:** Not tested

#### Test 4.2: Optional Fields  
- [ ] Create object with optional fields
- [ ] Generate example with includeOptional: true
- [ ] Verify some optional fields included
- [ ] **Status:** Not tested

#### Test 4.3: Nested Objects
- [ ] Create object with nested object property
- [ ] Generate example
- [ ] Verify nested structure created
- [ ] **Status:** Not tested

#### Test 4.4: dependentRequired
- [ ] Create schema with dependentRequired relationships
- [ ] Generate example
- [ ] Verify dependent fields appear when trigger field present
- [ ] **Status:** Not tested

### Category 5: Combinators

#### Test 5.1: allOf
- [ ] Create schema with allOf (merge multiple schemas)
- [ ] Generate example
- [ ] Verify example satisfies all sub-schemas
- [ ] **Status:** Not tested

#### Test 5.2: anyOf
- [ ] Create schema with anyOf
- [ ] Generate example
- [ ] Verify uses first option
- [ ] **Status:** Not tested

#### Test 5.3: oneOf
- [ ] Create schema with oneOf
- [ ] Generate example
- [ ] Verify uses exactly one option
- [ ] **Status:** Not tested

### Category 6: Semantic Detection

#### Test 6.1: Field Name - Email
- [ ] Create field named "email" or "דוא״ל"
- [ ] Generate example
- [ ] Verify generates valid email (user@example.com)
- [ ] **Status:** Not tested

#### Test 6.2: Field Name - Phone
- [ ] Create field named "phone" or "טלפון"
- [ ] Generate example
- [ ] Verify generates Israeli phone (050-1234567)
- [ ] **Status:** Not tested

#### Test 6.3: Field Name - Date
- [ ] Create field named "reportDate" or "תאריך"
- [ ] Generate example
- [ ] Verify generates date in correct format
- [ ] **Status:** TESTED - Partially working (generates 15-01-2025 but validation fails)

#### Test 6.4: Field Name - Name
- [ ] Create field named "name" or "שם"
- [ ] Generate example
- [ ] Verify generates Hebrew name
- [ ] **Status:** Not tested

### Category 7: Validation Features

#### Test 7.1: Valid Example Detection
- [ ] Create valid schema
- [ ] Generate example
- [ ] Verify shows green success indicator
- [ ] Verify shows "האימות עבר בהצלחה"
- [ ] **Status:** Not tested

#### Test 7.2: Invalid Example Detection
- [ ] Create schema with pattern
- [ ] Modify generator to produce invalid example
- [ ] Verify shows red error indicator
- [ ] Verify shows Hebrew error message
- [ ] **Status:** TESTED - Working (shows pattern error)

#### Test 7.3: Field-Level Errors
- [ ] Create complex schema with multiple validation rules
- [ ] Generate invalid example
- [ ] Verify each field error listed separately
- [ ] Verify field names shown in tags
- [ ] **Status:** TESTED - Working

### Category 8: Template Library

#### Test 8.1: Template Display
- [ ] Open template library
- [ ] Verify all 6 templates shown
- [ ] Verify categories (CRM, Validation, E-Commerce, Finance, HR, Forms)
- [ ] **Status:** TESTED - Working perfectly

#### Test 8.2: Template Search
- [ ] Search for "לקוח" (customer)
- [ ] Verify only relevant templates shown
- [ ] Clear search
- [ ] **Status:** Not tested

#### Test 8.3: Template Selection
- [ ] Select "פרופיל לקוח" template
- [ ] Verify schema editor populated with template
- [ ] Verify all template fields present
- [ ] **Status:** Not tested

#### Test 8.4: Category Filter
- [ ] Click "ניהול לקוחות" category
- [ ] Verify only CRM templates shown
- [ ] Clear filter
- [ ] **Status:** Not tested

## Test Execution Order

1. **Fix Current Issues (Priority)**
   - Fix Issue 1: Generator includes all fields
   - Fix Issue 2: Date pattern validation
   - Verify Issue 3: Check jsonjoy-builder features

2. **Category-by-Category Testing**
   - Start with Category 1 (String Constraints)
   - Get approval after each category
   - Fix any issues before moving to next category

3. **Integration Testing**
   - Test combinations of features
   - Test with complex schemas
   - Test Hebrew-specific scenarios

## Next Steps

1. Fix the three identified issues
2. Begin systematic testing starting with Category 1
3. Get your approval after each test category before proceeding
4. Document all findings and fixes

**Ready to start? I'll begin by fixing the current issues, then test Category 1 (String Constraints).**
