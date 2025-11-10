import React, { useState } from 'react';
import { 
  Typography, 
  Card, 
  Form, 
  Input, 
  Select, 
  Switch, 
  Button, 
  Space, 
  Alert, 
  Spin,
  InputNumber,
  Row,
  Col,
  message
} from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeftOutlined, SaveOutlined } from '@ant-design/icons';

const { Title, Paragraph } = Typography;
const { TextArea } = Input;
const { Option } = Select;

// TypeScript interfaces
interface CreateDataSourceRequest {
  name: string;
  supplierName: string;
  category: string;
  description?: string;
  connectionString: string;
  isActive: boolean;
  configurationSettings?: string;
  validationRules?: string;
  metadata?: string;
  fileFormat?: string;
  retentionDays?: number;
}

interface ApiResponse<T> {
  CorrelationId: string;
  Data: T;
  Error: any;
  IsSuccess: boolean;
}

const DataSourceForm: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Form submit handler
  const handleSubmit = async (values: CreateDataSourceRequest) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('http://localhost:5001/api/v1/datasource', {
        method: 'POST',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: values.name,
          supplierName: values.supplierName,
          category: values.category,
          description: values.description || null,
          connectionString: values.connectionString,
          isActive: values.isActive,
          configurationSettings: values.configurationSettings || null,
          validationRules: values.validationRules || null,
          metadata: values.metadata || null,
          fileFormat: values.fileFormat || null,
          retentionDays: values.retentionDays || null
        }),
      });

      if (!response.ok) {
        if (response.status === 409) {
          throw new Error(t('errors.duplicateName'));
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: ApiResponse<any> = await response.json();

      if (data.IsSuccess) {
        message.success(t('messages.dataSourceCreated'));
        navigate('/datasources');
      } else {
        throw new Error(data.Error?.Message || 'Failed to create data source');
      }
    } catch (err) {
      console.error('Error creating data source:', err);
      setError(err instanceof Error ? err.message : 'Failed to create data source');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            {t('datasources.create')}
          </Title>
          <Paragraph className="page-subtitle">
            צור מקור נתונים חדש במערכת עם הגדרות מותאמות אישית
          </Paragraph>
        </div>
        <Space>
          <Button 
            icon={<ArrowLeftOutlined />} 
            onClick={() => navigate('/datasources')}
          >
            {t('common.back')}
          </Button>
        </Space>
      </div>

      {error && (
        <Alert
          message={t('common.error')}
          description={error}
          type="error"
          closable
          onClose={() => setError(null)}
          style={{ marginBottom: 16 }}
        />
      )}

      <Card>
        <Spin spinning={loading}>
          <Form
            form={form}
            layout="vertical"
            onFinish={handleSubmit}
            initialValues={{
              isActive: true,
              category: 'financial'
            }}
          >
            <Row gutter={16}>
              <Col xs={24} lg={12}>
                <Form.Item
                  name="name"
                  label={t('datasources.fields.name')}
                  rules={[
                    { required: true, message: t('errors.required') },
                    { min: 2, message: 'שם מקור הנתונים חייב להיות לפחות 2 תווים' },
                    { max: 100, message: 'שם מקור הנתונים לא יכול להיות ארוך מ-100 תווים' }
                  ]}
                >
                  <Input 
                    placeholder="הזן שם ייחודי למקור הנתונים" 
                    maxLength={100}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} lg={12}>
                <Form.Item
                  name="supplierName"
                  label={t('datasources.fields.supplierName')}
                  rules={[
                    { required: true, message: t('errors.required') },
                    { min: 2, message: 'שם הספק חייב להיות לפחות 2 תווים' },
                    { max: 100, message: 'שם הספק לא יכול להיות ארוך מ-100 תווים' }
                  ]}
                >
                  <Input 
                    placeholder="הזן שם ספק הנתונים" 
                    maxLength={100}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} lg={12}>
                <Form.Item
                  name="category"
                  label={t('datasources.fields.category')}
                  rules={[
                    { required: true, message: t('errors.required') }
                  ]}
                >
                  <Select placeholder="בחר קטגוריה">
                    <Option value="financial">{t('datasources.categories.financial')}</Option>
                    <Option value="customers">{t('datasources.categories.customers')}</Option>
                    <Option value="inventory">{t('datasources.categories.inventory')}</Option>
                    <Option value="sales">{t('datasources.categories.sales')}</Option>
                    <Option value="operations">{t('datasources.categories.operations')}</Option>
                    <Option value="other">{t('datasources.categories.other')}</Option>
                  </Select>
                </Form.Item>
              </Col>

              <Col xs={24} lg={12}>
                <Form.Item
                  name="fileFormat"
                  label="פורמט הקובץ"
                >
                  <Select placeholder="בחר פורמט קובץ" allowClear>
                    <Option value="csv">CSV</Option>
                    <Option value="json">JSON</Option>
                    <Option value="xml">XML</Option>
                    <Option value="excel">Excel</Option>
                    <Option value="txt">Text</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} lg={16}>
                <Form.Item
                  name="connectionString"
                  label="נתיב החיבור"
                  rules={[
                    { required: true, message: t('errors.required') },
                    { max: 1000, message: 'נתיב החיבור לא יכול להיות ארוך מ-1000 תווים' }
                  ]}
                >
                  <Input 
                    placeholder="הזן נתיב קובץ או מחרוזת חיבור למקור הנתונים" 
                    maxLength={1000}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} lg={8}>
                <Form.Item
                  name="retentionDays"
                  label="תקופת שמירה (ימים)"
                >
                  <InputNumber 
                    min={1} 
                    max={3650} 
                    placeholder="30" 
                    style={{ width: '100%' }}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item
              name="description"
              label={t('datasources.fields.description')}
              rules={[
                { max: 500, message: 'התיאור לא יכול להיות ארוך מ-500 תווים' }
              ]}
            >
              <TextArea 
                rows={3}
                placeholder="הוסף תיאור מפורט על מקור הנתונים (אופציונלי)"
                maxLength={500}
                showCount
              />
            </Form.Item>

            <Row gutter={16}>
              <Col xs={24} lg={8}>
                <Form.Item
                  name="configurationSettings"
                  label="הגדרות תצורה (JSON)"
                >
                  <TextArea 
                    rows={3}
                    placeholder='{"polling_interval": 300, "batch_size": 1000}'
                  />
                </Form.Item>
              </Col>

              <Col xs={24} lg={8}>
                <Form.Item
                  name="validationRules"
                  label="כללי ולידציה (JSON)"
                >
                  <TextArea 
                    rows={3}
                    placeholder='{"required_fields": ["id", "name"], "max_errors": 100}'
                  />
                </Form.Item>
              </Col>

              <Col xs={24} lg={8}>
                <Form.Item
                  name="metadata"
                  label="מטא-דאטה (JSON)"
                >
                  <TextArea 
                    rows={3}
                    placeholder='{"owner": "team_name", "version": "1.0"}'
                  />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item
              name="isActive"
              valuePropName="checked"
              label={t('datasources.fields.status')}
            >
              <Switch 
                checkedChildren="פעיל" 
                unCheckedChildren="לא פעיל"
              />
            </Form.Item>

            <Form.Item style={{ marginTop: 32 }}>
              <Space>
                <Button
                  type="primary"
                  size="large"
                  htmlType="submit"
                  icon={<SaveOutlined />}
                  loading={loading}
                >
                  {t('common.create')}
                </Button>
                <Button
                  size="large"
                  onClick={() => navigate('/datasources')}
                  disabled={loading}
                >
                  {t('common.cancel')}
                </Button>
              </Space>
            </Form.Item>
          </Form>
        </Spin>
      </Card>
    </div>
  );
};

export default DataSourceForm;
