// DestinationEditorModal.tsx - Multi-Destination Output Editor
// Task-26: Enhanced Output Tab Component
// Version: 1.0
// Date: December 1, 2025

import React, { useState, useEffect } from 'react';
import {
  Modal,
  Form,
  Input,
  Radio,
  Switch,
  Space,
  Card,
  Button,
  Table,
  Select,
  Typography,
  message,
  Divider,
  Alert,
  Tag
} from 'antd';
import {
  PlusOutlined,
  DeleteOutlined,
  CloudServerOutlined,
  FolderOutlined,
  ApiOutlined,
  GlobalOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  SyncOutlined
} from '@ant-design/icons';
import type {
  OutputDestination,
  KafkaOutputConfig,
  FolderOutputConfig
} from '../shared/types';
import {
  testKafkaConnection,
  testFolderConnection,
  testSftpConnection
} from '../../../api/connection-test-api-client';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

// Simple UUID generator for browser compatibility
const generateUUID = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
};

interface DestinationEditorModalProps {
  visible: boolean;
  destination: OutputDestination | null;
  onSave: (destination: OutputDestination) => void;
  onCancel: () => void;
}

export const DestinationEditorModal: React.FC<DestinationEditorModalProps> = ({
  visible,
  destination,
  onSave,
  onCancel
}) => {
  const [form] = Form.useForm();
  const [destinationType, setDestinationType] = useState<string>(destination?.type || 'kafka');
  const [kafkaHeaders, setKafkaHeaders] = useState<Array<{ key: string; value: string }>>(
    destination?.kafkaConfig?.headers
      ? Object.entries(destination.kafkaConfig.headers).map(([key, value]) => ({ key, value }))
      : []
  );
  const [testingConnection, setTestingConnection] = useState<boolean>(false);
  const [connectionTestResult, setConnectionTestResult] = useState<'success' | 'failed' | null>(null);

  useEffect(() => {
    if (visible && destination) {
      form.setFieldsValue({
        name: destination.name,
        description: destination.description,
        type: destination.type,
        enabled: destination.enabled,
        outputFormat: destination.outputFormat,
        includeInvalidRecords: destination.includeInvalidRecords,
        // Kafka config
        kafkaBrokerServer: destination.kafkaConfig?.brokerServer,
        kafkaTopic: destination.kafkaConfig?.topic,
        kafkaMessageKey: destination.kafkaConfig?.messageKey,
        kafkaSecurityProtocol: destination.kafkaConfig?.securityProtocol,
        kafkaSaslMechanism: destination.kafkaConfig?.saslMechanism,
        kafkaUsername: destination.kafkaConfig?.username,
        kafkaPassword: destination.kafkaConfig?.password,
        // Folder config
        folderPath: destination.folderConfig?.path,
        folderFileNamePattern: destination.folderConfig?.fileNamePattern,
        folderCreateSubfolders: destination.folderConfig?.createSubfolders
      });
      setDestinationType(destination.type);

      if (destination.kafkaConfig?.headers) {
        setKafkaHeaders(
          Object.entries(destination.kafkaConfig.headers).map(([key, value]) => ({ key, value }))
        );
      }
    } else if (visible && !destination) {
      // New destination
      form.resetFields();
      form.setFieldsValue({
        type: 'kafka',
        enabled: true,
        outputFormat: null,
        includeInvalidRecords: null
      });
      setDestinationType('kafka');
      setKafkaHeaders([]);
    }
  }, [visible, destination, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      // Build destination object
      const updatedDestination: OutputDestination = {
        id: destination?.id || generateUUID(),
        name: values.name,
        description: values.description,
        type: values.type,
        enabled: values.enabled,
        outputFormat: values.outputFormat,
        includeInvalidRecords: values.includeInvalidRecords
      };

      // Add type-specific configuration
      if (values.type === 'kafka') {
        updatedDestination.kafkaConfig = {
          brokerServer: values.kafkaBrokerServer,
          topic: values.kafkaTopic,
          messageKey: values.kafkaMessageKey,
          securityProtocol: values.kafkaSecurityProtocol,
          saslMechanism: values.kafkaSaslMechanism,
          username: values.kafkaUsername,
          password: values.kafkaPassword,
          headers: kafkaHeaders.reduce((acc, { key, value }) => {
            if (key && value) {
              acc[key] = value;
            }
            return acc;
          }, {} as Record<string, string>)
        };
      } else if (values.type === 'folder') {
        updatedDestination.folderConfig = {
          path: values.folderPath,
          fileNamePattern: values.folderFileNamePattern,
          createSubfolders: values.folderCreateSubfolders
        };
      }

      onSave(updatedDestination);
      message.success('יעד הפלט נשמר בהצלחה');
    } catch (error) {
      console.error('Validation failed:', error);
      message.error('אנא מלא את כל השדות הנדרשים');
    }
  };

  const handleTestConnection = async () => {
    setTestingConnection(true);
    setConnectionTestResult(null);

    try {
      const values = await form.getFieldsValue();

      if (values.type === 'kafka') {
        // Validate Kafka connection fields
        await form.validateFields(['kafkaTopic']);

        // Call real Kafka connection testing API
        const result = await testKafkaConnection({
          brokerServer: values.kafkaBrokerServer || 'localhost:9092',
          topic: values.kafkaTopic || '',
          username: values.kafkaUsername,
          password: values.kafkaPassword,
          timeoutSeconds: 30
        });

        if (result.success) {
          setConnectionTestResult('success');
          const latency = result.details?.latencyMs ? ` (${result.details.latencyMs}ms)` : '';
          message.success(`חיבור ל-Kafka הצליח${latency}`);
        } else {
          setConnectionTestResult('failed');
          message.error(`חיבור ל-Kafka נכשל: ${result.errorDetails || result.message}`);
        }
      } else if (values.type === 'folder') {
        // Validate folder path
        await form.validateFields(['folderPath']);

        // Call real Folder validation API
        const result = await testFolderConnection({
          path: values.folderPath || '',
          checkWritePermissions: true,
          checkDiskSpace: true
        });

        if (result.success) {
          setConnectionTestResult('success');
          const diskSpace = result.details?.diskSpaceGB ? ` (${result.details.diskSpaceGB}GB available)` : '';
          message.success(`נתיב התיקייה תקין${diskSpace}`);
        } else {
          setConnectionTestResult('failed');
          message.error(`אימות נתיב נכשל: ${result.errorDetails || result.message}`);
        }
      } else if (values.type === 'sftp') {
        // Validate SFTP connection fields
        await form.validateFields(['sftpHost', 'sftpPort', 'sftpUsername', 'sftpRemotePath']);

        // Call real SFTP connection testing API
        const result = await testSftpConnection({
          host: values.sftpHost || '',
          port: values.sftpPort || 22,
          username: values.sftpUsername || '',
          password: values.sftpPassword,
          sshKey: values.sftpSshKey,
          remotePath: values.sftpRemotePath || '/',
          timeoutSeconds: 30
        });

        if (result.success) {
          setConnectionTestResult('success');
          message.success('חיבור SFTP הצליח');
        } else {
          setConnectionTestResult('failed');
          message.error(`חיבור SFTP נכשל: ${result.errorDetails || result.message}`);
        }
      }
    } catch (err: any) {
      setConnectionTestResult('failed');
      message.error(err.message || 'בדיקת החיבור נכשלה');
    } finally {
      setTestingConnection(false);
    }
  };

  const handleAddHeader = () => {
    setKafkaHeaders([...kafkaHeaders, { key: '', value: '' }]);
  };

  const handleRemoveHeader = (index: number) => {
    setKafkaHeaders(kafkaHeaders.filter((_, i) => i !== index));
  };

  const handleHeaderChange = (index: number, field: 'key' | 'value', value: string) => {
    const updated = [...kafkaHeaders];
    updated[index][field] = value;
    setKafkaHeaders(updated);
  };

  const headerColumns = [
    {
      title: 'מפתח',
      dataIndex: 'key',
      key: 'key',
      render: (_: any, record: any, index: number) => (
        <Input
          value={record.key}
          onChange={(e) => handleHeaderChange(index, 'key', e.target.value)}
          placeholder="source"
        />
      )
    },
    {
      title: 'ערך',
      dataIndex: 'value',
      key: 'value',
      render: (_: any, record: any, index: number) => (
        <Input
          value={record.value}
          onChange={(e) => handleHeaderChange(index, 'value', e.target.value)}
          placeholder="banking-system"
        />
      )
    },
    {
      title: '',
      key: 'actions',
      width: 60,
      render: (_: any, record: any, index: number) => (
        <Button
          type="link"
          danger
          icon={<DeleteOutlined />}
          onClick={() => handleRemoveHeader(index)}
        />
      )
    }
  ];

  return (
    <Modal
      title={destination ? 'עריכת יעד פלט' : 'הוספת יעד פלט חדש'}
      open={visible}
      onCancel={onCancel}
      onOk={handleSubmit}
      width={800}
      okText="שמור"
      cancelText="ביטול"
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
      >
        <Alert
          message="הגדרת יעד פלט"
          description="הגדר יעד פלט חדש לשליחת נתונים מעובדים. ניתן לבחור בין Kafka לשידור זמן-אמת או תיקייה לאחסון קבצים."
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
        />

        {/* Basic Settings */}
        <Form.Item
          name="name"
          label="שם יעד"
          rules={[{ required: true, message: 'נא להזין שם יעד' }]}
          tooltip="שם מזהה לייעד הפלט, לדוגמה: 'ניתוח זמן-אמת' או 'ארכיון יומי'"
        >
          <Input placeholder="Real-Time Analytics" />
        </Form.Item>

        <Form.Item
          name="description"
          label="תיאור / הערות"
          tooltip="תיאור אופציונלי המסביר את מטרת יעד הפלט ותפקידו במערכת"
        >
          <TextArea
            rows={2}
            placeholder="תיאור קצר של יעד הפלט ותפקידו"
            maxLength={200}
            showCount
          />
        </Form.Item>

        <Form.Item
          name="type"
          label="סוג יעד"
          rules={[{ required: true, message: 'נא לבחור סוג יעד' }]}
          tooltip="בחר את סוג היעד: Kafka לעיבוד זמן-אמת, Folder לאחסון מקומי"
        >
          <Radio.Group onChange={(e) => setDestinationType(e.target.value)}>
            <Space direction="horizontal" size="large">
              <Radio value="kafka">
                <Space>
                  <CloudServerOutlined style={{ color: '#1890ff' }} />
                  <span>Kafka</span>
                </Space>
              </Radio>
              <Radio value="folder">
                <Space>
                  <FolderOutlined style={{ color: '#faad14' }} />
                  <span>Folder</span>
                </Space>
              </Radio>
              <Radio value="sftp" disabled>
                <Space>
                  <ApiOutlined style={{ color: '#999' }} />
                  <span>SFTP (עתידי)</span>
                </Space>
              </Radio>
              <Radio value="http" disabled>
                <Space>
                  <GlobalOutlined style={{ color: '#999' }} />
                  <span>HTTP (עתידי)</span>
                </Space>
              </Radio>
            </Space>
          </Radio.Group>
        </Form.Item>

        <Form.Item name="enabled" valuePropName="checked">
          <Space>
            <Switch defaultChecked />
            <Text>מופעל</Text>
          </Space>
        </Form.Item>

        <Divider />

        {/* Kafka Configuration */}
        {destinationType === 'kafka' && (
          <Card
            title={
              <Space>
                <CloudServerOutlined style={{ color: '#1890ff' }} />
                <span>תצורת Kafka</span>
              </Space>
            }
            style={{ marginBottom: 16 }}
          >
            <Form.Item
              name="kafkaBrokerServer"
              label="שרת Kafka (Broker)"
              tooltip="כתובת שרת Kafka. אם לא מוגדר, המערכת תשתמש בשרת ברירת המחדל מההגדרות הגלובליות"
              extra={
                <Text type="secondary" style={{ fontSize: 12 }}>
                  אופציונלי - אם לא מוגדר, ישתמש בשרת ברירת המחדל
                </Text>
              }
            >
              <Input placeholder="localhost:9092" dir="ltr" style={{ direction: 'ltr', textAlign: 'left' }} />
            </Form.Item>

            <Form.Item
              name="kafkaTopic"
              label="נושא (Topic)"
              rules={[{ required: true, message: 'נא להזין נושא Kafka' }]}
              tooltip="שם ה-Topic ב-Kafka שאליו יישלחו ההודעות. חובה להגדיר"
            >
              <Input placeholder="banking-transactions-validated" dir="ltr" style={{ direction: 'ltr', textAlign: 'left' }} />
            </Form.Item>

            <Form.Item
              name="kafkaMessageKey"
              label="מפתח הודעה (Message Key)"
              tooltip="מפתח ייחודי להודעה. תומך בתבניות כמו {filename}, {datasource}, {timestamp}"
              extra={
                <Text type="secondary" style={{ fontSize: 12 }}>
                  Placeholders: {'{filename}'}, {'{datasource}'}, {'{timestamp}'}, {'{date}'}
                </Text>
              }
            >
              <Input placeholder="{filename}_{timestamp}" />
            </Form.Item>

            <Form.Item label="כותרות (Headers)">
              <Table
                dataSource={kafkaHeaders}
                columns={headerColumns}
                pagination={false}
                size="small"
                locale={{ emptyText: 'אין כותרות' }}
                rowKey={(_, index) => `header-${index}`}
              />
              <Button
                type="dashed"
                icon={<PlusOutlined />}
                onClick={handleAddHeader}
                style={{ marginTop: 8 }}
              >
                הוסף כותרת
              </Button>
            </Form.Item>

            <Divider orientation="left">אימות (Authentication)</Divider>

            <Form.Item
              name="kafkaSecurityProtocol"
              label="פרוטוקול אבטחה"
              tooltip="פרוטוקול האבטחה לחיבור ל-Kafka. PLAINTEXT ללא הצפנה, SASL_SSL להצפנה ואימות"
            >
              <Select placeholder="PLAINTEXT" allowClear>
                <Select.Option value="PLAINTEXT">PLAINTEXT</Select.Option>
                <Select.Option value="SASL_SSL">SASL_SSL</Select.Option>
                <Select.Option value="SASL_PLAINTEXT">SASL_PLAINTEXT</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item
              name="kafkaSaslMechanism"
              label="מנגנון SASL"
              tooltip="מנגנון האימות SASL. נדרש רק כאשר משתמשים ב-SASL_SSL או SASL_PLAINTEXT"
            >
              <Select placeholder="PLAIN" allowClear>
                <Select.Option value="PLAIN">PLAIN</Select.Option>
                <Select.Option value="SCRAM-SHA-256">SCRAM-SHA-256</Select.Option>
                <Select.Option value="SCRAM-SHA-512">SCRAM-SHA-512</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item
              name="kafkaUsername"
              label="שם משתמש"
              tooltip="שם משתמש לאימות SASL. נדרש כאשר משתמשים באימות"
            >
              <Input placeholder="kafka-user" />
            </Form.Item>

            <Form.Item
              name="kafkaPassword"
              label="סיסמה"
              tooltip="סיסמה לאימות SASL. נדרש כאשר משתמשים באימות"
            >
              <Input.Password placeholder="••••••••" />
            </Form.Item>

            <Divider />

            {/* Test Connection Button */}
            <Space direction="vertical" style={{ width: '100%' }}>
              <Button
                type="default"
                icon={testingConnection ? <SyncOutlined spin /> : <CloudServerOutlined />}
                onClick={handleTestConnection}
                loading={testingConnection}
                style={{ width: '100%' }}
              >
                בדוק חיבור ל-Kafka
              </Button>
              {connectionTestResult === 'success' && (
                <Tag icon={<CheckCircleOutlined />} color="success">
                  החיבור ל-Kafka הצליח
                </Tag>
              )}
              {connectionTestResult === 'failed' && (
                <Tag icon={<CloseCircleOutlined />} color="error">
                  החיבור ל-Kafka נכשל
                </Tag>
              )}
            </Space>
          </Card>
        )}

        {/* Folder Configuration */}
        {destinationType === 'folder' && (
          <Card
            title={
              <Space>
                <FolderOutlined style={{ color: '#faad14' }} />
                <span>תצורת Folder</span>
              </Space>
            }
            style={{ marginBottom: 16 }}
          >
            <Form.Item
              name="folderPath"
              label="נתיב תיקייה"
              rules={[{ required: true, message: 'נא להזין נתיב תיקייה' }]}
              tooltip="נתיב מלא לתיקייה שבה יישמרו הקבצים. יכול להיות תיקייה מקומית או ברשת"
            >
              <Input placeholder="C:\DataProcessing\Archive\Banking" dir="ltr" style={{ direction: 'ltr', textAlign: 'left' }} />
            </Form.Item>

            <Form.Item
              name="folderFileNamePattern"
              label="תבנית שם קובץ"
              tooltip="תבנית לשם הקובץ. תומך בתבניות כמו {filename}, {datasource}, {timestamp}, {date}"
              extra={
                <Text type="secondary" style={{ fontSize: 12 }}>
                  Placeholders: {'{filename}'}, {'{datasource}'}, {'{timestamp}'}, {'{date}'}
                </Text>
              }
            >
              <Input placeholder="{datasource}_{date}_{timestamp}.json" dir="ltr" style={{ direction: 'ltr', textAlign: 'left' }} />
            </Form.Item>

            <Form.Item name="folderCreateSubfolders" valuePropName="checked">
              <Space>
                <Switch />
                <Text>צור תיקיות משנה לפי תאריך</Text>
              </Space>
            </Form.Item>

            <Divider />

            {/* Test Connection Button */}
            <Space direction="vertical" style={{ width: '100%' }}>
              <Button
                type="default"
                icon={testingConnection ? <SyncOutlined spin /> : <FolderOutlined />}
                onClick={handleTestConnection}
                loading={testingConnection}
                style={{ width: '100%' }}
              >
                בדוק נתיב תיקייה
              </Button>
              {connectionTestResult === 'success' && (
                <Tag icon={<CheckCircleOutlined />} color="success">
                  נתיב התיקייה תקין
                </Tag>
              )}
              {connectionTestResult === 'failed' && (
                <Tag icon={<CloseCircleOutlined />} color="error">
                  נתיב התיקייה לא תקין
                </Tag>
              )}
            </Space>
          </Card>
        )}

        {/* Advanced Settings */}
        <Card title="הגדרות מתקדמות" style={{ marginBottom: 16 }}>
          <Alert
            message="הגדרות מתקדמות"
            description="הגדרות אלו דורסות את הגדרות ברירת המחדל הגלובליות רק עבור יעד פלט זה."
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <Form.Item
            name="outputFormat"
            label="דריסת פורמט פלט"
            tooltip="פורמט הפלט עבור יעד זה בלבד. אם לא מוגדר, ישתמש בפורמט ברירת המחדל הגלובלי"
            extra={
              <Text type="secondary" style={{ fontSize: 12 }}>
                משנה את ברירת המחדל הגלובלית רק עבור יעד זה
              </Text>
            }
          >
            <Select placeholder="ברירת מחדל" allowClear style={{ width: 300 }}>
              <Select.Option value={null}>ברירת מחדל</Select.Option>
              <Select.Option value="original">פורמט מקורי</Select.Option>
              <Select.Option value="json">JSON</Select.Option>
              <Select.Option value="csv">CSV</Select.Option>
              <Select.Option value="xml">XML</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="includeInvalidRecords"
            valuePropName="checked"
            tooltip="האם לכלול רשומות שלא עברו אימות בפלט של יעד זה"
          >
            <Space>
              <Switch />
              <Text>כולל רשומות שגויות (דריסה)</Text>
            </Space>
          </Form.Item>
        </Card>
      </Form>
    </Modal>
  );
};
