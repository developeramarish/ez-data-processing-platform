import React from 'react';
import { Space, Alert, Input, Typography } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';

const { Text } = Typography;

interface WizardStepGlobalMetricsProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

const WizardStepGlobalMetrics: React.FC<WizardStepGlobalMetricsProps> = ({ value, onChange }) => {
  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Alert
        message="יצירת מדד גלובלי חדש"
        description="מדד גלובלי חל על כל מקורות הנתונים במערכת. השלם את פרטי המדד בשלבים הבאים."
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
      />

      <div>
        <Text strong>שם השדה:</Text>
        <Text type="danger"> *</Text>
        <Input
          style={{ marginTop: 8, fontFamily: 'Monaco, Consolas, monospace' }}
          placeholder="לדוגמה: amount או transaction_count"
          value={value.fieldPath.startsWith('$.') ? value.fieldPath.substring(2) : value.fieldPath}
          onChange={(e) => {
            const fieldValue = e.target.value;
            // Automatically add $. prefix
            const fieldPath = fieldValue ? `$.${fieldValue}` : '';
            onChange({ fieldPath });
          }}
        />
        <Text type="secondary" style={{ fontSize: 11, display: 'block', marginTop: 4 }}>
          הזן רק את שם השדה - הקידומת "$." תתווסף אוטומטית
        </Text>
      </div>

      <Alert
        message="מדדים גלובליים - שיטת עבודה"
        description={
          <Space direction="vertical" size={4}>
            <Text style={{ fontSize: 12 }}>
              • מדד גלובלי מחושב עבור <strong>כל מקור נתונים</strong> בנפרד
            </Text>
            <Text style={{ fontSize: 12 }}>
              • לדוגמה: מדד "סך כמות" יחושב בנפרד עבור כל מקור נתונים שיש בו שדה זה
            </Text>
            <Text style={{ fontSize: 12 }}>
              • המדד ישתמש באותו הגדרה עבור כל מקורות הנתונים
            </Text>
          </Space>
        }
        type="success"
        showIcon
      />

      <Alert
        message="השלבים הבאים"
        description="בשלב הבא תוכל להגדיר את שם המדד, תיאור, סוג Prometheus, ונוסחת חישוב."
        type="info"
        showIcon
      />
    </Space>
  );
};

export default WizardStepGlobalMetrics;
