# Option A - Phase 2 Implementation Plan

**Date:** 2025-10-19  
**Status:** 📋 PLANNING  
**Scope:** Major enhancements with alert rules integration

## Requirements Summary

Based on user feedback, implementing:

1. ✏️ **Labels as Simple Input** → Auto-generate PromQL syntax
2. ⚠️ **Required Field Selection** → All metric types
3. 📋 **Multi-Step Wizard** → Better UX with Steps component
4. 🔤 **Hebrew→English Dictionary** → Metric name suggestions
5. 🚨 **Alert Rules System** → NEW major feature with PromQL expressions

## Phase 2 Architecture

### Component Structure

```
MetricConfigurationWizard (NEW)
├── Step 1: Data Source Selection
│   ├── Tab: Global Metrics
│   └── Tab: Specific Metrics
│       └── DataSourceSelector
├── Step 2: Schema Field Selection (REQUIRED)
│   ├── SchemaFieldSelector (enhanced)
│   ├── Field Type Explanation
│   └── Auto-suggest metric name
├── Step 3: Metric Details
│   ├── MetricNameHelper (NEW)
│   ├── Basic Info Form
│   └── Prometheus Type Selection
├── Step 4: Labels Configuration
│   ├── SimpleLabelInput (NEW)
│   └── PromQLPreview (NEW - read-only)
└── Step 5: Alert Rules (NEW)
    ├── AlertRuleBuilder (NEW)
    ├── ExpressionHelper (NEW)
    └── PromQLExpressionPreview (NEW)
```

## Feature Specifications

### 1. Labels as Simple Input with PromQL Generation

**User Input:**
```
status, region, customer_type
```

**Generated PromQL:**
```promql
{status="$status", region="$region", customer_type="$customer_type"}
```

**Implementation:**
```typescript
interface SimpleLabelInputProps {
  value: string; // "status, region, customer_type"
  onChange: (value: string, promql: string) => void;
  schemaFields: ParsedField[];
}

const generatePromQL = (labels: string): string => {
  const labelArray = labels.split(',').map(l => l.trim()).filter(Boolean);
  const promqlLabels = labelArray.map(label => `${label}="$${label}"`).join(', ');
  return `{${promqlLabels}}`;
};
```

**Validation:**
- Label names must match schema fields (with warning if not)
- Must be valid Prometheus label names: `^[a-zA-Z_][a-zA-Z0-9_]*$`
- Show real-time PromQL preview
- Display warning for non-schema fields

### 2. Required Field Selection

**Per Metric Type:**

**Counter:**
```
Required: Numeric field
Usage: Counts occurrences or increments
Example: transaction_amount → count transactions
Display: "שדה זה ייספור. כל פעם שהשדה משתנה, המדד יעלה."
```

**Gauge:**
```
Required: Numeric field  
Usage: Current value
Example: account_balance → track current balance
Display: "שדה זה ישמש כערך המדד. המדד ישקף את הערך הנוכחי."
```

**Histogram:**
```
Required: Numeric field
Usage: Value distribution
Example: response_time_ms → distribute into buckets
Display: "שדה זה יחולק ל-buckets. מתאים למדידת התפלגות ערכים."
```

**Summary:**
```
Required: Numeric field
Usage: Percentile calculations
Example: processing_time → calculate p50, p95, p99
Display: "שדה זה ישמש לחישוב אחוזונים (percentiles)."
```

### 3. Multi-Step Wizard

```typescript
<Steps current={currentStep} style={{ marginBottom: '24px' }}>
  <Step title="מקור נתונים" description="בחר מקור" icon={<DatabaseOutlined />} />
  <Step title="שדה" description="בחר שדה למדידה" icon={<FieldBinaryOutlined />} />
  <Step title="פרטי מדד" description="הגדר שם ותיאור" icon={<InfoCircleOutlined />} />
  <Step title="תוויות" description="הגדר labels" icon={<TagOutlined />} />
  <Step title="כללי התראה" description="אופציונלי" icon={<AlertOutlined />} />
</Steps>
```

**Navigation:**
- Next/Previous buttons
- Validation per step before advancing
- Save as draft at any step
- Skip alert rules (optional)

