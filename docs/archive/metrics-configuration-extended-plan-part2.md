# Metrics Configuration - Extended Plan (Part 2)

## Continuation from Part 1...

#### 3.2.4 Aggregation Helper (Continued)

##### 3. Aggregation Function Picker with Hebrew Descriptions (Continued)

```typescript
const aggregationFunctions = {
  // ... (continued from part 1)
  
  max: {
    name: "Maximum / מקסימום",
    symbol: "↑",
    description: "Find highest value",
    descriptionHebrew: "מציאת הערך הגבוה ביותר",
    applicableTypes: ["number", "integer", "decimal", "date"],
    example: "MAX(1, 2, 3) = 3",
    useCase: "Maximum transaction amount, peak usage time",
    useCaseHebrew: "סכום עסקה מקסימלי, זמן שימוש שיא"
  },
  
  median: {
    name: "Median / חציון",
    symbol: "~",
    description: "Find middle value",
    descriptionHebrew: "מציאת הערך האמצעי",
    applicableTypes: ["number", "integer", "decimal"],
    example: "MEDIAN(1, 2, 3, 4, 5) = 3",
    useCase: "Median response time, median order value",
    useCaseHebrew: "חציון זמן תגובה, חציון ערך הזמנה"
  },
  
  stddev: {
    name: "Standard Deviation / סטיית תקן",
    symbol: "σ",
    description: "Measure data variability",
    descriptionHebrew: "מדידת שונות הנתונים",
    applicableTypes: ["number", "integer", "decimal"],
    example: "STDDEV(1, 5, 5, 9) ≈ 2.83",
    useCase: "Price volatility, performance consistency",
    useCaseHebrew: "תנודתיות מחירים, עקביות ביצועים"
  }
};
```

#### 3.2.5 Alert Threshold Calculator

**Component:** `src/Frontend/src/components/metrics/AlertThresholdCalculator.tsx` (New)

**Features:**

##### 1. Statistical Analysis for Threshold Suggestions

```
┌─────────────────────────────────────────────────────────────┐
│  Alert Threshold Calculator / מחשבון סף התראות             │
├─────────────────────────────────────────────────────────────┤
│  Data Source: [Sales Transactions]                          │
│  Metric Field: [amount]                                     │
│  Analysis Period: [Last 30 days / 30 יום אחרונים]          │
│                                                             │
│  [Analyze / נתח]                                            │
├─────────────────────────────────────────────────────────────┤
│  Statistical Analysis / ניתוח סטטיסטי:                     │
│                                                             │
│  ┌─ Basic Statistics ────────────────────────┐             │
│  │ Mean (Average):        12,543 ILS         │             │
│  │ Median:                10,200 ILS         │             │
│  │ Standard Deviation:     3,421 ILS         │             │
│  │ Min Value:                 50 ILS         │             │
│  │ Max Value:             98,765 ILS         │             │
│  └────────────────────────────────────────────┘             │
│                                                             │
│  ┌─ Distribution ─────────────────────────────┐            │
│  │    [Histogram Visualization]              │            │
│  │                                            │            │
│  └────────────────────────────────────────────┘            │
│                                                             │
│  ┌─ Suggested Thresholds ────────────────────┐            │
│  │                                            │            │
│  │ Warning Level / רמת אזהרה:                │            │
│  │ [16,000] ILS (Mean + 1σ)                  │            │
│  │ ├─ Explanation: ~84% של הערכים מתחת      │            │
│  │ └─ לסף זה                                 │            │
│  │                                            │            │
│  │ Critical Level / רמת קריטיות:             │            │
│  │ [19,500] ILS (Mean + 2σ)                  │            │
│  │ ├─ Explanation: ~97.5% של הערכים מתחת    │            │
│  │ └─ לסף זה                                 │            │
│  │                                            │            │
│  │ [Use Suggestions / השתמש בהצעות]          │            │
│  │ [Customize / התאם אישית]                  │            │
│  └────────────────────────────────────────────┘            │
│                                                             │
│  ┌─ Percentile-Based Thresholds ─────────────┐            │
│  │ 95th Percentile: 22,100 ILS               │            │
│  │ 99th Percentile: 45,800 ILS               │            │
│  │                                            │            │
│  │ Recommended for: High-value anomalies     │            │
│  │ מומלץ עבור: חריגות בעלות ערך גבוה        │            │
│  └────────────────────────────────────────────┘            │
└─────────────────────────────────────────────────────────────┘
```

