import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card, Steps, Button, Space, message, Typography, Alert } from 'antd';
import { SaveOutlined, LeftOutlined, RightOutlined } from '@ant-design/icons';
import WizardStepDataSource from '../../components/metrics/WizardStepDataSource';
import WizardStepGlobalMetrics from '../../components/metrics/WizardStepGlobalMetrics';
import WizardStepField from '../../components/metrics/WizardStepField';
import WizardStepDetails from '../../components/metrics/WizardStepDetails';
import WizardStepLabels from '../../components/metrics/WizardStepLabels';
import WizardStepAlerts from '../../components/metrics/WizardStepAlerts';
import { metricsApi } from '../../services/metrics-api-client';
import type { AlertRule } from '../../components/metrics/AlertRuleBuilder';

const { Title } = Typography;

interface WizardData {
  // Step 1: Data Source
  scope: 'global' | 'datasource-specific';
  dataSourceId: string | null;
  dataSourceName: string | null;
  
  // Step 2: Field Selection
  fieldPath: string;
  
  // Step 3: Metric Details
  name: string;
  displayName: string;
  description: string;
  category: string;
  prometheusType: 'gauge' | 'counter' | 'histogram' | 'summary';
  
  // Step 4: Labels
  labelNames: string;
  labelsExpression: string;
  
  // Step 5: Alert Rules
  alertRules: AlertRule[];
  
  // Additional fields
  formula: string;
  formulaType: 'simple' | 'promql' | 'recording';
  retention: string;
  status: number;
}

