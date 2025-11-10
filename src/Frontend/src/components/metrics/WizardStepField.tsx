import React, { useCallback } from 'react';
import { Space, Alert, Select, Form } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';
import SchemaFieldSelector from './SchemaFieldSelector';

const { Option } = Select;

interface WizardStepFieldProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

const getFieldUsageExplanation = (prometheusType: string): string => {
  const explanations: Record<string, string> = {
    gauge: 'שדה זה ישמש כערך הנוכחי של ה-Gauge. ערכים יכולים לעלות ולרדת.',
    counter: 'שדה זה ישמש למונה שרק עולה. מתאים לספירת אירועים, עסקאות, שגיאות וכו\'.',
    histogram: 'שדה זה ישמש לחלוקת ערכים לדלי (buckets) להיסטוגרמה. מתאים למדידת זמני תגובה, גדלי בקשות וכו\'.',
    summary: 'שדה זה ישמש לחישוב סיכום סטטיסטי (percentiles). מתאים למדידות המחייבות חישובי percentiles.'
  };
  return explanations[prometheusType] || 'שדה זה ישמש כמקור הנתונים למדד.';
};

const WizardStepField: React.FC<WizardStepFieldProps> = ({ value, onChange }) => {
  const dataSourceId = value.scope === 'datasource-specific' ? value.dataSourceId : null;

  const handleFieldSelect = useCallback((fieldName: string) => {
    onChange({ fieldPath: fieldName });
  }, [onChange]);

  const handlePrometheusTypeChange = useCallback((type: string) => {
    onChange({ prometheusType: type as any });
  }, [onChange]);

  // Empty handler for labels - will be handled in Step 4
  const handleLabelsSelect = useCallback(() => {
    // Labels are handled in Step 4 (WizardStepLabels), not here
  }, []);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Alert
        message="בחירת שדה וסוג מדד"
        description="בחר תחילה את סוג המדד Prometheus ולאחר מכן את השדה מהסכמה שישמש למדידה. תוויות (Labels) יוגדרו בשלב מאוחר יותר."
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
      />

      {/* Prometheus Type Selection - Must be selected first */}
      <Form layout="vertical">
        <Form.Item label="סוג Prometheus" required>
          <Select
            value={value.prometheusType}
            onChange={handlePrometheusTypeChange}
            placeholder="בחר סוג מדד Prometheus"
          >
            <Option value="gauge">Gauge - ערך משתנה שיכול לעלות ולרדת</Option>
            <Option value="counter">Counter - מונה שרק עולה</Option>
            <Option value="histogram">Histogram - התפלגות ערכים לדליים</Option>
            <Option value="summary">Summary - סיכום סטטיסטי עם percentiles</Option>
          </Select>
        </Form.Item>
      </Form>

      {dataSourceId ? (
        <SchemaFieldSelector
          dataSourceId={dataSourceId}
          prometheusType={value.prometheusType}
          onFieldSelect={handleFieldSelect}
          onLabelsSelect={handleLabelsSelect}
          selectedField={value.fieldPath}
          selectedLabels={[]}
          showLabels={false}
        />
      ) : (
        <Alert
          message="מדד כללי"
          description="למדדים כלליים, יש לבחור שדה גנרי שקיים בכל מקורות הנתונים. לדוגמה: $.amount, $.quantity, $.status"
          type="info"
          showIcon
        />
      )}

      {value.fieldPath && value.prometheusType && (
        <Alert
          message={`שימוש בשדה עבור ${value.prometheusType}`}
          description={getFieldUsageExplanation(value.prometheusType)}
          type="info"
          showIcon
          style={{ marginTop: 16 }}
        />
      )}

      {!value.fieldPath && value.prometheusType && (
        <Alert
          message="נדרש שדה"
          description="לא ניתן להמשיך בלי לבחור שדה. בחר שדה מהסכמה או הזן נתיב JSON ידנית."
          type="error"
          showIcon
        />
      )}
    </Space>
  );
};

export default WizardStepField;
