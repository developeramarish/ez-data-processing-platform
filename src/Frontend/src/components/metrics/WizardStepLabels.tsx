import React from 'react';
import { Space, Alert } from 'antd';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';
import EnhancedLabelInput from './EnhancedLabelInput';

interface WizardStepLabelsProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

const WizardStepLabels: React.FC<WizardStepLabelsProps> = ({ value, onChange }) => {
  const handleLabelsChange = (labelNames: string, promqlExpression: string) => {
    onChange({
      labelNames,
      labelsExpression: promqlExpression
    });
  };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Alert
        message="תוויות (Labels) - אופציונלי"
        description="תוויות מאפשרות סינון וקיבוץ נתונים ב-Prometheus. ניתן להגדיר ערכים דינמיים (משתנים) או ערכים קבועים לכל תווית."
        type="info"
        showIcon
      />

      <EnhancedLabelInput
        value={value.labelNames}
        onChange={handleLabelsChange}
      />
    </Space>
  );
};

export default WizardStepLabels;
