# Metrics Configuration - Extended Implementation Plan

## Overview

This document extends the Metrics Configuration implementation with comprehensive helper tools, Hebrew documentation, and user-friendly interfaces matching the sophistication level of Schema Management.

---

## Phase 3 Extended: Metrics Configuration with Advanced Tools

### 3.1 Enhanced Backend Metrics Service

#### 3.1.3 Metric Formula Service

**File:** `src/Services/MetricsConfigurationService/Services/MetricFormulaService.cs`

```csharp
public interface IMetricFormulaService
{
    Task<FormulaValidationResult> ValidateFormula(string formula);
    Task<FormulaPreviewResult> PreviewFormula(string formula, string dataSourceId, int sampleSize = 100);
    Task<List<FormulaTemplate>> GetFormulaTemplates();
    Task<string> GenerateFormulaFromDescription(string description);
    Task<FormulaExplanation> ExplainFormula(string formula);
}

public class FormulaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public FormulaMetadata Metadata { get; set; }
}

public class FormulaMetadata
{
    public List<string> RequiredFields { get; set; }
    public string ReturnType { get; set; }
    public int EstimatedComplexity { get; set; }
}

public class FormulaPreviewResult
{
    public decimal? ResultValue { get; set; }
    public List<SampleDataPoint> SampleData { get; set; }
    public string ChartType { get; set; }
    public Dictionary<string, object> Statistics { get; set; }
}

public class FormulaTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NameHebrew { get; set; }
    public string Category { get; set; }
    public string Formula { get; set; }
    public string Description { get; set; }
    public string DescriptionHebrew { get; set; }
    public List<FormulaParameter> Parameters { get; set; }
    public string ExampleUsage { get; set; }
}

public class FormulaParameter
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string DescriptionHebrew { get; set; }
    public object DefaultValue { get; set; }
}
```

#### 3.1.4 Statistical Analysis Service

**File:** `src/Services/MetricsConfigurationService/Services/StatisticalAnalysisService.cs`

```csharp
public interface IStatisticalAnalysisService
{
    Task<ThresholdSuggestion> SuggestThresholds(string dataSourceId, string field, TimeWindow window);
    Task<TrendAnalysis> AnalyzeTrend(string metricId, TimeWindow window);
    Task<AnomalyDetection> DetectAnomalies(string metricId);
    Task<SeasonalityAnalysis> AnalyzeSeasonality(string metricId);
}

public class ThresholdSuggestion
{
    public decimal Mean { get; set; }
    public decimal Median { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal SuggestedWarningThreshold { get; set; }
    public decimal SuggestedCriticalThreshold { get; set; }
    public decimal Percentile95 { get; set; }
    public decimal Percentile99 { get; set; }
    public string Explanation { get; set; }
    public string ExplanationHebrew { get; set; }
}
```

#### 3.1.5 Additional API Endpoints

```csharp
// MetricController.cs - Additional endpoints

[HttpPost("formula/validate")]
public async Task<IActionResult> ValidateFormula(
    [FromBody] ValidateFormulaRequest request)
// Validate metric formula syntax and logic

[HttpPost("formula/preview")]
public async Task<IActionResult> PreviewFormula(
    [FromBody] PreviewFormulaRequest request)
// Preview formula results with sample data

[HttpGet("formula/templates")]
public async Task<IActionResult> GetFormulaTemplates(
    [FromQuery] string? category = null)
// Get pre-built formula templates

[HttpPost("formula/explain")]
public async Task<IActionResult> ExplainFormula(
    [FromBody] string formula)
// Get plain Hebrew explanation of formula

[HttpPost("formula/generate")]
public async Task<IActionResult> GenerateFormula(
    [FromBody] GenerateFormulaRequest request)
// Generate formula from natural language description

[HttpPost("thresholds/suggest")]
public async Task<IActionResult> SuggestThresholds(
    [FromBody] SuggestThresholdsRequest request)
// Suggest alert thresholds based on historical data

[HttpGet("{id}/trend")]
public async Task<IActionResult> GetTrendAnalysis(
    string id,
    [FromQuery] TimeWindow window)
// Analyze metric trend over time

[HttpPost("filters/validate")]
public async Task<IActionResult> ValidateFilters(
    [FromBody] List<MetricFilter> filters)
// Validate filter configuration
```

