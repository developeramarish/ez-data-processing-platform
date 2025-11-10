import React, { useState, useEffect } from 'react';
import { Card, Input, Space, Typography, Tag, Alert, Button } from 'antd';
import { TranslationOutlined, CopyOutlined, CheckOutlined } from '@ant-design/icons';

const { Text, Title } = Typography;
const { TextArea } = Input;

interface MetricNameHelperProps {
  hebrewDescription?: string;
  fieldName?: string;
  prometheusType?: string;
  value?: string;
  onChange?: (name: string) => void;
}

/**
 * Hebrew to English dictionary for metric names
 * Maps common Hebrew terms to Prometheus naming conventions
 */
const HEBREW_TO_ENGLISH_DICT: Record<string, string> = {
  // Transaction terms
  'עסקה': 'transaction',
  'עסקאות': 'transactions',
  'מכירה': 'sale',
  'מכירות': 'sales',
  'קנייה': 'purchase',
  'קניות': 'purchases',
  'העברה': 'transfer',
  'העברות': 'transfers',
  'תשלום': 'payment',
  'תשלומים': 'payments',
  
  // Customer terms
  'לקוח': 'customer',
  'לקוחות': 'customers',
  'משתמש': 'user',
  'משתמשים': 'users',
  'חשבון': 'account',
  'חשבונות': 'accounts',
  
  // Amount terms
  'סכום': 'amount',
  'סכומים': 'amounts',
  'כסף': 'money',
  'שווי': 'value',
  'מחיר': 'price',
  'עלות': 'cost',
  'הכנסה': 'revenue',
  'רווח': 'profit',
  'הפסד': 'loss',
  
  // Count terms
  'מונה': 'count',
  'ספירה': 'count',
  'מספר': 'number',
  'כמות': 'quantity',
  'סך': 'total',
  
  // Status terms
  'מצב': 'status',
  'סטטוס': 'status',
  'מוצלח': 'successful',
  'כשל': 'failed',
  'שגיאה': 'error',
  'שגיאות': 'errors',
  'תקין': 'valid',
  'לא תקין': 'invalid',
  'פעיל': 'active',
  'לא פעיל': 'inactive',
  
  // Time terms
  'יומי': 'daily',
  'שעתי': 'hourly',
  'שבועי': 'weekly',
  'חודשי': 'monthly',
  'שנתי': 'yearly',
  'זמן': 'time',
  'משך': 'duration',
  'תקופה': 'period',
  
  // Performance terms
  'מהירות': 'speed',
  'ביצועים': 'performance',
  'תגובה': 'response',
  'עיבוד': 'processing',
  'המתנה': 'waiting',
  'איחור': 'latency',
  
  // System terms
  'מערכת': 'system',
  'שירות': 'service',
  'שרת': 'server',
  'בקשה': 'request',
  'בקשות': 'requests',
  'תהליך': 'process',
  'תהליכים': 'processes',
  
  // Business terms
  'עסקי': 'business',
  'ארגוני': 'organizational',
  'פיננסי': 'financial',
  'תפעולי': 'operational',
  'אסטרטגי': 'strategic',
  
  // Metric suffixes
  'ממוצע': 'average',
  'מקסימום': 'maximum',
  'מינימום': 'minimum',
  'סטיית תקן': 'stddev',
  'אחוז': 'percentage',
  'יחס': 'ratio',
  'שיעור': 'rate'
};

/**
 * Translate Hebrew text to English using dictionary
 */
const translateHebrew = (text: string): string[] => {
  const words = text
    .toLowerCase()
    .split(/[\s,،]+/)
    .filter(w => w.length > 0);
  
  const translatedWords: string[] = [];
  
  for (const word of words) {
    const translation = HEBREW_TO_ENGLISH_DICT[word];
    if (translation) {
      translatedWords.push(translation);
    }
  }
  
  return translatedWords;
};

/**
 * Generate metric name suggestion based on inputs
 */
const suggestMetricName = (
  hebrewDesc: string,
  fieldName: string,
  prometheusType: string
): string => {
  const parts: string[] = [];
  
  // Start with field name if available
  if (fieldName) {
    const cleanFieldName = fieldName.replace(/^\$\./, '').replace(/\./g, '_');
    parts.push(cleanFieldName);
  }
  
  // Add translated Hebrew words
  if (hebrewDesc) {
    const translated = translateHebrew(hebrewDesc);
    parts.push(...translated);
  }
  
  // Add Prometheus type suffix for counters/histograms/summaries
  if (prometheusType === 'counter') {
    if (!parts.some(p => p === 'total' || p.endsWith('_total'))) {
      parts.push('total');
    }
  } else if (prometheusType === 'histogram' && !parts.some(p => p === 'bucket')) {
    parts.push('seconds'); // Common for histograms
  } else if (prometheusType === 'summary' && !parts.some(p => p === 'summary')) {
    parts.push('seconds'); // Common for summaries
  }
  
  // Join with underscores and clean up
  let suggestion = parts.join('_');
  
  // Clean up multiple underscores
  suggestion = suggestion.replace(/_+/g, '_');
  
  // Ensure valid Prometheus naming
  suggestion = suggestion.replace(/[^a-z0-9_]/g, '_');
  suggestion = suggestion.replace(/^[^a-z]/g, ''); // Must start with letter
  suggestion = suggestion.replace(/_$/g, ''); // Remove trailing underscore
  
  return suggestion || 'metric_name';
};

