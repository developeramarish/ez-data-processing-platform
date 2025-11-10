import React, { useState } from 'react';
import { Card, Button, Tag, Space, Typography, Row, Col, Select, DatePicker, Collapse, Descriptions } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  ExclamationCircleOutlined,
  ExportOutlined,
  DeleteOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { Panel } = Collapse;
const { Option } = Select;

interface InvalidRecord {
  id: string;
  recordId: string;
  dataSource: string;
  fileName: string;
  timestamp: string;
  errors: ValidationError[];
  originalData: any;
}

interface ValidationError {
  field: string;
  message: string;
  errorType: 'schema' | 'format' | 'required' | 'range';
}

const InvalidRecordsManagement: React.FC = () => {
  const { t } = useTranslation();
  const [selectedDataSource, setSelectedDataSource] = useState<string>('all');
  const [selectedErrorType, setSelectedErrorType] = useState<string>('all');

  // Mock invalid records data
  const invalidRecords: InvalidRecord[] = [
    {
      id: '1',
      recordId: 'TXN-20250917001',
      dataSource: 'נתוני עסקאות מכירות',
      fileName: 'transactions_2025_09_17_batch_1.json',
      timestamp: '2025-09-17 10:15:23',
      errors: [
        { field: 'amount', message: 'השדה amount הוא חובה אבל חסר', errorType: 'required' },
        { field: 'payment_method', message: 'השדה payment_method מכיל ערך לא תקין PAYPAL (מותר: CREDIT_CARD, DEBIT_CARD, CASH, CRYPTO)', errorType: 'schema' },
        { field: 'transaction_id', message: 'פורמט השדה transaction_id לא תקין: תבנית צפויה ^TXN-\\d{8}$', errorType: 'format' },
      ],
      originalData: {
        transaction_id: 'TXN-ABC123',
        payment_method: 'PAYPAL',
        timestamp: '2025-09-17T10:15:23Z',
        customer_id: 'CUST-456'
      }
    },
    {
      id: '2',
      recordId: 'TXN-20250917002',
      dataSource: 'נתוני עסקאות מכירות',
      fileName: 'transactions_2025_09_17_batch_1.json',
      timestamp: '2025-09-17 10:15:24',
      errors: [
        { field: 'amount', message: 'ערך השדה amount -150.00 נמוך מהערך המינימלי המותר 0', errorType: 'range' },
      ],
      originalData: {
        transaction_id: 'TXN-20250917',
        amount: -150.00,
        payment_method: 'CREDIT_CARD',
        timestamp: '2025-09-17T10:15:24Z'
      }
    },
  ];

  const renderErrorType = (errorType: string) => {
    const colors = {
      schema: 'red',
      format: 'orange',
      required: 'volcano',
      range: 'magenta',
    };
    const labels = {
      schema: 'אימות Schema',
      format: 'שגיאת פורמט', 
      required: 'שדה חובה חסר',
      range: 'שגיאת טווח',
    };
    return <Tag color={colors[errorType as keyof typeof colors]}>{labels[errorType as keyof typeof labels]}</Tag>;
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
          <Title level={5}>רשומת עסקה #{record.recordId}</Title>
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="מקור נתונים">{record.dataSource}</Descriptions.Item>
            <Descriptions.Item label="קובץ">{record.fileName}</Descriptions.Item>
            <Descriptions.Item label="חותמת זמן">{record.timestamp}</Descriptions.Item>
          </Descriptions>
        </Col>
        <Col>
          <Space>
            <Button size="small" icon={<ReloadOutlined />}>
              עבד מחדש
            </Button>
            <Button size="small" danger icon={<DeleteOutlined />}>
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
        <Title level={2}>
          <ExclamationCircleOutlined /> ניהול רשומות לא תקינות
        </Title>
        <Space>
          <Button icon={<ExportOutlined />}>
            יצא CSV
          </Button>
          <Button danger icon={<DeleteOutlined />}>
            מחיקה בכמויות
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
                <Option value="sales">נתוני עסקאות מכירות</Option>
                <Option value="feedback">טפסי משוב לקוחות</Option>
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
            <Button type="primary" icon={<SearchOutlined />} style={{ marginTop: 24 }}>
              חפש
            </Button>
          </Col>
        </Row>
      </Card>

      {/* Invalid Records List */}
      <div>
        {invalidRecords.map(renderInvalidRecord)}
      </div>

      <style>{`
        .invalid-records-page .page-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 24px;
          padding-bottom: 16px;
          border-bottom: 2px solid #e9ecef;
        }
      `}</style>
    </div>
  );
};

export default InvalidRecordsManagement;