const MetricConfigurationWizard: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = !!id;

  const [current, setCurrent] = useState(0);
  const [loading, setLoading] = useState(false);
  const [wizardData, setWizardData] = useState<WizardData>({
    scope: 'global',
    dataSourceId: null,
    dataSourceName: null,
    fieldPath: '',
    name: '',
    displayName: '',
    description: '',
    category: 'business',
    prometheusType: 'gauge',
    labelNames: '',
    labelsExpression: '',
    alertRules: [],
    formula: '',
    formulaType: 'simple',
    retention: '30d',
    status: 0
  });

  const loadMetric = async (metricId: string) => {
    setLoading(true);
    try {
      const metric = await metricsApi.getById(metricId);
      setWizardData({
        scope: metric.scope || 'global',
        dataSourceId: metric.dataSourceId || null,
        dataSourceName: metric.dataSourceName || null,
        fieldPath: metric.fieldPath || '',
        name: metric.name || '',
        displayName: metric.displayName || '',
        description: metric.description || '',
        category: metric.category || 'business',
        prometheusType: (metric as any).prometheusType || 'gauge',
        labelNames: (metric as any).labelNames || '',
        labelsExpression: (metric as any).labelsExpression || '',
        alertRules: (metric as any).alertRules || [],
        formula: metric.formula || '',
        formulaType: (metric as any).formulaType || 'simple',
        retention: metric.retention || '30d',
        status: metric.status || 0
      });
    } catch (error) {
      message.error('שגיאה בטעינת המדד');
      console.error('Error loading metric:', error);
      navigate('/metrics');
    } finally {
      setLoading(false);
    }
  };

  // Load existing metric for edit mode
  useEffect(() => {
    if (isEditMode && id) {
      loadMetric(id);
    }
  }, [isEditMode, id]);

  const updateWizardData = (data: Partial<WizardData>) => {
    setWizardData(prev => ({ ...prev, ...data }));
  };

  const validateStep = (step: number): boolean => {
    switch (step) {
      case 0: // Data Source
        if (wizardData.scope === 'datasource-specific' && !wizardData.dataSourceId) {
          message.warning('יש לבחור מקור נתונים');
          return false;
        }
        return true;
        
      case 1: // Global Metric or Field Selection
        if (wizardData.scope === 'global') {
          // For global metrics, check if a predefined metric is selected
          if (!wizardData.name || !wizardData.fieldPath) {
            message.warning('יש לבחור מדד גלובלי');
            return false;
          }
        } else {
          // For specific metrics, require field selection
          if (!wizardData.fieldPath) {
            message.warning('יש לבחור שדה - זהו שדה חובה');
            return false;
          }
        }
        return true;
        
      case 2: // Metric Details
        if (!wizardData.name) {
          message.warning('יש להזין שם מדד');
          return false;
        }
        if (!/^[a-z][a-z0-9_]*$/.test(wizardData.name)) {
          message.warning('שם המדד חייב להתחיל באות קטנה ולהכיל רק אותיות קטנות, מספרים וקו תחתון');
          return false;
        }
        if (!wizardData.displayName) {
          message.warning('יש להזין שם תצוגה');
          return false;
        }
        if (!wizardData.prometheusType) {
          message.warning('יש לבחור סוג Prometheus');
          return false;
        }
        return true;
        
      case 3: // Labels
        // Labels are optional, so always valid
        return true;
        
      case 4: // Alert Rules
        // Alert rules are optional, so always valid
        return true;
        
      default:
        return true;
    }
  };

  const next = () => {
    if (validateStep(current)) {
      setCurrent(current + 1);
    }
  };

  const prev = () => {
    setCurrent(current - 1);
  };

  const handleSubmit = async () => {
    if (!validateStep(current)) {
      console.log('Validation failed for current step:', current);
      return;
    }

    console.log('=== Starting handleSubmit ===');
    console.log('Edit mode:', isEditMode, 'ID:', id);
    setLoading(true);
    let success = false;
    
    try {
      // Convert formulaType string to number for API
      const formulaTypeMap = { 'simple': 0, 'promql': 1, 'recording': 2 };
      const formulaTypeNumber = formulaTypeMap[wizardData.formulaType] || 0;
      
      const payload = {
        name: wizardData.name,
        displayName: wizardData.displayName,
        description: wizardData.description,
        category: wizardData.category,
        scope: wizardData.scope,
        dataSourceId: wizardData.dataSourceId,
        dataSourceName: wizardData.dataSourceName,
        formula: wizardData.formula,
        formulaType: formulaTypeNumber,
        fieldPath: wizardData.fieldPath,
        prometheusType: wizardData.prometheusType,
        labelNames: wizardData.labelNames,
        labelsExpression: wizardData.labelsExpression,
        labels: wizardData.labelNames ? wizardData.labelNames.split(',').map(l => l.trim()).filter(Boolean) : [],
        alertRules: wizardData.alertRules,
        retention: wizardData.retention,
        status: wizardData.status,
        createdBy: 'User',
        updatedBy: 'User'
      };

      console.log('Submitting metric payload:', payload);

      if (isEditMode && id) {
        console.log('Updating existing metric with ID:', id);
        const result = await metricsApi.update(id, payload);
        console.log('Update API result:', result);
        message.success('המדד עודכן בהצלחה');
        success = true;
        console.log('Success flag set to true after update');
      } else {
        console.log('Creating new metric');
        const result = await metricsApi.create(payload);
        console.log('Create API result:', result);
        message.success('המדד נוצר בהצלחה');
        success = true;
        console.log('Success flag set to true after create');
      }
    } catch (error: any) {
      console.error('=== ERROR in handleSubmit ===');
      console.error('Error object:', error);
      console.error('Error message:', error.message);
      console.error('Error response:', error.response);
      console.error('Error response data:', error.response?.data);
      message.error('שגיאה בשמירת המדד: ' + (error.message || 'Unknown error'));
    } finally {
      console.log('=== Finally block - success:', success, '===');
      setLoading(false);
      
      // Navigate back on success
      if (success) {
        console.log('Success is true, scheduling navigation in 800ms');
        setTimeout(() => {
          console.log('Executing navigation to /metrics');
          try {
            navigate('/metrics', { replace: true });
            console.log('Navigate called successfully');
          } catch (navError) {
            console.error('Navigation error:', navError);
          }
        }, 800);
      } else {
        console.log('Success is false, NOT navigating back to list');
      }
    }
  };

  const steps = [
    {
      title: 'מקור נתונים',
      description: 'בחר האם מדד כללי או פרטי',
      content: (
        <WizardStepDataSource
          value={wizardData}
          onChange={updateWizardData}
        />
      )
    },
    {
      title: wizardData.scope === 'global' ? 'בחירת מדד גלובלי' : 'בחירת שדה',
      description: wizardData.scope === 'global' ? 'בחר מדד מוגדר מראש' : 'שדה חובה לקישור המדד',
      content: wizardData.scope === 'global' ? (
        <WizardStepGlobalMetrics
          value={wizardData}
          onChange={updateWizardData}
        />
      ) : (
        <WizardStepField
          value={wizardData}
          onChange={updateWizardData}
        />
      )
    },
    {
      title: 'פרטי מדד',
      description: 'שם, תיאור וסוג המדד',
      content: (
        <WizardStepDetails
          value={wizardData}
          onChange={updateWizardData}
        />
      )
    },
    {
      title: 'תוויות',
      description: 'הגדרת תוויות למדד (אופציונלי)',
      content: (
        <WizardStepLabels
          value={wizardData}
          onChange={updateWizardData}
        />
      )
    },
    {
      title: 'כללי התראה',
      description: 'הוספת התראות (אופציונלי)',
      content: (
        <WizardStepAlerts
          value={wizardData}
          onChange={updateWizardData}
        />
      )
    }
  ];

  return (
    <div style={{ padding: 24 }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div>
            <Title level={2}>
              {isEditMode ? 'עריכת מדד' : 'יצירת מדד חדש'}
            </Title>
          </div>

          {/* Edit Mode Context Banner */}
          {isEditMode && wizardData.name && (
            <Alert
              message="מדד בעריכה"
              description={
                <Space direction="vertical" size={4}>
                  <Typography.Text><strong>שם מדד:</strong> {wizardData.displayName} ({wizardData.name})</Typography.Text>
                  <Typography.Text><strong>שדה:</strong> {wizardData.fieldPath}</Typography.Text>
                  {wizardData.dataSourceName && (
                    <Typography.Text><strong>מקור נתונים:</strong> {wizardData.dataSourceName}</Typography.Text>
                  )}
                  {wizardData.scope && (
                    <Typography.Text><strong>היקף:</strong> {wizardData.scope === 'global' ? 'גלובלי' : 'פרטני'}</Typography.Text>
                  )}
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    ניתן לנווט בין השלבים באמצעות לחיצה על השלבים למעלה
                  </Typography.Text>
                </Space>
              }
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}

          <Steps
            current={current}
            items={steps.map(step => ({
              title: step.title,
              description: step.description
            }))}
            onChange={(step) => {
              // Allow clicking on steps to navigate (especially useful in edit mode)
              if (step < current || isEditMode) {
                setCurrent(step);
              }
            }}
          />

          <div style={{ minHeight: 400, marginTop: 24 }}>
            {steps[current].content}
          </div>

          <div style={{ marginTop: 24, display: 'flex', justifyContent: 'space-between' }}>
            <div>
              {current > 0 && (
                <Button onClick={prev} icon={<LeftOutlined />}>
                  הקודם
                </Button>
              )}
            </div>
            <div>
              <Space>
                <Button onClick={() => navigate('/metrics')}>
                  ביטול
                </Button>
                {current < steps.length - 1 && (
                  <Button type="primary" onClick={next}>
                    הבא
                    <RightOutlined />
                  </Button>
                )}
                {current === steps.length - 1 && (
                  <Button
                    type="primary"
                    onClick={handleSubmit}
                    loading={loading}
                    icon={<SaveOutlined />}
                  >
                    שמור
                  </Button>
                )}
              </Space>
            </div>
          </div>
        </Space>
      </Card>
    </div>
  );
};

export default MetricConfigurationWizard;
export type { WizardData };
