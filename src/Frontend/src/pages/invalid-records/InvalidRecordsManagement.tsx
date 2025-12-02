import React, { useState, useEffect } from 'react';
import { Card, Button, Tag, Space, Typography, Row, Col, Select, DatePicker, Collapse, Descriptions, message, Spin } from 'antd';
import {
  ExclamationCircleOutlined,
  ExportOutlined,
  DeleteOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { invalidRecordsApiClient, InvalidRecord } from '../../services/invalidrecords-api-client';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Panel } = Collapse;
const { Option } = Select;

const InvalidRecordsManagement: React.FC = () => {
  const [selectedDataSource, setSelectedDataSource] = useState<string>('all');
  const [selectedErrorType, setSelectedErrorType] = useState<string>('all');
  const [loading, setLoading] = useState(false);
  const [invalidRecords, setInvalidRecords] = useState<InvalidRecord[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  // Fetch invalid records from API
  const fetchRecords = async () => {
    setLoading(true);
    try {
      const result = await invalidRecordsApiClient.getList({
        page: currentPage,
        pageSize,
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
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

  useEffect(() => {
    fetchRecords();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDataSource, selectedErrorType, currentPage]);

  const handleReprocess = async (recordId: string) => {
    try {
      await invalidRecordsApiClient.reprocessRecord(recordId);
      message.success('Record reprocessed successfully');
      fetchRecords();
    } catch (error: any) {
      message.error(error.message || 'Failed to reprocess record');
    }
  };

  const handleDelete = async (recordId: string) => {
    try {
      await invalidRecordsApiClient.deleteRecord(recordId);
      message.success('Record deleted successfully');
      fetchRecords();
    } catch (error: any) {
      message.error(error.message || 'Failed to delete record');
    }
  };

  const handleExport = async () => {
    try {
      const blob = await invalidRecordsApiClient.exportToCsv({
        dataSourceId: selectedDataSource === 'all' ? undefined : selectedDataSource,
        errorType: selectedErrorType === 'all' ? undefined : selectedErrorType,
      });
      
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `invalid-records-${new Date().toISOString().split('T')[0]}.csv`;
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
            יצא CSV ({totalCount})
          </Button>
        </Space>
      </div>

      {/* Search and Filter Controls */}
      <Card style={{ marginBottom: 24 }}>
        <Row gutter={[16, 16]} align="middle">
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>מקור נתונים:</Text>
              <Select
                style={{ width: '100%' }}
                value={selectedDataSource}
                onChange={setSelectedDataSource}
              >
                <Option value="all">כל מקורות הנתונים</Option>
              </Select>
            </Space>
          </Col>
          <Col span={6}>
            <Space direction="vertical" size="small" style={{ width: '100%' }}>
              <Text>טווח תאריכים:</Text>
              <RangePicker style={{ width: '100%' }} />
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
                <Option value="schema">אימות Schema</Option>
                <Option value="format">שגיאת פורמט</Option>
                <Option value="required">שדה חובה חסר</Option>
                <Option value="range">שגיאת טווח</Option>
              </Select>
            </Space>
          </Col>
          <Col span={6}>
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