### 4. Hebrew→English Metric Name Helper

**Built-in Dictionary:**
```typescript
const hebrewToEnglishDict = {
  // Common business terms
  'עסקה': 'transaction',
  'עסקאות': 'transactions', 
  'מכירה': 'sale',
  'מכירות': 'sales',
  'לקוח': 'customer',
  'לקוחות': 'customers',
  'הזמנה': 'order',
  'הזמנות': 'orders',
  'תשלום': 'payment',
  'תשלומים': 'payments',
  'סכום': 'amount',
  'כמות': 'quantity',
  'מחיר': 'price',
  'יתרה': 'balance',
  'חשבון': 'account',
  'כרטיס': 'card',
  
  // Actions
  'מונה': 'count',
  'סך': 'total',
  'ממוצע': 'average',
  'מקסימום': 'maximum',
  'מינימום': 'minimum',
  'שעתי': 'hourly',
  'יומי': 'daily',
  'חודשי': 'monthly',
  'שנתי': 'yearly',
  
  // Status
  'פעיל': 'active',
  'לא פעיל': 'inactive',
  'ממתין': 'pending',
  'הושלם': 'completed',
  'נכשל': 'failed',
  'בוטל': 'cancelled',
  
  // Metrics
  'זמן תגובה': 'response_time',
  'זמן עיבוד': 'processing_time',
  'שיעור שגיאות': 'error_rate',
  'זמינות': 'availability',
  'ביצועים': 'performance',
};

function suggestMetricName(
  hebrewDescription: string,
  fieldName?: string,
  prometheusType?: string
): string {
  // Tokenize Hebrew input
  const words = hebrewDescription.trim().split(/\s+/);
  
  // Translate each word
  const englishWords = words.map(word => 
    hebrewToEnglishDict[word.toLowerCase()] || transliterate(word)
  );
  
  // Add Prometheus type prefix if applicable
  const prefix = prometheusType === 'counter' ? '' : `${prometheusType}_`;
  
  // Combine: [prefix_][field_name_][description]
  const parts = [
    prefix,
    fieldName ? `${fieldName}_` : '',
    ...englishWords
  ].filter(Boolean);
  
  // Convert to snake_case and ensure Prometheus compliance
  return parts.join('_').toLowerCase().replace(/[^a-z0-9_]/g, '_');
}
```

### 5. Alert Rules System (NEW Major Feature)

**Prometheus Alert Rule Structure:**
```yaml
groups:
  - name: business_alerts
    interval: 1m
    rules:
      - alert: HighTransactionFailureRate
        expr: |
          (
            rate(bank_transactions_failed_total[5m]) 
            / 
            rate(bank_transactions_total[5m])
          ) > 0.05
        for: 5m
        labels:
          severity: warning
          category: business
        annotations:
          summary: "שיעור כשלון עסקאות גבוה"
          description: "{{ $value | humanizePercentage }} מהעסקאות נכשלו ב-5 הדקות האחרונות"
```

**Key Components:**

1. **Alert Name:** English identifier
2. **Expression (expr):** PromQL query
3. **Duration (for):** How long condition must be true
4. **Labels:** Categorization
5. **Annotations:** Hebrew descriptions for users

**PromQL Expression Examples:**

```promql
# Rate of errors
rate(errors_total[5m]) > 10

# Percentage calculation  
(errors_total / requests_total) * 100 > 5

# Comparison between metrics
metric_a > metric_b * 1.5

# Aggregation
sum by (status) (transactions_total) > 1000

# Time-based
increase(sales_total[1h]) < 100
```

**AlertRuleBuilder Component:**

```typescript
interface AlertRule {
  name: string;
  expr: string;
  for: string; // e.g., "5m", "10m", "1h"
  severity: 'info' | 'warning' | 'critical';
  summary: string; // Hebrew
  description: string; // Hebrew with template variables
}

interface AlertRuleBuilderProps {
  metricName: string;
  availableMetrics: MetricConfiguration[]; // All metrics from this data source
  onRuleChange: (rule: AlertRule) => void;
}
```

**Expression Helper Features:**

