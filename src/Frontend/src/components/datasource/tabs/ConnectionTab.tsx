import React from 'react';
import { Form, Input, Select, InputNumber, Button, Space, Alert, Row, Col, Tag } from 'antd';
import { FormInstance } from 'antd/es/form';
import { ApiOutlined, FileOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { KAFKA_SECURITY_PROTOCOLS, KAFKA_OFFSET_RESET } from '../shared/constants';

const { Option } = Select;
const { TextArea } = Input;

interface ConnectionTabProps {
  form: FormInstance;
  t: (key: string) => string;
  connectionType: string;
  testingConnection: boolean;
  connectionTestResult: 'success' | 'failed' | null;
  onTestConnection: () => void;
}

export const ConnectionTab: React.FC<ConnectionTabProps> = ({
  form,
  t,
  connectionType,
  testingConnection,
  connectionTestResult,
  onTestConnection
}) => {
  return (
    <>
      <Alert
        message="הגדרות חיבור למקור הנתונים"
        description="הגדר כיצד המערכת תתחבר למקור הנתונים ותאסוף קבצים"
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Form.Item
        name="connectionType"
        label="סוג החיבור"
        rules={[{ required: true, message: t('errors.required') }]}
      >
        <Select placeholder="בחר סוג חיבור">
          <Option value="Local">
            <Space>
              <FileOutlined />
              Local - תיקייה מקומית
            </Space>
          </Option>
          <Option value="SFTP">
            <Space>
              <ApiOutlined />
              SFTP - Secure FTP
            </Space>
          </Option>
          <Option value="FTP">
            <Space>
              <ApiOutlined />
              FTP - File Transfer Protocol
            </Space>
          </Option>
          <Option value="HTTP">
            <Space>
              <ApiOutlined />
              HTTP/HTTPS - Web API
            </Space>
          </Option>
          <Option value="Kafka">
            <Space>
              <ApiOutlined />
              Kafka - Message Queue
            </Space>
          </Option>
        </Select>
      </Form.Item>

      {/* Connection fields based on type */}
      {(connectionType === 'SFTP' || connectionType === 'FTP') && (
        <>
          <Row gutter={16}>
            <Col xs={24} lg={16}>
              <Form.Item
                name="connectionHost"
                label="שרת (Host)"
                rules={[{ required: true, message: t('errors.required') }]}
              >
                <Input className="ltr-field" placeholder="לדוגמה: ftp.example.com" />
              </Form.Item>
            </Col>
            <Col xs={24} lg={8}>
              <Form.Item
                name="connectionPort"
                label="פורט"
                initialValue={connectionType === 'SFTP' ? 22 : 21}
              >
                <InputNumber 
                  min={1} 
                  max={65535} 
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="connectionUsername"
                label="שם משתמש"
                rules={[{ required: true, message: t('errors.required') }]}
              >
                <Input placeholder="שם משתמש לחיבור" />
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="connectionPassword"
                label="סיסמה"
                rules={[{ required: true, message: t('errors.required') }]}
              >
                <Input.Password placeholder="סיסמה לחיבור" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="connectionPath"
            label="נתיב בשרת"
            rules={[{ required: true, message: t('errors.required') }]}
          >
            <Input className="ltr-field" placeholder="/path/to/files/" />
          </Form.Item>

          <Form.Item
            name="filePattern"
            label="תבנית קובץ (File Pattern)"
            initialValue="*.*"
            rules={[
              { required: true, message: t('errors.required') },
              {
                pattern: /^(\*\.[\w]+|\*\.\*|[\w-]+_\*\.[\w]+|[\w-]+\.[\w]+)$/,
                message: 'תבנית לא תקינה. דוגמאות: *.csv, *.*, data_*.xml'
              }
            ]}
            tooltip="תבנית לסינון קבצים בתיקייה. דוגמאות: *.csv (כל קבצי CSV), *.* (כל הקבצים), data_*.xml"
          >
            <Input
              className="ltr-field"
              placeholder="*.csv, *.json, data_*.xml, *.*"
            />
          </Form.Item>
        </>
      )}

      {connectionType === 'HTTP' && (
        <Form.Item
          name="connectionUrl"
          label="כתובת URL"
          rules={[
            { required: true, message: t('errors.required') },
            { type: 'url', message: t('errors.invalidUrl') }
          ]}
        >
          <Input className="ltr-field" placeholder="https://api.example.com/data/files" />
        </Form.Item>
      )}

      {connectionType === 'Local' && (
        <>
          <Form.Item
            name="connectionPath"
            label="נתיב מקומי"
            rules={[{ required: true, message: t('errors.required') }]}
            tooltip="נתיב מלא לתיקייה במחשב או ברשת"
          >
            <Input className="ltr-field" placeholder="C:\Data\Files או \\server\share\files" />
          </Form.Item>

          <Form.Item
            name="filePattern"
            label="תבנית קובץ (File Pattern)"
            initialValue="*.*"
            rules={[
              { required: true, message: t('errors.required') },
              {
                pattern: /^(\*\.[\w]+|\*\.\*|[\w-]+_\*\.[\w]+|[\w-]+\.[\w]+)$/,
                message: 'תבנית לא תקינה. דוגמאות: *.csv, *.*, data_*.xml'
              }
            ]}
            tooltip="תבנית לסינון קבצים בתיקייה. דוגמאות: *.csv (כל קבצי CSV), *.* (כל הקבצים), data_*.xml"
          >
            <Input
              className="ltr-field"
              placeholder="*.csv, *.json, data_*.xml, *.*"
            />
          </Form.Item>
        </>
      )}

      {/* Kafka specific fields */}
      {connectionType === 'Kafka' && (
        <>
          <Form.Item
            name="kafkaBrokers"
            label="Kafka Brokers"
            rules={[{ required: true, message: t('errors.required') }]}
            tooltip="רשימת Kafka brokers (מופרדים בפסיקים)"
          >
            <TextArea
              className="ltr-field"
              rows={2}
              placeholder="localhost:9092,broker2:9092,broker3:9092"
            />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaTopic"
                label="Topic Name"
                rules={[{ required: true, message: t('errors.required') }]}
                tooltip="שם ה-Topic ל-Consume ממנו"
              >
                <Input className="ltr-field" placeholder="data-events" />
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaConsumerGroup"
                label="Consumer Group"
                tooltip="Consumer Group ID (ייחודי לאפליקציה)"
              >
                <Input className="ltr-field" placeholder="ez-data-processor-group" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaSecurityProtocol"
                label="Security Protocol"
                initialValue="PLAINTEXT"
              >
                <Select>
                  {KAFKA_SECURITY_PROTOCOLS.map(opt => (
                    <Option key={opt.value} value={opt.value}>{opt.label}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaOffsetReset"
                label="Auto Offset Reset"
                tooltip="מאיפה להתחיל לקרוא כאשר אין offset שמור"
                initialValue="latest"
              >
                <Select>
                  {KAFKA_OFFSET_RESET.map(opt => (
                    <Option key={opt.value} value={opt.value}>{opt.label}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaUsername"
                label="Username (SASL)"
                tooltip="נדרש רק עבור SASL authentication"
              >
                <Input placeholder="kafka-user" />
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="kafkaPassword"
                label="Password (SASL)"
                tooltip="נדרש רק עבור SASL authentication"
              >
                <Input.Password placeholder="kafka-password" />
              </Form.Item>
            </Col>
          </Row>

          <Alert
            message="הגדרות Kafka מתקדמות"
            description={
              <ul style={{ margin: 0, paddingRight: 20 }}>
                <li>Brokers: רשימת כתובות Kafka brokers (host:port)</li>
                <li>Topic: שם ה-Topic לצריכת הודעות</li>
                <li>Consumer Group: מזהה ייחודי למעקב אחר offset</li>
                <li>Security: PLAINTEXT למערכות מקומיות, SSL/SASL לפרודקשן</li>
              </ul>
            }
            type="info"
            showIcon
            style={{ marginTop: 16 }}
          />
        </>
      )}

      {/* Connection Test Button */}
      <Form.Item>
        <Space>
          <Button
            type="default"
            icon={<ApiOutlined />}
            onClick={onTestConnection}
            loading={testingConnection}
            disabled={!connectionType}
          >
            בדוק חיבור
          </Button>
          {connectionTestResult === 'success' && (
            <Tag icon={<CheckCircleOutlined />} color="success">
              חיבור הצליח
            </Tag>
          )}
          {connectionTestResult === 'failed' && (
            <Tag icon={<CloseCircleOutlined />} color="error">
              חיבור נכשל
            </Tag>
          )}
        </Space>
      </Form.Item>
    </>
  );
};
