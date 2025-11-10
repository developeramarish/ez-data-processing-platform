import React, { useState, useEffect } from 'react';
import { 
  Card, Space, Typography, Select, Input, Alert, Tag, Button
} from 'antd';
import { 
  FunctionOutlined, CodeOutlined, CheckCircleOutlined, 
  CloseCircleOutlined, InfoCircleOutlined 
} from '@ant-design/icons';
import PromQLExpressionHelperDialog from './PromQLExpressionHelperDialog';

const { Text } = Typography;
const { TextArea } = Input;
const { Option } = Select;

export type FormulaType = 'simple' | 'promql' | 'recording';

interface FormulaBuilderProps {
  fieldPath: string;
  formula: string;
  formulaType: FormulaType;
  onChange: (formula: string, formulaType: FormulaType) => void;
}

// Formula templates for common PromQL patterns
const FORMULA_TEMPLATES = [
  {
    id: 'simple',
    nameHebrew: 'ערך פשוט',
    nameEnglish: 'Simple Value',
    description: 'ערך ישיר מהשדה',
    example: 'field_name',
    requiresField: true
  },
  {
    id: 'sum',
    nameHebrew: 'סכום',
    nameEnglish: 'Sum',
    description: 'סכום כל הערכים',
    template: 'sum($FIELD)',
    example: 'sum(amount)',
    requiresField: true
  },
  {
    id: 'avg',
    nameHebrew: 'ממוצע',
    nameEnglish: 'Average',
    description: 'ממוצע של כל הערכים',
    template: 'avg($FIELD)',
    example: 'avg(response_time)',
    requiresField: true
  },
  {
    id: 'rate',
    nameHebrew: 'קצב שינוי',
    nameEnglish: 'Rate',
    description: 'קצב שינוי per-second',
    template: 'rate($FIELD[5m])',
    example: 'rate(requests[5m])',
    requiresField: true
  },
  {
    id: 'increase',
    nameHebrew: 'עלייה',
    nameEnglish: 'Increase',
    description: 'עלייה במשך פרק זמן',
    template: 'increase($FIELD[5m])',
    example: 'increase(requests[5m])',
    requiresField: true
  },
  {
    id: 'count',
    nameHebrew: 'ספירה',
    nameEnglish: 'Count',
    description: 'ספירת מספר הדגימות',
    template: 'count($FIELD)',
    example: 'count(transactions)',
    requiresField: true
  },
  {
    id: 'max',
    nameHebrew: 'מקסימום',
    nameEnglish: 'Max',
    description: 'ערך מקסימלי',
    template: 'max($FIELD)',
    example: 'max(response_time)',
    requiresField: true
  },
  {
    id: 'min',
    nameHebrew: 'מינימום',
    nameEnglish: 'Min',
    description: 'ערך מינימלי',
    template: 'min($FIELD)',
    example: 'min(response_time)',
    requiresField: true
  },
  {
    id: 'custom',
    nameHebrew: 'ביטוי מותאם אישית',
    nameEnglish: 'Custom Expression',
    description: 'כתוב ביטוי PromQL משלך',
    example: 'sum(rate(field_name[5m])) by (label)',
    requiresField: false
  }
];

