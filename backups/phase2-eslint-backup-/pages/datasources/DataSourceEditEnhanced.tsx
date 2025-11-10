import React, { useState, useEffect } from 'react';
import { Typography, Card, Form, Button, Space, Alert, Spin, message, Divider, Tabs } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeftOutlined, SaveOutlined, FileOutlined, ApiOutlined, ClockCircleOutlined, SafetyOutlined, BellOutlined, FileTextOutlined } from '@ant-design/icons';
import { type JSONSchema } from 'jsonjoy-builder';

// Import tab components
import { BasicInfoTab } from '../../components/datasource/tabs/BasicInfoTab';
import { ConnectionTab } from '../../components/datasource/tabs/ConnectionTab';
import { FileSettingsTab } from '../../components/datasource/tabs/FileSettingsTab';
import { SchemaTab } from '../../components/datasource/tabs/SchemaTab';
import { ScheduleTab } from '../../components/datasource/tabs/ScheduleTab';
import { ValidationTab } from '../../components/datasource/tabs/ValidationTab';
import { NotificationsTab } from '../../components/datasource/tabs/NotificationsTab';
import CronHelperDialog from '../../components/datasource/CronHelperDialog';

// Import shared utilities
import { buildConnectionString, frequencyToCron, extractFileTypeFromPattern } from '../../components/datasource/shared/helpers';
import { DataSource, ApiResponse } from '../../components/datasource/shared/types';

const { Title, Paragraph } = Typography;
const { TabPane } = Tabs;

