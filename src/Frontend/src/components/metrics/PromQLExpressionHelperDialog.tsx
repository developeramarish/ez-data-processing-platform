import React, { useState, useEffect } from 'react';
import { Modal, Tabs, Space, Typography, Card, Input, Button, Tag, Alert, Divider } from 'antd';
import { CodeOutlined, BookOutlined, ThunderboltOutlined, FunctionOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';

const { Text } = Typography;
const { TextArea } = Input;

interface PromQLExpressionHelperDialogProps {
  visible: boolean;
  onClose: () => void;
  onSelect: (expression: string) => void;
  currentExpression?: string;
  metricName?: string;
}

const PROMQL_FUNCTIONS = [
  {
    category: 'צבירה (Aggregation)',
    functions: [
      { name: 'sum', syntax: 'sum(metric)', description: 'סכום כל הערכים', example: 'sum(http_requests_total)', detail: 'מחשב את הסכום של כל הערכים. שימושי לצבירת בקשות, שגיאות וכו\'' },
      { name: 'avg', syntax: 'avg(metric)', description: 'ממוצע', example: 'avg(cpu_usage)', detail: 'מחשב ממוצע של כל הערכים' },
      { name: 'min', syntax: 'min(metric)', description: 'מינימום', example: 'min(response_time)', detail: 'מוצא את הערך המינימלי' },
      { name: 'max', syntax: 'max(metric)', description: 'מקסימום', example: 'max(memory_usage)', detail: 'מוצא את הערך המקסימלי' },
      { name: 'count', syntax: 'count(metric)', description: 'ספירה', example: 'count(up == 1)', detail: 'סופר כמה ערכים יש' },
      { name: 'sum by', syntax: 'sum(metric) by (label)', description: 'סכום לפי תווית', example: 'sum(requests) by (status)', detail: 'צובר לפי קבוצות של תוויות' },
      { name: 'avg by', syntax: 'avg(metric) by (label)', description: 'ממוצע לפי תווית', example: 'avg(latency) by (endpoint)', detail: 'ממוצע לכל קבוצה' },
      { name: 'topk', syntax: 'topk(5, metric)', description: 'K הגבוהים ביותר', example: 'topk(10, cpu_usage)', detail: 'מחזיר את ה-K ערכים הגבוהים ביותר' },
      { name: 'bottomk', syntax: 'bottomk(5, metric)', description: 'K הנמוכים ביותר', example: 'bottomk(5, free_memory)', detail: 'מחזיר את ה-K ערכים הנמוכים ביותר' }
    ]
  },
  {
    category: 'זמן (Time)',
    functions: [
      { name: 'rate', syntax: 'rate(metric[5m])', description: 'קצב שינוי לשנייה', example: 'rate(requests_total[5m])', detail: 'מחשב קצב שינוי לשנייה עבור counter. חובה למדדי counter' },
      { name: 'irate', syntax: 'irate(metric[5m])', description: 'קצב מיידי', example: 'irate(errors_total[1m])', detail: 'קצב מיידי - רגיש יותר לשינויים פתאומיים' },
      { name: 'increase', syntax: 'increase(metric[1h])', description: 'עלייה בפרק זמן', example: 'increase(sales_total[1h])', detail: 'כמה עלה המונה בפרק הזמן הנתון' },
      { name: 'avg_over_time', syntax: 'avg_over_time(metric[5m])', description: 'ממוצע על פני זמן', example: 'avg_over_time(cpu[10m])', detail: 'ממוצע של כל הערכים בחלון הזמן' },
      { name: 'max_over_time', syntax: 'max_over_time(metric[5m])', description: 'מקסימום על פני זמן', example: 'max_over_time(latency[1h])', detail: 'הערך המקסימלי בחלון הזמן' },
      { name: 'min_over_time', syntax: 'min_over_time(metric[5m])', description: 'מינימום על פני זמן', example: 'min_over_time(free_memory[30m])', detail: 'הערך המינימלי בחלון הזמן' },
      { name: 'sum_over_time', syntax: 'sum_over_time(metric[5m])', description: 'סכום על פני זמן', example: 'sum_over_time(errors[1h])', detail: 'סכום כל הערכים בחלון הזמן' },
      { name: 'changes', syntax: 'changes(metric[5m])', description: 'מספר שינויים', example: 'changes(version[1h])', detail: 'כמה פעמים הערך השתנה' },
      { name: 'deriv', syntax: 'deriv(metric[5m])', description: 'נגזרת', example: 'deriv(disk_usage[1h])', detail: 'קצב השינוי של gauge - נגזרת לפי זמן' }
    ]
  },
  {
    category: 'פעולות חשבון (Math)',
    functions: [
      { name: 'abs', syntax: 'abs(metric)', description: 'ערך מוחלט', example: 'abs(temperature)', detail: 'מחזיר ערך מוחלט (ללא סימן)' },
      { name: 'ceil', syntax: 'ceil(metric)', description: 'עיגול למעלה', example: 'ceil(cpu_usage)', detail: 'עיגול כלפי מעלה למספר שלם' },
      { name: 'floor', syntax: 'floor(metric)', description: 'עיגול למטה', example: 'floor(response_time)', detail: 'עיגול כלפי מטה למספר שלם' },
      { name: 'round', syntax: 'round(metric)', description: 'עיגול', example: 'round(memory_percent)', detail: 'עיגול רגיל למספר השלם הקרוב' },
      { name: 'clamp_max', syntax: 'clamp_max(metric, 100)', description: 'הגבל מקסימום', example: 'clamp_max(cpu, 100)', detail: 'מגביל ערך מקסימלי' },
      { name: 'clamp_min', syntax: 'clamp_min(metric, 0)', description: 'הגבל מינימום', example: 'clamp_min(temp, 0)', detail: 'מגביל ערך מינימלי' },
      { name: 'sqrt', syntax: 'sqrt(metric)', description: 'שורש ריבועי', example: 'sqrt(variance)', detail: 'מחשב שורש ריבועי' },
      { name: 'ln', syntax: 'ln(metric)', description: 'לוגריתם טבעי', example: 'ln(growth_rate)', detail: 'לוגריתם בבסיס e' },
      { name: 'log2', syntax: 'log2(metric)', description: 'לוגריתם בסיס 2', example: 'log2(data_size)', detail: 'לוגריתם בבסיס 2' },
      { name: 'log10', syntax: 'log10(metric)', description: 'לוגריתם בסיס 10', example: 'log10(population)', detail: 'לוגריתם בבסיס 10' }
    ]
  },
  {
    category: 'השוואות (Comparison)',
    functions: [
      { name: 'greater', syntax: 'metric > 10', description: 'גדול מ (>)', example: 'cpu_usage > 80' },
      { name: 'less', syntax: 'metric < 10', description: 'קטן מ (<)', example: 'free_memory < 1000' },
      { name: 'equals', syntax: 'metric == 1', description: 'שווה ל (==)', example: 'up == 1' },
      { name: 'not_equals', syntax: 'metric != 0', description: 'לא שווה ל (!=)', example: 'errors != 0' },
      { name: 'absent', syntax: 'absent(metric)', description: 'בדוק היעדרות', example: 'absent(heartbeat)' }
    ]
  },
  {
    category: 'אופרטורים לוגיים (Logical)',
    functions: [
      { name: 'and', syntax: 'metric1 and metric2', description: 'וגם', example: 'up == 1 and cpu < 90' },
      { name: 'or', syntax: 'metric1 or metric2', description: 'או', example: 'errors > 10 or latency > 5' },
      { name: 'unless', syntax: 'metric1 unless metric2', description: 'אלא אם כן', example: 'alerts unless maintenance' }
    ]
  }
];

const COMMON_PATTERNS = [
  {
    name: 'שיעור שגיאות',
    expression: '(sum(rate(errors_total[5m])) / sum(rate(requests_total[5m]))) * 100',
    description: 'חישוב אחוז שגיאות מתוך כלל הבקשות',
    category: 'errors',
    useCase: 'מעקב אחר איכות השירות - מתריע כאשר שיעור השגיאות גבוה מדי'
  },
  {
    name: 'ממוצע זמן תגובה P95',
    expression: 'histogram_quantile(0.95, rate(response_time_bucket[5m]))',
    description: 'חישוב percentile 95 לזמן תגובה',
    category: 'performance',
    useCase: '95% מהבקשות מסתיימות תחת זמן זה - מדד טוב לביצועים'
  },
  {
    name: 'שימוש CPU ממוצע',
    expression: 'avg(rate(cpu_seconds_total[5m])) * 100',
    description: 'שימוש ממוצע ב-CPU באחוזים',
    category: 'resources',
    useCase: 'מעקב אחר עומס CPU על השרתים'
  },
  {
    name: 'קצב בקשות לשנייה',
    expression: 'sum(rate(http_requests_total[5m]))',
    description: 'מספר בקשות HTTP לשנייה',
    category: 'throughput',
    useCase: 'מדידת throughput של האפליקציה'
  },
  {
    name: 'זיהוי שירותים לא פעילים',
    expression: 'up == 0',
    description: 'מציאת שירותים שאינם זמינים',
    category: 'availability',
    useCase: 'התראה מיידית על שירות שנפל'
  },
  {
    name: 'שימוש זיכרון באחוזים',
    expression: '(1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100',
    description: 'חישוב אחוז שימוש בזיכרון',
    category: 'resources',
    useCase: 'מעקב אחר צריכת זיכרון'
  },
  {
    name: 'תפוסת דיסק באחוזים',
    expression: '(1 - (node_filesystem_avail_bytes / node_filesystem_size_bytes)) * 100',
    description: 'חישוב אחוז תפוסת דיסק',
    category: 'resources',
    useCase: 'התראה על מחסור בשטח אחסון'
  },
  {
    name: 'קצב עלייה חד',
    expression: '(metric - metric offset 1h) > 1000',
    description: 'זיהוי עלייה חדה במדד',
    category: 'anomaly',
    useCase: 'גילוי חריגות - עלייה משמעותית במדד'
  },
  {
    name: 'שינוי באחוזים',
    expression: '((metric - metric offset 1h) / metric offset 1h) * 100',
    description: 'אחוז השינוי לעומת שעה קודמת',
    category: 'trends',
    useCase: 'מעקב אחר מגמות ושינויים'
  },
  {
    name: 'זמינות שירות',
    expression: 'avg_over_time(up[24h]) * 100',
    description: 'אחוז זמינות שירות ב-24 שעות אחרונות',
    category: 'availability',
    useCase: 'מדידת SLA - זמינות השירות'
  }
];

const PromQLExpressionHelperDialog: React.FC<PromQLExpressionHelperDialogProps> = ({
  visible,
  onClose,
  onSelect,
  currentExpression = '',
  metricName = 'metric'
}) => {
  const [expression, setExpression] = useState(currentExpression);
  const [validationResult, setValidationResult] = useState<{ valid: boolean; message: string } | null>(null);

  useEffect(() => {
    if (visible) {
      setExpression(currentExpression);
    }
  }, [visible, currentExpression]);

  // Replace 'metric' placeholder with actual metric name in syntax
  const getActualSyntax = (syntax: string): string => {
    if (!metricName || metricName === 'metric') return syntax;
    return syntax.replace(/\bmetric\b/g, metricName);
  };

  // Basic PromQL validation
  useEffect(() => {
    if (!expression) {
      setValidationResult(null);
      return;
    }

    // Basic validation rules
    const squareBrackets = expression.match(/[[\]]/g);
    const curlyBrackets = expression.match(/[{}]/g);
    const openParens = expression.match(/[(]/g);
    const closeParens = expression.match(/[)]/g);
    
    const checks = [
      { test: expression.length > 0, message: 'ביטוי לא יכול להיות ריק' },
      { test: !expression.includes('  '), message: 'הסר רווחים כפולים' },
      { test: !squareBrackets || squareBrackets.length % 2 === 0, message: 'סוגריים מרובעות לא מאוזנות' },
      { test: !curlyBrackets || curlyBrackets.length % 2 === 0, message: 'סוגריים מסולסלות לא מאוזנות' },
      { test: (openParens?.length || 0) === (closeParens?.length || 0), message: 'סוגריים עגולות לא מאוזנות' }
    ];

    const failedCheck = checks.find(c => !c.test);
    if (failedCheck) {
      setValidationResult({ valid: false, message: failedCheck.message });
    } else {
      setValidationResult({ valid: true, message: 'הביטוי נראה תקין' });
    }
  }, [expression]);

  const handleInsertFunction = (syntax: string) => {
    setExpression(prev => prev + (prev ? ' ' : '') + syntax);
  };

  const handleUsePattern = (pattern: string) => {
    setExpression(pattern);
  };

  const handleApply = () => {
    onSelect(expression);
    onClose();
  };

  return (
    <Modal
      title={
        <Space>
          <CodeOutlined />
          <span>עוזר ביטויי PromQL - בונה ביטויים אינטראקטיבי</span>
        </Space>
      }
      open={visible}
      onCancel={onClose}
      width={1000}
      style={{ top: 20 }}
      footer={[
        <Button key="cancel" onClick={onClose}>
          ביטול
        </Button>,
        <Button 
          key="apply" 
          type="primary" 
          onClick={handleApply}
          disabled={validationResult?.valid === false}
          icon={validationResult?.valid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
        >
          השתמש בביטוי
        </Button>
      ]}
    >
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        {/* Expression Editor */}
        <Card size="small">
          <Space direction="vertical" style={{ width: '100%' }} size="small">
            <Text strong>ביטוי PromQL:</Text>
            <TextArea
              className="ltr-field"
              rows={5}
              value={expression}
              onChange={(e) => setExpression(e.target.value)}
              placeholder="הזן ביטוי PromQL או השתמש בפונקציות ובתבניות למטה&#10;דוגמה: rate(http_requests_total[5m]) > 100"
              style={{ 
                fontSize: 13,
                backgroundColor: '#f6f8fa'
              }}
              status={validationResult?.valid === false ? 'error' : validationResult?.valid ? '' : ''}
            />
            {validationResult && (
              <Alert
                message={validationResult.valid ? 'תקין' : 'שגיאה'}
                description={validationResult.message}
                type={validationResult.valid ? 'success' : 'error'}
                showIcon
                icon={validationResult.valid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
              />
            )}
          </Space>
        </Card>

        <Divider style={{ margin: '12px 0' }} />

        <Tabs
          items={[
            {
              key: 'functions',
              label: (
                <Space>
                  <FunctionOutlined />
                  <span>פונקציות</span>
                </Space>
              ),
              children: (
                <div>
                  {PROMQL_FUNCTIONS.map((category, idx) => (
                    <div key={idx} style={{ marginBottom: 16 }}>
                      <Text strong>{category.category}</Text>
                      <Space wrap style={{ marginTop: 8 }}>
                        {category.functions.map((func, fidx) => (
                          <Card
                            key={fidx}
                            size="small"
                            hoverable
                            onClick={() => handleInsertFunction(getActualSyntax(func.syntax))}
                            style={{ cursor: 'pointer', width: '100%', marginBottom: 8 }}
                          >
                            <Space direction="vertical" size={2} style={{ width: '100%' }}>
                              <Space>
                                <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                                  {func.name}
                                </Tag>
                                <Text strong style={{ fontSize: 12 }}>{func.description}</Text>
                              </Space>
                              <Text type="secondary" style={{ fontSize: 10 }}>
                                {(func as any).detail}
                              </Text>
                              <Card size="small" style={{ backgroundColor: '#f0f0f0', marginTop: 4 }}>
                                <Text type="secondary" style={{ fontSize: 10, fontFamily: 'monospace', direction: 'ltr' }}>
                                  {getActualSyntax(func.example)}
                                </Text>
                              </Card>
                            </Space>
                          </Card>
                        ))}
                      </Space>
                    </div>
                  ))}
                </div>
              )
            },
            {
              key: 'patterns',
              label: (
                <Space>
                  <ThunderboltOutlined />
                  <span>תבניות נפוצות</span>
                </Space>
              ),
              children: (
                <Space direction="vertical" style={{ width: '100%' }} size="small">
                  {COMMON_PATTERNS.map((pattern, idx) => (
                    <Card
                      key={idx}
                      size="small"
                      hoverable
                      onClick={() => handleUsePattern(pattern.expression)}
                      style={{ cursor: 'pointer', marginBottom: 8 }}
                    >
                      <Space direction="vertical" size={6} style={{ width: '100%' }}>
                        <Space>
                          <Text strong style={{ fontSize: 14 }}>{pattern.name}</Text>
                          <Tag color="purple">{(pattern as any).category}</Tag>
                        </Space>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          {pattern.description}
                        </Text>
                        <Text style={{ fontSize: 11, color: '#666' }}>
                          <strong>שימוש:</strong> {(pattern as any).useCase}
                        </Text>
                        <Card size="small" style={{ backgroundColor: '#f6f8fa', marginTop: 4 }}>
                          <pre style={{ 
                            margin: 0, 
                            fontFamily: 'Monaco, Consolas, monospace',
                            fontSize: 11,
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word',
                            direction: 'ltr',
                            textAlign: 'left'
                          }}>
                            {pattern.expression}
                          </pre>
                        </Card>
                        <Button size="small" type="dashed" block>
                          לחץ להשתמש בתבנית זו
                        </Button>
                      </Space>
                    </Card>
                  ))}
                </Space>
              )
            },
            {
              key: 'docs',
              label: (
                <Space>
                  <BookOutlined />
                  <span>עזרה</span>
                </Space>
              ),
              children: (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                  <Alert
                    message="תחביר PromQL"
                    description={
                      <div style={{ fontSize: 12 }}>
                        <div><strong>בחירת מדד:</strong> <code>metric_name</code></div>
                        <div><strong>עם תוויות:</strong> <code>{`metric_name{label="value"}`}</code></div>
                        <div><strong>טווח זמן:</strong> <code>metric_name[5m]</code></div>
                        <div><strong>פעולות חשבון:</strong> <code>metric1 + metric2</code></div>
                        <div><strong>צבירה:</strong> <code>sum(metric) by (label)</code></div>
                      </div>
                    }
                    type="info"
                    showIcon
                  />
                  
                  <Card size="small" title="יחידות זמן">
                    <Space wrap>
                      <Tag>s = שניות</Tag>
                      <Tag>m = דקות</Tag>
                      <Tag>h = שעות</Tag>
                      <Tag>d = ימים</Tag>
                      <Tag>w = שבועות</Tag>
                      <Tag>y = שנים</Tag>
                    </Space>
                  </Card>

                  <Card size="small" title="אופרטורים">
                    <Space wrap>
                      <Tag>+ - * / %</Tag>
                      <Tag>{'== != > < >= <='}</Tag>
                      <Tag>and or unless</Tag>
                    </Space>
                  </Card>

                  <Alert
                    message="דוגמאות נוספות"
                    description={
                      <pre style={{ fontSize: 11, fontFamily: 'monospace', margin: 0 }}>
                        {`rate(metric[5m]) > 0.1
sum(metric) by (instance)
metric{job="app"} / 1024
avg(metric) without (instance)`}
                      </pre>
                    }
                    type="info"
                  />
                </Space>
              )
            }
          ]}
        />
      </Space>
    </Modal>
  );
};

export default PromQLExpressionHelperDialog;
