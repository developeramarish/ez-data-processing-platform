# Metrics Implementation - Simplification Analysis
## Based on Prometheus & Grafana Best Practices

**Date:** October 16, 2025  
**Research Sources:** Prometheus.io, Grafana docs, Context7, Industry examples

---

## Executive Summary

**Finding:** We mixed metric instrumentation (what to track) with querying (how to view).

**Impact:** Overcomplicated UI, confusing users, not aligned with Prometheus standards.

**Recommendation:** Simplify to pure Prometheus pattern OR clearly separate concerns.

---

## What Prometheus Metrics Actually Are

### The Simple Truth
A Prometheus metric has only:
1. **Name:** `business_transactions_total`
2. **Type:** Counter, Gauge, Histogram, or Summary
3. **Labels:** Key-value pairs `{payment_method="credit", status="completed"}`
4. **Value:** A number that gets recorded

### Example (Real Prometheus):
```csharp
// Define metric (at application startup)
var counter = meter.CreateCounter<long>("business_transactions_total",
  description: "Total number of business transactions"
);

// Record value when event happens (at runtime)
counter.Add(1,
  new KeyValuePair<string, object>("payment_method", "credit_card"),
  new KeyValuePair<string, object>("status", "completed"),
  new KeyValuePair<string, object>("data_source", "bank_api")
);
```

**That's it.** No formulas. No WHERE. No GROUP BY. No aggregations.

---

## What We Built (The Problem)

### Current Implementation
Our "metric" includes:
- ❌ Formulas: `SUM(amount) WHERE status="completed"`
- ❌ Aggregation functions: SUM, AVG, COUNT, MEDIAN
- ❌ Filter conditions: WHERE, AND, OR operators
- ❌ Time windows: daily, weekly, monthly
- ❌ GROUP BY: field grouping

### The Confusion
**These are PromQL QUERIES, not metric definitions!**

**Example of confusion:**
```
Our "metric":
SUM(amount) WHERE status="completed" GROUP BY DATE(timestamp) WINDOW 1d

Reality:
- Metric: business_transactions_total (just increments!)
- Query:  sum(rate(business_transactions_total{status="completed"}[1d]))
```

---

## How It Should Work (Industry Standard)

### Phase 1: Metric Instrumentation (Our App)
**User configures WHAT to track:**
```
Metric Name: business_daily_sales
Type: Counter
Data Source: Bank Transactions
Field: transaction_amount (from schema!)
Labels:
  - payment_method (from schema field)
  - currency (from schema field)
  - status (from schema field)
```

**Backend generates code:**
```csharp
var counter = meter.CreateCounter<long>("business_daily_sales");

// When processing data:
foreach (var record in transactions) {
  counter.Add(record.TransactionAmount,
    new KeyValuePair<string, object>("payment_method", record.PaymentMethod),
    new KeyValuePair<string, object>("currency", record.Currency),
    new KeyValuePair<string, object>("status", record.Status)
  );
}
```

### Phase 2: Querying (Grafana Dashboards - Week 3)
**User creates dashboard panel in Grafana with PromQL:**
```promql
# Daily sales by payment method
sum by (payment_method) (
  rate(business_daily_sales{status="completed"}[1d])
)

# Average transaction value
sum(business_daily_sales) / sum(business_transaction_count)
```

---

## Critical Missing Feature: Schema Field Integration

### Your Observation ✅
**You said:** "Metrics that are not global should use specific fields of a data source, the editor should allow to select the correct fields"

**You're 100% correct!**

### Current (Wrong):
```typescript
// Mock generic fields
const fields = ['amount', 'quantity', 'price', 'status'];
```

User has no idea if these fields exist in their data source.

### Correct:
```typescript
// When user selects data source "Bank Transactions (ds001)"
// Fetch its schema from SchemaManagementService
const schema = await schemaApi.getByDataSourceId('ds001');

// Extract fields from JSON Schema
const fields = [
  { name: 'transaction_id', type: 'string' },
  { name: 'transaction_amount', type: 'number' },
  { name: 'customer_id', type: 'string' },
  { name: 'payment_method', type: 'string', enum: ['credit', 'debit', 'cash'] },
  { name: 'transaction_date', type: 'datetime' },
  { name: 'status', type: 'string', enum: ['completed', 'pending', 'failed'] }
];

// Show THESE in dropdown
```

**Benefits:**
- ✅ User sees ACTUAL fields from their data
- ✅ Validation: field exists
- ✅ Type safety: know if field is number/string
- ✅ Enum values: show payment method options
- ✅ No typos: dropdowns prevent errors

---

## Recommendations

### Option A: Simplify to Pure Prometheus (Recommended)

