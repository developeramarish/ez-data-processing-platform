import React from 'react';
import {
  Card, Space, Typography, Alert, Tag
} from 'antd';
import {
  FunctionOutlined, InfoCircleOutlined
} from '@ant-design/icons';

const { Text } = Typography;

interface FormulaBuilderProps {
  fieldPath: string;
  formula: string;
  onChange: (formula: string) => void;
}

/**
 * FormulaBuilder Component
 *
 * IMPORTANT: This component only supports JSON path extraction (Simple formula type).
 *
 * WHY:
 * - Metrics are EXTRACTED during file processing using JSON paths (e.g., $.Amount)
 * - The ValidationService extracts values from validated records using these paths
 * - PromQL expressions CANNOT be used for extraction - they're for QUERYING Prometheus
 *
 * The two-stage pipeline:
 * 1. EXTRACTION (this component): JSON paths like $.Amount, $.Status
 * 2. QUERYING (Prometheus/Grafana): PromQL like rate(), sum(), avg()
 *
 * For PromQL expressions, use the AlertRuleBuilder component which is designed
 * for creating alert rules that query already-collected metrics.
 */
const FormulaBuilder: React.FC<FormulaBuilderProps> = ({
  fieldPath,
  formula,
  onChange
}) => {
  // Always use the field path as the formula for JSON path extraction
  React.useEffect(() => {
    if (formula !== fieldPath) {
      onChange(fieldPath);
    }
  }, [fieldPath, formula, onChange]);

  return (
    <Card>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Space>
            <FunctionOutlined style={{ fontSize: 18, color: '#1890ff' }} />
            <Text strong style={{ fontSize: 14 }}>
              נתיב חילוץ ערך (JSON Path)
            </Text>
          </Space>
          <div style={{ marginTop: 4 }}>
            <Text type="secondary" style={{ fontSize: 12 }}>
              נתיב JSON לחילוץ הערך מהרשומה המאומתת
            </Text>
          </div>
        </div>

        {/* Simple Extraction Display */}
        <Alert
          message="חילוץ ערך פשוט"
          description={
            <Space direction="vertical">
              <Text>המדד יחלץ את הערך מהנתיב: <Tag color="blue">{fieldPath || 'לא נבחר'}</Tag></Text>
              <Text type="secondary" style={{ fontSize: 11 }}>
                הערך יחולץ מכל רשומה שעברה אימות בהצלחה ויישלח ל-Prometheus
              </Text>
            </Space>
          }
          type="info"
          showIcon
        />

        {/* Field Path Preview */}
        {fieldPath && (
          <div>
            <Text strong>נתיב הנתונים:</Text>
            <Card
              size="small"
              style={{
                marginTop: 8,
                backgroundColor: '#f6f8fa',
                border: '1px solid #52c41a'
              }}
            >
              <pre style={{
                margin: 0,
                fontFamily: 'Monaco, Consolas, monospace',
                fontSize: 14,
                color: '#24292e',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}>
                {fieldPath}
              </pre>
            </Card>
          </div>
        )}

        {/* Important Note */}
        <Alert
          message={
            <Space direction="vertical" size={4}>
              <Text strong style={{ fontSize: 12 }}>
                חילוץ ערך - JSON Path
              </Text>
              <Text style={{ fontSize: 11 }}>
                המערכת מחלצת את הערך מהשדה שנבחר בכל רשומה שעוברת אימות.
                לדוגמה: אם בחרת $.Amount, המערכת תחלץ את ערך השדה Amount מכל רשומה.
              </Text>
            </Space>
          }
          type="success"
          showIcon
          icon={<InfoCircleOutlined />}
          style={{ fontSize: 12 }}
        />

        {/* Architecture Note */}
        <Alert
          message="הערה על ארכיטקטורה"
          description={
            <Space direction="vertical" size={4}>
              <Text style={{ fontSize: 11 }}>
                <strong>שלב 1 - חילוץ:</strong> JSON Path (כאן) - חילוץ ערכים מרשומות
              </Text>
              <Text style={{ fontSize: 11 }}>
                <strong>שלב 2 - שאילתות:</strong> PromQL (ב-Grafana/Alerts) - ניתוח הנתונים שנאספו
              </Text>
              <Text type="secondary" style={{ fontSize: 10 }}>
                ביטויי PromQL כמו rate(), sum(), avg() משמשים רק לשאילתות ב-Prometheus,
                לא לחילוץ ערכים. להגדרת התראות עם PromQL, השתמש בהגדרת כללי התראה.
              </Text>
            </Space>
          }
          type="warning"
          showIcon
          icon={<InfoCircleOutlined />}
          style={{ fontSize: 11 }}
        />
      </Space>
    </Card>
  );
};

export default FormulaBuilder;