---

### 3.2 Extended Frontend Metrics Configuration

#### 3.2.2 Metric Formula Helper Dialog

**Component:** `src/Frontend/src/components/metrics/MetricFormulaHelper.tsx` (New)

**Features:**

##### 1. Common Formula Templates Library

```typescript
const commonFormulas = {
  dailySum: {
    id: "daily_sum",
    name: "Daily Sum / סכום יומי",
    category: "aggregation",
    formula: "SUM({field}) GROUP BY DATE({timestamp}) WINDOW 1d",
    description: "Calculate daily sum of a numeric field",
    descriptionHebrew: "חישוב סכום יומי של שדה מספרי",
    parameters: [
      {
        name: "field",
        type: "number",
        description: "Numeric field to sum",
        descriptionHebrew: "שדה מספרי לסיכום"
      },
      {
        name: "timestamp",
        type: "datetime",
        description: "Timestamp field for grouping",
        descriptionHebrew: "שדה תאריך לקיבוץ"
      }
    ],
    examples: [
      {
        scenario: "Total daily sales",
        scenarioHebrew: "סך מכירות יומי",
        formula: "SUM(amount) GROUP BY DATE(transaction_date) WINDOW 1d"
      }
    ]
  },
  
  successRate: {
    id: "success_rate",
    name: "Success Rate / אחוז הצלחה",
    category: "percentage",
    formula: "(COUNT({field} WHERE {condition}) / COUNT({field})) * 100",
    description: "Calculate percentage of successful records",
    descriptionHebrew: "חישוב אחוז רשומות מוצלחות",
    parameters: [
      {
        name: "field",
        type: "any",
        description: "Field to count",
        descriptionHebrew: "שדה לספירה"
      },
      {
        name: "condition",
        type: "boolean",
        description: "Success condition",
        descriptionHebrew: "תנאי הצלחה"
      }
    ],
    examples: [
      {
        scenario: "Processing success rate",
        scenarioHebrew: "אחוז הצלחת עיבוד",
        formula: "(COUNT(*) WHERE status = 'success') / COUNT(*)) * 100"
      }
    ]
  },
  
  averageProcessingTime: {
    id: "avg_processing_time",
    name: "Average Processing Time / ממוצע זמן עיבוד",
    category: "time",
    formula: "AVG(DATEDIFF(second, {start_time}, {end_time}))",
    description: "Calculate average processing duration in seconds",
    descriptionHebrew: "חישוב ממוצע זמן עיבוד בשניות",
    parameters: [
      {
        name: "start_time",
        type: "datetime",
        description: "Process start time",
        descriptionHebrew: "זמן תחילת עיבוד"
      },
      {
        name: "end_time",
        type: "datetime",
        description: "Process end time",
        descriptionHebrew: "זמן סיום עיבוד"
      }
    ],
    examples: [
      {
        scenario: "Average file processing time",
        scenarioHebrew: "ממוצע זמן עיבוד קובץ",
        formula: "AVG(DATEDIFF(second, started_at, completed_at))"
      }
    ]
  },
  
  errorCountByType: {
    id: "error_count_by_type",
    name: "Error Count by Type / ספירת שגיאות לפי סוג",
    category: "error_analysis",
    formula: "COUNT(*) WHERE {error_field} IS NOT NULL GROUP BY {error_type_field}",
    description: "Count errors grouped by error type",
    descriptionHebrew: "ספירת שגיאות מקובצות לפי סוג שגיאה",
    parameters: [
      {
        name: "error_field",
        type: "string",
        description: "Error message field",
        descriptionHebrew: "שדה הודעת שגיאה"
      },
      {
        name: "error_type_field",
        type: "string",
        description: "Error type/category field",
        descriptionHebrew: "שדה סוג/קטגוריית שגיאה"
      }
    ],
    examples: [
      {
        scenario: "Validation errors by type",
        scenarioHebrew: "שגיאות אימות לפי סוג",
        formula: "COUNT(*) WHERE error_message IS NOT NULL GROUP BY error_type"
      }
    ]
  },
  
  growthTrend: {
    id: "growth_trend",
    name: "Growth Trend / מגמת גידול",
    category: "trend",
    formula: "((SUM({field}) CURRENT - SUM({field}) PREVIOUS) / SUM({field}) PREVIOUS) * 100",
    description: "Calculate growth percentage compared to previous period",
    descriptionHebrew: "חישוב אחוז גידול ביחס לתקופה קודמת",
    parameters: [
      {
        name: "field",
        type: "number",
        description: "Numeric field to measure growth",
        descriptionHebrew: "שדה מספרי למדידת גידול"
      }
    ],
    examples: [
      {
        scenario: "Week-over-week transaction growth",
        scenarioHebrew: "גידול עסקאות שבוע על שבוע",
        formula: "((SUM(amount) WEEK 0) - SUM(amount) WEEK -1) / SUM(amount) WEEK -1) * 100"
      }
    ]
  },
  
  movingAverage: {
    id: "moving_average",
    name: "Moving Average / ממוצע נע",
    category: "statistical",
    formula: "AVG({field}) OVER (ORDER BY {timestamp} ROWS {window} PRECEDING)",
    description: "Calculate moving average over specified window",
    descriptionHebrew: "חישוב ממוצע נע על פני חלון זמן מוגדר",
    parameters: [
      {
        name: "field",
        type: "number",
        description: "Numeric field",
        descriptionHebrew: "שדה מספרי"
      },
      {
        name: "timestamp",
        type: "datetime",
        description: "Time field for ordering",
        descriptionHebrew: "שדה זמן למיון"
      },
      {
        name: "window",
        type: "integer",
        description: "Window size (number of rows)",
        descriptionHebrew: "גודל חלון (מספר שורות)"
      }
    ],
    examples: [
      {
        scenario: "7-day moving average of sales",
        scenarioHebrew: "ממוצע נע 7 ימים של מכירות",
        formula: "AVG(amount) OVER (ORDER BY date ROWS 7 PRECEDING)"
      }
    ]
  },
  
  percentile: {
    id: "percentile",
    name: "Percentile / אחוזון",
    category: "statistical",
    formula: "PERCENTILE({field}, {percentile_value})",
    description: "Calculate percentile value of a field",
    descriptionHebrew: "חישוב ערך אחוזון של שדה",
    parameters: [
      {
        name: "field",
        type: "number",
        description: "Numeric field",
        descriptionHebrew: "שדה מספרי"
      },
      {
        name: "percentile_value",
        type: "number",
        description: "Percentile (0-100)",
        descriptionHebrew: "אחוזון (0-100)"
      }
    ],
    examples: [
      {
        scenario: "95th percentile response time",
        scenarioHebrew: "אחוזון 95 של זמן תגובה",
        formula: "PERCENTILE(response_time_ms, 95)"
      }
    ]
  },
  
  uniqueCount: {
    id: "unique_count",
    name: "Unique Count / ספירת ערכים ייחודיים",
    category: "aggregation",
    formula: "COUNT(DISTINCT {field})",
    description: "Count unique values in a field",
    descriptionHebrew: "ספירת ערכים ייחודיים בשדה",
    parameters: [
      {
        name: "field",
        type: "any",
        description: "Field to count unique values",
        descriptionHebrew: "שדה לספירת ערכים ייחודיים"
      }
    ],
    examples: [
      {
        scenario: "Unique customers per day",
        scenarioHebrew: "לקוחות ייחודיים ליום",
        formula: "COUNT(DISTINCT customer_id) GROUP BY DATE(order_date)"
      }
    ]
  }
};
```

