import React, { useState, useEffect } from 'react';
import { Card, Input, Space, Typography, Tag, Alert } from 'antd';
import { TagsOutlined, CheckCircleOutlined, WarningOutlined } from '@ant-design/icons';

const { Text, Title } = Typography;
const { TextArea } = Input;

interface SimpleLabelInputProps {
  value?: string;
  onChange?: (labelNames: string, promqlExpression: string) => void;
}

/**
 * Validate label name according to Prometheus naming rules
 */
const validateLabelName = (name: string): boolean => {
  // Prometheus label names must match: [a-zA-Z_][a-zA-Z0-9_]*
  return /^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name);
};

/**
 * Generate PromQL labels expression from comma-separated label names
 * Input: "status, region, customer_type"
 * Output: {status="$status", region="$region", customer_type="$customer_type"}
 */
const generatePromQLExpression = (labelNames: string): string => {
  if (!labelNames || !labelNames.trim()) {
    return '';
  }

  const labels = labelNames
    .split(',')
    .map(l => l.trim())
    .filter(l => l.length > 0);

  if (labels.length === 0) {
    return '';
  }

  const promqlParts = labels.map(label => `${label}="$${label}"`);
  return `{${promqlParts.join(', ')}}`;
};

/**
 * Parse label names from input and validate
 */
const parseLabels = (input: string): { 
  labels: string[]; 
  valid: string[];
  invalid: string[];
} => {
  const labels = input
    .split(',')
    .map(l => l.trim())
    .filter(l => l.length > 0);

  const valid: string[] = [];
  const invalid: string[] = [];

  labels.forEach(label => {
    if (validateLabelName(label)) {
      valid.push(label);
    } else {
      invalid.push(label);
    }
  });

  return { labels, valid, invalid };
};

/**
 * SimpleLabelInput component
 * Allows users to enter label names as simple comma-separated text
 * and automatically generates PromQL expression
 */
const SimpleLabelInput: React.FC<SimpleLabelInputProps> = ({
  value = '',
  onChange
}) => {
  const [localValue, setLocalValue] = useState(value);
  const [promqlExpression, setPromqlExpression] = useState('');
  const [parsedLabels, setParsedLabels] = useState<{ labels: string[]; valid: string[]; invalid: string[] }>({
    labels: [],
    valid: [],
    invalid: []
  });

  // Parse and validate labels when input changes
  useEffect(() => {
    const parsed = parseLabels(localValue);
    setParsedLabels(parsed);

    // Generate PromQL expression
    const expression = generatePromQLExpression(localValue);
    setPromqlExpression(expression);

    // Notify parent
    onChange?.(localValue, expression);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [localValue]); // Only depend on localValue, not onChange to prevent infinite loop

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setLocalValue(e.target.value);
  };

  const hasInvalidLabels = parsedLabels.invalid.length > 0;
  const hasValidLabels = parsedLabels.valid.length > 0;

  return (
    <Card size="small" style={{ marginBottom: 16 }}>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Space>
            <TagsOutlined style={{ fontSize: 18, color: '#1890ff' }} />
            <Title level={5} style={{ margin: 0 }}>
              תוויות (Labels) למדד
            </Title>
          </Space>
          <Text type="secondary" style={{ fontSize: 12 }}>
            הזן שמות תוויות מופרדים בפסיק. המערכת תייצר אוטומטית ביטוי PromQL
          </Text>
        </div>

        <div>
          <Text strong>שמות תוויות:</Text>
          <TextArea
            value={localValue}
            onChange={handleChange}
            placeholder="לדוגמה: status, region, customer_type"
            rows={2}
            status={hasInvalidLabels ? 'error' : ''}
            style={{ marginTop: 8 }}
          />
          <Text type="secondary" style={{ fontSize: 11, display: 'block', marginTop: 4 }}>
            הזן שמות תוויות מופרדים בפסיק. שמות חייבים להתחיל באות או קו תחתון
          </Text>
        </div>

        {hasValidLabels && (
          <div>
            <Text strong>תוויות תקינות:</Text>
            <div style={{ marginTop: 8 }}>
              {parsedLabels.valid.map((label, idx) => (
                <Tag 
                  key={idx} 
                  color="green" 
                  icon={<CheckCircleOutlined />}
                  style={{ marginBottom: 4 }}
                >
                  {label}
                </Tag>
              ))}
            </div>
          </div>
        )}

        {hasInvalidLabels && (
          <Alert
            message="תוויות לא תקינות"
            description={
              <div>
                <Text>התוויות הבאות אינן תואמות לכללי Prometheus:</Text>
                <div style={{ marginTop: 8 }}>
                  {parsedLabels.invalid.map((label, idx) => (
                    <Tag 
                      key={idx} 
                      color="red" 
                      icon={<WarningOutlined />}
                      style={{ marginBottom: 4 }}
                    >
                      {label}
                    </Tag>
                  ))}
                </div>
                <Text type="secondary" style={{ fontSize: 11, marginTop: 8, display: 'block' }}>
                  שמות תוויות חייבים להתחיל באות (a-z, A-Z) או קו תחתון (_) ולהכיל רק אותיות, מספרים וקו תחתון
                </Text>
              </div>
            }
            type="error"
            showIcon
          />
        )}

        {promqlExpression && (
          <div>
            <Text strong>ביטוי PromQL שנוצר:</Text>
            <Card 
              size="small" 
              style={{ 
                marginTop: 8, 
                backgroundColor: '#f6f8fa',
                border: '1px solid #d1d5da'
              }}
            >
              <pre style={{ 
                margin: 0, 
                fontFamily: 'Monaco, Consolas, monospace',
                fontSize: 13,
                color: '#24292e',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}>
                {promqlExpression}
              </pre>
            </Card>
          </div>
        )}

        <Alert
          message="דוגמאות לשימוש"
          description={
            <div style={{ fontSize: 12 }}>
              <div style={{ marginBottom: 8 }}>
                <strong>קלט:</strong> <code>status, region</code>
              </div>
              <div style={{ marginBottom: 8 }}>
                <strong>PromQL:</strong> <code>{`{status="$status", region="$region"}`}</code>
              </div>
              <div style={{ marginTop: 12 }}>
                התוויות משמשות לסינון וקיבוץ נתונים ב-Prometheus. בחר תוויות שמתאימות לשדות בסכמת הנתונים.
              </div>
            </div>
          }
          type="info"
          showIcon
          style={{ fontSize: 12 }}
        />
      </Space>
    </Card>
  );
};

export default SimpleLabelInput;
export { generatePromQLExpression, validateLabelName, parseLabels };