const DataSourceEditEnhanced: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [form] = Form.useForm();
  
  // State
  const [loading, setLoading] = useState<boolean>(false);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<DataSource | null>(null);
  const [parsedConfig, setParsedConfig] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<string>('basic');
  const [testingConnection, setTestingConnection] = useState<boolean>(false);
  const [connectionTestResult, setConnectionTestResult] = useState<'success' | 'failed' | null>(null);
  const [cronHelperVisible, setCronHelperVisible] = useState<boolean>(false);
  const [jsonSchema, setJsonSchema] = useState<JSONSchema>({});

  // Watch form fields
  const connectionType = Form.useWatch('connectionType', form);
  const fileType = Form.useWatch('fileType', form);
  const scheduleFrequency = Form.useWatch('scheduleFrequency', form);
  const cronExpression = Form.useWatch('cronExpression', form);

  // Handlers
  const handleSchemaChange = (newSchema: JSONSchema) => {
    setJsonSchema(newSchema);
  };

  const handleCronHelperSelect = (expression: string) => {
    form.setFieldsValue({ cronExpression: expression });
    message.success('ביטוי Cron עודכן');
  };

  const handleTestConnection = async () => {
    setTestingConnection(true);
    setConnectionTestResult(null);

    try {
      await form.validateFields(['connectionType', 'connectionHost', 'connectionPort', 'connectionUsername', 'connectionPassword', 'connectionPath']);
      await new Promise(resolve => setTimeout(resolve, 2000));
      setConnectionTestResult('success');
      message.success('חיבור הצליח');
    } catch (err) {
      setConnectionTestResult('failed');
      message.error('חיבור נכשל');
    } finally {
      setTestingConnection(false);
    }
  };

  // Fetch data source
  const fetchDataSource = async () => {
    if (!id) {
      setError('Data source ID is required');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`http://localhost:5001/api/v1/datasource/${id}`, {
        method: 'GET',
        headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
      });

      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

      const data: ApiResponse<DataSource> = await response.json();

      if (data.IsSuccess) {
        setDataSource(data.Data);
        
        if (data.Data.JsonSchema) {
          setJsonSchema(data.Data.JsonSchema);
        }
        
        const additionalConfig = data.Data.AdditionalConfiguration || {};
        let configSettings: any = {};
        let connectionConfig: any = {};
        let fileConfig: any = {};
        let schedule: any = {};
        let validationRules: any = {};
        let notificationSettings: any = {};

        try {
          if (additionalConfig.ConfigurationSettings) {
            configSettings = JSON.parse(additionalConfig.ConfigurationSettings);
            connectionConfig = configSettings.connectionConfig || {};
            fileConfig = configSettings.fileConfig || {};
            schedule = configSettings.schedule || {};
            validationRules = configSettings.validationRules || {};
            notificationSettings = configSettings.notificationSettings || {};
          }
        } catch (e) {
          console.warn('Failed to parse configuration settings:', e);
        }
        
        setParsedConfig(configSettings);
        
        form.setFieldsValue({
          name: data.Data.Name,
          supplierName: data.Data.SupplierName,
          category: data.Data.Category,
          description: data.Data.Description,
          isActive: data.Data.IsActive,
          retentionDays: additionalConfig.RetentionDays || 30,
          connectionType: connectionConfig.type || 'Local',
          connectionHost: connectionConfig.host,
          connectionPort: connectionConfig.port,
          connectionUsername: connectionConfig.username,
          connectionPassword: connectionConfig.password,
          connectionPath: connectionConfig.path || data.Data.FilePath,
          connectionUrl: connectionConfig.url,
          // Kafka fields
          kafkaBrokers: connectionConfig.brokers,
          kafkaTopic: connectionConfig.topic,
          kafkaConsumerGroup: connectionConfig.consumerGroup,
          kafkaSecurityProtocol: connectionConfig.securityProtocol,
          kafkaOffsetReset: connectionConfig.offsetReset,
          fileType: fileConfig.type || extractFileTypeFromPattern(data.Data.FilePattern) || 'CSV',
          csvDelimiter: fileConfig.delimiter || ',',
          hasHeaders: fileConfig.hasHeaders !== false,
          excelSheet: fileConfig.sheetName,
          encoding: fileConfig.encoding || 'UTF-8',
          scheduleFrequency: schedule.frequency || 'Manual',
          cronExpression: schedule.cronExpression,
          scheduleEnabled: schedule.enabled || false,
          skipInvalidRecords: validationRules.skipInvalidRecords || false,
          maxErrorsAllowed: validationRules.maxErrorsAllowed,
          notifyOnSuccess: notificationSettings.onSuccess || false,
          notifyOnFailure: notificationSettings.onFailure !== false,
          notificationRecipients: notificationSettings.recipients?.join(', ') || '',
        });
      } else {
        throw new Error(data.Error?.Message || 'Failed to fetch data source');
      }
    } catch (err) {
      console.error('Error fetching data source:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch data source');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (values: any) => {
    if (!id || !dataSource) {
      message.error('Data source information is missing');
      return;
    }

    setSaving(true);

    try {
      const existingConfig = parsedConfig || {};
      
      let cronExpressionToSave = values.cronExpression;
      if (values.scheduleFrequency !== 'Custom' && values.scheduleFrequency !== 'Manual') {
        cronExpressionToSave = frequencyToCron(values.scheduleFrequency);
      }

      const mergedConfig = {
        connectionConfig: {
          ...(existingConfig.connectionConfig || {}),
          type: values.connectionType || existingConfig.connectionConfig?.type,
          host: values.connectionHost ?? existingConfig.connectionConfig?.host,
          port: values.connectionPort ?? existingConfig.connectionConfig?.port,
          username: values.connectionUsername ?? existingConfig.connectionConfig?.username,
          password: values.connectionPassword ?? existingConfig.connectionConfig?.password,
          path: values.connectionPath ?? existingConfig.connectionConfig?.path,
          url: values.connectionUrl ?? existingConfig.connectionConfig?.url,
          // Kafka-specific fields
          brokers: values.kafkaBrokers ?? existingConfig.connectionConfig?.brokers,
          topic: values.kafkaTopic ?? existingConfig.connectionConfig?.topic,
          consumerGroup: values.kafkaConsumerGroup ?? existingConfig.connectionConfig?.consumerGroup,
          securityProtocol: values.kafkaSecurityProtocol ?? existingConfig.connectionConfig?.securityProtocol,
          offsetReset: values.kafkaOffsetReset ?? existingConfig.connectionConfig?.offsetReset
        },
        fileConfig: {
          ...(existingConfig.fileConfig || {}),
          type: values.fileType || existingConfig.fileConfig?.type,
          delimiter: values.csvDelimiter ?? existingConfig.fileConfig?.delimiter,
          hasHeaders: values.hasHeaders ?? existingConfig.fileConfig?.hasHeaders,
          sheetName: values.excelSheet ?? existingConfig.fileConfig?.sheetName,
          encoding: values.encoding || existingConfig.fileConfig?.encoding
        },
        schedule: {
          ...(existingConfig.schedule || {}),
          frequency: values.scheduleFrequency || existingConfig.schedule?.frequency,
          cronExpression: cronExpressionToSave ?? existingConfig.schedule?.cronExpression,
          enabled: values.scheduleEnabled ?? existingConfig.schedule?.enabled
        },
        validationRules: {
          ...(existingConfig.validationRules || {}),
          skipInvalidRecords: values.skipInvalidRecords ?? existingConfig.validationRules?.skipInvalidRecords,
          maxErrorsAllowed: values.maxErrorsAllowed ?? existingConfig.validationRules?.maxErrorsAllowed
        },
        notificationSettings: {
          ...(existingConfig.notificationSettings || {}),
          onSuccess: values.notifyOnSuccess ?? existingConfig.notificationSettings?.onSuccess,
          onFailure: values.notifyOnFailure ?? existingConfig.notificationSettings?.onFailure,
          recipients: values.notificationRecipients 
            ? values.notificationRecipients.split(',').map((r: string) => r.trim())
            : existingConfig.notificationSettings?.recipients || []
        }
      };

      const requestPayload: any = {
        Id: id,
        Name: values.name || dataSource.Name,
        SupplierName: values.supplierName || dataSource.SupplierName,
        Category: values.category || dataSource.Category,
        Description: values.description ?? dataSource.Description,
        ConnectionString: buildConnectionString(values) || dataSource.FilePath,
        IsActive: values.isActive ?? dataSource.IsActive,
        ConfigurationSettings: JSON.stringify(mergedConfig),
        ValidationRules: null,
        Metadata: null,
        FileFormat: mergedConfig.fileConfig.type || null,
        RetentionDays: values.retentionDays ?? dataSource.AdditionalConfiguration?.RetentionDays
      };

      if (jsonSchema && Object.keys(jsonSchema).length > 0) {
        requestPayload.JsonSchema = jsonSchema;
      }

      const response = await fetch(`http://localhost:5001/api/v1/datasource/${id}`, {
        method: 'PUT',
        headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
        body: JSON.stringify(requestPayload),
      });

      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

      const data: ApiResponse<any> = await response.json();

      if (data.IsSuccess) {
        message.success(t('messages.dataSourceUpdated'));
        // Refresh the data source instead of navigating away
        await fetchDataSource();
        // Reset to basic info tab to show updated data
        setActiveTab('basic');
      } else {
        throw new Error(data.Error?.Message || 'Failed to update data source');
      }
    } catch (err) {
      console.error('Error updating data source:', err);
      message.error(err instanceof Error ? err.message : 'Failed to update data source');
    } finally {
      setSaving(false);
    }
  };

  useEffect(() => {
    fetchDataSource();
  }, [id]);

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
        <div style={{ marginTop: 16 }}>טוען נתוני מקור נתונים...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <div className="page-header">
          <div><Title level={2} style={{ margin: 0 }}>שגיאה בטעינת מקור נתונים</Title></div>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>{t('common.back')}</Button>
        </div>
        <Alert message={t('common.error')} description={error} type="error" showIcon />
      </div>
    );
  }

  if (!dataSource) {
    return (
      <div>
        <div className="page-header">
          <div><Title level={2} style={{ margin: 0 }}>מקור נתונים לא נמצא</Title></div>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>{t('common.back')}</Button>
        </div>
        <Alert message="מקור נתונים לא נמצא" description="מקור הנתונים שביקשת לא נמצא או שנמחק." type="warning" showIcon />
      </div>
    );
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>עריכת מקור נתונים</Title>
          <Paragraph className="page-subtitle">
            עריכת הגדרות מקור נתונים עם תמיכה בכל ההגדרות המתקדמות
          </Paragraph>
        </div>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>{t('common.back')}</Button>
        </Space>
      </div>

      <Card>
        <Spin spinning={saving}>
          <Form form={form} layout="vertical" onFinish={handleSubmit} preserve={true}>
            <Tabs activeKey={activeTab} onChange={setActiveTab} type="card" destroyInactiveTabPane={false}>
              <TabPane tab={<span><FileOutlined /> מידע בסיסי</span>} key="basic">
                <BasicInfoTab form={form} t={t} />
              </TabPane>

              <TabPane tab={<span><ApiOutlined /> הגדרות חיבור</span>} key="connection">
                <ConnectionTab 
                  form={form} 
                  t={t} 
                  connectionType={connectionType}
                  testingConnection={testingConnection}
                  connectionTestResult={connectionTestResult}
                  onTestConnection={handleTestConnection}
                />
              </TabPane>

              <TabPane tab={<span><FileOutlined /> הגדרות קובץ</span>} key="file">
                <FileSettingsTab form={form} t={t} fileType={fileType} />
              </TabPane>

              <TabPane tab={<span><FileTextOutlined /> הגדרת Schema</span>} key="schema">
                <SchemaTab jsonSchema={jsonSchema} onChange={handleSchemaChange} />
              </TabPane>

              <TabPane tab={<span><ClockCircleOutlined /> תזמון</span>} key="schedule">
                <ScheduleTab 
                  form={form} 
                  t={t} 
                  scheduleFrequency={scheduleFrequency}
                  cronExpression={cronExpression}
                  onOpenCronHelper={() => setCronHelperVisible(true)}
                />
              </TabPane>

              <TabPane tab={<span><SafetyOutlined /> כללי אימות</span>} key="validation">
                <ValidationTab form={form} t={t} />
              </TabPane>

              <TabPane tab={<span><BellOutlined /> התראות</span>} key="notifications">
                <NotificationsTab form={form} t={t} />
              </TabPane>
            </Tabs>

            <Divider />
            <Form.Item style={{ marginTop: 24, marginBottom: 0 }}>
              <Space size="middle">
                <Button type="primary" size="large" htmlType="submit" icon={<SaveOutlined />} loading={saving} disabled={connectionTestResult === 'failed'}>
                  {t('common.update')}
                </Button>
                <Button size="large" onClick={() => navigate(`/datasources/${id}`)} disabled={saving}>
                  {t('common.cancel')}
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Spin>
      </Card>

      <CronHelperDialog
        visible={cronHelperVisible}
        onClose={() => setCronHelperVisible(false)}
        onSelect={handleCronHelperSelect}
        currentValue={form.getFieldValue('cronExpression')}
      />
    </div>
  );
};

export default DataSourceEditEnhanced;
