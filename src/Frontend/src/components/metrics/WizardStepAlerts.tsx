import React, { useState, useEffect, useCallback } from 'react';
import { Space, Alert } from 'antd';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';
import AlertRuleBuilder, { type AlertRule } from './AlertRuleBuilder';
import metricsApi from '../../services/metrics-api-client';

interface WizardStepAlertsProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

const WizardStepAlerts: React.FC<WizardStepAlertsProps> = ({ value, onChange }) => {
  const [availableMetrics, setAvailableMetrics] = useState<string[]>([]);

  const loadAvailableMetrics = useCallback(async () => {
    try {
      if (value.dataSourceId) {
        // Get metrics for this data source
        const metrics = await metricsApi.getByDataSource(value.dataSourceId);
        const metricNames = metrics.map(m => m.name).filter(Boolean);
        setAvailableMetrics(metricNames);
      } else {
        // For global metrics, get all metrics
        const metrics = await metricsApi.getAll();
        const metricNames = metrics.map(m => m.name).filter(Boolean);
        setAvailableMetrics(metricNames);
      }
    } catch (error) {
      console.error('Error loading metrics:', error);
      setAvailableMetrics([]);
    }
  }, [value.dataSourceId]);

  useEffect(() => {
    loadAvailableMetrics();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value.dataSourceId]); // Only depend on dataSourceId, not loadAvailableMetrics

  const handleAlertsChange = (rules: AlertRule[]) => {
    onChange({ alertRules: rules });
  };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Alert
        message="שלב אופציונלי - כללי התראה"
        description="ניתן לדלג על שלב זה ולהוסיף כללי התראה מאוחר יותר. כללי התראה מאפשרים קבלת התראות אוטומטיות כאשר המדד עובר סף מסוים."
        type="info"
        showIcon
      />

      <AlertRuleBuilder
        value={value.alertRules}
        onChange={handleAlertsChange}
        availableMetrics={availableMetrics}
        dataSourceId={value.dataSourceId || undefined}
        currentMetricName={value.name}
      />
    </Space>
  );
};

export default WizardStepAlerts;