##### 2. Visual Formula Builder

**UI Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│  Metric Formula Builder / בונה נוסחת מדד                   │
├─────────────────────────────────────────────────────────────┤
│  Templates / תבניות:                                        │
│  [סכום יומי] [אחוז הצלחה] [ממוצע זמן] [ספירת שגיאות]      │
│  [מגמת גידול] [ממוצע נע] [אחוזון] [ספירה ייחודית]         │
├─────────────────────────────────────────────────────────────┤
│  Visual Builder / בונה חזותי:                              │
│                                                             │
│  Function: [Dropdown: SUM, AVG, COUNT, MIN, MAX, etc.]     │
│  Field:    [Dropdown from schema: amount, quantity, etc.]  │
│                                                             │
│  ┌─ Time Window ──────────────────────────────────┐        │
│  │ [●] Last 24 hours   / 24 שעות אחרונות          │        │
│  │ [ ] Last 7 days     / 7 ימים אחרונים           │        │
│  │ [ ] Last 30 days    / 30 יום אחרונים           │        │
│  │ [ ] Custom / מותאם אישית                        │        │
│  └───────────────────────────────────────────────┘         │
│                                                             │
│  ┌─ Group By ─────────────────────────────────────┐        │
│  │ [☑] Group by date    / קיבוץ לפי תאריך         │        │
│  │ [ ] Group by category / קיבוץ לפי קטגוריה     │        │
│  │ [ ] Group by custom field                      │        │
│  └───────────────────────────────────────────────┘         │
│                                                             │
│  Generated Formula:                                         │
│  ┌───────────────────────────────────────────────┐         │
│  │ SUM(amount) GROUP BY DATE(timestamp)          │         │
│  │ WINDOW 24h                                    │         │
│  └───────────────────────────────────────────────┘         │
│                                                             │
│  [Test Formula / בדוק נוסחה] [Use Formula / השתמש]         │
└─────────────────────────────────────────────────────────────┘
```

##### 3. Formula Tester with Live Preview

```typescript
interface FormulaTestResult {
  isValid: boolean;
  result: number;
  sampleSize: number;
  chartData: ChartDataPoint[];
  explanation: string;
  explanationHebrew: string;
  performance: {
    executionTime: number;
    complexity: string;
  };
}
```

**UI Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│  Formula Tester / בודק נוסחאות                             │
├─────────────────────────────────────────────────────────────┤
│  Formula: SUM(amount) WHERE status = 'completed'            │
│                                                             │
│  Data Source: [Dropdown: Sales Transactions]               │
│  Sample Size: [____100____] records                         │
│                                                             │
│  [Test / בדוק]                                              │
├─────────────────────────────────────────────────────────────┤
│  Results / תוצאות:                                          │
│                                                             │
│  ✓ Formula is valid / הנוסחה תקינה                         │
│  Result Value: 127,543.50 ILS                              │
│  Based on: 100 sample records                              │
│                                                             │
│  Chart Preview:                                             │
│  ┌────────────────────────────────────────────┐            │
│  │     [Live Chart Visualization]             │            │
│  │                                             │            │
│  └────────────────────────────────────────────┘            │
│                                                             │
│  Explanation / הסבר:                                        │
│  הנוסחה מחשבת את סכום כל השדות 'amount' ברשומות          │
│  בהן שדה ה-'status' שווה ל-'completed'.                   │
│                                                             │
│  Performance / ביצועים:                                     │
│  Execution Time: 45ms                                       │
│  Complexity: Low / נמוכה                                    │
└─────────────────────────────────────────────────────────────┘
```