##### 2. Visual Threshold Visualization

```typescript
interface ThresholdVisualization {
  metricName: string;
  currentValue: number;
  warningThreshold: number;
  criticalThreshold: number;
  historicalData: HistoricalDataPoint[];
  projectedTrend: TrendLine;
}
```

**UI Component:**
```
┌─────────────────────────────────────────────────────────────┐
│  Threshold Visualization / הדמיית סף                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Current Value: 13,245 ILS                                 │
│                                                             │
│  ┌────────────────────────────────────────┐                │
│  │        [Line Chart with Zones]         │                │
│  │  ╔═══════════════════════════════════╗ │                │
│  │  ║ Critical (>19,500)      [RED]    ║ │                │
│  │  ╠═══════════════════════════════════╣ │                │
│  │  ║ Warning (16,000-19,500) [YELLOW] ║ │                │
│  │  ╠═══════════════════════════════════╣ │                │
│  │  ║ Normal (<16,000)        [GREEN]  ║ │                │
│  │  ╚═══════════════════════════════════╝ │                │
│  │                                        │                │
│  │     [Historical trend line]            │                │
│  │     ● Current position                 │                │
│  │     - - - Projected trend              │                │
│  └────────────────────────────────────────┘                │
│                                                             │
│  Status: ✓ Normal / תקין                                   │
│  Distance from warning: 2,755 ILS (17%)                    │
└─────────────────────────────────────────────────────────────┘
```

##### 3. Smart Threshold Recommendations

```typescript
interface ThresholdRecommendation {
  level: 'warning' | 'critical';
  value: number;
  method: string;
  methodHebrew: string;
  confidence: number;
  reasoning: string;
  reasoningHebrew: string;
  historicalTriggers: {
    falsePositives: number;
    truePositives: number;
    accuracy: number;
  };
}

const recommendations = [
  {
    level: 'warning',
    value: 16000,
    method: 'Statistical (Mean + 1 StdDev)',
    methodHebrew: 'סטטיסטי (ממוצע + סטיית תקן)',
    confidence: 0.85,
    reasoning: 'Based on normal distribution, this threshold will trigger for approximately 16% of values',
    reasoningHebrew: 'בהתבסס על התפלגות נורמלית, סף זה יופעל עבור כ-16% מהערכים',
    historicalTriggers: {
      falsePositives: 12,
      truePositives: 45,
      accuracy: 0.79
    }
  }
];
```

#### 3.2.6 Metric Documentation Generator

**Component:** `src/Frontend/src/components/metrics/MetricDocumentationGenerator.tsx` (New)

**Features:**

##### 1. Auto-Generate Hebrew Documentation

```typescript
interface MetricDocumentation {
  metricName: string;
  metricNameHebrew: string;
  description: string;
  descriptionHebrew: string;
  formula: string;
  formulaExplanation: string;
  formulaExplanationHebrew: string;
  dataSource: string;
  updateFrequency: string;
  filters: FilterDocumentation[];
  thresholds: ThresholdDocumentation[];
  usageGuide: string;
  usageGuideHebrew: string;
  interpretation: string;
  interpretationHebrew: string;
}
```

**Hebrew Documentation Template:**

```markdown
# מדד: {metric_name_hebrew}

## תיאור
{description_hebrew}

## נוסחת חישוב
```
{formula}
```

### הסבר הנוסחה
{formula_explanation_hebrew}

## מקור נתונים
- **מקור**: {data_source_name}
- **תדירות עדכון**: {update_frequency}
- **חלון זמן**: {time_window}

## תנאי סינון
{filters_list_hebrew}

## סף התראות

### אזהרה
- **ערך**: {warning_threshold}
- **תנאי**: {warning_condition_hebrew}
- **משמעות**: {warning_meaning_hebrew}

### קריטי
- **ערך**: {critical_threshold}
- **תנאי**: {critical_condition_hebrew}
- **משמעות**: {critical_meaning_hebrew}

## מדריך שימוש
{usage_guide_hebrew}

## פרשנות תוצאות

### ערכים נמוכים
{low_values_interpretation_hebrew}

### ערכים נורמליים
{normal_values_interpretation_hebrew}

### ערכים גבוהים
{high_values_interpretation_hebrew}

## דוגמאות

### דוגמה 1
{example_1_hebrew}

### דוגמה 2
{example_2_hebrew}

## שאלות נפוצות

### שאלה 1: {question_1_hebrew}
{answer_1_hebrew}

### שאלה 2: {question_2_hebrew}
{answer_2_hebrew}
```