const FormulaBuilder: React.FC<FormulaBuilderProps> = ({
  fieldPath,
  formula,
  formulaType,
  onChange
}) => {
  const [selectedType, setSelectedType] = useState<FormulaType>(formulaType || 'simple');
  const [selectedTemplate, setSelectedTemplate] = useState<string>('simple');
  const [customFormula, setCustomFormula] = useState(formula || '');
  const [showPromQLHelper, setShowPromQLHelper] = useState(false);
  const [expressionValid, setExpressionValid] = useState(true);

  // Validate PromQL expression
  const validatePromQLExpression = (expr: string): boolean => {
    if (!expr || expr.trim().length === 0) return false;
    
    const squareBrackets = expr.match(/[[\]]/g);
    const curlyBrackets = expr.match(/[{}]/g);
    const openParens = expr.match(/[(]/g);
    const closeParens = expr.match(/[)]/g);
    
    if (squareBrackets && squareBrackets.length % 2 !== 0) return false;
    if (curlyBrackets && curlyBrackets.length % 2 !== 0) return false;
    if ((openParens?.length || 0) !== (closeParens?.length || 0)) return false;
    
    const hasMetricOrFunction = /[a-zA-Z_][a-zA-Z0-9_]*/.test(expr);
    if (!hasMetricOrFunction) return false;
    
    return true;
  };

  // Generate formula based on template and field
  const generateFormula = (templateId: string, field: string): string => {
    const template = FORMULA_TEMPLATES.find(t => t.id === templateId);
    if (!template) return field;
    
    if (template.id === 'simple') {
      return field;
    }
    
    if (template.template) {
      // Extract field name from path (e.g., $.amount -> amount)
      const fieldName = field.startsWith('$.') ? field.substring(2) : field;
      return template.template.replace('$FIELD', fieldName);
    }
    
    return field;
  };

  const handleFormulaTypeChange = (type: FormulaType) => {
    setSelectedType(type);
    
    // Update formula based on new type
    if (type === 'simple') {
      const newFormula = fieldPath;
      setCustomFormula(newFormula);
      onChange(newFormula, 'simple');
      setExpressionValid(true);
    } else if (type === 'promql') {
      // Generate initial PromQL formula
      const newFormula = generateFormula('simple', fieldPath);
      setCustomFormula(newFormula);
      onChange(newFormula, 'promql');
      setExpressionValid(validatePromQLExpression(newFormula));
      setSelectedTemplate('simple');
    } else if (type === 'recording') {
      // Keep existing formula for recording
      onChange(customFormula, 'recording');
      setExpressionValid(customFormula.length > 0);
    }
  };

  const handleTemplateChange = (templateId: string) => {
    setSelectedTemplate(templateId);
    if (templateId !== 'custom') {
      const newFormula = generateFormula(templateId, fieldPath);
      setCustomFormula(newFormula);
      onChange(newFormula, 'promql');
      setExpressionValid(validatePromQLExpression(newFormula));
    }
  };

  const handleCustomFormulaChange = (value: string) => {
    setCustomFormula(value);
    onChange(value, selectedType);
    setExpressionValid(validatePromQLExpression(value));
  };

  const handlePromQLHelperSelect = (expr: string) => {
    setCustomFormula(expr);
    onChange(expr, 'promql');
    setExpressionValid(validatePromQLExpression(expr));
    setShowPromQLHelper(false);
    // Force template to custom when selecting from helper
    setSelectedTemplate('custom');
  };

  // Extract field name from fieldPath (e.g., $.attendance_rate -> attendance_rate)
  const getFieldName = (): string => {
    if (!fieldPath) return '';
    return fieldPath.startsWith('$.') ? fieldPath.substring(2) : fieldPath;
  };

  // Generate humanized Hebrew description of PromQL expression
  const getExpressionDescription = (expr: string): string => {
    if (!expr) return '';
    
    const fieldName = getFieldName() || 'המדד';
    
    // Comprehensive pattern matching for PromQL expressions
    const descriptions: { pattern: RegExp; description: (match: RegExpMatchArray) => string }[] = [
      // Complex aggregations with grouping
      {
        pattern: /sum\(rate\(([^[]+)\[([^\]]+)\]\)\)\s*by\s*\(([^)]+)\)/i,
        description: (m) => `סכום קצב השינוי של ${fieldName} לפי: ${m[3]}, חושב על פני ${m[2]}`
      },
      {
        pattern: /avg\(rate\(([^[]+)\[([^\]]+)\]\)\)/i,
        description: (m) => `ממוצע קצב השינוי של ${fieldName} במהלך ${m[2]}`
      },
      // Histogram quantiles
      {
        pattern: /histogram_quantile\(([^,]+),\s*rate\(([^[]+)\[([^\]]+)\]\)\)/i,
        description: (m) => `percentile ${parseFloat(m[1]) * 100} של ${fieldName} במהלך ${m[3]}`
      },
      // Rate expressions
      {
        pattern: /rate\(([^[]+)\[([^\]]+)\]\)\s*>\s*([0-9.]+)/i,
        description: (m) => `בדיקה אם קצב השינוי של ${fieldName} (${m[2]}) גבוה מ-${m[3]}`
      },
      // Basic aggregations
      {
        pattern: /^sum\(([^)]+)\)\s+by\s+\(([^)]+)\)$/,
        description: (m) => `סכום ${fieldName} מקובץ לפי תוויות: ${m[2]}`
      },
      {
        pattern: /^avg\(([^)]+)\)\s+by\s+\(([^)]+)\)$/,
        description: (m) => `ממוצע ${fieldName} מקובץ לפי תוויות: ${m[2]}`
      },
      {
        pattern: /^sum\(([^)]+)\)$/,
        description: () => `סכום כל הערכים של ${fieldName} מכל המקורות`
      },
      {
        pattern: /^avg\(([^)]+)\)$/,
        description: () => `ממוצע של ${fieldName} מכל המקורות`
      },
      {
        pattern: /^max\(([^)]+)\)$/,
        description: () => `הערך המקסימלי של ${fieldName}`
      },
      {
        pattern: /^min\(([^)]+)\)$/,
        description: () => `הערך המינימלי של ${fieldName}`
      },
      {
        pattern: /^count\(([^)]+)\)$/,
        description: () => `ספירת כמה דגימות קיימות של ${fieldName}`
      },
      // Time-based functions
      {
        pattern: /^rate\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `קצב השינוי לשנייה של ${fieldName}, חושב על פני ${m[2]} אחרונות`
      },
      {
        pattern: /^irate\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `קצב שינוי מיידי של ${fieldName} על בסיס ${m[2]} אחרונות`
      },
      {
        pattern: /^increase\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `עלייה מצטברת של ${fieldName} במהלך ${m[2]} אחרונות`
      },
      {
        pattern: /^avg_over_time\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `ממוצע של ${fieldName} על פני ${m[2]} אחרונות`
      },
      {
        pattern: /^max_over_time\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `ערך מקסימלי של ${fieldName} שנמדד ב-${m[2]} אחרונות`
      },
      {
        pattern: /^min_over_time\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `ערך מינימלי של ${fieldName} שנמדד ב-${m[2]} אחרונות`
      },
      {
        pattern: /^sum_over_time\(([^[]+)\[([^\]]+)\]\)$/,
        description: (m) => `סכום כל המדידות של ${fieldName} על פני ${m[2]} אחרונות`
      },
      // Math operations
      {
        pattern: /\(([^)]+)\)\s*\/\s*\(([^)]+)\)\s*\*\s*100/,
        description: () => `חישוב אחוז/יחס עבור ${fieldName}`
      },
      {
        pattern: /\(([^)]+)\)\s*\+\s*\(([^)]+)\)/,
        description: () => `חיבור של ביטויים עבור ${fieldName}`
      },
      {
        pattern: /\(([^)]+)\)\s*-\s*\(([^)]+)\)/,
        description: () => `חיסור בין ביטויים עבור ${fieldName}`
      },
      // Comparisons
      {
        pattern: /(\S+)\s*>\s*([0-9.]+)/,
        description: (m) => `בדיקה אם ${fieldName} גדול מ-${m[2]}`
      },
      {
        pattern: /(\S+)\s*<\s*([0-9.]+)/,
        description: (m) => `בדיקה אם ${fieldName} קטן מ-${m[2]}`
      },
      {
        pattern: /(\S+)\s*==\s*([0-9.]+)/,
        description: (m) => `בדיקה אם ${fieldName} שווה ל-${m[2]}`
      },
      // Offset
      {
        pattern: /offset\s+([0-9]+[smhdwy])/i,
        description: (m) => `השוואה עם ${fieldName} מלפני ${m[1]}`
      }
    ];

    for (const { pattern, description } of descriptions) {
      const match = expr.match(pattern);
      if (match) {
        return description(match);
      }
    }

    // Fallback descriptions based on keywords
    if (expr.includes('histogram_quantile')) return `חישוב percentile עבור ${fieldName}`;
    if (expr.includes('rate') && expr.includes('[')) return `חישוב קצב שינוי של ${fieldName}`;
    if (expr.includes('sum') && expr.includes('by')) return `סכום ${fieldName} מקובץ לפי תוויות`;
    if (expr.includes('avg') && expr.includes('by')) return `ממוצע ${fieldName} מקובץ לפי תוויות`;
    if (expr.includes('rate')) return `קצב שינוי של ${fieldName}`;
    if (expr.includes('sum')) return `סכום של ${fieldName}`;
    if (expr.includes('avg')) return `ממוצע של ${fieldName}`;
    if (expr.includes('max')) return `מקסימום של ${fieldName}`;
    if (expr.includes('min')) return `מינימום של ${fieldName}`;
    if (expr.includes('count')) return `ספירה של ${fieldName}`;
    
    return `ביטוי PromQL מותאם אישית - ${expr.length > 50 ? 'ביטוי מורכב' : 'מחשב ערך עבור ' + fieldName}`;
  };

  return (
    <Card>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Space>
            <FunctionOutlined style={{ fontSize: 18, color: '#1890ff' }} />
            <Text strong style={{ fontSize: 14 }}>
              נוסחת חישוב (Formula)
            </Text>
          </Space>
          <div style={{ marginTop: 4 }}>
            <Text type="secondary" style={{ fontSize: 12 }}>
              הגדר כיצד לחשב את ערך המדד
            </Text>
          </div>
        </div>

        {/* Formula Type Selector */}
        <div>
          <Text strong>סוג נוסחה:</Text>
          <Select
            style={{ width: '100%', marginTop: 8 }}
            value={selectedType}
            onChange={handleFormulaTypeChange}
          >
            <Option value="simple">
              <Space>
                <Text>פשוט</Text>
                <Text type="secondary" style={{ fontSize: 11 }}>ערך ישיר מהשדה</Text>
              </Space>
            </Option>
            <Option value="promql">
              <Space>
                <Text>PromQL</Text>
                <Text type="secondary" style={{ fontSize: 11 }}>ביטוי חישוב מתקדם</Text>
              </Space>
            </Option>
            <Option value="recording">
              <Space>
                <Text>Recording Rule</Text>
                <Text type="secondary" style={{ fontSize: 11 }}>הפניה לכלל מוגדר</Text>
              </Space>
            </Option>
          </Select>
        </div>

        {/* Simple Type */}
        {selectedType === 'simple' && (
          <Alert
            message="ערך פשוט"
            description={
              <Space direction="vertical">
                <Text>המדד יציג את הערך הישיר מהשדה: <Tag>{fieldPath}</Tag></Text>
                <Text type="secondary" style={{ fontSize: 11 }}>
                  זוהי האופציה הפשוטה ביותר, מומלצת לרוב המקרים
                </Text>
              </Space>
            }
            type="info"
            showIcon
          />
        )}

        {/* PromQL Type */}
        {selectedType === 'promql' && (
          <>
            <div>
              <Text strong>תבנית PromQL:</Text>
              <Select
                style={{ width: '100%', marginTop: 8 }}
                value={selectedTemplate}
                onChange={handleTemplateChange}
                showSearch
                filterOption={(input, option) => {
                  const label = option?.label;
                  if (typeof label === 'string') {
                    return label.toLowerCase().includes(input.toLowerCase());
                  }
                  return false;
                }}
              >
                {FORMULA_TEMPLATES.map(template => (
                  <Option 
                    key={template.id} 
                    value={template.id}
                    label={template.nameHebrew}
                  >
                    <Space>
                      <Text strong>{template.nameHebrew}</Text>
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        {template.nameEnglish}
                      </Text>
                    </Space>
                  </Option>
                ))}
              </Select>
            </div>

            {selectedTemplate === 'custom' && (
              <>
                <Button
                  icon={<CodeOutlined />}
                  onClick={() => setShowPromQLHelper(true)}
                  block
                >
                  פתח עוזר ביטויי PromQL
                </Button>
                <div>
                  <Text strong>ביטוי PromQL:</Text>
                  <TextArea
                    style={{ marginTop: 8, fontFamily: 'Monaco, Consolas, monospace' }}
                    rows={3}
                    placeholder="לדוגמה: sum(rate(requests[5m])) by (status)"
                    value={customFormula}
                    onChange={(e) => handleCustomFormulaChange(e.target.value)}
                    status={customFormula && !expressionValid ? 'error' : ''}
                  />
                </div>
              </>
            )}

            {/* Generated Expression Preview */}
            {customFormula && selectedType === 'promql' && (
              <div>
                <Space>
                  <Text strong>נוסחה שנוצרה:</Text>
                  {expressionValid ? (
                    <Tag color="green" icon={<CheckCircleOutlined />}>תקינה</Tag>
                  ) : (
                    <Tag color="red" icon={<CloseCircleOutlined />}>שגיאה</Tag>
                  )}
                </Space>
                
                {/* Humanized Description - Key forces re-render on formula change */}
                <Alert
                  key={customFormula}
                  message={getExpressionDescription(customFormula)}
                  type="info"
                  showIcon
                  icon={<InfoCircleOutlined />}
                  style={{ marginTop: 8, marginBottom: 8 }}
                />
                
                <Card 
                  size="small" 
                  style={{ 
                    marginTop: 8,
                    backgroundColor: expressionValid ? '#f6f8fa' : '#fff2f0',
                    border: expressionValid ? '1px solid #d1d5da' : '1px solid #ffccc7'
                  }}
                >
                  <pre style={{ 
                    margin: 0,
                    fontFamily: 'Monaco, Consolas, monospace',
                    fontSize: 12,
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word'
                  }}>
                    {customFormula}
                  </pre>
                </Card>
                {!expressionValid && (
                  <Alert
                    message="ביטוי לא תקין"
                    description="בדוק סוגריים, תחביר ושמות פונקציות"
                    type="error"
                    showIcon
                    style={{ marginTop: 8 }}
                    icon={<CloseCircleOutlined />}
                  />
                )}
              </div>
            )}

            {/* Template Description */}
            {selectedTemplate !== 'custom' && selectedTemplate && (
              <Alert
                message={FORMULA_TEMPLATES.find(t => t.id === selectedTemplate)?.description}
                description={
                  <div style={{ marginTop: 8 }}>
                    <Text strong style={{ fontSize: 11 }}>דוגמה:</Text>
                    <pre style={{ 
                      fontSize: 11, 
                      background: '#ffffff',
                      padding: 8,
                      marginTop: 4,
                      borderRadius: 4,
                      border: '1px solid #d9d9d9'
                    }}>
                      {FORMULA_TEMPLATES.find(t => t.id === selectedTemplate)?.example}
                    </pre>
                  </div>
                }
                type="success"
                showIcon
                icon={<InfoCircleOutlined />}
              />
            )}
          </>
        )}

        {/* Recording Rule Type */}
        {selectedType === 'recording' && (
          <>
            <div>
              <Text strong>שם Recording Rule:</Text>
              <Input
                style={{ marginTop: 8, fontFamily: 'Monaco, Consolas, monospace' }}
                placeholder="לדוגמה: job:requests:rate5m"
                value={customFormula}
                onChange={(e) => {
                  setCustomFormula(e.target.value);
                  onChange(e.target.value, 'recording');
                }}
              />
            </div>
            <Alert
              message="הפניה ל-Recording Rule"
              description="הזן את השם המלא של Recording Rule קיים ב-Prometheus"
              type="info"
              showIcon
            />
          </>
        )}

        {/* Info Box */}
        <Alert
          message={
            <Space direction="vertical" size={4}>
              <Text strong style={{ fontSize: 12 }}>
                {selectedType === 'simple' && 'ערך פשוט - מומלץ לרוב המקרים'}
                {selectedType === 'promql' && 'PromQL - לחישובים מתקדמים'}
                {selectedType === 'recording' && 'Recording Rule - לביצועים מיטביים'}
              </Text>
              <Text style={{ fontSize: 11 }}>
                {selectedType === 'simple' && 'השתמש בערך הישיר מהשדה ללא חישובים נוספים'}
                {selectedType === 'promql' && 'השתמש בפונקציות Prometheus לצבירה, קצב שינוי ועוד'}
                {selectedType === 'recording' && 'הפנה למדד מחושב מראש בPrometheus'}
              </Text>
            </Space>
          }
          type={selectedType === 'simple' ? 'success' : 'info'}
          showIcon
          style={{ fontSize: 12 }}
        />

        {/* PromQL Expression Helper Dialog */}
        <PromQLExpressionHelperDialog
          visible={showPromQLHelper}
          onClose={() => setShowPromQLHelper(false)}
          onSelect={handlePromQLHelperSelect}
          currentExpression={customFormula}
          metricName={getFieldName()}
        />
      </Space>
    </Card>
  );
};

export default FormulaBuilder;
