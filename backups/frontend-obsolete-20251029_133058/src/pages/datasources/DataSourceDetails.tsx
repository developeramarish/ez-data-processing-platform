import React, { useState, useEffect } from 'react';
import { 
  Card, 
  Typography, 
  Space, 
  Button, 
  Alert, 
  Spin, 
  Descriptions, 
  Tag, 
  Divider,
  Row,
  Col,
  Statistic
} from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeftOutlined, EditOutlined } from '@ant-design/icons';

const { Title, Paragraph, Text } = Typography;

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

const DataSourceDetails: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<DataSource | null>(null);

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

  // Load data source on component mount
  useEffect(() => {
    fetchDataSource();
  }, [id]);

  const getCategoryLabel = (category: string) => {
    const categoryMap: { [key: string]: string } = {
      'financial': t('datasources.categories.financial'),
      'customers': t('datasources.categories.customers'),
      'inventory': t('datasources.categories.inventory'),
      'sales': t('datasources.categories.sales'),
      'operations': t('datasources.categories.operations'),
      'other': t('datasources.categories.other'),
    };
    return categoryMap[category] || category;
  };

  const formatJson = (jsonString: string | undefined) => {
    if (!jsonString) return 'לא הוגדר';
    try {
      return JSON.stringify(JSON.parse(jsonString), null, 2);
    } catch {
      return jsonString;
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
        <div style={{ marginTop: 16 }}>טוען פרטי מקור נתונים...</div>
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
            פרטי מקור נתונים
          </Title>
          <Paragraph className="page-subtitle">
            צפייה מפורטת בהגדרות וסטטיסטיקות מקור הנתונים
          </Paragraph>
        </div>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>
            {t('common.back')}
          </Button>
          <Button 
            type="primary" 
            icon={<EditOutlined />} 
            onClick={() => navigate(`/datasources/${id}/edit`)}
          >
            {t('common.edit')}
          </Button>
        </Space>
      </div>

      {/* Basic Information */}
      <Card title="מידע בסיסי" style={{ marginBottom: 16 }}>
        <Descriptions column={2} bordered>
          <Descriptions.Item label="שם מקור הנתונים" span={2}>
            <Text strong>{dataSource.Name}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="ספק הנתונים">
            {dataSource.SupplierName}
          </Descriptions.Item>
          <Descriptions.Item label="קטגוריה">
            <Tag color="blue">{getCategoryLabel(dataSource.Category)}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="סטטוס">
            <Tag color={dataSource.IsActive ? 'green' : 'red'}>
              {dataSource.IsActive ? t('common.active') : t('common.inactive')}
            </Tag>
          </Descriptions.Item>
            <Descriptions.Item label="פורמט קובץ">
              {dataSource.FilePattern && dataSource.FilePattern !== '*.*' ? (
                <Tag>{dataSource.FilePattern.toUpperCase()}</Tag>
              ) : 'לא הוגדר'}
            </Descriptions.Item>
          <Descriptions.Item label="תיאור" span={2}>
            {dataSource.Description || 'לא הוגדר תיאור'}
          </Descriptions.Item>
            <Descriptions.Item label="נתיב החיבור" span={2}>
              <Text code style={{ wordBreak: 'break-all' }}>
                {dataSource.FilePath}
              </Text>
            </Descriptions.Item>
        </Descriptions>
      </Card>

      {/* Statistics */}
      <Card title="סטטיסטיקות" style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col span={6}>
            <Statistic
              title="קבצים שעובדו"
              value={dataSource.TotalFilesProcessed}
              valueStyle={{ color: '#3f8600' }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="רשומות שגויות"
              value={dataSource.TotalErrorRecords}
              valueStyle={{ color: '#cf1322' }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="תקופת שמירה"
              value={dataSource.AdditionalConfiguration?.RetentionDays || 'לא הוגדר'}
              suffix={dataSource.AdditionalConfiguration?.RetentionDays ? 'ימים' : ''}
              valueStyle={{ color: '#1890ff' }}
            />
          </Col>
          <Col span={6}>
            <Statistic
              title="עיבוד אחרון"
              value={dataSource.LastProcessedAt 
                ? new Date(dataSource.LastProcessedAt).toLocaleDateString(i18n.language)
                : t('common.never')
              }
              valueStyle={{ color: '#722ed1' }}
            />
          </Col>
        </Row>
      </Card>

      {/* Timestamps */}
      <Card title="תאריכים" style={{ marginBottom: 16 }}>
        <Descriptions column={2} bordered>
          <Descriptions.Item label="תאריך יצירה">
            {new Date(dataSource.CreatedAt).toLocaleString(i18n.language)}
          </Descriptions.Item>
          <Descriptions.Item label="תאריך עדכון אחרון">
            {new Date(dataSource.UpdatedAt).toLocaleString(i18n.language)}
          </Descriptions.Item>
          <Descriptions.Item label="עיבוד אחרון">
            {dataSource.LastProcessedAt 
              ? new Date(dataSource.LastProcessedAt).toLocaleString(i18n.language)
              : t('common.never')
            }
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {/* Advanced Settings */}
      <Card title="הגדרות מתקדמות">
        <Row gutter={[16, 16]}>
          <Col span={8}>
            <Title level={5}>הגדרות תצורה</Title>
            <Card size="small" style={{ minHeight: 200 }}>
              <pre style={{ 
                whiteSpace: 'pre-wrap', 
                fontSize: '12px', 
                margin: 0,
                maxHeight: '150px',
                overflow: 'auto'
              }}>
              {formatJson(dataSource.AdditionalConfiguration?.ConfigurationSettings)}
              </pre>
            </Card>
          </Col>
          <Col span={8}>
            <Title level={5}>כללי ולידציה</Title>
            <Card size="small" style={{ minHeight: 200 }}>
              <pre style={{ 
                whiteSpace: 'pre-wrap', 
                fontSize: '12px', 
                margin: 0,
                maxHeight: '150px',
                overflow: 'auto'
              }}>
              {formatJson(dataSource.AdditionalConfiguration?.ValidationRules)}
              </pre>
            </Card>
          </Col>
          <Col span={8}>
            <Title level={5}>מטא-דטא</Title>
            <Card size="small" style={{ minHeight: 200 }}>
              <pre style={{ 
                whiteSpace: 'pre-wrap', 
                fontSize: '12px', 
                margin: 0,
                maxHeight: '150px',
                overflow: 'auto'
              }}>
              {formatJson(dataSource.AdditionalConfiguration?.Metadata)}
              </pre>
            </Card>
          </Col>
        </Row>
      </Card>
    </div>
  );
};

export default DataSourceDetails;