##### 2. Export Options

```
┌─────────────────────────────────────────────────────────────┐
│  Export Documentation / יצוא תיעוד                          │
├─────────────────────────────────────────────────────────────┤
│  Format / פורמט:                                            │
│  [●] Markdown (.md)                                         │
│  [ ] PDF (.pdf)                                             │
│  [ ] HTML (.html)                                           │
│  [ ] Word (.docx)                                           │
│                                                             │
│  Language / שפה:                                            │
│  [●] Hebrew / עברית                                         │
│  [ ] English / אנגלית                                       │
│  [ ] Both / שתיהן                                           │
│                                                             │
│  Include / כלול:                                            │
│  [☑] Formula explanation / הסבר נוסחה                       │
│  [☑] Threshold details / פרטי סף                            │
│  [☑] Examples / דוגמאות                                     │
│  [☑] FAQ / שאלות נפוצות                                     │
│  [☑] Visual charts / תרשימים ויזואליים                     │
│                                                             │
│  [Export / יצא] [Preview / תצוגה מקדימה]                   │
└─────────────────────────────────────────────────────────────┘
```

#### 3.2.7 Metric Templates

**Pre-built Templates for Israeli Business Metrics:**

```typescript
const metricTemplates = {
  salesMetrics: {
    category: "Sales / מכירות",
    templates: [
      {
        id: "daily_revenue",
        name: "Daily Revenue / הכנסות יומיות",
        nameHebrew: "הכנסות יומיות",
        formula: "SUM(amount) WHERE status = 'completed' GROUP BY DATE(transaction_date) WINDOW 1d",
        description: "Track total daily revenue from completed transactions",
        descriptionHebrew: "מעקב אחר סך ההכנסות היומיות מעסקאות שהושלמו",
        thresholds: {
          warning: { value: 50000, condition: "less than" },
          critical: { value: 30000, condition: "less than" }
        }
      },
      {
        id: "conversion_rate",
        name: "Conversion Rate / שיעור המרה",
        nameHebrew: "שיעור המרה",
        formula: "(COUNT(*) WHERE status = 'completed') / COUNT(*) * 100",
        description: "Percentage of successful conversions",
        descriptionHebrew: "אחוז ההמרות המוצלחות",
        thresholds: {
          warning: { value: 70, condition: "less than" },
          critical: { value: 50, condition: "less than" }
        }
      },
      {
        id: "average_order_value",
        name: "Average Order Value / ערך הזמנה ממוצע",
        nameHebrew: "ערך הזמנה ממוצע",
        formula: "AVG(amount) WHERE status = 'completed'",
        description: "Average value of completed orders",
        descriptionHebrew: "ערך ממוצע של הזמנות שהושלמו",
        thresholds: {
          warning: { value: 500, condition: "less than" },
          critical: { value: 300, condition: "less than" }
        }
      }
    ]
  },
  
  qualityMetrics: {
    category: "Quality / איכות",
    templates: [
      {
        id: "error_rate",
        name: "Error Rate / שיעור שגיאות",
        nameHebrew: "שיעור שגיאות",
        formula: "(COUNT(*) WHERE has_errors = true) / COUNT(*) * 100",
        description: "Percentage of records with errors",
        descriptionHebrew: "אחוז הרשומות עם שגיאות",
        thresholds: {
          warning: { value: 5, condition: "greater than" },
          critical: { value: 10, condition: "greater than" }
        }
      },
      {
        id: "validation_success_rate",
        name: "Validation Success Rate / שיעור הצלחת אימות",
        nameHebrew: "שיעור הצלחת אימות",
        formula: "(COUNT(*) WHERE validation_status = 'passed') / COUNT(*) * 100",
        description: "Percentage of records passing validation",
        descriptionHebrew: "אחוז הרשומות שעברו אימות",
        thresholds: {
          warning: { value: 95, condition: "less than" },
          critical: { value: 90, condition: "less than" }
        }
      }
    ]
  },
  
  performanceMetrics: {
    category: "Performance / ביצועים",
    templates: [
      {
        id: "avg_processing_time",
        name: "Average Processing Time / זמן עיבוד ממוצע",
        nameHebrew: "זמן עיבוד ממוצע",
        formula: "AVG(DATEDIFF(second, started_at, completed_at))",
        description: "Average time to process records in seconds",
        descriptionHebrew: "זמן ממוצע לעיבוד רשומות בשניות",
        thresholds: {
          warning: { value: 60, condition: "greater than" },
          critical: { value: 120, condition: "greater than" }
        }
      },
      {
        id: "throughput",
        name: "Throughput / תפוקה",
        nameHebrew: "תפוקה",
        formula: "COUNT(*) / DATEDIFF(hour, MIN(timestamp), MAX(timestamp))",
        description: "Records processed per hour",
        descriptionHebrew: "רשומות שעובדו לשעה",
        thresholds: {
          warning: { value: 1000, condition: "less than" },
          critical: { value: 500, condition: "less than" }
        }
      }
    ]
  },
  
  operationalMetrics: {
    category: "Operational / תפעול",
    templates: [
      {
        id: "system_uptime",
        name: "System Uptime / זמן פעילות מערכת",
        nameHebrew: "זמן פעילות מערכת",
        formula: "(COUNT(*) WHERE status = 'active') / COUNT(*) * 100",
        description: "Percentage of time system is operational",
        descriptionHebrew: "אחוז הזמן שבו המערכת פעילה",
        thresholds: {
          warning: { value: 99, condition: "less than" },
          critical: { value: 95, condition: "less than" }
        }
      },
      {
        id: "failed_jobs",
        name: "Failed Jobs / משימות שנכשלו",
        nameHebrew: "משימות שנכשלו",
        formula: "COUNT(*) WHERE status = 'failed' WINDOW 1h",
        description: "Number of failed jobs per hour",
        descriptionHebrew: "מספר משימות שנכשלו לשעה",
        thresholds: {
          warning: { value: 5, condition: "greater than" },
          critical: { value: 10, condition: "greater than" }
        }
      }
    ]
  }
};
```

