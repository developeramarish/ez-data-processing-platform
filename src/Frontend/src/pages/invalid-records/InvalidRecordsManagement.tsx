import React, { useState, useEffect } from 'react';
import { Card, Button, Tag, Space, Typography, Row, Col, Select, DatePicker, Collapse, Descriptions, message, Spin } from 'antd';
import {
  ExclamationCircleOutlined,
  ExportOutlined,
  DeleteOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { invalidRecordsApiClient, InvalidRecord, Statistics } from '../../services/invalidrecords-api-client';
import dayjs, { Dayjs } from 'dayjs';
import { Statistic } from 'antd';
import { CheckCircleOutlined, EyeInvisibleOutlined, BarChartOutlined, DatabaseOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Panel } = Collapse;
const { Option } = Select;

interface DataSource {
  ID: string;
  Name: string;
  Category: string;
}

const InvalidRecordsManagement: React.FC = () => {
  const [selectedDataSource, setSelectedDataSource] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedErrorType, setSelectedErrorType] = useState<string>('all');
  const [selectedTimeRange, setSelectedTimeRange] = useState<string>('all');
  const [dateRange, setDateRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);
  const [loading, setLoading] = useState(false);
  const [invalidRecords, setInvalidRecords] = useState<InvalidRecord[]>([]);
  const [dataSources, setDataSources] = useState<DataSource[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [statistics, setStatistics] = useState<Statistics | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  // Fetch datasources for filter dropdown
  const fetchDataSources = async () => {
    try {
      const response = await fetch('http://localhost:5001/api/v1/datasource?page=1&pageSize=100');
      const data = await response.json();

      if (data.Data?.Items) {
        setDataSources(data.Data.Items);
        const uniqueCategories = [...new Set(data.Data.Items.map((ds: DataSource) => ds.Category))];
        setCategories(uniqueCategories);
      }
    } catch (error) {
      console.error('Failed to fetch datasources:', error);
    }
  };

  // Fetch statistics for dashboard
  const fetchStatistics = async () => {
    try {
      const stats = await invalidRecordsApiClient.getStatistics();
      setStatistics(stats);
    } catch (error) {
      console.error('Failed to fetch statistics:', error);
    }
  };

  // Calculate date range based on time interval selection
  const getDateRangeFromInterval = (interval: string): [string, string] | undefined => {
    if (interval === 'all') return undefined;

    const now = dayjs();
    let start: Dayjs;

    switch (interval) {
      case 'hour':
        start = now.subtract(1, 'hour');
        break;
      case 'day':
        start = now.subtract(1, 'day');
        break;
      case 'week':
        start = now.subtract(1, 'week');
        break;
      case 'month':
        start = now.subtract(1, 'month');
        break;
      default:
        return undefined;
    }

    return [start.toISOString(), now.toISOString()];
  };

  // Fetch invalid records from API
  const fetchRecords = async () => {
    setLoading(true);
    try {
      let startDate, endDate;

      // Use custom date range if set, otherwise use time interval
      if (dateRange && dateRange[0] && dateRange[1]) {
        startDate = dateRange[0].toISOString();
        endDate = dateRange[1].toISOString();
      } else {
        const intervalRange = getDateRangeFromInterval(selectedTimeRange);
        if (intervalRange) {
          [startDate, endDate] = intervalRange;
        }
      }

      const result = await invalidRecordsApiClient.getList({
        page: currentPage,
        pageSize,
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
        startDate,
        endDate,
      });

      setInvalidRecords(result.data);
      setTotalCount(result.totalCount);
    } catch (error: any) {
      message.error(error.message || 'Failed to load invalid records');
      setInvalidRecords([]);
    } finally {
      setLoading(false);
    }
  };

  // Get filtered datasources based on selected category
  const getFilteredDataSources = () => {
    if (selectedCategory === 'all') return dataSources;
    return dataSources.filter(ds => ds.Category === selectedCategory);
  };

  // Handle category change - reset datasource selection if it's not in the new category
  const handleCategoryChange = (category: string) => {
    setSelectedCategory(category);
    if (category !== 'all') {
      const filteredDs = dataSources.filter(ds => ds.Category === category);
      if (selectedDataSource !== 'all' && !filteredDs.find(ds => ds.ID === selectedDataSource)) {
        setSelectedDataSource('all');
      }
    }
  };

  useEffect(() => {
    fetchDataSources();
    fetchStatistics();
  }, []);

  useEffect(() => {
    fetchRecords();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDataSource, selectedErrorType, selectedTimeRange, dateRange, currentPage]);

  const handleReprocess = async (recordId: string) => {
    try {
      await invalidRecordsApiClient.reprocessRecord(recordId);
      message.success('Record reprocessed successfully');
      await Promise.all([fetchRecords(), fetchStatistics()]);
    } catch (error: any) {
      message.error(error.message || 'Failed to reprocess record');
    }
  };

  const handleDelete = async (recordId: string) => {
    try {
      await invalidRecordsApiClient.deleteRecord(recordId);
      message.success('Record deleted successfully');
      await Promise.all([fetchRecords(), fetchStatistics()]);
    } catch (error: any) {
      message.error(error.message || 'Failed to delete record');
    }
  };

  const handleExport = async () => {
    try {
      // Export current filtered records as JSON
      const jsonData = JSON.stringify(invalidRecords, null, 2);
      const blob = new Blob([jsonData], { type: 'application/json' });

      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `invalid-records-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);

      message.success('Export completed successfully');
    } catch (error: any) {
      message.error(error.message || 'Failed to export data');
    }
  };

  const renderErrorType = (errorType: string) => {
    const colors: Record<string, string> = {
      schema: 'red',
      format: 'orange',
      required: 'volcano',
      range: 'magenta',
      SchemaValidation: 'red',
    };
    const labels: Record<string, string> = {
      schema: 'אימות Schema',
      format: 'שגיאת פורמט', 
      required: 'שדה חובה חסר',
      range: 'שגיאת טווח',
      SchemaValidation: 'אימות Schema',
    };
    return <Tag color={colors[errorType] || 'red'}>{labels[errorType] || errorType}</Tag>;
  };

  const renderInvalidRecord = (record: InvalidRecord) => (
    <Card 
      key={record.id}
      style={{ 
        marginBottom: 16, 
        backgroundColor: '#fff5f5',
        borderRight: '4px solid #e74c3c',
      }}
    >
      <Row justify="space-between" align="top">
        <Col flex="auto">
          <Title level={5}>רשומה #{record.id}</Title>
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="מקור נתונים">{record.dataSourceName}</Descriptions.Item>
            <Descriptions.Item label="קובץ">{record.fileName}</Descriptions.Item>
            <Descriptions.Item label="חותמת זמן">{new Date(record.createdAt).toLocaleString('he-IL')}</Descriptions.Item>
            {record.lineNumber && (
              <Descriptions.Item label="שורה">{record.lineNumber}</Descriptions.Item>
            )}
          </Descriptions>
        </Col>
        <Col>
          <Space>
            <Button 
              size="small" 
              icon={<ReloadOutlined />} 
              onClick={() => handleReprocess(record.id)}
            >
              עבד מחדש
            </Button>
            <Button 
              size="small" 
              danger 
              icon={<DeleteOutlined />} 
              onClick={() => handleDelete(record.id)}
            >
              מחק
            </Button>
          </Space>
        </Col>
      </Row>

      <div style={{ marginTop: 12, color: '#e74c3c' }}>
        <Text strong>שגיאות אימות:</Text>
        <ul style={{ marginTop: 8, paddingRight: 20 }}>
          {record.errors.map((error, index) => (
            <li key={index}>
              <Space>
                {renderErrorType(error.errorType)}
                <Text>{error.message}</Text>
              </Space>
            </li>
          ))}
        </ul>
      </div>

      <Collapse ghost style={{ marginTop: 10 }}>
        <Panel header="הצג נתוני רשומה מקורית" key="1">
          <div 
            style={{
              backgroundColor: '#2d3748',
              color: '#68d391',
              padding: 12,
              borderRadius: 6,
              fontFamily: 'monospace',
              fontSize: '12px',
              direction: 'ltr',
              textAlign: 'left',
            }}
          >
            <pre>{JSON.stringify(record.originalData, null, 2)}</pre>
          </div>
        </Panel>
      </Collapse>
    </Card>
  );

  return (
    <div className="invalid-records-page">
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            ניהול רשומות לא תקינות
          </Title>
        </div>
        <Space>
          <Button
            icon={<ExportOutlined />}
            onClick={handleExport}
            disabled={totalCount === 0}
          >
            יצא JSON ({totalCount})
          </Button>
        </Space>
      </div>

      {/* Statistics Dashboard */}
      {statistics && (
        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col span={6}>
            <Card>
              <Statistic
                title="סה״כ רשומות לא תקינות"
                value={statistics.totalInvalidRecords}
                prefix={<ExclamationCircleOutlined />}
                valueStyle={{ color: '#ff4d4f' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="רשומות שנבדקו"
                value={statistics.reviewedRecords}
                prefix={<CheckCircleOutlined />}
                valueStyle={{ color: '#52c41a' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="רשומות שהתעלמו"
                value={statistics.ignoredRecords}
                prefix={<EyeInvisibleOutlined />}
                valueStyle={{ color: '#faad14' }}
              />
            </Card>
          </Col>
          <Col span={6}>
            <Card>
              <Statistic
                title="סוגי שגיאות"
                value={Object.keys(statistics.byErrorType).length}
                prefix={<BarChartOutlined />}
                valueStyle={{ color: '#1890ff' }}
              />
            </Card>
          </Col>
        </Row>
      )}

      {/* Search and Filter Controls */}
      <Card style={{ marginBottom: 24 }}>
        <Row gutter={[16, 16]}>
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>קטגוריה:</Text>
              <Select
                style={{ width: '100%' }}
                value={selectedCategory}
                onChange={handleCategoryChange}
              >
                <Option value="all">כל הקטגוריות</Option>
                {categories.map(cat => (
                  <Option key={cat} value={cat}>{cat}</Option>
                ))}
              </Select>
            </Space>
          </Col>
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>מקור נתונים:</Text>
              <Select
                style={{ width: '100%' }}
                value={selectedDataSource}
                onChange={setSelectedDataSource}
                showSearch
                optionFilterProp="children"
              >
                <Option value="all">כל מקורות הנתונים</Option>
                {getFilteredDataSources().map(ds => (
                  <Option key={ds.ID} value={ds.ID}>{ds.Name}</Option>
                ))}
              </Select>
            </Space>
          </Col>
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>סוג שגיאה:</Text>
              <Select
                style={{ width: '100%' }}
                value={selectedErrorType}
                onChange={setSelectedErrorType}
              >
                <Option value="all">כל השגיאות</Option>
                <Option value="SchemaValidation">אימות Schema</Option>
                <Option value="FormatError">שגיאת פורמט</Option>
                <Option value="RequiredField">שדה חובה חסר</Option>
                <Option value="RangeError">שגיאת טווח</Option>
              </Select>
            </Space>
          </Col>
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>טווח זמן:</Text>
              <Select
                style={{ width: '100%' }}
                value={selectedTimeRange}
                onChange={(value) => {
                  setSelectedTimeRange(value);
                  if (value !== 'custom') setDateRange(null);
                }}
              >
                <Option value="all">כל הזמנים</Option>
                <Option value="hour">שעה אחרונה</Option>
                <Option value="day">יום אחרון</Option>
                <Option value="week">שבוע אחרון</Option>
                <Option value="month">חודש אחרון</Option>
                <Option value="custom">טווח מותאם אישית</Option>
              </Select>
            </Space>
          </Col>
        </Row>
        {selectedTimeRange === 'custom' && (
          <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
            <Col span={12}>
              <Space direction="vertical" size="small" style={{ width: '100%' }}>
                <Text>תאריכים מותאמים:</Text>
                <RangePicker
                  style={{ width: '100%' }}
                  value={dateRange}
                  onChange={(dates) => setDateRange(dates as [Dayjs | null, Dayjs | null])}
                  showTime
                />
              </Space>
            </Col>
            <Col span={12}>
              <Button
                type="primary"
                icon={<SearchOutlined />}
                style={{ marginTop: 24 }}
                onClick={fetchRecords}
                loading={loading}
              >
                חפש
              </Button>
            </Col>
          </Row>
        )}
      </Card>

      {/* Invalid Records List */}
      <Spin spinning={loading} tip="טוען רשומות לא תקינות...">
        {invalidRecords.length === 0 && !loading ? (
          <Card style={{ textAlign: 'center', padding: '40px' }}>
            <ExclamationCircleOutlined style={{ fontSize: '48px', color: '#bbb', marginBottom: '16px' }} />
            <Title level={4}>אין רשומות לא תקינות</Title>
            <Text type="secondary">לא נמצאו רשומות לא תקינות עם הפילטרים הנבחרים</Text>
          </Card>
        ) : (
          <div>
            {invalidRecords.map(renderInvalidRecord)}
            
            {totalCount > pageSize && (
              <div style={{ textAlign: 'center', marginTop: 24 }}>
                <Button.Group>
                  <Button 
                    onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                  >
                    הקודם
                  </Button>
                  <Button disabled>
                    עמוד {currentPage} מתוך {Math.ceil(totalCount / pageSize)}
                  </Button>
                  <Button 
                    onClick={() => setCurrentPage(p => p + 1)}
                    disabled={currentPage >= Math.ceil(totalCount / pageSize)}
                  >
                    הבא
                  </Button>
                </Button.Group>
              </div>
            )}
          </div>
        )}
      </Spin>
    </div>
  );
};

export default InvalidRecordsManagement;
