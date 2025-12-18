import React from 'react';
import { Form, Input, Select, Space, Divider } from 'antd';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';
import MetricNameHelper from './MetricNameHelper';
import FormulaBuilder from './FormulaBuilder';

const { TextArea } = Input;
const { Option } = Select;

interface WizardStepDetailsProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

const WizardStepDetails: React.FC<WizardStepDetailsProps> = ({ value, onChange }) => {
  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <MetricNameHelper
        hebrewDescription={value.description}
        fieldName={value.fieldPath}
        prometheusType={value.prometheusType}
        value={value.name}
        onChange={(name) => onChange({ name })}
      />

      <Form layout="vertical">
        <Form.Item label="שם תצוגה (עברית)" required>
          <Input
            value={value.displayName}
            onChange={(e) => onChange({ displayName: e.target.value })}
            placeholder="לדוגמה: סך עסקאות יומי"
          />
        </Form.Item>

        <Form.Item label="תיאור">
          <TextArea
            rows={3}
            value={value.description}
            onChange={(e) => onChange({ description: e.target.value })}
            placeholder="תאר את המדד ואת מה הוא מודד"
          />
        </Form.Item>

        <Form.Item label="קטגוריה" required>
          <Select
            value={value.category}
            onChange={(val) => onChange({ category: val })}
          >
            <Option value="business">עסקי</Option>
            <Option value="technical">טכני</Option>
            <Option value="operational">תפעולי</Option>
            <Option value="financial">פיננסי</Option>
          </Select>
        </Form.Item>

        <Form.Item label="סוג Prometheus" required>
          <Select
            value={value.prometheusType}
            onChange={(val) => onChange({ prometheusType: val as any })}
          >
            <Option value="gauge">Gauge - ערך משתנה שיכול לעלות ולרדת</Option>
            <Option value="counter">Counter - מונה שרק עולה</Option>
            <Option value="histogram">Histogram - התפלגות ערכים לדליים</Option>
            <Option value="summary">Summary - סיכום סטטיסטי עם percentiles</Option>
          </Select>
        </Form.Item>
      </Form>

      <Divider />

      <FormulaBuilder
        fieldPath={value.fieldPath}
        formula={value.formula}
        onChange={(formula) => onChange({ formula })}
      />
    </Space>
  );
};

export default WizardStepDetails;