1. **Template-Based:**
```typescript
const expressionTemplates = {
  'rate_above_threshold': {
    hebrew: 'קצב מעל סף',
    template: 'rate({metric}[{duration}]) > {threshold}',
    params: ['duration', 'threshold'],
    example: 'rate(errors_total[5m]) > 10'
  },
  'percentage': {
    hebrew: 'אחוז',
    template: '({metric_a} / {metric_b}) * 100',
    params: ['metric_a', 'metric_b'],
    example: '(errors_total / requests_total) * 100'
  },
  'comparison': {
    hebrew: 'השוואה בין מדדים',
    template: '{metric_a} {operator} {metric_b}',
    params: ['metric_a', 'operator', 'metric_b'],
    example: 'current_stock < minimum_stock'
  }
};
```

2. **Visual Builder:**
```typescript
<Select placeholder="בחר תבנית ביטוי">
  <Option value="rate_above_threshold">קצב מעל סף</Option>
  <Option value="percentage">אחוז / יחס</Option>
  <Option value="comparison">השוואה</Option>
  <Option value="aggregation">צבירה (sum/avg/max/min)</Option>
  <Option value="custom">ביטוי מותאם אישית</Option>
</Select>

{/* Dynamic parameter inputs based on selected template */}
<Form.Item label="משך זמן (duration)">
  <Select>
    <Option value="1m">דקה</Option>
    <Option value="5m">5 דקות</Option>
    <Option value="15m">15 דקות</Option>
    <Option value="1h">שעה</Option>
  </Select>
</Form.Item>

<Form.Item label="סף (threshold)">
  <InputNumber />
</Form.Item>

{/* Real-time PromQL preview */}
<Card title="ביטוי PromQL שנוצר" type="inner">
  <Typography.Text code>{generatedExpression}</Typography.Text>
</Card>
```

3. **Metrics Selector:**
```typescript
<Select 
  mode="multiple"
  placeholder="בחר מדדים לשימוש בביטוי"
  options={availableMetrics.map(m => ({
    value: m.name,
    label: `${m.displayName} (${m.name})`,
    description: m.description
  }))}
/>
```

## Implementation Steps

### Step 1: Multi-Step Wizard Structure
**Files:** MetricConfigurationWizard.tsx (NEW)
- Install/use Ant Design Steps
- Implement step navigation
- State management across steps
- Validation per step

### Step 2: Required Field Selection
**Files:** SchemaFieldSelector.tsx, MetricConfigurationForm validation
- Add required validation
- Update field selector to be mandatory
- Add explanatory text per Prometheus type
- Show error if not selected

### Step 3: Simple Label Input
**Files:** SimpleLabelInput.tsx (NEW), SchemaFieldSelector.tsx
- Replace multi-select with text input
- Split by comma
- Generate PromQL: `{label1="$label1", label2="$label2"}`
- Display generated syntax
- Validate label names

### Step 4: Metric Name Helper
**Files:** MetricNameHelper.tsx (NEW)
- Built-in Hebrew→English dictionary
- Auto-suggest based on:
  * Hebrew description
  * Selected field name
  * Prometheus type
  * Category
- Show suggestion + allow manual edit
- Validate Prometheus naming: `^[a-z][a-z0-9_]*$`

### Step 5: Alert Rules System
**Files:** 
- AlertRuleBuilder.tsx (NEW)
- ExpressionTemplateSelector.tsx (NEW)
- PromQLExpressionHelper.tsx (NEW)
- Backend: Add AlertRule model

**Sub-steps:**
1. Research Prometheus alert syntax (using web_fetch)
2. Create expression templates library
3. Build visual expression builder
4. Implement PromQL preview
5. Add metrics selector for expressions
6. Integrate with metric form

## Data Model Changes

### Frontend TypeScript

```typescript
// Update MetricConfiguration
interface MetricConfiguration {
  // ... existing fields
  fieldPath: string; // NOW REQUIRED
  labelsExpression?: string; // NEW: Generated PromQL labels
  labelNames?: string; // NEW: Simple comma-separated: "status, region"
  alertRules?: AlertRule[]; // NEW
}

interface AlertRule {
  id?: string;
  name: string; // English identifier
  displayName: string; // Hebrew
  expr: string; // PromQL expression
  for: string; // Duration: "5m", "1h"
  severity: 'info' | 'warning' | 'critical';
  labels: { [key: string]: string };
  annotations: {
    summary: string; // Hebrew
    description: string; // Hebrew with template vars
  };
  enabled: boolean;
}
```