##### 4. Hebrew Formula Explainer

**Component:** `src/Frontend/src/components/metrics/FormulaExplainer.tsx`

```typescript
interface FormulaExplanation {
  hebrew: string;
  english: string;
  breakdown: {
    component: string;
    explanation: string;
    explanationHebrew: string;
  }[];
  examples: string[];
}

// Example explanations
const explanations = {
  "SUM(amount) WHERE status = 'completed'": {
    hebrew: "סכום כל הערכים בשדה 'amount' עבור רשומות בהן שדה 'status' שווה ל-'completed'",
    english: "Sum of all values in 'amount' field for records where 'status' equals 'completed'",
    breakdown: [
      {
        component: "SUM(amount)",
        explanation: "Sum all values in the amount field",
        explanationHebrew: "סכום כל הערכים בשדה amount"
      },
      {
        component: "WHERE status = 'completed'",
        explanation: "Only for records with status 'completed'",
        explanationHebrew: "רק עבור רשומות עם סטטוס 'completed'"
      }
    ],
    examples: [
      "If you have 3 records: amount=100, status='completed'; amount=200, status='pending'; amount=150, status='completed', the result will be 250 (100 + 150)"
    ]
  }
};
```

#### 3.2.3 Filter Condition Builder

**Component:** `src/Frontend/src/components/metrics/FilterConditionBuilder.tsx` (New)

