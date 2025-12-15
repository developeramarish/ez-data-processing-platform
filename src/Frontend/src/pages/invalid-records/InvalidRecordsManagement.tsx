import React, { useState, useEffect } from 'react';
import { Card, Button, Tag, Space, Typography, Row, Col, Select, DatePicker, Collapse, Descriptions, message, Spin, Statistic, Pagination, Popconfirm, Table, Tooltip } from 'antd';
import {
  ExclamationCircleOutlined,
  ExportOutlined,
  DeleteOutlined,
  ReloadOutlined,
  SearchOutlined,
  CheckCircleOutlined,
  EyeInvisibleOutlined,
  BarChartOutlined,
  DatabaseOutlined,
  ClearOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { invalidRecordsApiClient, InvalidRecord, Statistics } from '../../services/invalidrecords-api-client';
import dayjs, { Dayjs } from 'dayjs';
import EditRecordModal from '../../components/invalid-records/EditRecordModal';
import { translateValidationError, extractErroredFields } from '../../utils/validationErrorTranslator';

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
  const [filteredStatistics, setFilteredStatistics] = useState<Statistics | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [selectedRecord, setSelectedRecord] = useState<InvalidRecord | null>(null);
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

  // Fetch filtered statistics based on current filters
  const fetchFilteredStatistics = async () => {
    try {
      // Calculate date range for filtered statistics
      let startDate, endDate;
      if (dateRange && dateRange[0] && dateRange[1]) {
        startDate = dateRange[0].toISOString();
        endDate = dateRange[1].toISOString();
      } else {
        const intervalRange = getDateRangeFromInterval(selectedTimeRange);
        if (intervalRange) {
          [startDate, endDate] = intervalRange;
        }
      }

      // Fetch all filtered records to calculate statistics
      const allFilteredRecords = await invalidRecordsApiClient.getList({
        page: 1,
        pageSize: 1000,
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
        startDate,
        endDate,
      });

      // Calculate filtered statistics from records
      const filteredStats: Statistics = {
        totalInvalidRecords: allFilteredRecords.totalCount,
        reviewedRecords: allFilteredRecords.data.filter(r => r.isReviewed).length,
        ignoredRecords: allFilteredRecords.data.filter(r => r.isIgnored).length,
        byDataSource: {},
        byErrorType: {},
        bySeverity: {}
      };

      // Group by datasource
      allFilteredRecords.data.forEach(r => {
        filteredStats.byDataSource[r.dataSourceName] =
          (filteredStats.byDataSource[r.dataSourceName] || 0) + 1;
      });

      // Group by error type
      allFilteredRecords.data.forEach(r => {
        filteredStats.byErrorType[r.errorType] =
          (filteredStats.byErrorType[r.errorType] || 0) + 1;
      });

      // Group by severity
      allFilteredRecords.data.forEach(r => {
        filteredStats.bySeverity[r.severity] =
          (filteredStats.bySeverity[r.severity] || 0) + 1;
      });

      setFilteredStatistics(filteredStats);
    } catch (error) {
      console.error('Failed to fetch filtered statistics:', error);
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
    fetchFilteredStatistics();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDataSource, selectedErrorType, selectedTimeRange, dateRange, currentPage]);

  const handleReprocess = (record: InvalidRecord) => {
    // Open edit modal instead of direct reprocess
    setSelectedRecord(record);
    setEditModalVisible(true);
  };

  const handleEditSuccess = async () => {
    // Refresh both records list and statistics after successful edit
    await Promise.all([fetchRecords(), fetchStatistics(), fetchFilteredStatistics()]);
  };

  const handleDelete = async (recordId: string) => {
    try {
      await invalidRecordsApiClient.deleteRecord(recordId);
      message.success('Record deleted successfully');
      await Promise.all([fetchRecords(), fetchStatistics(), fetchFilteredStatistics()]);
    } catch (error: any) {
      message.error(error.message || 'Failed to delete record');
    }
  };

  const handleDeleteAll = async () => {
    try {
      setLoading(true);
      // Fetch all record IDs matching current filters
      const allRecords = await invalidRecordsApiClient.getList({
        page: 1,
        pageSize: 1000, // Get all records
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
      });

      if (allRecords.data.length === 0) {
        message.info('No records to delete');
        return;
      }

      const recordIds = allRecords.data.map(r => r.id);

      // Call bulk delete API
      const response = await fetch('http://localhost:5007/api/v1/invalid-records/bulk/delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ recordIds, requestedBy: 'User' })
      });

      if (response.ok) {
        message.success(`Successfully deleted ${recordIds.length} records`);
        await Promise.all([fetchRecords(), fetchStatistics(), fetchFilteredStatistics()]);
        setCurrentPage(1);
      } else {
        message.error('Failed to delete records');
      }
    } catch (error: any) {
      message.error(error.message || 'Failed to delete all records');
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async () => {
    try {
      setLoading(true);

      // Calculate date range for export (same as fetchRecords)
      let startDate, endDate;
      if (dateRange && dateRange[0] && dateRange[1]) {
        startDate = dateRange[0].toISOString();
        endDate = dateRange[1].toISOString();
      } else {
        const intervalRange = getDateRangeFromInterval(selectedTimeRange);
        if (intervalRange) {
          [startDate, endDate] = intervalRange;
        }
      }

      // Fetch ALL records matching current filters (not just current page)
      const allRecords = await invalidRecordsApiClient.getList({
        page: 1,
        pageSize: totalCount || 1000, // Get all records (use totalCount or max 1000)
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
        startDate,
        endDate,
      });

      // Export with UTF-8 BOM for Hebrew support
      const jsonData = JSON.stringify(allRecords.data, null, 2);
      const blob = new Blob(['\ufeff' + jsonData], { type: 'application/json;charset=utf-8' });

      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `invalid-records-${new Date().toISOString().split('T')[0]}.json`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);

      message.success(`Exported ${allRecords.data.length} records successfully`);
    } catch (error: any) {
      message.error(error.message || 'Failed to export data');
    } finally {
      setLoading(false);
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

  // Render original record as a table with errored fields highlighted
  const renderOriginalRecord = (record: InvalidRecord) => {
    const erroredFields = extractErroredFields(record.errors);
    const data = record.originalData || {};

    return (
      <Table
        dataSource={Object.entries(data).map(([key, value]) => ({
          key,
          field: key,
          value: typeof value === 'object' ? JSON.stringify(value) : String(value ?? ''),
          hasError: erroredFields.has(key),
        }))}
        columns={[
          {
            title: 'שדה',
            dataIndex: 'field',
            key: 'field',
            width: 150,
            render: (text: string, row: any) => (
              <span style={{
                fontWeight: row.hasError ? 'bold' : 'normal',
                color: row.hasError ? '#e74c3c' : 'inherit',
              }}>
                {row.hasError && <WarningOutlined style={{ marginLeft: 4, color: '#e74c3c' }} />}
                {text}
              </span>
            ),
          },
          {
            title: 'ערך',
            dataIndex: 'value',
            key: 'value',
            render: (text: string, row: any) => (
              <span style={{
                color: row.hasError ? '#e74c3c' : 'inherit',
                fontFamily: 'monospace',
                direction: 'ltr',
                display: 'inline-block',
              }}>
                {text}
              </span>
            ),
          },
        ]}
        pagination={false}
        size="small"
        rowClassName={(row: any) => row.hasError ? 'errored-field-row' : ''}
        style={{ direction: 'rtl' }}
      />
    );
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
              onClick={() => handleReprocess(record)}
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
        <div style={{ marginTop: 8 }}>
          {record.errors.map((error, index) => {
            const translated = translateValidationError(error.message);
            return (
              <Tag
                key={index}
                color="red"
                style={{ marginBottom: 4, fontSize: '13px', padding: '4px 8px' }}
              >
                <WarningOutlined style={{ marginLeft: 4 }} />
                {translated.shortMessage}
              </Tag>
            );
          })}
        </div>
      </div>

      <Collapse ghost style={{ marginTop: 10 }}>
        <Panel header="הצג נתוני רשומה מקורית" key="1">
          {renderOriginalRecord(record)}
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
          <Popconfirm
            title="מחק רשומות מסוננות?"
            description={`פעולה זו תמחק ${totalCount} רשומות לא תקינות לפי הפילטרים הנוכחיים ותעדכן את הסטטיסטיקות. האם להמשיך?`}
            onConfirm={handleDeleteAll}
            okText="מחק"
            cancelText="ביטול"
            okButtonProps={{ danger: true }}
          >
            <Button
              danger
              icon={<ClearOutlined />}
              disabled={totalCount === 0}
              loading={loading}
            >
              מחק הכל ({totalCount})
            </Button>
          </Popconfirm>
          <Button
            icon={<ExportOutlined />}
            onClick={handleExport}
            disabled={totalCount === 0}
          >
            יצא JSON ({totalCount})
          </Button>
        </Space>
      </div>

      {/* Global Statistics Dashboard */}
      {statistics && (
        <>
          <Title level={4} style={{ marginBottom: 16 }}>סטטיסטיקות כלליות</Title>
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
        </>
      )}

      {/* Filtered Statistics Dashboard */}
      {filteredStatistics && (
        <>
          <Title level={4} style={{ marginBottom: 16 }}>סטטיסטיקות מסוננות (תצוגה נוכחית)</Title>
          <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
            <Col span={6}>
              <Card style={{ borderColor: '#1890ff', borderWidth: 2 }}>
                <Statistic
                  title="רשומות מוצגות"
                  value={filteredStatistics.totalInvalidRecords}
                  prefix={<SearchOutlined />}
                  valueStyle={{ color: '#1890ff' }}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card style={{ borderColor: '#52c41a', borderWidth: 2 }}>
                <Statistic
                  title="נבדקו (מסוננות)"
                  value={filteredStatistics.reviewedRecords}
                  prefix={<CheckCircleOutlined />}
                  valueStyle={{ color: '#52c41a' }}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card style={{ borderColor: '#faad14', borderWidth: 2 }}>
                <Statistic
                  title="התעלמו (מסוננות)"
                  value={filteredStatistics.ignoredRecords}
                  prefix={<EyeInvisibleOutlined />}
                  valueStyle={{ color: '#faad14' }}
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card style={{ borderColor: '#1890ff', borderWidth: 2 }}>
                <Statistic
                  title="סוגי שגיאות (מסוננות)"
                  value={Object.keys(filteredStatistics.byErrorType).length}
                  prefix={<BarChartOutlined />}
                  valueStyle={{ color: '#1890ff' }}
                />
              </Card>
            </Col>
          </Row>
        </>
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
            {(() => {
              // Group records by dataSource
              const groupedRecords = invalidRecords.reduce((acc, record) => {
                const key = record.dataSourceId;
                if (!acc[key]) {
                  acc[key] = {
                    dataSourceName: record.dataSourceName,
                    dataSourceId: record.dataSourceId,
                    records: []
                  };
                }
                acc[key].records.push(record);
                return acc;
              }, {} as Record<string, {dataSourceName: string, dataSourceId: string, records: InvalidRecord[]}>);

              // Render with Collapse grouping
              return (
                <Collapse
                  items={Object.entries(groupedRecords).map(([dsId, group]) => ({
                    key: dsId,
                    label: (
                      <Space>
                        <DatabaseOutlined />
                        <Text strong>{group.dataSourceName}</Text>
                        <Tag color="red">{group.records.length} רשומות</Tag>
                      </Space>
                    ),
                    children: (
                      <Space direction="vertical" style={{ width: '100%' }}>
                        {group.records.map(renderInvalidRecord)}
                      </Space>
                    )
                  }))}
                  defaultActiveKey={Object.keys(groupedRecords).slice(0, 1)}
                  style={{ marginBottom: 24 }}
                />
              );
            })()}

            {totalCount > pageSize && (
              <Pagination
                current={currentPage}
                pageSize={pageSize}
                total={totalCount}
                onChange={(page) => setCurrentPage(page)}
                showSizeChanger={false}
                showTotal={(total) => `סה"כ ${total} רשומות לא תקינות`}
                style={{ marginTop: 24, textAlign: 'center' }}
              />
            )}
          </div>
        )}
      </Spin>

      {/* Edit Record Modal */}
      <EditRecordModal
        visible={editModalVisible}
        record={selectedRecord}
        onClose={() => setEditModalVisible(false)}
        onSuccess={handleEditSuccess}
      />
    </div>
  );
};

export default InvalidRecordsManagement;
