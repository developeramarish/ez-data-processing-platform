import React, { useState } from 'react';
import { Card, Button, Form, Input, Select, Tabs, Typography, Space, Table, Tag, Modal, Row, Col } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  EyeOutlined,
  FileTextOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;
const { TextArea } = Input;
const { Option } = Select;

interface SchemaField {
  id: string;
  name: string;
  type: string;
  required: boolean;
  description?: string;
  validation?: string;
}

interface SchemaInfo {
  id: string;
  name: string;
  dataSource: string;
  description: string;
  version: string;
  fields: SchemaField[];
  status: 'active' | 'inactive' | 'draft';
  updatedAt: string;
}

const SchemaManagement: React.FC = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState('visual-editor');
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [selectedSchema, setSelectedSchema] = useState<SchemaInfo | null>(null);

  // Mock data for schemas
  const schemas: SchemaInfo[] = [
    {
      id: '1',
      name: 'sales_transaction_v2.1',
      dataSource: 'נתוני עסקאות מכירות',
      description: 'Schema מעודכן לעסקאות מכירות כולל אמצעי תשלום חדשים',
      version: 'v2.1',
      fields: [
        { id: '1', name: 'transaction_id', type: 'string', required: true, validation: '^TXN-\\d{8}$' },
        { id: '2', name: 'amount', type: 'number', required: true, validation: 'min: 0, max: 999999.99' },
        { id: '3', name: 'payment_method', type: 'enum', required: true, validation: 'CREDIT_CARD, DEBIT_CARD, CASH, CRYPTO' },
      ],
      status: 'active',
      updatedAt: '2025-09-17 10:30',
    },
    {
      id: '2',
      name: 'customer_feedback_v1.2',
      dataSource: 'טפסי משוב לקוחות',
      description: 'Schema לטפסי משוב לקוחות',
      version: 'v1.2',
      fields: [
        { id: '1', name: 'feedback_id', type: 'string', required: true },
        { id: '2', name: 'satisfaction_score', type: 'number', required: true, validation: '1-10' },
        { id: '3', name: 'comment', type: 'string', required: false },
      ],
      status: 'active',
      updatedAt: '2025-09-16 14:20',
    },
  ];

  const columns = [
    {
      title: t('datasources.fields.name'),
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Text strong>{text}</Text>,
    },
    {
      title: 'מקור נתונים',
      dataIndex: 'dataSource',
      key: 'dataSource',
    },
    {
      title: 'גרסה',
      dataIndex: 'version',
      key: 'version',
      render: (version: string) => <Tag color="blue">{version}</Tag>,
    },
    {
      title: t('common.status'),
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const color = status === 'active' ? 'green' : status === 'inactive' ? 'red' : 'orange';
        return <Tag color={color}>{status === 'active' ? 'פעיל' : status === 'inactive' ? 'לא פעיל' : 'טיוטה'}</Tag>;
      },
    },
    {
      title: 'עודכן לאחרונה',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
    },
    {
      title: t('common.actions'),
      key: 'actions',
      render: (_: any, record: SchemaInfo) => (
        <Space>
          <Button 
            type="link" 
            size="small" 
            icon={<EyeOutlined />}
            onClick={() => handleViewSchema(record)}
          >
            צפה
          </Button>
          <Button 
            type="link" 
            size="small" 
            icon={<EditOutlined />}
            onClick={() => handleEditSchema(record)}
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

  const fieldColumns = [
    {
      title: 'שם שדה',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Text code strong>{text}</Text>,
    },
    {
      title: 'סוג',
      dataIndex: 'type',
      key: 'type',
      render: (type: string) => <Tag>{type}</Tag>,
    },
    {
      title: 'חובה',
      dataIndex: 'required',
      key: 'required',
      render: (required: boolean) => <Tag color={required ? 'red' : 'default'}>{required ? 'כן' : 'לא'}</Tag>,
    },
    {
      title: 'אימות',
      dataIndex: 'validation',
      key: 'validation',
      render: (validation?: string) => validation ? <Text type="secondary">{validation}</Text> : '-',
    },
  ];

  const handleViewSchema = (schema: SchemaInfo) => {
    setSelectedSchema(schema);
    setIsModalVisible(true);
  };

  const handleEditSchema = (schema: SchemaInfo) => {
    setSelectedSchema(schema);
    // Navigate to edit mode
  };

  const jsonSchemaExample = `{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "transaction_id": {
      "type": "string",
      "pattern": "^TXN-\\\\d{8}$",
      "description": "מזהה עסקה ייחודי"
    },
    "amount": {
      "type": "number",
      "minimum": 0,
      "maximum": 999999.99
    },
    "payment_method": {
      "type": "string",
      "enum": ["CREDIT_CARD", "DEBIT_CARD", "CASH", "CRYPTO"]
    },
    "timestamp": {
      "type": "string",
      "format": "date-time"
    }
  },
  "required": ["transaction_id", "amount", "payment_method"]
}`;

  return (
    <div className="schema-management-page">
      <div className="page-header">
        <Title level={2}>
          <FileTextOutlined /> ניהול Schema
        </Title>
        <Button type="primary" icon={<PlusOutlined />}>
          צור Schema חדש
        </Button>
      </div>

      <Row gutter={[24, 24]}>
        <Col span={12}>
          <Card title="הגדרת Schema">
            <Form layout="vertical">
              <Form.Item label="מקור נתונים">
                <Select placeholder="בחר מקור נתונים">
                  <Option value="sales">נתוני עסקאות מכירות</Option>
                  <Option value="feedback">טפסי משוב לקוחות</Option>
                  <Option value="inventory">עדכוני מלאי</Option>
                </Select>
              </Form.Item>
              <Form.Item label="שם Schema">
                <Input placeholder="sales_transaction_v2.1" />
              </Form.Item>
              <Form.Item label="תיאור">
                <TextArea rows={3} placeholder="Schema מעודכן לעסקאות מכירות כולל אמצעי תשלום חדשים" />
              </Form.Item>
            </Form>
          </Card>
        </Col>

        <Col span={12}>
          <Card title="אשף Schema">
            <div style={{ backgroundColor: '#f8f9fa', border: '2px dashed #dee2e6', padding: 20, borderRadius: 8 }}>
              <div style={{ marginBottom: 10, padding: 15, backgroundColor: 'white', borderRadius: 6, borderRight: '4px solid #3498db' }}>
                <Text strong>transaction_id</Text> (string, חובה)<br />
                <Text type="secondary" style={{ fontSize: '12px' }}>תבנית: ^TXN-\d{8}$</Text>
                <Button type="link" size="small" style={{ float: 'left' }}>ערוך</Button>
              </div>
              <div style={{ marginBottom: 10, padding: 15, backgroundColor: 'white', borderRadius: 6, borderRight: '4px solid #3498db' }}>
                <Text strong>amount</Text> (number, חובה)<br />
                <Text type="secondary" style={{ fontSize: '12px' }}>מינימום: 0, מקסימום: 999999.99</Text>
                <Button type="link" size="small" style={{ float: 'left' }}>ערוך</Button>
              </div>
              <div style={{ marginBottom: 15, padding: 15, backgroundColor: 'white', borderRadius: 6, borderRight: '4px solid #3498db' }}>
                <Text strong>payment_method</Text> (enum, חובה)<br />
                <Text type="secondary" style={{ fontSize: '12px' }}>ערכים: CREDIT_CARD, DEBIT_CARD, CASH, CRYPTO</Text>
                <Button type="link" size="small" style={{ float: 'left' }}>ערוך</Button>
              </div>
              <Button type="primary" icon={<PlusOutlined />}>
                הוסף שדה
              </Button>
            </div>
          </Card>
        </Col>
      </Row>

      <Card style={{ marginTop: 24 }}>
        <Tabs
          activeKey={activeTab}
          onChange={setActiveTab}
          items={[
            {
              key: 'visual-editor',
              label: 'עורך חזותי',
              children: (
                <div>
                  <Title level={4}>בונה Schema חזותי</Title>
                  <Text>גרור ושחרר שדות כדי לבנות את ה-Schema שלך באופן חזותי.</Text>
                  <div style={{ marginTop: 16, marginBottom: 24 }}>
                    <Space wrap>
                      <Button>שדה String</Button>
                      <Button>שדה Number</Button>
                      <Button>שדה Boolean</Button>
                      <Button>שדה Array</Button>
                      <Button>שדה Object</Button>
                    </Space>
                  </div>
                  {selectedSchema && (
                    <Table
                      columns={fieldColumns}
                      dataSource={selectedSchema.fields}
                      rowKey="id"
                      pagination={false}
                      size="small"
                    />
                  )}
                </div>
              ),
            },
            {
              key: 'json-editor',
              label: 'עורך JSON',
              children: (
                <div>
                  <Title level={4}>הגדרת JSON Schema</Title>
                  <div 
                    style={{
                      backgroundColor: '#2d3748',
                      color: '#68d391',
                      padding: 20,
                      borderRadius: 6,
                      fontFamily: 'monospace',
                      fontSize: '14px',
                      lineHeight: 1.4,
                      overflowX: 'auto',
                      direction: 'ltr',
                    }}
                  >
                    <pre>{jsonSchemaExample}</pre>
                  </div>
                </div>
              ),
            },
            {
              key: 'validation',
              label: 'אימות',
              children: (
                <div>
                  <Title level={4}>אימות Schema</Title>
                  <Form.Item label="נתוני בדיקה">
                    <TextArea rows={5} placeholder="הזן נתוני דוגמה לאימות כנגד Schema..." />
                  </Form.Item>
                  <Button type="primary">
                    אמת
                  </Button>
                </div>
              ),
            },
            {
              key: 'examples',
              label: 'דוגמאות',
              children: (
                <div>
                  <Title level={4}>דוגמאות נתוני דוגמה</Title>
                  <div 
                    style={{
                      backgroundColor: '#2d3748',
                      color: '#68d391',
                      padding: 20,
                      borderRadius: 6,
                      fontFamily: 'monospace',
                      fontSize: '14px',
                      direction: 'ltr',
                    }}
                  >
                    <pre>{`{
  "transaction_id": "TXN-20250917",
  "amount": 125.50,
  "payment_method": "CREDIT_CARD",
  "timestamp": "2025-09-17T10:30:00Z"
}`}</pre>
                  </div>
                </div>
              ),
            },
          ]}
        />
      </Card>

      {/* Schemas Table */}
      <Card title="Schemas קיימים" style={{ marginTop: 24 }}>
        <Table
          columns={columns}
          dataSource={schemas}
          rowKey="id"
          pagination={false}
        />
      </Card>

      <Modal
        title="פרטי Schema"
        open={isModalVisible}
        onOk={() => setIsModalVisible(false)}
        onCancel={() => setIsModalVisible(false)}
        width={800}
      >
        {selectedSchema && (
          <div>
            <Title level={4}>{selectedSchema.name}</Title>
            <Text>{selectedSchema.description}</Text>
            <Table
              columns={fieldColumns}
              dataSource={selectedSchema.fields}
              rowKey="id"
              pagination={false}
              size="small"
              style={{ marginTop: 16 }}
            />
          </div>
        )}
      </Modal>

      <style>{`
        .schema-management-page .page-header {
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

export default SchemaManagement;