**Features:**

##### 1. Visual Filter Builder

```
┌─────────────────────────────────────────────────────────────┐
│  Filter Conditions / תנאי סינון                            │
├─────────────────────────────────────────────────────────────┤
│  Condition 1:                                               │
│  Field:    [Dropdown: status, amount, date, etc.]          │
│  Operator: [Dropdown with Hebrew:                          │
│             - equals / שווה ל                               │
│             - not equals / לא שווה ל                        │
│             - greater than / גדול מ                         │
│             - less than / קטן מ                             │
│             - contains / מכיל                               │
│             - in list / ברשימה                              │
│             - between / בין ]                              │
│  Value:    [Input field with validation]                   │
│                                                             │
│  [AND / וגם]  [OR / או]                                     │
│                                                             │
│  Condition 2:                                               │
│  ...                                                        │
│                                                             │
│  [+ Add Condition / הוסף תנאי]                              │
│                                                             │
│  Preview:                                                   │
│  ┌───────────────────────────────────────────────┐         │
│  │ status = 'active' AND amount > 1000           │         │
│  └───────────────────────────────────────────────┘         │
│                                                             │
│  Matching Records: ~1,234 / רשומות תואמות                  │
└─────────────────────────────────────────────────────────────┘
```

##### 2. Common Filter Templates

```typescript
const filterTemplates = {
  lastWeek: {
    name: "Last Week / שבוע אחרון",
    filters: [
      {
        field: "created_at",
        operator: "gte",
        value: "NOW() - 7 DAYS"
      }
    ]
  },
  activeOnly: {
    name: "Active Only / פעילים בלבד",
    filters: [
      {
        field: "is_active",
        operator: "equals",
        value: true
      }
    ]
  },
  highValue: {
    name: "High Value / ערך גבוה",
    filters: [
      {
        field: "amount",
        operator: "gt",
        value: 10000
      }
    ]
  },
  errors: {
    name: "Errors Only / שגיאות בלבד",
    filters: [
      {
        field: "status",
        operator: "equals",
        value: "error"
      }
    ]
  },
  todayCompleted: {
    name: "Today Completed / הושלם היום",
    filters: [
      {
        field: "completed_at",
        operator: "gte",
        value: "TODAY()"
      },
      {
        field: "status",
        operator: "equals",
        value: "completed"
      }
    ]
  }
};
```

##### 3. Smart Filter Suggestions

Based on data analysis, suggest relevant filters:

```typescript
interface FilterSuggestion {
  field: string;
  operator: string;
  suggestedValue: any;
  reason: string;
  reasonHebrew: string;
  impactEstimate: string;
}

// Example suggestions
const suggestions = [
  {
    field: "amount",
    operator: "between",
    suggestedValue: [0, 50000],
    reason: "95% of records fall within this range",
    reasonHebrew: "95% מהרשומות נמצאות בטווח זה",
    impactEstimate: "Will include 95% of data"
  }
];
```

#### 3.2.4 Aggregation Helper

**Component:** `src/Frontend/src/components/metrics/AggregationHelper.tsx` (New)

**Features:**

##### 1. Time Window Selector