**Metric Form Simplified:**
```
┌─────────────────────────────────────────────────┐
│ Create Metric                                   │
├─────────────────────────────────────────────────┤
│ Name: business_transactions_total               │
│ Display Name: סך עסקאות                        │
│ Description: [text area]                        │
│                                                 │
│ Type: [Counter ▼] [Gauge] [Histogram]          │
│ ℹ️ Counter: Only increases (events, requests)   │
│                                                 │
│ Data Source: [Bank Transactions ▼]             │
│ Field to Measure: [transaction_amount ▼]       │
│   ↑ Fetched from schema!                        │
│                                                 │
│ Prometheus Labels:                              │
│ ☑ payment_method (from schema)                  │
│ ☑ status (from schema)                          │
│ ☑ currency (from schema)                        │
│                                                 │
│ [Cancel] [Save Metric]                          │
└─────────────────────────────────────────────────┘
```

**Remove entirely:**
- Formula builders
- Filter conditions
- Aggregation helpers

**Move to Week 3:**
- Formula/Filter/Aggregation → Dashboard panel configuration
- PromQL query builder → When creating Grafana panels

---

### Option B: Hybrid - Keep But Clarify

**Rename to "Calculated Metrics":**
```
Metric Types:
  [●] Simple Metric
      → Just track events (pure Prometheus)
  [ ] Calculated Metric
      → Pre-aggregated value (custom calculation)
```

**Simple Metric Form:**
- Type, Data Source, Field, Labels (simple!)

**Calculated Metric Form:**
- All our current builders
- Clarify: "This runs periodically and stores result"

**Still add schema integration for both!**

---

### Option C: Just Add Schema Integration

**Keep everything we built, but:**
1. Fetch schema fields when data source selected
2. Replace mock fields with real schema fields
3. Add field type indicators (number/string)
4. Show enum values for string fields

**Smallest change, addresses your main concern.**

---

## Implementation Plan (Option A - Recommended)

### 1. Simplify MetricConfigurationForm
**Remove:**
- FormulaTemplateLibrary tab
- VisualFormulaBuilder tab  
- FilterConditionBuilder component
- AggregationHelper component

**Keep:**
- Basic info fields
- Prometheus type selector (Counter/Gauge/Histogram/Summary)
- Data source selector
- **Schema field selector** ← NEW!
- Labels from schema fields ← NEW!

### 2. Add Schema Field Fetcher
**New component:** `SchemaFieldSelector.tsx`
```typescript
// Fetches schema when data source selected
const fetchSchemaFields = async (dataSourceId) => {
  const response = await fetch(`http://localhost:5001/api/v1/schema?dataSourceId=${dataSourceId}`);
  const data = await response.json();
  if (data.isSuccess && data.data.length > 0) {
    const schema = data.data[0];
    return extractFieldsFromJsonSchema(schema.jsonSchemaContent);
  }
  return [];
};
```

### 3. Extract Fields from JSON Schema
```typescript
const extractFieldsFromJsonSchema = (jsonSchemaContent: string) => {
  const schema = JSON.parse(jsonSchemaContent);
  const properties = schema.properties || {};
  
  return Object.entries(properties).map(([name, prop]: [string, any]) => ({
    name,
    type: prop.type,
    description: prop.description,
    enum: prop.enum,
    format: prop.format
  }));
};
```

### 4. Update Backend
**MetricConfiguration entity:**
```csharp
public string PrometheusType { get; set; } // "Counter", "Gauge", "Histogram", "Summary"
public string FieldToMeasure { get; set; } // "transaction_amount"
public List<string> PrometheusLabels { get; set; } // ["payment_method", "status"]
```

**Remove:**
- Formula field
- Complex aggregation settings

---

## Comparison Table

| Feature | Current | Option A (Simple) | Option B (Hybrid) | Option C (Just Schema) |
|---------|---------|-------------------|-------------------|----------------------|
| **Complexity** | High | Low ✅ | Medium | High |
| **Prometheus Aligned** | No | Yes ✅ | Partial | No |
| **Schema Integration** | No | Yes ✅ | Yes ✅ | Yes ✅ |
| **User-Friendly** | Complex | Simple ✅ | Moderate | Complex |
| **Grafana Integration** | Unclear | Clear (Week 3) | Clear | Unclear |
| **Implementation Time** | Done | 2-3 hours | 3-4 hours | 1 hour |

---

## Recommendation: Option A

**Why:**
1. ✅ Aligns with Prometheus standards
2. ✅ Simple for users (just pick what to track)
3. ✅ Adds schema field integration (your key request)
4. ✅ Clear separation: metrics vs. dashboards
5. ✅ Formula builders still useful in Week 3 (dashboard queries)

**What changes:**
- Simplify form (remove 3 helper components)
- Add schema field fetching (your main ask)
- Keep components for Week 3 reuse
- Update documentation

---

## Next Steps

**Please choose:**
- **A** - Simplify to pure Prometheus (2-3 hours work)
- **B** - Hybrid approach (3-4 hours)
- **C** - Just add schema integration (1 hour)

I'll implement your chosen approach immediately.
