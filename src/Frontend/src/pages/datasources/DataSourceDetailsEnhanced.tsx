import React, { useState, useEffect, useCallback } from 'react';
import { Card, Typography, Space, Button, Alert, Spin, Tabs, Row, Col, Statistic } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeftOutlined, EditOutlined, FileOutlined, ApiOutlined, ClockCircleOutlined, SafetyOutlined, BellOutlined, InfoCircleOutlined, FileTextOutlined, LineChartOutlined, ExportOutlined } from '@ant-design/icons';

// Import detail tab components
import {
  BasicInfoDetailsTab,
  ConnectionDetailsTab,
  FileDetailsTab,
  ScheduleDetailsTab,
  ValidationDetailsTab,
  NotificationsDetailsTab,
  OutputDetailsTab
} from '../../components/datasource/details/AllDetailsTabsExport';
import { SchemaDetailsTab } from '../../components/datasource/details/SchemaDetailsTab';
import { RelatedMetricsTab } from '../../components/datasource/details/RelatedMetricsTab';

// Import shared utilities
import { DataSource, ApiResponse } from '../../components/datasource/shared/types';
import { extractFileTypeFromPattern } from '../../components/datasource/shared/helpers';

const { Title, Paragraph } = Typography;
const { TabPane } = Tabs;

const DataSourceDetailsEnhanced: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<DataSource | null>(null);
  const [parsedConfig, setParsedConfig] = useState<any>(null);
  const [schemas, setSchemas] = useState<any[]>([]);
  const [loadingSchemas, setLoadingSchemas] = useState<boolean>(false);

  const fetchSchemas = async () => {
    setLoadingSchemas(true);
    try {
      const response = await fetch('http://localhost:5001/api/v1/schema', {
        method: 'GET',
        headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
      });

      if (response.ok) {
        const data = await response.json();
        if (data.isSuccess && data.data) {
          setSchemas(data.data);
        }
      }
    } catch (err) {
      console.error('Error fetching schemas:', err);
    } finally {
      setLoadingSchemas(false);
    }
  };

  const fetchDataSource = useCallback(async () => {
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
        
        if (data.Data.AdditionalConfiguration?.ConfigurationSettings) {
          try {
            const config = JSON.parse(data.Data.AdditionalConfiguration.ConfigurationSettings);
            setParsedConfig(config);
          } catch (e) {
            console.warn('Failed to parse configuration:', e);
          }
        }
      } else {
        throw new Error(data.Error?.Message || 'Failed to fetch data source');
      }
    } catch (err) {
      console.error('Error fetching data source:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch data source');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchDataSource();
    fetchSchemas();
  }, [fetchDataSource]);

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

  const getFileTypeDisplay = () => {
    if (fileConfig.type) return fileConfig.type;
    if (dataSource?.FilePattern) {
      return extractFileTypeFromPattern(dataSource.FilePattern);
    }
    return 'לא הוגדר';
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

  const connectionConfig = parsedConfig?.connectionConfig || {};
  const fileConfig = parsedConfig?.fileConfig || {};
  const schedule = parsedConfig?.schedule || {};
  const validationRules = parsedConfig?.validationRules || {};
  const notificationSettings = parsedConfig?.notificationSettings || {};
  const outputConfig = parsedConfig?.outputConfig || null;

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>פרטי מקור נתונים</Title>
          <Paragraph className="page-subtitle">צפייה מפורטת בכל הגדרות וסטטיסטיקות מקור הנתונים</Paragraph>
        </div>
        <Space>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/datasources')}>{t('common.back')}</Button>
          <Button type="primary" icon={<EditOutlined />} onClick={() => navigate(`/datasources/${id}/edit`)}>{t('common.edit')}</Button>
        </Space>
      </div>

      <Row gutter={16} style={{ marginBottom: 16 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card><Statistic title="קבצים שעובדו" value={dataSource.TotalFilesProcessed} valueStyle={{ color: '#3f8600' }} prefix="📊" /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card><Statistic title="רשומות שגויות" value={dataSource.TotalErrorRecords} valueStyle={{ color: '#cf1322' }} prefix="⚠️" /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card><Statistic title="תקופת שמירה" value={dataSource.AdditionalConfiguration?.RetentionDays || 0} suffix="ימים" valueStyle={{ color: '#1890ff' }} prefix="📁" /></Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card><Statistic title="סטטוס" value={dataSource.IsActive ? 'פעיל' : 'לא פעיל'} valueStyle={{ color: dataSource.IsActive ? '#3f8600' : '#cf1322' }} prefix={dataSource.IsActive ? '✅' : '❌'} /></Card>
        </Col>
      </Row>

      <Card>
        <Tabs defaultActiveKey="basic" type="card">
          <TabPane tab={<span><InfoCircleOutlined /> מידע בסיסי</span>} key="basic">
            <BasicInfoDetailsTab dataSource={dataSource} getCategoryLabel={getCategoryLabel} />
          </TabPane>

          <TabPane tab={<span><ApiOutlined /> חיבור</span>} key="connection">
            <ConnectionDetailsTab connectionConfig={connectionConfig} dataSource={dataSource} />
          </TabPane>

          <TabPane tab={<span><FileOutlined /> קובץ</span>} key="file">
            <FileDetailsTab fileConfig={fileConfig} getFileTypeDisplay={getFileTypeDisplay} />
          </TabPane>

          <TabPane tab={<span><FileTextOutlined /> Schema</span>} key="schema">
            <SchemaDetailsTab jsonSchema={dataSource.JsonSchema} dataSourceId={id} onEdit={() => navigate(`/datasources/${id}/edit`)} />
          </TabPane>

          <TabPane tab={<span><ClockCircleOutlined /> תזמון</span>} key="schedule">
            <ScheduleDetailsTab schedule={schedule} />
          </TabPane>

          <TabPane tab={<span><SafetyOutlined /> אימות</span>} key="validation">
            <ValidationDetailsTab validationRules={validationRules} schemas={schemas} dataSourceId={id} loadingSchemas={loadingSchemas} />
          </TabPane>

          <TabPane tab={<span><BellOutlined /> התראות</span>} key="notifications">
            <NotificationsDetailsTab notificationSettings={notificationSettings} />
          </TabPane>

          <TabPane tab={<span><ExportOutlined /> פלט</span>} key="output">
            <OutputDetailsTab outputConfig={outputConfig} />
          </TabPane>

          <TabPane tab={<span><LineChartOutlined /> Metrics</span>} key="metrics">
            <RelatedMetricsTab dataSourceId={id!} />
          </TabPane>
        </Tabs>
      </Card>
    </div>
  );
};

export default DataSourceDetailsEnhanced;