/**
 * Validate Prometheus metric name
 */
const validatePrometheusName = (name: string): { valid: boolean; error?: string } => {
  if (!name) {
    return { valid: false, error: 'שם המדד הוא שדה חובה' };
  }
  
  if (!/^[a-z][a-z0-9_]*$/.test(name)) {
    return {
      valid: false,
      error: 'שם מדד Prometheus חייב להתחיל באות קטנה ולהכיל רק אותיות קטנות, מספרים וקו תחתון'
    };
  }
  
  if (name.length < 3) {
    return { valid: false, error: 'שם המדד חייב להכיל לפחות 3 תווים' };
  }
  
  if (name.length > 100) {
    return { valid: false, error: 'שם המדד ארוך מדי (מקסימום 100 תווים)' };
  }
  
  return { valid: true };
};

/**
 * MetricNameHelper component
 * Helps users translate Hebrew descriptions to valid Prometheus metric names
 */
const MetricNameHelper: React.FC<MetricNameHelperProps> = ({
  hebrewDescription = '',
  fieldName = '',
  prometheusType = 'gauge',
  value = '',
  onChange
}) => {
  const [localValue, setLocalValue] = useState(value);
  const [suggestion, setSuggestion] = useState('');
  const [copied, setCopied] = useState(false);
  const [validation, setValidation] = useState<{ valid: boolean; error?: string }>({ valid: true });

  // Generate suggestion when inputs change
  useEffect(() => {
    if (hebrewDescription || fieldName) {
      const suggested = suggestMetricName(hebrewDescription, fieldName, prometheusType);
      setSuggestion(suggested);
    }
  }, [hebrewDescription, fieldName, prometheusType]);

  // Validate on value change
  useEffect(() => {
    const result = validatePrometheusName(localValue);
    setValidation(result);
  }, [localValue]);

  const handleApplySuggestion = () => {
    setLocalValue(suggestion);
    onChange?.(suggestion);
  };

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(localValue);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newValue = e.target.value;
    setLocalValue(newValue);
    onChange?.(newValue);
  };

  return (
    <Card size="small" style={{ marginBottom: 16 }}>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Space>
            <TranslationOutlined style={{ fontSize: 18, color: '#1890ff' }} />
            <Title level={5} style={{ margin: 0 }}>
              עוזר תרגום שם המדד
            </Title>
          </Space>
          <Text type="secondary" style={{ fontSize: 12 }}>
            מתרגם תיאור בעברית לשם מדד Prometheus תקין
          </Text>
        </div>

        {suggestion && (
          <Alert
            message="הצעה לשם מדד"
            description={
              <Space direction="vertical" style={{ width: '100%' }}>
                <div>
                  <Tag color="blue" style={{ fontSize: 14, padding: '4px 12px' }}>
                    {suggestion}
                  </Tag>
                </div>
                <Button size="small" type="primary" onClick={handleApplySuggestion}>
                  השתמש בהצעה זו
                </Button>
              </Space>
            }
            type="info"
            showIcon
          />
        )}

        <div>
          <Text strong>שם מדד Prometheus:</Text>
          <Space.Compact style={{ width: '100%', marginTop: 8 }}>
            <TextArea
              value={localValue}
              onChange={handleChange}
              placeholder="לדוגמה: transaction_amount_total"
              rows={2}
              status={validation.valid ? '' : 'error'}
            />
            <Button
              icon={copied ? <CheckOutlined /> : <CopyOutlined />}
              onClick={handleCopy}
              disabled={!localValue}
            >
              {copied ? 'הועתק' : 'העתק'}
            </Button>
          </Space.Compact>
          {!validation.valid && (
            <Text type="danger" style={{ fontSize: 12 }}>
              {validation.error}
            </Text>
          )}
        </div>

        <Alert
          message="כללי שמות Prometheus"
          description={
            <ul style={{ margin: 0, paddingRight: 20 }}>
              <li>חייב להתחיל באות קטנה (a-z)</li>
              <li>יכול להכיל אותיות קטנות, מספרים וקו תחתון (_)</li>
              <li>מומלץ להשתמש בסיומות: _total (counter), _seconds (histogram/summary)</li>
              <li>דוגמאות: http_requests_total, response_time_seconds, active_users</li>
            </ul>
          }
          type="info"
          showIcon
          style={{ fontSize: 12 }}
        />

        {hebrewDescription && (
          <div>
            <Text type="secondary" style={{ fontSize: 12 }}>
              <strong>זוהו מילים:</strong>{' '}
              {translateHebrew(hebrewDescription).map((word, idx) => (
                <Tag key={idx} color="geekblue" style={{ marginBottom: 4 }}>
                  {word}
                </Tag>
              ))}
            </Text>
          </div>
        )}
      </Space>
    </Card>
  );
};

export default MetricNameHelper;
export { suggestMetricName, validatePrometheusName, translateHebrew };
