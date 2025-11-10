import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Form,
  Input,
  Select,
  Button,
  Space,
  Typography,
  Tabs,
  Divider,
  message,
  Row,
  Col,
} from 'antd';
import { SaveOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import FormulaTemplateLibrary from '../../components/metrics/FormulaTemplateLibrary';
import VisualFormulaBuilder from '../../components/metrics/VisualFormulaBuilder';
import FilterConditionBuilder from '../../components/metrics/FilterConditionBuilder';
import AggregationHelper from '../../components/metrics/AggregationHelper';
import metricsApi from '../../services/metrics-api-client';
import type { FormulaTemplate } from '../../components/metrics/FormulaTemplateLibrary';

const { Title, Text } = Typography;
const { TextArea } = Input;
const { Option } = Select;

const MetricConfigurationForm: React.FC = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  // Formula state
  const [formula, setFormula] = useState('');
  const [formulaMetadata, setFormulaMetadata] = useState<any>(null);

  // Filter state
  const [filterConditions, setFilterConditions] = useState<any[]>([]);
  const [filterString, setFilterString] = useState('');

  // Aggregation state
  const [aggregationSettings, setAggregationSettings] = useState<any>(null);

  const isEditMode = !!id;

  // Handle template selection
  const handleTemplateSelect = (template: FormulaTemplate) => {
    setFormula(template.formula);
    message.success(`תבנית "${template.nameHebrew}" נבחרה`);
  };

  // Handle formula generation from visual builder
  const handleFormulaGenerated = (generatedFormula: string, metadata: any) => {
    setFormula(generatedFormula);
    setFormulaMetadata(metadata);
  };

  // Handle filter changes
  const handleFiltersChange = (filters: any[], filterStr: string) => {
    setFilterConditions(filters);
    setFilterString(filterStr);
  };

  // Handle aggregation changes
  const handleAggregationChange = (settings: any) => {
    setAggregationSettings(settings);
  };

  // Handle form submission
  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      const metricData = {
        name: values.name,
        displayName: values.displayName,
        description: values.description,
        category: values.category,
        scope: values.scope,
        dataSourceId: values.scope === 'datasource-specific' ? values.dataSourceId : null,
        dataSourceName: values.dataSourceName,
        formula: formula,
        fieldPath: values.fieldPath,
        labels: values.labels?.split(',').map((l: string) => l.trim()).filter(Boolean) || [],
        retention: values.retention,
        status: values.status,
        createdBy: 'User',
      };

      if (isEditMode) {
        await metricsApi.update(id, {
          ...metricData,
          updatedBy: 'User',
        });
        message.success(t('metrics.messages.metricUpdated'));
      } else {
        await metricsApi.create(metricData);
        message.success(t('metrics.messages.metricCreated'));
      }

      navigate('/metrics-config');
    } catch (error) {
      message.error(isEditMode ? 'שגיאה בעדכון מדד' : 'שגיאה ביצירת מדד');
      console.error('Error saving metric:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <div style={{ marginBottom: '24px' }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/metrics-config')}
          style={{ marginBottom: '16px' }}
        >
          {t('common.back')}
        </Button>
        <Title level={2}>
          {isEditMode ? t('metrics.configuration.edit') : t('metrics.configuration.create')}
        </Title>
      </div>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          scope: 'global',
          status: 1,
          retention: '30d',
          category: 'performance',
        }}
      >
        <Row gutter={24}>
          {/* Left Column: Basic Info */}
          <Col span={12}>
            <Card title={t('metrics.form.basicInfo')}>
              <Form.Item
                name="name"
                label={t('metrics.form.metricName')}
                rules={[
                  { required: true, message: 'שדה חובה' },
                  { pattern: /^[a-z0-9_]+$/, message: 'רק אותיות אנגליות קטנות, מספרים וקו תחתון' },
                ]}
              >
                <Input placeholder="sales_daily_total" />
              </Form.Item>

              <Form.Item
                name="displayName"
                label={t('metrics.form.displayName')}
                rules={[{ required: true, message: 'שדה חובה' }]}
              >
                <Input placeholder="סך מכירות יומי" />
              </Form.Item>

              <Form.Item
                name="description"
                label={t('metrics.form.description')}
                rules={[{ required: true, message: 'שדה חובה' }]}
              >
                <TextArea rows={3} placeholder="תאר את המדד ואת מטרתו" />
              </Form.Item>

              <Form.Item
                name="category"
                label={t('metrics.form.category')}
                rules={[{ required: true, message: 'שדה חובה' }]}
              >
                <Select>
                  <Option value="performance">{t('metrics.categories.performance')}</Option>
                  <Option value="quality">{t('metrics.categories.quality')}</Option>
                  <Option value="efficiency">{t('metrics.categories.efficiency')}</Option>
                  <Option value="financial">{t('metrics.categories.financial')}</Option>
                  <Option value="operations">{t('metrics.categories.operations')}</Option>
                  <Option value="customer">{t('metrics.categories.customer')}</Option>
                  <Option value="custom">{t('metrics.categories.custom')}</Option>
                </Select>
              </Form.Item>

              <Form.Item
                name="scope"
                label="היקף המדד"
                rules={[{ required: true, message: 'שדה חובה' }]}
              >
                <Select>
                  <Option value="global">כללי (Global) - חל על כל מקורות הנתונים</Option>
                  <Option value="datasource-specific">ספציפי (Custom) - מקור נתונים אחד</Option>
                </Select>
              </Form.Item>

              <Form.Item
                noStyle
                shouldUpdate={(prevValues, currentValues) => prevValues.scope !== currentValues.scope}
              >
                {({ getFieldValue }) =>
                  getFieldValue('scope') === 'datasource-specific' && (
                    <>
                      <Form.Item
                        name="dataSourceId"
                        label={t('metrics.form.dataSource')}
                        rules={[{ required: true, message: 'שדה חובה' }]}
                      >
                        <Select placeholder="בחר מקור נתונים">
                          <Option value="ds001">בנק לאומי - עסקאות</Option>
                          <Option value="ds002">מערכת CRM - לקוחות</Option>
                          <Option value="ds003">מערכת מלאי</Option>
                        </Select>
                      </Form.Item>

                      <Form.Item name="dataSourceName" label="שם מקור הנתונים (תצוגה)">
                        <Input placeholder="בנק לאומי - עסקאות" />
                      </Form.Item>
                    </>
                  )
                }
              </Form.Item>

              <Form.Item name="fieldPath" label="נתיב שדה (JSON Path)">
                <Input placeholder="$.amount" />
              </Form.Item>

              <Form.Item name="labels" label="תוויות (Prometheus Labels)">
                <Input placeholder="data_source, status, payment_method" />
              </Form.Item>

              <Form.Item name="retention" label="תקופת שמירה">
                <Select>
                  <Option value="7d">7 ימים</Option>
                  <Option value="30d">30 יום</Option>
                  <Option value="90d">90 יום</Option>
                  <Option value="180d">180 יום</Option>
                  <Option value="365d">שנה</Option>
                </Select>
              </Form.Item>

              <Form.Item name="status" label="סטטוס">
                <Select>
                  <Option value={0}>{t('metrics.status.draft')}</Option>
                  <Option value={1}>{t('metrics.status.active')}</Option>
                  <Option value={2}>{t('metrics.status.inactive')}</Option>
                </Select>
              </Form.Item>
            </Card>
          </Col>

          {/* Right Column: Formula, Filters, Aggregation */}
          <Col span={12}>
            {/* Formula Builder */}
            <Card title={t('metrics.form.formula')} style={{ marginBottom: '16px' }}>
              <Tabs
                items={[
                  {
                    key: 'templates',
                    label: 'תבניות מוכנות',
                    children: <FormulaTemplateLibrary onTemplateSelect={handleTemplateSelect} />,
                  },
                  {
                    key: 'visual',
                    label: 'בונה חזותי',
                    children: <VisualFormulaBuilder onFormulaGenerated={handleFormulaGenerated} />,
                  },
                  {
                    key: 'manual',
                    label: 'הזנה ידנית',
                    children: (
                      <div>
                        <Text strong>נוסחה:</Text>
                        <TextArea
                          rows={4}
                          value={formula}
                          onChange={(e) => setFormula(e.target.value)}
                          placeholder='SUM(amount) WHERE status="completed"'
                          style={{ marginTop: '8px', fontFamily: 'Monaco, Consolas, monospace' }}
                        />
                      </div>
                    ),
                  },
                ]}
              />

              {formula && (
                <div style={{ marginTop: '16px', background: '#f0f0f0', padding: '12px', borderRadius: '6px' }}>
                  <Text strong>נוסחה נוכחית:</Text>
                  <pre style={{ margin: '8px 0 0 0', fontFamily: 'Monaco, Consolas, monospace', fontSize: '13px' }}>
                    {formula}
                  </pre>
                </div>
              )}
            </Card>

            {/* Filters */}
            <Card title={t('metrics.form.filters')} style={{ marginBottom: '16px' }}>
              <FilterConditionBuilder onFiltersChange={handleFiltersChange} />
            </Card>

            {/* Aggregation */}
            <Card title={t('metrics.form.aggregation')}>
              <AggregationHelper onSettingsChange={handleAggregationChange} />
            </Card>
          </Col>
        </Row>

        {/* Submit Buttons */}
        <Card style={{ marginTop: '24px' }}>
          <Space>
            <Button type="primary" size="large" htmlType="submit" icon={<SaveOutlined />} loading={loading}>
              {t('common.save')}
            </Button>
            <Button size="large" onClick={() => navigate('/metrics-config')}>
              {t('common.cancel')}
            </Button>
          </Space>
        </Card>
      </Form>
    </div>
  );
};

export default MetricConfigurationForm;
