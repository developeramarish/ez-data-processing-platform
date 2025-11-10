import React, { useState } from 'react';
import { Card, Button, Form, Input, Select, Table, Tag, Space, Typography, Row, Col, Switch } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  BarChartOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;
const { Option } = Select;

interface MetricInfo {
  id: string;
  name: string;
  type: 'Counter' | 'Gauge' | 'Histogram' | 'Summary';
  description: string;
  labels: string[];
  retention: string;
  status: 'active' | 'inactive';
  fieldPath?: string;
}

const MetricsConfiguration: React.FC = () => {
  const { t } = useTranslation();
  const [form] = Form.useForm();

  // Mock metrics data
  const metrics: MetricInfo[] = [
    {
      id: '1',
      name: 'files_processed_count',
      type: 'Counter',
      description: 'ספירת קבצים שעובדו לכל מחזור polling',
      labels: ['data_source', 'status'],
      retention: '30 יום',
      status: 'active',
    },
    {
      id: '2',
      name: 'processing_duration_seconds',
      type: 'Histogram',
      description: 'זמן שנדרש לעיבוד כל קובץ',
      labels: ['data_source'],
      retention: '7 ימים',
      status: 'active',
    },
    {
      id: '3',
      name: 'transaction_amount_total',
      type: 'Counter',
      description: 'סכום כולל של עסקאות מעובדות',
      labels: ['data_source', 'payment_method'],
      retention: '30 יום',
      status: 'active',
      fieldPath: '$.amount',
    },
    {
      id: '4',
      name: 'validation_error_rate',
      type: 'Gauge',
      description: 'שיעור שגיאות אימות',
      labels: ['data_source', 'error_type'],
      retention: '7 ימים',
      status: 'active',
    },
  ];

  const columns = [
    {
      title: 'שם מדד',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Text code strong>{text}</Text>,
    },
    {
      title: 'סוג',
      dataIndex: 'type',
      key: 'type',
      render: (type: string) => <Tag color="blue">{type}</Tag>,
    },
    {
      title: 'תוויות',
      dataIndex: 'labels',
      key: 'labels',
      render: (labels: string[]) => (
        <Space>
          {labels.map(label => <Tag key={label}>{label}</Tag>)}
        </Space>
      ),
    },
    {
      title: 'שמירה',
      dataIndex: 'retention',
      key: 'retention',
    },
    {
      title: t('common.status'),
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => (
        <Tag color={status === 'active' ? 'green' : 'red'}>
          {status === 'active' ? 'פעיל' : 'לא פעיל'}
        </Tag>
      ),
    },
    {
      title: t('common.actions'),
      key: 'actions',
      render: (_: any, record: MetricInfo) => (
        <Space>
          <Button 
            type="link" 
            size="small" 
            icon={<EditOutlined />}
          >
            ערוך
          </Button>
          <Button 
            type="link" 
            size="small" 
            danger 
            icon={<DeleteOutlined />}
          >
            מחק
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div className="metrics-config-page">
      <div className="page-header">
        <Title level={2}>
          <BarChartOutlined /> הגדרות מדדים
        </Title>
        <Button type="primary" icon={<PlusOutlined />}>
          הוסף הגדרת מדד
        </Button>
      </div>

      <Row gutter={[24, 24]}>
        {/* General Metrics Card */}
        <Col span={12}>
          <Card title="מדדים כלליים" className="metrics-card">
            <Space direction="vertical" style={{ width: '100%' }}>
              <div className="metric-item">
                <div>
                  <Text strong>files_processed_count</Text>
                  <br />
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    ספירת קבצים שעובדו לכל מחזור polling
                  </Text>
                </div>
                <Tag color="green">פעיל</Tag>
              </div>
              <div className="metric-item">
                <div>
                  <Text strong>processing_duration_seconds</Text>
                  <br />
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    זמן שנדרש לעיבוד כל קובץ
                  </Text>
                </div>
                <Tag color="green">פעיל</Tag>
              </div>
              <div className="metric-item">
                <div>
                  <Text strong>file_size_bytes</Text>
                  <br />
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    גודל קבצים מעובדים בבתים
                  </Text>
                </div>
                <Tag color="green">פעיל</Tag>
              </div>
            </Space>
          </Card>
        </Col>

        {/* Custom Metrics Form */}
        <Col span={12}>
          <Card title="מדדים מותאמים אישית" className="metrics-card">
            <Form form={form} layout="vertical">
              <Form.Item
                label="שם מדד"
                name="metricName"
                rules={[{ required: true, message: 'נדרש שם מדד' }]}
              >
                <Input placeholder="transaction_amount_total" />
              </Form.Item>
              
              <Form.Item
                label="סוג מדד"
                name="metricType"
                rules={[{ required: true, message: 'נדרש סוג מדד' }]}
              >
                <Select placeholder="בחר סוג מדד">
                  <Option value="Counter">Counter</Option>
                  <Option value="Gauge">Gauge</Option>
                  <Option value="Histogram">Histogram</Option>
                  <Option value="Summary">Summary</Option>
                </Select>
              </Form.Item>
              
              <Form.Item
                label="תיאור"
                name="description"
                rules={[{ required: true, message: 'נדרש תיאור' }]}
              >
                <Input placeholder="סכום כולל של עסקאות מעובדות" />
              </Form.Item>
              
              <Form.Item
                label="נתיב שדה"
                name="fieldPath"
              >
                <Input placeholder="$.amount" />
              </Form.Item>
              
              <Form.Item>
                <Button type="primary" style={{ backgroundColor: '#27ae60' }}>
                  הוסף מדד
                </Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>
      </Row>

      {/* Prometheus Configuration Table */}
      <Card title="הגדרת Prometheus" style={{ marginTop: 24 }}>
        <Table
          columns={columns}
          dataSource={metrics}
          rowKey="id"
          pagination={false}
          className="metrics-table"
        />
      </Card>

    </div>
  );
};

export default MetricsConfiguration;