#### 3.2.8 Hebrew Localization for Metrics

**File:** `src/Frontend/src/i18n/locales/he.json` (Add comprehensive metrics section)

```json
{
  "metrics": {
    "title": "ניהול מדדים",
    "subtitle": "הגדר ועקוב אחר מדדי ביצועים",
    "create": "צור מדד חדש",
    "edit": "ערוך מדד",
    "delete": "מחק מדד",
    "duplicate": "שכפל מדד",
    "preview": "תצוגה מקדימה",
    
    "formula": {
      "title": "נוסחת חישוב",
      "builder": "בונה נוסחאות",
      "tester": "בודק נוסחאות",
      "templates": "תבניות נוסחאות",
      "validate": "אמת נוסחה",
      "explain": "הסבר נוסחה",
      "generate": "צור נוסחה",
      "valid": "הנוסחה תקינה",
      "invalid": "הנוסחה אינה תקינה",
      "complexity": "מורכבות",
      "performance": "ביצועים",
      "helper": {
        "title": "עזרת נוסחאות",
        "commonPatterns": "תבניות נפוצות",
        "visualBuilder": "בונה חזותי",
        "manualEntry": "הזנה ידנית"
      }
    },
    
    "aggregation": {
      "title": "צבירה",
      "function": "פונקציית צבירה",
      "sum": "סכום",
      "avg": "ממוצע",
      "count": "ספירה",
      "min": "מינימום",
      "max": "מקסימום",
      "median": "חציון",
      "percentile": "אחוזון",
      "stddev": "סטיית תקן",
      "uniqueCount": "ספירה ייחודית"
    },
    
    "timeWindow": {
      "title": "חלון זמן",
      "realtime": "זמן אמת",
      "hourly": "שעתי",
      "daily": "יומי",
      "weekly": "שבועי",
      "monthly": "חודשי",
      "custom": "מותאם אישית",
      "rollingWindow": "חלון מתגלגל",
      "lookback": "תקופת הסתכלות אחורה",
      "alignment": "יישור"
    },
    
    "filters": {
      "title": "תנאי סינון",
      "add": "הוסף תנאי",
      "remove": "הסר תנאי",
      "field": "שדה",
      "operator": "אופרטור",
      "value": "ערך",
      "and": "וגם",
      "or": "או",
      "operators": {
        "equals": "שווה ל",
        "notEquals": "לא שווה ל",
        "greaterThan": "גדול מ",
        "lessThan": "קטן מ",
        "greaterThanOrEqual": "גדול או שווה ל",
        "lessThanOrEqual": "קטן או שווה ל",
        "contains": "מכיל",
        "notContains": "אינו מכיל",
        "startsWith": "מתחיל ב",
        "endsWith": "מסתיים ב",
        "inList": "ברשימה",
        "notInList": "לא ברשימה",
        "between": "בין",
        "isNull": "ריק",
        "isNotNull": "לא ריק"
      },
      "templates": {
        "title": "תבניות סינון",
        "lastWeek": "שבוע אחרון",
        "lastMonth": "חודש אחרון",
        "today": "היום",
        "activeOnly": "פעילים בלבד",
        "errorsOnly": "שגיאות בלבד",
        "highValue": "ערך גבוה"
      },
      "suggestions": {
        "title": "הצעות סינון",
        "based": "בהתבסס על ניתוח נתונים",
        "impact": "השפעה משוערת"
      }
    },
    
    "grouping": {
      "title": "קיבוץ",
      "field": "שדה לקיבוץ",
      "maxGroups": "מספר קבוצות מקסימלי",
      "showOthers": "הצג 'אחרים'",
      "sortBy": "מיין לפי",
      "valueDescending": "ערך יורד",
      "valueAscending": "ערך עולה",
      "name": "שם"
    },
    
    "alerts": {
      "title": "התראות",
      "threshold": "סף התראה",
      "warning": "אזהרה",
      "critical": "קריטי",
      "condition": "תנאי",
      "value": "ערך",
      "recipients": "נמענים",
      "severity": "חומרה",
      "calculator": {
        "title": "מחשבון סף",
        "analyze": "נתח נתונים",
        "suggest": "הצע סף",
        "statistics": "סטטיסטיקה",
        "distribution": "התפלגות",
        "recommendations": "המלצות",
        "confidence": "רמת ביטחון",
        "accuracy": "דיוק היסטורי"
      }
    },
    
    "documentation": {
      "title": "תיעוד",
      "generate": "צור תיעוד",
      "export": "יצא",
      "format": "פורמט",
      "language": "שפה",
      "include": "כלול",
      "preview": "תצוגה מקדימה",
      "sections": {
        "description": "תיאור",
        "formula": "נוסחה",
        "dataSource": "מקור נתונים",
        "filters": "תנאי סינון",
        "thresholds": "סף התראות",
        "usage": "מדריך שימוש",
        "interpretation": "פרשנות תוצאות",
        "examples": "דוגמאות",
        "faq": "שאלות נפוצות"
      }
    },
    
    "templates": {
      "title": "תבניות מדדים",
      "categories": {
        "sales": "מכירות",
        "quality": "איכות",
        "performance": "ביצועים",
        "operational": "תפעול"
      },
      "use": "השתמש בתבנית",
      "customize": "התאם אישית"
    },
    
    "messages": {
      "createSuccess": "המדד נוצר בהצלחה",
      "updateSuccess": "המדד עודכן בהצלחה",
      "deleteSuccess": "המדד נמחק בהצלחה",
      "formulaValid": "הנוסחה תקינה",
      "formulaInvalid": "הנוסחה אינה תקינה",
      "testSuccess": "הבדיקה הצליחה",
      "testFailed": "הבדיקה נכשלה",
      "thresholdCalculated": "הסף חושב בהצלחה",
      "documentationGenerated": "התיעוד נוצר בהצלחה"
    }
  }
}
```

---

## Summary

This extended Metrics Configuration plan provides:

1. ✅ **8+ Formula Templates** in Hebrew for common Israeli business scenarios
2. ✅ **Visual Formula Builder** with drag-and-drop interface
3. ✅ **Formula Tester** with live preview and Hebrew explanations
4. ✅ **Filter Condition Builder** with smart suggestions
5. ✅ **Aggregation Helper** with Hebrew function descriptions
6. ✅ **Alert Threshold Calculator** with statistical analysis
7. ✅ **Documentation Generator** with automatic Hebrew generation
8. ✅ **Pre-built Templates** for Sales, Quality, Performance, and Operational metrics
9. ✅ **Comprehensive Hebrew Localization** covering all UI elements

The implementation matches the sophistication level of Schema Management with:
- Helper pop-up tools and dialogs
- Hebrew documentation throughout
- Statistical analysis capabilities
- Visual builders and testers
- Smart suggestions based on data analysis
- Template library for quick start
- Export capabilities in multiple formats

**Estimated Development Time:** 2-3 weeks (following Schema Management completion)

---

## End of Extended Metrics Configuration Plan
