# Metrics Configuration - New Issues Fixes Status

## Date: October 20, 2025, 10:44 AM
## Status: 2 of 3 Issues Fixed, 1 Requires Backend Investigation

---

## Summary

After the initial three issues were fixed, three new issues were reported. Here's the status:

| Issue | Status | Details |
|-------|--------|---------|
| 1. Save button not working | ✅ FIXED | Added formulaType conversion |
| 2. Dropdown text overflow | ✅ FIXED | Added optionLabelProp |
| 3. Add datasource-specific metrics | ⚠️ BLOCKED | Schema assignment backend issue |

---

## Issue 1: Save Button Not Working ✅ FIXED

### Problem
Save button was returning 400 error when editing metrics.

### Root Cause
Frontend was sending `formulaType` as a string ("simple", "promql", "recording"), but backend expects an integer (0, 1, 2).

Additionally, `UpdateMetricRequest` interface was missing several fields that the backend requires:
- `formulaType`
- `labelNames` 
- `labelsExpression`
- `alertRules`

### Fix Implemented

**File 1:** `src/Frontend/src/services/metrics-api-client.ts`
```typescript
export interface UpdateMetricRequest {
  // ... existing fields ...
  formulaType?: number; // 0=Simple, 1=PromQL, 2=Recording
  labelNames?: string | null;
  labelsExpression?: string | null;
  alertRules?: any[];
  // ... rest of fields ...
}
```

**File 2:** `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`
```typescript
// Convert formulaType string to number for API
const formulaTypeMap = { 'simple': 0, 'promql': 1, 'recording': 2 };
const formulaTypeNumber = formulaTypeMap[wizardData.formulaType] || 0;

const payload = {
  // ...
  formulaType: formulaTypeNumber, // Send as number
  // ...
};
```

### Testing Status
**Needs Testing** - Should be tested in browser after frontend rebuild

---

## Issue 2: Dropdown Text Overflow ✅ FIXED

### Problem
Text in global metrics dropdown was extending outside the control boundaries.

### Root Cause
Dropdown was showing the complex option content (icon + div + nested text) instead of just the label.

### Fix Implemented

**File:** `src/Frontend/src/components/metrics/WizardStepGlobalMetrics.tsx`
```typescript
<Select
  // ... other props ...
  optionLabelProp="label"  // NEW: Display only the label property
  // ...
>
```

This tells the Select component to display only the `label` prop value when option is selected, instead of rendering the entire option content.

### Testing Status
**Needs Testing** - Should be tested in browser after frontend rebuild

---

## Issue 3: Add Datasource-Specific Metrics ⚠️ BLOCKED

### Problem
All datasources have no metrics because:
1. Previous test metrics were deleted (23 inconsistent metrics removed)
2. Only 8 valid global metrics were created
3. No datasource-specific metrics exist

### Investigation Results

**Services Status:**
- ✅ Frontend running on port 3000
- ✅ DataSourceManagementService running on port 5001
- ✅ MetricsConfigurationService running on port 5002
- ✅ All services responding correctly

**Datasources Found:** 6 datasources in database
- ds001: הזנת פרופילי משתמשים
- ds002: הזנת עסקאות מכירות
- ds003: הזנת קטלוג מוצרים
- ds004: הזנת רשומות עובדים
- ds005: הזנת דוחות כספיים
- ds006: הזנת סקרי לקוחות

**Critical Issue Found:** API Response Missing SchemaId

When fetching datasource details via API:
```
curl http://localhost:5001/api/v1/datasource/ds005
```

Response shows NO `SchemaId` field at all, even though the response from earlier curl showed `JsonSchema: {}`.

**This indicates a backend serialization problem** where:
- SchemaId exists in the database (UI displays it)
- SchemaId is NOT being serialized in the API response
- Need to find and fix the DataSource entity model

###Files Modified
1. ✅ `src/Frontend/src/services/metrics-api-client.ts` - Added missing fields
2. ✅ `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx` - Added formulaType conversion
3. ✅ `src/Frontend/src/components/metrics/WizardStepGlobalMetrics.tsx` - Fixed dropdown overflow
4. ✅ `clean-and-seed-valid-metrics.py` - Script to create valid global metrics
5. 📝 `create-datasource-specific-metrics.py` - Script ready but blocked by schema issue

### Scripts Created

#### clean-and-seed-valid-metrics.py
**Status:** ✅ Working
**Purpose:** Delete all metrics and create 8 valid global metrics
**Result:** Successfully created 8 global metrics matching GLOBAL_METRIC_OPTIONS

#### create-datasource-specific-metrics.py  
**Status:** ⚠️ Blocked
**Purpose:** Create datasource-specific metrics based on schemas
**Blocker:** SchemaId not available in API response

---

## Next Steps

### Immediate (Required for Issue 3)
1. **Find DataSource entity model file**
   - Check Repositories folder
   - Check if using MongoDB.Entities
   - Locate the DataSource class definition

2. **Fix SchemaId serialization**
   - Ensure SchemaId property exists in model
   - Check if property is marked with [BsonIgnore] or similar
   - Fix serialization attributes if needed

3. **Verify fix**
   - Restart DataSourceManagementService
   - Test API returns SchemaId
   - Run create-datasource-specific-metrics.py script

### After SchemaId Fix
4. **Create datasource-specific metrics**
   - Run script to generate metrics from actual schemas
   - Mix of simple and PromQL formulas
   - Based on actual numeric fields in schemas

5. **Test all three fixes in browser**
   - Test save button works (Issue 1)
   - Test dropdown displays correctly (Issue 2)
   - Test datasource-specific metrics display (Issue 3)

---

## Current Data State

### Global Metrics (8 total)
All valid and matching GLOBAL_METRIC_OPTIONS:
- file_size_bytes
- files_total
- records_total
- valid_records_total
- invalid_records_total
- processing_duration_seconds
- error_rate_percentage
- data_completeness_percentage

### Datasource-Specific Metrics (0 total)
Waiting for schema assignment fix to generate.

---

## Recommendations

1. **Priority 1:** Fix SchemaId serialization issue in DataSourceManagementService
2. **Priority 2:** Test the two completed fixes (save button + dropdown)
3. **Priority 3:** Generate datasource-specific metrics after SchemaId fix
4. **Priority 4:** Comprehensive end-to-end testing

---

## Technical Notes

### FormulaType Enum Mapping
```
Frontend (string) → Backend (int)
'simple'          → 0
'promql'          → 1
'recording'       → 2
```

### API Response Format  
DataSourceManagementService returns:
```json
{
  "Data": {
    "Items": [ {...}, {...} ]
  }
}
```

MetricsConfigurationService returns:
```json
{
  "isSuccess": true,
  "data": [ {...}, {...} ]
}
```

Different casing and structure between services!

---

## Sign-Off

**Fixes Completed:** 2 of 3  
**Testing Required:** Yes (after frontend rebuild)  
**Blocker:** SchemaId serialization in DataSourceManagementService  
**Next Action:** Find and fix DataSource entity model to include SchemaId in API response
