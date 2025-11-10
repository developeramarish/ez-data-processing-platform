import React, { useState, useEffect } from 'react';
import { 
  Form, 
  Input, 
  Button, 
  Card, 
  Typography, 
  Space, 
  Select, 
  Switch, 
  InputNumber,
  message,
  Alert,
  Spin
} from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeftOutlined, SaveOutlined } from '@ant-design/icons';

const { Title, Paragraph } = Typography;
const { TextArea } = Input;

// TypeScript interfaces for API response - matches backend DataProcessingDataSource entity
interface DataSource {
  ID: string;
  Name: string;
  SupplierName: string;
  Category: string;
  Description?: string;
  IsActive: boolean;
  FilePath: string; // Backend uses FilePath instead of ConnectionString
  FilePattern: string; // Backend uses FilePattern instead of FileFormat
  AdditionalConfiguration?: { // Backend stores advanced settings here
    ConfigurationSettings?: string;
    ValidationRules?: string;
    Metadata?: string;
    RetentionDays?: number;
  };
  CreatedAt: string;
  UpdatedAt: string;
  LastProcessedAt?: string;
  TotalFilesProcessed: number;
  TotalErrorRecords: number;
  SchemaVersion: number;
  PollingRate: string; // TimeSpan serialized as string
  JsonSchema: any; // BsonDocument serialized
}

interface ApiResponse<T> {
  CorrelationId: string;
  Data: T;
  Error: any;
  IsSuccess: boolean;
}

interface UpdateDataSourceRequest {
  Id: string;
  Name: string;
  SupplierName: string;
  Category: string;
  Description?: string;
  ConnectionString: string;
  IsActive: boolean;
  ConfigurationSettings?: string;
  ValidationRules?: string;
  Metadata?: string;
  FileFormat?: string;
  RetentionDays?: number;
}

