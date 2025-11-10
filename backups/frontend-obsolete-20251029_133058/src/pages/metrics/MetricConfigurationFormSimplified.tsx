import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Form,
  Input,
  Select,
  Button,
  Space,
  Typography,
  Divider,
  message,
  Row,
  Col,
  Alert,
  Spin,
  Tabs,
} from 'antd';
import { SaveOutlined, ArrowLeftOutlined, InfoCircleOutlined, LoadingOutlined, GlobalOutlined, DatabaseOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import SchemaFieldSelector from '../../components/metrics/SchemaFieldSelector';
import metricsApi from '../../services/metrics-api-client';

const { Title, Text, Paragraph } = Typography;
const { TextArea } = Input;
const { Option } = Select;

interface ParsedField {
  name: string;
  displayName: string;
  type: string;
  description: string;
  isNumeric: boolean;
  enumValues?: string[];
}

const MetricConfigurationFormSimplified: React.FC = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [loadingMetric, setLoadingMetric] = useState(false);
  const [activeTab, setActiveTab] = useState<string>('global');
  
  // State for schema integration
  const [selectedDataSourceId, setSelectedDataSourceId] = useState<string | null>(null);
  const [selectedPrometheusType, setSelectedPrometheusType] = useState<string>('');
  const [selectedField, setSelectedField] = useState<string>('');
  const [selectedLabels, setSelectedLabels] = useState<string[]>([]);

  const isEditMode = !!id;

  // Load metric data in edit mode
  useEffect(() => {
    if (isEditMode && id) {
      loadMetricData(id);
    }
  }, [id, isEditMode]);

  const loadMetricData = async (metricId: string) => {
    setLoadingMetric(true);
    try {
      const metric = await metricsApi.getById(metricId);
      
      // Set form values
      form.setFieldsValue({
        name: metric.name,
        displayName: metric.displayName,
        description: metric.description,
        category: metric.category,
        scope: metric.scope,
        dataSourceId: metric.dataSourceId,
        dataSourceName: metric.dataSourceName,
        prometheusType: metric.prometheusType || 'counter',
        fieldPath: metric.fieldPath,
        labels: Array.isArray(metric.labels) ? metric.labels.join(',') : '',
        retention: metric.retention,
        status: metric.status,
      });

      // Set state values
      if (metric.dataSourceId) {
        setSelectedDataSourceId(metric.dataSourceId);
        setActiveTab('specific'); // Switch to specific tab if metric has data source
      } else {
        setActiveTab('global');
      }
      if (metric.prometheusType) {
        setSelectedPrometheusType(metric.prometheusType);
      }
      if (metric.fieldPath) {
        setSelectedField(metric.fieldPath);
      }
      if (metric.labels && Array.isArray(metric.labels)) {
        setSelectedLabels(metric.labels);
      }

      message.success('נתוני מדד נטענו בהצלחה');
    } catch (error) {
      message.error('שגיאה בטעינת נתוני מדד');
      console.error('Error loading metric:', error);
      navigate('/metrics-config');
    } finally {
      setLoadingMetric(false);
    }
  };

  // Handle field selection from schema
  const handleFieldSelect = (fieldName: string, field: ParsedField) => {
    setSelectedField(fieldName);
    form.setFieldValue('fieldPath', fieldName);
  };

  // Handle labels selection from schema
  const handleLabelsSelect = (labels: string[]) => {
    setSelectedLabels(labels);
    form.setFieldValue('labels', labels.join(','));
  };

  // Prometheus type info
  const prometheusTypeInfo = {
    counter: {
      name: 'Counter',
      nameHe: 'מונה',
      description: 'רק עולה - מתאים לספירת אירועים, בקשות, שגיאות, עסקאות',
      example: 'סך עסקאות, מספר בקשות, כמות שגיאות',
      icon: '📈',
    },
    gauge: {
      name: 'Gauge',
      nameHe: 'מד',
      description: 'יכול לעלות ולרדת - מתאים למצב נוכחי, יתרות, מלאי',
      example: 'יתרת חשבון, מלאי נוכחי, זיכרון פנוי',
      icon: '📊',
    },
    histogram: {
      name: 'Histogram',
      nameHe: 'היסטוגרמה',
      description: 'התפלגות עם buckets - מתאים לזמני תגובה, גדלי קבצים',
      example: 'זמן עיבוד, סכום עסקה, גודל קובץ',
      icon: '📉',
    },
    summary: {
      name: 'Summary',
      nameHe: 'סיכום',
      description: 'אחוזונים מחושבים - מתאים למדידות מתקדמות',
      example: 'אחוזון 95 של זמן תגובה',
      icon: '📋',
    },
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
        dataSourceName: values.dataSourceName || null,
        prometheusType: values.prometheusType,
        fieldPath: values.fieldPath || '',
        formula: '', // Empty for pure Prometheus - formulas will be in dashboards
        labels: selectedLabels,
        retention: values.retention,
        status: values.status,
        createdBy: 'User',
      };

      if (isEditMode) {
        await metricsApi.update(id, {
          ...metricData,
          updatedBy: 'User',
        });
        message.success('מדד עודכן בהצלחה');
      } else {
        await metricsApi.create(metricData);
        message.success('מדד נוצר בהצלחה');
      }

      navigate('/metrics-config');
    } catch (error) {
      message.error(isEditMode ? 'שגיאה בעדכון מדד' : 'שגיאה ביצירת מדד');
      console.error('Error saving metric:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loadingMetric) {
    return (
      <div style={{ padding: '24px', textAlign: 'center' }}>
        <Spin size="large" tip="טוען נתוני מדד..." />
      </div>
    );
  }

  return (
    <div style={{ padding: '24px', maxWidth: '1400px', margin: '0 auto' }}>
      {/* Header Section */}
      <Card 
        style={{ marginBottom: '24px', background: 'linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%)' }}
        bordered={false}
      >
        <Space direction="vertical" size="small" style={{ width: '100%' }}>
          <Button
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate('/metrics-config')}
            type="link"
          >
            חזרה לרשימת מדדים
          </Button>
          <Title level={2} style={{ margin: 0 }}>
            {isEditMode ? '✏️ עריכת מדד Prometheus' : '➕ יצירת מדד Prometheus חדש'}
          </Title>
          <Paragraph type="secondary" style={{ margin: 0, fontSize: '14px' }}>
            {isEditMode 
              ? 'עדכן את הגדרות המדד. שים לב ששינויים ישפיעו על איסוף הנתונים.'
              : 'הגדר מדד פשוט המבוסס על Prometheus. נוסחאות מורכבות ו-aggregations נוצרים בשלב הדשבורד.'}
          </Paragraph>
          {isEditMode && (
            <Alert
              message="מצב עריכה"
              description="אתה עורך מדד קיים. שינויים ישמרו מיד לאחר לחיצה על 'עדכן מדד'."
              type="info"
              showIcon
              style={{ marginTop: '12px' }}
            />
          )}
        </Space>
      </Card>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          scope: 'global',
          status: 1,
          retention: '30d',
          category: 'performance',
          prometheusType: 'counter',
        }}
      >
        {/* Tabs for Global vs Specific */}
        <Card style={{ marginBottom: '24px' }}>
          <Tabs
            activeKey={activeTab}
            onChange={(key) => {
              setActiveTab(key);
              // Update scope field when switching tabs
              form.setFieldValue('scope', key === 'global' ? 'global' : 'datasource-specific');
              
              // Clear data source fields when switching to global
              if (key === 'global') {
                setSelectedDataSourceId(null);
                setSelectedField('');
                setSelectedLabels([]);
                form.setFieldValue('dataSourceId', null);
                form.setFieldValue('dataSourceName', null);
                form.setFieldValue('fieldPath', '');
                form.setFieldValue('labels', '');
              }
            }}
            items={[
              {
                key: 'global',
                label: (
                  <span style={{ fontSize: '15px', fontWeight: 500 }}>
                    <GlobalOutlined /> מדדים כלליים
                  </span>
                ),
                children: (
                  <Alert
                    message="מדדים כלליים"
                    description="מדדים אלו חלים על כל מקורות הנתונים במערכת. מתאימים למדדים כלליים כמו ביצועי מערכת, זמינות, וכדומה."
                    type="info"
                    showIcon
                    icon={<GlobalOutlined />}
                    style={{ marginBottom: '16px' }}
                  />
                ),
              },
              {
                key: 'specific',
                label: (
                  <span style={{ fontSize: '15px', fontWeight: 500 }}>
                    <DatabaseOutlined /> מדדים פרטניים
                  </span>
                ),
                children: (
                  <Alert
                    message="מדדים פרטניים"
                    description="מדדים אלו קשורים למקור נתונים ספציפי ומשתמשים בסכמה שלו. בחר מקור נתונים כדי לגשת לשדות הסכמה."
                    type="info"
                    showIcon
                    icon={<DatabaseOutlined />}
                    style={{ marginBottom: '16px' }}
                  />
                ),
              },
            ]}
          />
        </Card>

        {/* Hidden scope field - controlled by tabs */}
        <Form.Item name="scope" hidden>
          <Input />
        </Form.Item>

        <Row gutter={24}>
          {/* Left Column: Basic Info & Type */}
          <Col xs={24} lg={12}>
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              {/* Basic Information */}
              <Card title={<Space><InfoCircleOutlined />מידע בסיסי</Space>}>
                <Form.Item
                  name="name"
                  label="שם מדד (Metric Name)"
                  rules={[
                    { required: true, message: 'שדה חובה' },
                    { 
                      pattern: /^[a-z0-9_]+$/, 
                      message: 'רק אותיות אנגליות קטנות, מספרים וקו תחתון' 
                    },
                  ]}
                  extra="שם טכני בפורמט Prometheus: sales_daily_total"
                >
                  <Input placeholder="sales_daily_total" />
                </Form.Item>

                <Form.Item
                  name="displayName"
                  label="כותרת תצוגה"
                  rules={[{ required: true, message: 'שדה חובה' }]}
                >
                  <Input placeholder="סך מכירות יומי" />
                </Form.Item>

                <Form.Item
                  name="description"
                  label="תיאור"
                  rules={[{ required: true, message: 'שדה חובה' }]}
                >
                  <TextArea rows={3} placeholder="תאר את המדד ואת מטרתו העסקית" />
                </Form.Item>

                <Form.Item
                  name="category"
                  label="קטגוריה"
                  rules={[{ required: true, message: 'שדה חובה' }]}
                >
                  <Select>
                    <Option value="performance">ביצועים (Performance)</Option>
                    <Option value="quality">איכות (Quality)</Option>
                    <Option value="efficiency">יעילות (Efficiency)</Option>
                    <Option value="financial">פיננסי (Financial)</Option>
                    <Option value="operations">תפעול (Operations)</Option>
                    <Option value="customer">לקוח (Customer)</Option>
                    <Option value="custom">מותאם אישית (Custom)</Option>
                  </Select>
                </Form.Item>
              </Card>

              {/* Prometheus Type */}
              <Card title={<Space><InfoCircleOutlined />סוג מדד Prometheus</Space>}>
                <Alert
                  message="חשוב להבין"
                  description="בחר את סוג המדד בהתאם לאופי הנתונים. זה משפיע על איך Prometheus יאחסן ויציג את הנתונים."
                  type="info"
                  showIcon
                  style={{ marginBottom: '16px' }}
                />

                <Form.Item
                  name="prometheusType"
                  label="סוג מדד"
                  rules={[{ required: true, message: 'שדה חובה' }]}
                >
                  <Select 
                    onChange={(value) => setSelectedPrometheusType(value)}
                    style={{ width: '100%' }}
                    optionLabelProp="label"
                  >
                    {Object.entries(prometheusTypeInfo).map(([key, info]) => (
                      <Option 
                        key={key} 
                        value={key}
                        label={`${info.icon} ${info.nameHe} (${info.name})`}
                      >
                        <div>
                          <div>
                            <Text strong>
                              {info.icon} {info.nameHe} ({info.name})
                            </Text>
                          </div>
                          <div>
                            <Text type="secondary" style={{ fontSize: '12px' }}>
                              {info.description}
                            </Text>
                          </div>
                        </div>
                      </Option>
                    ))}
                  </Select>
                </Form.Item>

                {selectedPrometheusType && (
                  <Card size="small" type="inner" style={{ backgroundColor: '#f0f5ff' }}>
                    <Space direction="vertical" size="small">
                      <Text strong>
                        {prometheusTypeInfo[selectedPrometheusType as keyof typeof prometheusTypeInfo]?.icon}{' '}
                        {prometheusTypeInfo[selectedPrometheusType as keyof typeof prometheusTypeInfo]?.nameHe}
                      </Text>
                      <Text type="secondary" style={{ fontSize: '12px' }}>
                        {prometheusTypeInfo[selectedPrometheusType as keyof typeof prometheusTypeInfo]?.description}
                      </Text>
                      <Text type="secondary" style={{ fontSize: '12px' }}>
                        <strong>דוגמאות:</strong>{' '}
                        {prometheusTypeInfo[selectedPrometheusType as keyof typeof prometheusTypeInfo]?.example}
                      </Text>
                    </Space>
                  </Card>
                )}
              </Card>

              {/* Settings */}
              <Card title={<Space><InfoCircleOutlined />הגדרות</Space>}>
                <Form.Item name="retention" label="תקופת שמירה">
                  <Select>
                    <Option value="7d">7 ימים</Option>
                    <Option value="30d">30 יום (מומלץ)</Option>
                    <Option value="90d">90 יום</Option>
                    <Option value="180d">180 יום</Option>
                    <Option value="365d">שנה</Option>
                  </Select>
                </Form.Item>

                <Form.Item name="status" label="סטטוס">
                  <Select>
                    <Option value={0}>טיוטה - לא פעיל</Option>
                    <Option value={1}>פעיל - נאסף</Option>
                    <Option value={2}>מושהה - לא נאסף</Option>
                  </Select>
                </Form.Item>
              </Card>
            </Space>
          </Col>

          {/* Right Column: Data Source & Schema Fields (conditional based on tab) */}
          <Col xs={24} lg={12}>
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
              {/* Data Source Selection - only for specific metrics */}
              {activeTab === 'specific' && (
                <Card title={<Space><DatabaseOutlined />בחירת מקור נתונים</Space>}>
                  <Form.Item
                    name="dataSourceId"
                    label="מקור נתונים"
                    rules={[{ required: activeTab === 'specific', message: 'שדה חובה' }]}
                  >
                    <Select 
                      placeholder="בחר מקור נתונים"
                      onChange={(value) => setSelectedDataSourceId(value)}
                    >
                      <Option value="ds001">בנק לאומי - עסקאות</Option>
                      <Option value="ds002">מערכת CRM - לקוחות</Option>
                      <Option value="ds003">מערכת מלאי</Option>
                      <Option value="ds004">מערכת כרטיסי אשראי</Option>
                      <Option value="ds005">מערכת הזמנות</Option>
                      <Option value="ds006">מערכת משאבי אנוש</Option>
                    </Select>
                  </Form.Item>

                  <Form.Item 
                    name="dataSourceName" 
                    label="שם מקור הנתונים (תצוגה)"
                    extra="אופציונלי - לתצוגה בממשק"
                  >
                    <Input placeholder="בנק לאומי - עסקאות" />
                  </Form.Item>
                </Card>
              )}

              {/* Schema Fields Integration */}
              {activeTab === 'specific' && selectedDataSourceId && (
                <Card title={<Space><InfoCircleOutlined />שדות מתוך סכמה</Space>}>
                  <SchemaFieldSelector
                    dataSourceId={selectedDataSourceId}
                    prometheusType={selectedPrometheusType}
                    onFieldSelect={handleFieldSelect}
                    onLabelsSelect={handleLabelsSelect}
                    selectedField={selectedField}
                    selectedLabels={selectedLabels}
                  />
                  <Form.Item name="fieldPath" hidden>
                    <Input />
                  </Form.Item>
                  <Form.Item name="labels" hidden>
                    <Input />
                  </Form.Item>
                </Card>
              )}

              {/* Placeholder for global metrics */}
              {activeTab === 'global' && (
                <Card title={<Space><GlobalOutlined />מדד כללי</Space>}>
                  <Alert
                    message="מדד כללי - ללא סכמה"
                    description="מדד זה אינו מוגבל לסכמה ספציפית. ניתן להגדיר פרמטרים נוספים בהתאם לצורך."
                    type="info"
                    showIcon
                    style={{ marginBottom: '16px' }}
                  />
                  <Paragraph type="secondary" style={{ fontSize: '13px' }}>
                    <ul style={{ paddingRight: '20px', marginTop: '8px' }}>
                      <li>מדדים כלליים חלים על כל מקורות הנתונים</li>
                      <li>מתאימים למדדים כמו זמן תגובה כללי, זמינות מערכת, וכדומה</li>
                      <li>לא דורשים בחירת שדות מסכמה ספציפית</li>
                    </ul>
                  </Paragraph>
                </Card>
              )}

              {/* Show message when no data source selected in specific tab */}
              {activeTab === 'specific' && !selectedDataSourceId && (
                <Card title={<Space><InfoCircleOutlined />הנחיות</Space>}>
                  <Alert
                    message="בחר מקור נתונים"
                    description="בחר מקור נתונים בכרטיס למעלה כדי לראות את השדות הזמינים מהסכמה."
                    type="warning"
                    showIcon
                  />
                </Card>
              )}
            </Space>
          </Col>
        </Row>

        {/* Submit Buttons */}
        <Card 
          style={{ marginTop: '24px', background: '#fafafa' }}
          bordered={false}
        >
          <Row justify="space-between" align="middle">
            <Col>
              <Space size="middle">
                <Button 
                  type="primary" 
                  size="large" 
                  htmlType="submit" 
                  icon={<SaveOutlined />} 
                  loading={loading}
                  style={{ minWidth: '140px' }}
                >
                  {isEditMode ? '💾 עדכן מדד' : '➕ צור מדד'}
                </Button>
                <Button 
                  size="large" 
                  onClick={() => navigate('/metrics-config')}
                  style={{ minWidth: '100px' }}
                >
                  ביטול
                </Button>
              </Space>
            </Col>
            <Col>
              <Text type="secondary" style={{ fontSize: '12px' }}>
                {isEditMode ? 'שינויים ישמרו מיד' : 'המדד ייווצר אחרי שמירה'}
              </Text>
            </Col>
          </Row>
        </Card>
      </Form>
    </div>
  );
};

export default MetricConfigurationFormSimplified;
