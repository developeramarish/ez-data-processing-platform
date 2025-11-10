# Option A Implementation: Simplify to Pure Prometheus
## Align with Prometheus Standards + Add Schema Field Integration

**Date:** October 19, 2025  
**Decision:** User chose Option A - Simplify metrics to pure Prometheus pattern

---

## Changes to Implement

### 1. Simplified Metric Form (Remove Complexity)

**Remove from MetricConfigurationForm:**
- ❌ Formula Template Library tab
- ❌ Visual Formula Builder tab
- ❌ Manual formula entry tab
- ❌ FilterConditionBuilder component
- ❌ AggregationHelper component

**Keep Simple Form:**
```
Basic Information:
- Name (machine name)
- Display Name (Hebrew)
- Description
- Category

Prometheus Configuration:
- Type: Counter/Gauge/Histogram/Summary
- Data Source (dropdown)
- Field to Measure (from schema!) ← NEW
- Prometheus Labels (from schema fields) ← NEW

Settings:
- Retention period
- Status (Draft/Active/Inactive)
```

---

### 2. Add Schema Field Integration (Critical Feature)

**New Component:** `SchemaFieldSelector.tsx`

**Functionality:**
```typescript
// When user selects data source "Bank Transactions (ds001)"
const handleDataSourceChange = async (dataSourceId: string) => {
  // Fetch schema from DataSourceManagementService
  const response = await fetch(`http://localhost:5001/api/v1/schema?dataSourceId=${dataSourceId}`);
  const data = await response.json();
  
  if (data.isSuccess && data.data.length > 0) {
    const schema = data.data[0];
    const fields = extractFieldsFromJsonSchema(schema.JsonSchemaContent);
    setAvailableFields(fields);
  }
};

// Extract fields from JSON Schema
const extractFieldsFromJsonSchema = (jsonSchemaContent: string) => {
  const schema = JSON.parse(jsonSchemaContent);
  const properties = schema.properties || {};
  
  return Object.entries(properties).map(([name, prop]: [string, any]) => ({
    name,
    type: prop.type,
    description: prop.description,
    enum: prop.enum,
    format: prop.format,
    isNumeric: ['number', 'integer'].includes(prop.type)
  }));
};
```

**UI:**
```
Data Source: [Bank Transactions ▼]
  ↓ (fetches schema)
  
Field to Measure: [transaction_amount ▼]
  Options from schema:
  - transaction_amount (number) - סכום עסקה
  - customer_id (string) - מזהה לקוח
  - payment_method (string) - אמצעי תשלום
  - transaction_date (date) - תאריך עסקה
  - status (string) - סטטוס [completed, pending, failed]
```

---

### 3. Update Backend Model

**Simplify MetricConfiguration:**
```csharp
public class MetricConfiguration : Entity
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    
    // Prometheus-specific fields
    public string PrometheusType { get; set; } // "Counter", "Gauge", "Histogram", "Summary"
    public string Scope { get; set; } // "global" or "datasource-specific"
    public string? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    
    // Field configuration
    public string? FieldToMeasure { get; set; } // "transaction_amount"
    public List<string>? PrometheusLabels { get; set; } // ["payment_method", "status"]
    
    // Metadata
    public string? Retention { get; set; }
    public int Status { get; set; }
    
    // Remove these:
    // ❌ Formula
    // ❌ FieldPath
    // ❌ Complex aggregation settings
}
```

---

### 4. Move Complex Components to Dashboard (Week 3)

**Keep for later:**
- FormulaTemplateLibrary.tsx → Dashboard query builder
- VisualFormulaBuilder.tsx → PromQL query constructor
- FilterConditionBuilder.tsx → Dashboard filter panel
- AggregationHelper.tsx → Dashboard aggregation settings

**These will be reused in Week 3** when building Grafana dashboard integration.

---

## Implementation Steps

### Step 1: Create Simplified Metric Form
**File:** `src/Frontend/src/pages/metrics/MetricConfigurationFormSimplified.tsx`

**Sections:**
1. Basic Info (Name, Display, Description, Category)
2. Prometheus Type Selector (4 types with explanations)
3. Data Source Selector
4. Schema Field Selector (fetches from API)
5. Prometheus Labels (multi-select from schema fields)
6. Settings (Retention, Status)

### Step 2: Create Schema Field Selector Component
**File:** `src/Frontend/src/components/metrics/SchemaFieldSelector.tsx`

**Features:**
- Fetches schema when data source changes
- Parses JSON Schema properties
- Shows field type (number/string/date)
- Shows enum values for string fields
- Filters numeric fields for Counter/Gauge/Histogram

### Step 3: Update Routes
**File:** `src/Frontend/src/App.tsx`
- Change from MetricConfigurationForm to MetricConfigurationFormSimplified

### Step 4: Update Backend (Optional)
**File:** `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs`
- Add PrometheusType field
- Add FieldToMeasure field
- Rename Labels to PrometheusLabels
- Remove Formula field (or keep for backward compat)

### Step 5: Test End-to-End
1. Select data source → Schema fields load ✅
2. Pick field → Shows correct type ✅
3. Select labels → From schema fields ✅
4. Save → Persists to MongoDB ✅
5. Verify → Simpler, cleaner, Prometheus-aligned ✅

---

## Benefits of Option A

### For Users:
✅ Simpler form (no overwhelming options)
✅ Clear: "What do you want to track?"
✅ Schema field validation (no typos)
✅ Prometheus-standard approach

### For Developers:
✅ Cleaner code architecture
✅ Separation of concerns (metrics vs. queries)
✅ Components reusable in Week 3
✅ Easier to maintain

### For System:
✅ Aligned with OpenTelemetry/.NET Meter API
✅ Compatible with Prometheus/Grafana
✅ Correct instrumentation pattern
✅ Scalable architecture

---

## Timeline

**Estimated Time:** 2-3 hours

**Tasks:**
1. Create simplified form (1 hour)
2. Add schema field selector (1 hour)
3. Update routes and test (30 min)
4. Documentation (30 min)

---

## What Happens to Existing Work?

**Formula Builders (Days 2-4):**
- ✅ Kept in codebase
- ✅ Moved to dashboard folder for Week 3
- ✅ Will be reused for PromQL query construction
- ✅ Nothing wasted!

**Backend:**
- ✅ Existing metrics still work
- ✅ Backward compatible
- ✅ Can support both patterns

---

**Status:** Ready to implement Option A
**Next:** Build simplified form with schema integration
