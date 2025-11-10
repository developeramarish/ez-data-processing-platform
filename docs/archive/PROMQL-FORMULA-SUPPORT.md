# PromQL Formula Support - Implementation Complete

## Overview
Added comprehensive PromQL support for metric formulas, allowing users to create simple field-based metrics or complex computed metrics using Prometheus Query Language.

## Implementation Details

### Backend Changes

#### 1. New FormulaType Enum
**File:** `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs`

```csharp
public enum FormulaType
{
    Simple = 0,      // Direct field value (e.g., "$.amount")
    PromQL = 1,      // PromQL expression (e.g., "rate(requests[5m])")
    Recording = 2    // Recording rule reference (e.g., "job:requests:rate5m")
}
```

#### 2. Updated MetricConfiguration Model
Added `FormulaType` property:
```csharp
public class MetricConfiguration : Entity
{
    public string Formula { get; set; } = string.Empty;
    public FormulaType FormulaType { get; set; } = FormulaType.Simple;
    // ... other properties
}
```

#### 3. Repository Updates
- Added `FormulaType` to `MetricRepository.UpdateAsync` method
- Ensures formula type persists correctly on updates

#### 4. Controller Updates
- Added `FormulaType` mapping in `Create` and `Update` actions
- Properly transfers formula type between request and entity

### Frontend Changes

#### 1. New FormulaBuilder Component
**File:** `src/Frontend/src/components/metrics/FormulaBuilder.tsx`

A reusable component that provides:
- Three formula type options: Simple, PromQL, Recording
- Template library for common PromQL patterns
- Real-time validation
- PromQL expression helper integration
- Visual feedback for valid/invalid expressions

**Features:**
- **Simple Mode**: Direct field value (default, recommended)
- **PromQL Mode**: 9 pre-built templates + custom expressions
  - Sum, Average, Rate, Increase
  - Count, Max, Min
  - Custom PromQL expression
- **Recording Rule Mode**: Reference to existing Prometheus recording rules

#### 2. Updated WizardStepDetails
**File:** `src/Frontend/src/components/metrics/WizardStepDetails.tsx`

Integrated `FormulaBuilder` component into Step 3 (Metric Details):
- Added import for FormulaBuilder
- Included formula builder below Prometheus type selector
- Handles formula and formulaType state updates

#### 3. Updated Wizard
**File:** `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`

- Added `formulaType` to `WizardData` interface
- Initialize `formulaType: 'simple'` in state
- Load `formulaType` when editing existing metrics
- Include `formulaType` in API payload

## Formula Templates

### Pre-built PromQL Templates

1. **Simple** - Direct field value (default)
   ```promql
   field_name
   ```

2. **Sum** - Total of all values
   ```promql
   sum(field_name)
   ```

3. **Average** - Mean of all values
   ```promql
   avg(field_name)
   ```

4. **Rate** - Per-second rate of change
   ```promql
   rate(field_name[5m])
   ```

5. **Increase** - Increase over time period
   ```promql
   increase(field_name[5m])
   ```

6. **Count** - Number of samples
   ```promql
   count(field_name)
   ```

7. **Max** - Maximum value
   ```promql
   max(field_name)
   ```

8. **Min** - Minimum value
   ```promql
   min(field_name)
   ```

9. **Custom** - User-defined PromQL expression
   - Opens PromQL Expression Helper
   - Full validation support

## User Workflow

### Creating Metric with Formula

1. Navigate to Step 3 (Metric Details)
2. Fill in basic details (name, display name, etc.)
3. Scroll to Formula section
4. Select formula type:
   - **Simple**: For basic metrics (default, recommended)
   - **PromQL**: For computed metrics (aggregations, rates)
   - **Recording**: For referencing pre-computed metrics

5. For PromQL type:
   - Select a template (Sum, Average, Rate, etc.)
   - OR select "Custom" and use the PromQL helper
   - Preview generated expression
   - Validation feedback in real-time

6. Continue to next steps (Labels, Alerts)
7. Save metric

### Validation

- PromQL expressions are validated in real-time
- Checks for:
  - Balanced brackets `[]`, `{}`, `()`
  - Valid metric/function names
  - No double spaces
  - Non-empty expression

- Visual indicators:
  - ✅ Green tag for valid expressions
  - ❌ Red tag for invalid expressions
  - Error message with guidance

## Integration with Existing Features

### Compatible With
✅ Alert Rules - Use the same metric in alert expressions  
✅ Labels - Combine formulas with dynamic labels  
✅ Global Metrics - Apply PromQL formulas globally  
✅ DataSource-Specific Metrics - Use formulas for specific sources  
✅ PromQL Helper Dialog - Reuses existing helper component  

