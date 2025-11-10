export interface AlertTemplateParameter {
  name: string;
  labelHebrew: string;
  type: 'string' | 'number' | 'duration' | 'metric';
  required: boolean;
  defaultValue?: string;
  placeholderHebrew?: string;
  description?: string;
}

export interface AlertExpressionTemplate {
  id: string;
  nameHebrew: string;
  nameEnglish: string;
  descriptionHebrew: string;
  template: string;
  example: string;
  parameters: AlertTemplateParameter[];
  category: string;
}

/**
 * Library of pre-defined alert expression templates
 * Based on Prometheus alerting best practices
 */
export const ALERT_EXPRESSION_TEMPLATES: AlertExpressionTemplate[] = [
  {
    id: 'high_rate',
    nameHebrew: 'קצב גבוה',
    nameEnglish: 'High Rate',
    descriptionHebrew: 'התראה כאשר קצב השינוי של המדד עולה על סף מסוים',
    template: 'rate({metric}[{duration}]) > {threshold}',
    example: 'rate(errors_total[5m]) > 10',
    category: 'rate',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'duration',
        labelHebrew: 'פרק זמן',
        type: 'duration',
        required: true,
        defaultValue: '5m',
        placeholderHebrew: 'לדוגמה: 5m, 10m, 1h'
      },
      {
        name: 'threshold',
        labelHebrew: 'סף',
        type: 'number',
        required: true,
        placeholderHebrew: 'ערך מספרי'
      }
    ]
  },
  {
    id: 'percentage_threshold',
    nameHebrew: 'אחוז מעל סף',
    nameEnglish: 'Percentage Threshold',
    descriptionHebrew: 'התראה כאשר יחס בין שני מדדים עולה על אחוז מסוים',
    template: '({numerator} / {denominator}) * 100 > {threshold}',
    example: '(failed_transactions / total_transactions) * 100 > 5',
    category: 'percentage',
    parameters: [
      {
        name: 'numerator',
        labelHebrew: 'מונה (מדד עליון)',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'denominator',
        labelHebrew: 'מכנה (מדד תחתון)',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'threshold',
        labelHebrew: 'אחוז סף',
        type: 'number',
        required: true,
        placeholderHebrew: 'לדוגמה: 5, 10, 20',
        description: 'אחוזים (0-100)'
      }
    ]
  },
  {
    id: 'value_comparison',
    nameHebrew: 'השוואת ערך',
    nameEnglish: 'Value Comparison',
    descriptionHebrew: 'התראה כאשר ערך המדד עובר סף מסוים (גדול מ/קטן מ)',
    template: '{metric} {operator} {threshold}',
    example: 'cpu_usage > 80',
    category: 'comparison',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'operator',
        labelHebrew: 'אופרטור',
        type: 'string',
        required: true,
        defaultValue: '>',
        placeholderHebrew: '>, <, >=, <=, ==, !='
      },
      {
        name: 'threshold',
        labelHebrew: 'ערך סף',
        type: 'number',
        required: true,
        placeholderHebrew: 'ערך מספרי'
      }
    ]
  },
  {
    id: 'absence_detection',
    nameHebrew: 'זיהוי היעדרות נתונים',
    nameEnglish: 'Absence Detection',
    descriptionHebrew: 'התראה כאשר מדד לא מדווח נתונים במשך פרק זמן מסוים',
    template: 'absent({metric}[{duration}])',
    example: 'absent(heartbeat_total[10m])',
    category: 'absence',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'duration',
        labelHebrew: 'פרק זמן',
        type: 'duration',
        required: true,
        defaultValue: '10m',
        placeholderHebrew: 'לדוגמה: 5m, 10m, 30m'
      }
    ]
  },
  {
    id: 'spike_detection',
    nameHebrew: 'זיהוי קפיצה חדה',
    nameEnglish: 'Spike Detection',
    descriptionHebrew: 'התראה כאשר שיעור עלייה של מדד עולה על אחוז מסוים',
    template: '(({metric} - {metric} offset {lookback}) / {metric} offset {lookback}) * 100 > {threshold}',
    example: '((requests_total - requests_total offset 1h) / requests_total offset 1h) * 100 > 50',
    category: 'change',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'lookback',
        labelHebrew: 'פרק זמן להשוואה',
        type: 'duration',
        required: true,
        defaultValue: '1h',
        placeholderHebrew: 'לדוגמה: 5m, 1h, 24h'
      },
      {
        name: 'threshold',
        labelHebrew: 'אחוז שינוי',
        type: 'number',
        required: true,
        placeholderHebrew: 'לדוגמה: 50, 100, 200',
        description: 'אחוזי שינוי'
      }
    ]
  },
  {
    id: 'average_above_threshold',
    nameHebrew: 'ממוצע מעל סף',
    nameEnglish: 'Average Above Threshold',
    descriptionHebrew: 'התראה כאשר ממוצע המדד בפרק זמן נתון עולה על סף',
    template: 'avg_over_time({metric}[{duration}]) > {threshold}',
    example: 'avg_over_time(response_time_seconds[5m]) > 2',
    category: 'aggregation',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'duration',
        labelHebrew: 'פרק זמן',
        type: 'duration',
        required: true,
        defaultValue: '5m',
        placeholderHebrew: 'לדוגמה: 5m, 15m, 1h'
      },
      {
        name: 'threshold',
        labelHebrew: 'ערך סף',
        type: 'number',
        required: true,
        placeholderHebrew: 'ערך מספרי'
      }
    ]
  },
  {
    id: 'sum_exceeds',
    nameHebrew: 'סכום עולה על סף',
    nameEnglish: 'Sum Exceeds Threshold',
    descriptionHebrew: 'התראה כאשר סכום המדד על פני פרק זמן עולה על סף',
    template: 'sum(rate({metric}[{duration}])) > {threshold}',
    example: 'sum(rate(errors_total[5m])) > 100',
    category: 'aggregation',
    parameters: [
      {
        name: 'metric',
        labelHebrew: 'מדד',
        type: 'metric',
        required: true,
        placeholderHebrew: 'בחר מדד'
      },
      {
        name: 'duration',
        labelHebrew: 'פרק זמן',
        type: 'duration',
        required: true,
        defaultValue: '5m',
        placeholderHebrew: 'לדוגמה: 5m, 15m, 1h'
      },
      {
        name: 'threshold',
        labelHebrew: 'ערך סף',
        type: 'number',
        required: true,
        placeholderHebrew: 'ערך מספרי'
      }
    ]
  },
  {
    id: 'custom_expression',
    nameHebrew: 'ביטוי מותאם אישית',
    nameEnglish: 'Custom Expression',
    descriptionHebrew: 'הזן ביטוי PromQL מותאם אישית',
    template: '{expression}',
    example: 'up == 0',
    category: 'custom',
    parameters: [
      {
        name: 'expression',
        labelHebrew: 'ביטוי PromQL',
        type: 'string',
        required: true,
        placeholderHebrew: 'הזן ביטוי PromQL מלא',
        description: 'ביטוי PromQL חופשי'
      }
    ]
  }
];

/**
 * Get alert templates by category
 */
export const getTemplatesByCategory = (category: string): AlertExpressionTemplate[] => {
  return ALERT_EXPRESSION_TEMPLATES.filter(t => t.category === category);
};

/**
 * Get all template categories
 */
export const getTemplateCategories = (): { key: string; labelHebrew: string }[] => {
  return [
    { key: 'rate', labelHebrew: 'קצב שינוי' },
    { key: 'percentage', labelHebrew: 'אחוזים' },
    { key: 'comparison', labelHebrew: 'השוואות' },
    { key: 'absence', labelHebrew: 'היעדרות' },
    { key: 'change', labelHebrew: 'שינויים' },
    { key: 'aggregation', labelHebrew: 'צבירה' },
    { key: 'custom', labelHebrew: 'מותאם אישית' }
  ];
};

/**
 * Generate PromQL expression from template and parameters
 */
export const generateExpression = (
  template: AlertExpressionTemplate,
  parameters: Record<string, string>
): string => {
  let expression = template.template;
  
  template.parameters.forEach(param => {
    const value = parameters[param.name] || param.defaultValue || '';
    expression = expression.replace(new RegExp(`\\{${param.name}\\}`, 'g'), value);
  });
  
  return expression;
};