```
┌─────────────────────────────────────────────────────────────┐
│  Time Window / חלון זמן                                     │
├─────────────────────────────────────────────────────────────┤
│  Real-time / זמן אמת:                                       │
│  [ ] Real-time (no aggregation) / זמן אמת (ללא צבירה)     │
│                                                             │
│  Time-based / מבוסס זמן:                                    │
│  [●] Hourly / שעתי          Window: [__1__] hour(s)       │
│  [ ] Daily / יומי           Window: [__1__] day(s)        │
│  [ ] Weekly / שבועי         Window: [__1__] week(s)       │
│  [ ] Monthly / חודשי        Window: [__1__] month(s)      │
│  [ ] Custom / מותאם אישית                                   │
│                                                             │
│  ┌─ Advanced Settings ─────────────────────────┐           │
│  │ Rolling Window / חלון מתגלגל:                │           │
│  │ [☑] Use rolling window                      │           │
│  │ Look-back period: [__7__] days              │           │
│  │                                              │           │
│  │ Alignment / יישור:                           │           │
│  │ [●] Calendar boundaries / גבולות לוח שנה    │           │
│  │ [ ] Sliding window / חלון מחליק             │           │
│  └──────────────────────────────────────────────┘           │
│                                                             │
│  Preview / תצוגה מקדימה:                                    │
│  Data will be grouped into hourly buckets                  │
│  Example: 10:00-11:00, 11:00-12:00, etc.                   │
└─────────────────────────────────────────────────────────────┘
```

##### 2. Grouping Field Selector

```
┌─────────────────────────────────────────────────────────────┐
│  Group By / קיבוץ לפי                                       │
├─────────────────────────────────────────────────────────────┤
│  [ ] No grouping / ללא קיבוץ                                │
│  [●] Group by field / קיבוץ לפי שדה                         │
│                                                             │
│  Field: [Dropdown: category, status, region, etc.]         │
│  Max Groups: [___10___]                                     │
│  [☑] Show "Others" for remaining / הצג "אחרים" לשאר        │
│                                                             │
│  Sort By / מיין לפי:                                        │
│  [●] Value descending / ערך יורד                            │
│  [ ] Value ascending / ערך עולה                             │
│  [ ] Name / שם                                              │
│                                                             │
│  Preview Distribution / תצוגה מקדימה של התפלגות:            │
│  ┌────────────────────────────────────────────┐            │
│  │ Category A: 45% (1,234 records)            │            │
│  │ Category B: 30% (821 records)              │            │
│  │ Category C: 15% (410 records)              │            │
│  │ Others:     10% (273 records)              │            │
│  └────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────┘
```

##### 3. Aggregation Function Picker with Hebrew Descriptions

```typescript
const aggregationFunctions = {
  sum: {
    name: "Sum / סכום",
    symbol: "Σ",
    description: "Add all values together",
    descriptionHebrew: "חיבור כל הערכים יחד",
    applicableTypes: ["number", "integer", "decimal"],
    example: "1 + 2 + 3 = 6",
    useCase: "Total sales amount, total transactions",
    useCaseHebrew: "סך סכום מכירות, סך עסקאות"
  },
  avg: {
    name: "Average / ממוצע",
    symbol: "x̄",
    description: "Calculate mean value",
    descriptionHebrew: "חישוב ערך ממוצע",
    applicableTypes: ["number", "integer", "decimal"],
    example: "(1 + 2 + 3) / 3 = 2",
    useCase: "Average transaction amount, average processing time",
    useCaseHebrew: "ממוצע סכום עסקה, ממוצע זמן עיבוד"
  },
  count: {
    name: "Count / ספירה",
    symbol: "#",
    description: "Count number of records",
    descriptionHebrew: "ספירת מספר רשומות",
    applicableTypes: ["any"],
    example: "3 records = 3",
    useCase: "Number of transactions, number of errors",
    useCaseHebrew: "מספר עסקאות, מספר שגיאות"
  },
  min: {
    name: "Minimum / מינימום",
    symbol: "↓",
    description: "Find lowest value",
    descriptionHebrew: "מציאת הערך הנמוך ביותר",