### Field Path Requirement
All metrics still require a `fieldPath` (JSON path to schema field). The formula can use this field path for computations:
- Simple: Uses field value directly
- PromQL: Can aggregate/transform the field
- Recording: Can reference pre-computed metrics based on the field

## API Changes

### Request Models
Both `CreateMetricRequest` and `UpdateMetricRequest` now include:
```csharp
public FormulaType FormulaType { get; set; } = FormulaType.Simple;
```

### API Endpoints
No changes to endpoints - existing endpoints now support the new field:
- `POST /api/v1/metrics` - Create with formula type
- `PUT /api/v1/metrics/{id}` - Update with formula type
- `GET /api/v1/metrics/{id}` - Returns formula type

## Examples

### Example 1: Simple Metric
```json
{
  "name": "total_amount",
  "fieldPath": "$.amount",
  "formula": "$.amount",
  "formulaType": "simple"
}
```

### Example 2: Aggregated Metric
```json
{
  "name": "avg_response_time",
  "fieldPath": "$.response_time",
  "formula": "avg(response_time)",
  "formulaType": "promql"
}
```

### Example 3: Rate Calculation
```json
{
  "name": "request_rate",
  "fieldPath": "$.requests",
  "formula": "rate(requests[5m])",
  "formulaType": "promql"
}
```

### Example 4: Custom Complex Formula
```json
{
  "name": "error_percentage",
  "fieldPath": "$.errors",
  "formula": "sum(rate(errors[5m])) / sum(rate(requests[5m])) * 100",
  "formulaType": "promql"
}
```

## Testing

### Test Scenarios
1. ✅ Create metric with simple formula
2. ✅ Create metric with PromQL template
3. ✅ Create metric with custom PromQL
4. ✅ Edit existing metric and change formula type
5. ✅ Validate PromQL expressions
6. ✅ Formula persistence to MongoDB

### Test Commands
```powershell
# Test metric creation with PromQL formula
.\test-metric-with-alerts.ps1

# Test metric update with formula
.\test-update-metric-with-alerts.ps1
```

## Future Enhancements

### Phase 2 (Optional)
- [ ] Server-side PromQL validation using Prometheus API
- [ ] Formula preview with sample data
- [ ] Export metrics as Prometheus recording rules (YAML)
- [ ] Formula testing/debugging interface
- [ ] Integration with actual Prometheus instance

### Phase 3 (Future)
- [ ] Formula performance metrics
- [ ] Formula version history
- [ ] Formula sharing/templates library
- [ ] AI-assisted formula suggestions

## Benefits

### For Users
✅ **Simple by default** - Simple formula type for basic use cases  
✅ **Powerful when needed** - Full PromQL support for advanced users  
✅ **Template library** - Pre-built formulas for common patterns  
✅ **Validation** - Real-time feedback on expression validity  
✅ **Helper tools** - PromQL expression helper for guidance  

### For System
✅ **Prometheus-native** - Aligns with Prometheus architecture  
✅ **Performance** - Can leverage recording rules  
✅ **Flexibility** - Supports simple to complex use cases  
✅ **Extensible** - Easy to add new templates  

## Migration Notes

### Existing Metrics
- Existing metrics without `formulaType` will default to `Simple`
- Existing `formula` values are preserved
- No breaking changes to existing functionality

### Backward Compatibility
✅ All existing metrics continue to work  
✅ API responses include new fields  
✅ UI gracefully handles missing fields  

## Status
**IMPLEMENTATION: COMPLETE** ✅

- Backend model: ✅ Complete
- Repository: ✅ Complete  
- Controller: ✅ Complete
- Frontend component: ✅ Complete
- Wizard integration: ✅ Complete
- Validation: ✅ Complete
- Documentation: ✅ Complete

## Files Modified

### Backend (3 files)
1. `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs`
2. `src/Services/MetricsConfigurationService/Repositories/MetricRepository.cs`
3. `src/Services/MetricsConfigurationService/Controllers/MetricController.cs`

### Frontend (3 files)
1. `src/Frontend/src/components/metrics/FormulaBuilder.tsx` (NEW)
2. `src/Frontend/src/components/metrics/WizardStepDetails.tsx`
3. `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`

## Related Features
- Alert Rules with PromQL expressions
- PromQL Expression Helper Dialog
- Formula Template Library (existing)
- Metric Name Helper

---
**Date:** October 19, 2025  
**Version:** 1.0  
**Status:** Production Ready
