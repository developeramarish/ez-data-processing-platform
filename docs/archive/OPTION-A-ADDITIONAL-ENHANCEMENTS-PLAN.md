# Option A - Additional Enhancements Plan

**Date:** 2025-10-19  
**Status:** 📋 PLANNING  
**Previous Work:** OPTION-A-ENHANCEMENTS-COMPLETE.md

## New Requirements

### 1. Labels as Free Text Input ✏️
**Current:** Dropdown selecting schema fields for labels  
**Requested:** Free text input with Prometheus syntax validation

**Changes Needed:**
- Replace multi-select dropdown with Input/TextArea for labels
- Add validation for Prometheus label format: `label_name="value"`
- Support comma-separated labels: `status="active", region="north"`
- Validation regex: `^[a-zA-Z_][a-zA-Z0-9_]*="[^"]*"(,\s*[a-zA-Z_][a-zA-Z0-9_]*="[^"]*")*$`
- Show examples and helper text
- Real-time validation feedback

### 2. One Field Per Metric (Required) ⚠️
**Current:** Optional field selection  
**Requested:** Mandatory single field selection as metric value source

**Rationale:**
- Counter: Increments based on field changes
- Gauge: Takes value directly from field
- Histogram: Distributes field values into buckets
- Summary: Calculates percentiles from field values

**Changes Needed:**
- Make field selection REQUIRED for all Prometheus types
- Add clear explanation per type about how field is used
- Remove "optional" state - must select exactly one field
- Update form validation

### 3. Form Flow Improvements 🔄
**Current:** Fill all fields, then select data source, then schema loads  
**Requested:** Better wizard-like flow

**Proposed Flow:**
```
Step 1: Select Data Source (for specific metrics)
  ↓
Step 2: Schema loads → Select Field
  ↓
Step 3: Auto-suggest metric name based on field
  ↓
Step 4: Fill remaining details (description, category, etc.)
```

**Changes Needed:**
- Reorganize form order
- Add step indicators
- Clear field values when data source changes
- Auto-populate metric name suggestion
- Better loading states

### 4. Hebrew to English Metric Name Helper 🔤
**Requested:** Tool to translate Hebrew descriptions to Prometheus-compliant English names

**Implementation Options:**

#### Option A: Real-time Translation Helper
```typescript
<Input.Group compact>
  <Input 
    style={{ width: '50%' }} 
    placeholder="תאר את המדד בעברית"
    onChange={(e) => translateToMetricName(e.target.value)}
  />
  <Input 
    style={{ width: '50%' }}
    value={suggestedMetricName}
    placeholder="metric_name_suggestion"
  />
</Input.Group>
```

**Translation Rules:**
- Hebrew → English using transliteration or semantic mapping
- Apply snake_case
- Ensure Prometheus compliance: `^[a-z][a-z0-9_]*$`
- Add common prefixes based on Prometheus type and category

#### Option B: MCP Server Integration
- Use Exa search to find similar metric names
- Use Context7 for Prometheus naming conventions
- Suggest based on field name + category

#### Option C: Built-in Suggestions Library
```typescript
const metricSuggestions = {
  'עסקאות': 'transactions',
  'מכירות': 'sales',
  'לקוחות': 'customers',
  'סכום': 'amount',
  'מונה': 'count',
  // ...
};
```

## Implementation Priority

### Phase 1: Critical Fixes (High Priority)
1. **Labels as Free Text** - Changes data model, needs backend update too
2. **Required Field Selection** - Important for metric correctness

### Phase 2: UX Improvements (Medium Priority)
3. **Form Flow Reorganization** - Improves user experience
4. **Metric Name Helper** - Nice to have, can be simple version first

## Technical Considerations

### Backend Changes Needed

1. **Labels Field Type Change:**
```csharp
// Current: string[] labels
// Proposed: string labelsExpression
public string LabelsExpression { get; set; } // e.g., "status=\"active\", region=\"north\""
```

2. **Field Path Validation:**
```csharp
// Make required for specific metrics
[Required(ErrorMessage = "Field path is required for data source-specific metrics")]
public string FieldPath { get; set; }
```

### Frontend Changes Needed

1. **SchemaFieldSelector Component:**
- Change labels from multi-select to text input
- Add Prometheus syntax validation
- Show validation errors inline

2. **MetricConfigurationFormSimplified:**
- Reorganize form order
- Add metric name suggestion component
- Add field requirement validation
- Improve state management

3. **New Component: MetricNameHelper:**
```typescript
interface MetricNameHelperProps {
  hebrewDescription: string;
  fieldName?: string;
  category?: string;
  prometheusType?: string;
  onSuggestion: (name: string) => void;
}
```

## Estimated Effort

- **Labels Free Text:** 2-3 hours (frontend + backend)
- **Required Field:** 1 hour (validation logic)
- **Form Flow Reorganization:** 3-4 hours (major restructure)
- **Metric Name Helper:** 2-3 hours (simple) or 5-6 hours (with MCP integration)

**Total:** 8-16 hours depending on approach

## Recommendations

1. **Start with Labels Free Text** - Most critical for Prometheus compliance
2. **Add Field Requirement** - Quick win for data quality
3. **Form Flow** - Can be iterative improvement
4. **Helper Tool** - Start simple, enhance later

## Questions to Clarify

1. **Labels Syntax:** Should we support PromQL label syntax or just label names?
   - Full syntax: `{status="active", region="north"}`  
   - Or just names: `status, region` (values come from data)

2. **Field Selection:** Should numeric fields be required only for numeric metric types?
   - Counter/Gauge/Histogram/Summary: Numeric required
   - Or any field type for any metric type?

3. **Metric Name Helper:** Which implementation approach do you prefer?
   - Simple transliteration
   - MCP-based intelligent suggestion
   - Built-in dictionary

4. **Form Flow:** Should this be a multi-step wizard or keep single-page with better organization?

Please provide guidance on these questions so I can implement the enhancements according to your preferences.