### Backend C# Model

```csharp
public class MetricConfiguration
{
    // ... existing properties
    
    [Required(ErrorMessage = "Field path is required")]
    public string FieldPath { get; set; }
    
    public string? LabelNames { get; set; } // "status, region"
    public string? LabelsExpression { get; set; } // "{status=\"$status\"}"
    
    public List<AlertRule>? AlertRules { get; set; }
}

public class AlertRule
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Expression { get; set; } // PromQL
    public string For { get; set; } // Duration
    public string Severity { get; set; }
    public Dictionary<string, string> Labels { get; set; }
    public Dictionary<string, string> Annotations { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

## Prometheus Alert Rules Reference

### Common PromQL Functions

```promql
# Rate - events per second
rate(metric_name[5m])

# Increase - total increase over time
increase(metric_name[1h])

# Aggregations
sum(metric_name)
avg(metric_name)  
max(metric_name)
min(metric_name)
count(metric_name)

# Aggregations with grouping
sum by (label) (metric_name)
avg by (region, status) (metric_name)

# Comparison operators
>, <, >=, <=, ==, !=

# Arithmetic
metric_a + metric_b
metric_a - metric_b
metric_a * metric_b
metric_a / metric_b

# Boolean operators
and, or, unless

# Time-based
metric_name offset 1h
metric_name[5m:1m] # subquery
```

### Expression Templates Library

```typescript
const alertExpressionTemplates = {
  high_rate: {
    displayName: 'קצב גבוה',
    description: 'התראה כאשר הקצב עולה על סף',
    template: 'rate({metric}[{duration}]) > {threshold}',
    params: {
      duration: { type: 'duration', default: '5m' },
      threshold: { type: 'number', default: 10 }
    },
    example: 'rate(errors_total[5m]) > 10',
    hebrewExample: 'קצב שגיאות מעל 10 לשנייה'
  },
  
  percentage_threshold: {
    displayName: 'אחוז מעל סף',
    description: 'התראה כאשר אחוז עולה על סף',
    template: '({numerator} / {denominator}) * 100 > {threshold}',
    params: {
      numerator: { type: 'metric', description: 'מונה' },
      denominator: { type: 'metric', description: 'מכנה' },
      threshold: { type: 'number', default: 5, suffix: '%' }
    },
    example: '(failed_transactions / total_transactions) * 100 > 5',
    hebrewExample: 'אחוז עסקאות כושלות מעל 5%'
  },
  
  value_comparison: {
    displayName: 'השוואת ערכים',
    description: 'התראה כאשר מדד A חורג ביחס למדד B',
    template: '{metric_a} {operator} {metric_b} {multiplier}',
    params: {
      metric_a: { type: 'metric' },
      operator: { type: 'select', options: ['>', '<', '>=', '<=', '==', '!='] },
      metric_b: { type: 'metric' },
      multiplier: { type: 'number', default: 1, optional: true }
    },
    example: 'current_inventory < minimum_inventory * 1.2',
    hebrewExample: 'מלאי נוכחי נמוך מהמינימום'
  },
  
  absence: {
    displayName: 'היעדר נתונים',
    description: 'התראה כאשר לא התקבלו נתונים',
    template: 'absent({metric})',
    params: {},
    example: 'absent(heartbeat_total)',
    hebrewExample: 'לא התקבל heartbeat'
  },
  
  spike_detection: {
    displayName: 'זיהוי קפיצה',
    description: 'התראה על שינוי פתאומי',
    template: 'abs(delta({metric}[{duration}])) > {threshold}',
    params: {
      duration: { type: 'duration', default: '5m' },
      threshold: { type: 'number', default: 100 }
    },
    example: 'abs(delta(sales_amount[5m])) > 1000',
    hebrewExample: 'קפיצה של מעל 1000 בסכום מכירות'
  }
};
```

## UI Mockups

### Step 1: Data Source (Specific Metrics Tab)
```
┌─────────────────────────────────────┐
│ [Global] [📊 Specific] ← Tabs       │
├─────────────────────────────────────┤
│ 🔹 בחר מקור נתונים                  │
│ ┌─────────────────────────────────┐ │
│ │ [▼] בנק לאומי - עסקאות          │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [הבא >]                             │
└─────────────────────────────────────┘
```

### Step 2: Field Selection
```
┌─────────────────────────────────────┐
│ סכמה: פרופיל משתמש פשוט             │
│ 📊 8 שדות | 🟢 0 מתאימים למדד       │
│                                     │
│ 🔹 בחר שדה למדידה *                 │
│ ℹ️  שדה זה ישמש כערך המדד           │
│ ┌─────────────────────────────────┐ │
│ │ [▼] userId - string              │ │
│ └──────────────────────────────────│
│                                     │
│ 💡 שם מדד מוצע:                    │
│ ┌─────────────────────────────────┐ │
│ │ user_profile_user_id            │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [< הקודם]  [הבא >]                  │
└─────────────────────────────────────┘
```

### Step 4: Labels
```
┌─────────────────────────────────────┐
│ 🏷️  תוויות (Labels)                 │
│                                     │
│ הזן שמות labels מופרדים בפסיק:      │
│ ┌─────────────────────────────────┐ │
│ │ status, region, customer_type   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ 📝 ביטוי PromQL שנוצר:              │
│ ┌─────────────────────────────────┐ │
│ │ {status="$status",              │ │
│ │  region="$region",              │ │
│ │  customer_type="$customer_type"}│ │
│ └─────────────────────────────────┘ │
│                                     │
│ ⚠️  שדות אלו חייבים להיות בסכמה     │
│                                     │
│ [< הקודם]  [הבא >]                  │
└─────────────────────────────────────┘
```

### Step 5: Alert Rules (Optional)
```
┌─────────────────────────────────────┐
│ 🚨 כללי התראה (אופציונלי)            │
│                                     │
│ ➕ הוסף כלל התראה                   │
│                                     │
│ ┌─ כלל #1 ─────────────────────────┐│
││ שם: שיעור שגיאות גבוה              ││
││                                    ││
││ בחר תבנית:                         ││
││ [▼] אחוז מעל סף                    ││
││                                    ││
││ מונה: [▼] failed_transactions      ││
││ מכנה: [▼] total_transactions       ││
││ סף: [5] %                          ││
││                                    ││
││ 📝 ביטוי שנוצר:                    ││
││ ┌────────────────────────────────┐ ││
││ │ (failed_transactions /         │ ││
││ │  total_transactions) * 100 > 5 │ ││
││ └────────────────────────────────┘ ││
│└────────────────────────────────────┘│
│                                     │
│ [< הקודם]  [💾 שמור מדד]            │
└─────────────────────────────────────┘
```

## Implementation Phases

### Phase 1: Core Enhancements (Priority 1)
1. Multi-step wizard structure
2. Required field selection
3. Hebrew metric name helper
4. Simple label input with PromQL generation

**Estimated Time:** 2-3 days

### Phase 2: Alert Rules (Priority 2)  
1. Research Prometheus alert syntax
2. Build expression templates library
3. Create AlertRuleBuilder component
4. Create ExpressionHelper with Hebrew support
5. Integrate with metric form
6. Backend API for alert rules

**Estimated Time:** 3-4 days

### Phase 3: Advanced Features (Priority 3)
1. Alert rule validation
2. Test alert expressions
3. Alert history/status
4. Integration with Prometheus AlertManager

**Estimated Time:** 2-3 days

## Next Steps

1. ✅ Review this plan with user
2. ⏳ Get approval on approach
3. ⏳ Research Prometheus documentation (using web_fetch since Exa unavailable)
4. ⏳ Start implementing Phase 1
5. ⏳ Iterate based on testing feedback

## Questions for User

1. Should I proceed with Phase 1 implementation now?
2. Do you want alert rules in the MVP or save for later?
3. Should alert rules be stored in the metric document or separate collection?
4. Do you need alert rule management UI (list, edit, delete, enable/disable)?