const DataSourceEdit: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [form] = Form.useForm();
  
  const [loading, setLoading] = useState<boolean>(false);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<DataSource | null>(null);

  // Category options
  const categoryOptions = [
    { value: 'financial', label: t('datasources.categories.financial') },
    { value: 'customers', label: t('datasources.categories.customers') },
    { value: 'inventory', label: t('datasources.categories.inventory') },
    { value: 'sales', label: t('datasources.categories.sales') },
    { value: 'operations', label: t('datasources.categories.operations') },
    { value: 'other', label: t('datasources.categories.other') },
  ];

  // File format options
  const fileFormatOptions = [
    { value: 'csv', label: 'CSV' },
    { value: 'json', label: 'JSON' },
    { value: 'xml', label: 'XML' },
    { value: 'excel', label: 'Excel' },
    { value: 'txt', label: 'Text' },
  ];

  // Fetch data source details
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
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: ApiResponse<DataSource> = await response.json();

      if (data.IsSuccess) {
        setDataSource(data.Data);
        
        // Extract advanced settings from AdditionalConfiguration
        const additionalConfig = data.Data.AdditionalConfiguration || {};
        
        // Populate form fields
        form.setFieldsValue({
          name: data.Data.Name,
          supplierName: data.Data.SupplierName,
          category: data.Data.Category,
          description: data.Data.Description,
          connectionString: data.Data.FilePath, // Map FilePath back to ConnectionString for frontend
          isActive: data.Data.IsActive,
          configurationSettings: additionalConfig.ConfigurationSettings || '',
          validationRules: additionalConfig.ValidationRules || '',
          metadata: additionalConfig.Metadata || '',
          fileFormat: data.Data.FilePattern === '*.*' ? null : data.Data.FilePattern, // Map FilePattern back to FileFormat
          retentionDays: additionalConfig.RetentionDays || null,
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

  // Handle form submission
  const onFinish = async (values: any) => {
    if (!id || !dataSource) {
      message.error('Data source information is missing');
      return;
    }

    setSaving(true);

    try {
      const updateRequest: UpdateDataSourceRequest = {
        Id: id,
        Name: values.name,
        SupplierName: values.supplierName,
        Category: values.category,
        Description: values.description || null,
        ConnectionString: values.connectionString,
        IsActive: values.isActive || false,
        ConfigurationSettings: values.configurationSettings || null,
        ValidationRules: values.validationRules || null,
        Metadata: values.metadata || null,
        FileFormat: values.fileFormat || null,
        RetentionDays: values.retentionDays != null && values.retentionDays !== '' ? Number(values.retentionDays) : undefined,
      };

      const response = await fetch(`http://localhost:5001/api/v1/datasource/${id}`, {
        method: 'PUT',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(updateRequest),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: ApiResponse<any> = await response.json();

      if (data.IsSuccess) {
        message.success(t('messages.dataSourceUpdated'));
        navigate('/datasources');
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

  // Load data source on component mount
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
          <div>
            <Title level={2} style={{ margin: 0 }}>
              שגיאה בטעינת מקור נתונים
            </Title>
          </div>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>
            {t('common.back')}
          </Button>
        </div>
        <Alert
          message={t('common.error')}
          description={error}
          type="error"
          showIcon
        />
      </div>
    );
  }

  if (!dataSource) {
    return (
      <div>
        <div className="page-header">
          <div>
            <Title level={2} style={{ margin: 0 }}>
              מקור נתונים לא נמצא
            </Title>
          </div>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>
            {t('common.back')}
          </Button>
        </div>
        <Alert
          message="מקור נתונים לא נמצא"
          description="מקור הנתונים שביקשת לא נמצא או שנמחק."
          type="warning"
          showIcon
        />
      </div>
    );
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            עריכת מקור נתונים
          </Title>
          <Paragraph className="page-subtitle">
            עריכת הגדרות מקור נתונים קיים במערכת
          </Paragraph>
        </div>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>
          {t('common.back')}
        </Button>
      </div>

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={onFinish}
          autoComplete="off"
          scrollToFirstError
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <Form.Item
              label={t('datasources.fields.name')}
              name="name"
              rules={[
                { required: true, message: 'שם מקור הנתונים נדרש' },
                { min: 2, max: 100, message: 'שם מקור הנתונים חייב להיות בין 2 ל-100 תווים' }
              ]}
            >
              <Input placeholder="הזן שם ייחודי למקור הנתונים" />
            </Form.Item>

            <Form.Item
              label={t('datasources.fields.supplierName')}
              name="supplierName"
              rules={[
                { required: true, message: 'שם ספק הנתונים נדרש' },
                { min: 2, max: 100, message: 'שם ספק הנתונים חייב להיות בין 2 ל-100 תווים' }
              ]}
            >
              <Input placeholder="הזן שם ספק הנתונים" />
            </Form.Item>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <Form.Item
              label={t('datasources.fields.category')}
              name="category"
              rules={[{ required: true, message: 'קטגוריה נדרשת' }]}
            >
              <Select
                placeholder="בחר קטגוריה"
                options={categoryOptions}
              />
            </Form.Item>

            <Form.Item
              label="פורמט הקובץ"
              name="fileFormat"
            >
              <Select
                placeholder="בחר פורמט קובץ"
                allowClear
                options={fileFormatOptions}
              />
            </Form.Item>
          </div>

          <Form.Item
            label="נתיב החיבור"
            name="connectionString"
            rules={[
              { required: true, message: 'נתיב החיבור נדרש' },
              { max: 1000, message: 'נתיב החיבור לא יכול להיות ארוך מ-1000 תווים' }
            ]}
          >
            <Input placeholder="הזן נתיב קובץ או מחרוזת חיבור למקור הנתונים" />
          </Form.Item>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: '16px', alignItems: 'end' }}>
            <Form.Item
              label="תקופת שמירה (ימים)"
              name="retentionDays"
              rules={[
                { type: 'number', min: 1, message: 'תקופת שמירה חייבת להיות לפחות יום אחד' },
                { type: 'number', max: 3650, message: 'תקופת שמירה לא יכולה להיות יותר מ-3650 ימים (10 שנים)' }
              ]}
            >
              <InputNumber 
                style={{ width: '100%' }}
                placeholder="30"
                min={1}
                max={3650}
              />
            </Form.Item>

            <Form.Item
              name="isActive"
              valuePropName="checked"
              style={{ marginBottom: '24px' }}
            >
              <div style={{ textAlign: 'center' }}>
                <div style={{ marginBottom: '8px', fontSize: '14px' }}>פעיל</div>
                <Switch />
              </div>
            </Form.Item>
          </div>

          <Form.Item
            label={t('datasources.fields.description')}
            name="description"
            rules={[{ max: 500, message: 'תיאור לא יכול להיות ארוך מ-500 תווים' }]}
          >
            <TextArea
              rows={4}
              placeholder="תיאור מפורט על מקור הנתונים (אופציונלי)"
              showCount
              maxLength={500}
            />
          </Form.Item>

          <Title level={4} style={{ marginTop: '32px' }}>הגדרות מתקדמות</Title>
          
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '16px' }}>
            <Form.Item
              label="הגדרות תצורה (JSON)"
              name="configurationSettings"
            >
              <TextArea
                rows={4}
                placeholder='{"owner": "team_name", "version": "1.0"}'
              />
            </Form.Item>

            <Form.Item
              label="כללי ולידציה (JSON)"
              name="validationRules"
            >
              <TextArea
                rows={4}
                placeholder='{"required_fields": ["id", "name"], "max_errors": 100}'
              />
            </Form.Item>

            <Form.Item
              label="מטא-דטא (JSON)"
              name="metadata"
            >
              <TextArea
                rows={4}
                placeholder='{"polling_interval": 300, "batch_size": 1000}'
              />
            </Form.Item>
          </div>

          <div style={{ textAlign: 'center', marginTop: '32px' }}>
            <Space size="middle">
              <Button onClick={() => navigate('/datasources')}>
                {t('common.cancel')}
              </Button>
              <Button 
                type="primary" 
                htmlType="submit" 
                icon={<SaveOutlined />}
                loading={saving}
              >
                {t('common.update')}
              </Button>
            </Space>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default DataSourceEdit;
