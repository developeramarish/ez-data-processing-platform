import React, { useState } from 'react';
import { Card, Button, Form, Input, Select, Tag, Space, Typography, Row, Col, Tabs, Switch } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  BellOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  MailOutlined,
  SlackOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;
const { Option } = Select;
const { TabPane } = Tabs;

interface NotificationRule {
  id: string;
  name: string;
  condition: string;
  recipients: string[];
  channels: string[];
  status: 'active' | 'inactive';
  type: 'validation' | 'prometheus' | 'system';
}

const NotificationsManagement: React.FC = () => {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  const [activeTab, setActiveTab] = useState('validation-notifications');

  // Mock notification rules data
  const validationRules: NotificationRule[] = [
    {
      id: '1',
      name: 'התרעת שיעור שגיאות גבוה',
      condition: 'שיעור שגיאות אימות > 5% לכל מקור נתונים',
      recipients: ['data-team@company.com', 'john.doe@company.com'],
      channels: ['Email', 'Slack (#data-alerts)'],
      status: 'active',
      type: 'validation',
    },
    {
      id: '2',
      name: 'אזהרת עיכוב עיבוד',
      condition: 'עיבוד קובץ נמשך יותר מ-10 דקות',
      recipients: ['ops-team@company.com'],
      channels: ['Email', 'PagerDuty'],
      status: 'active',
      type: 'validation',
    },
  ];

  const prometheusRules: NotificationRule[] = [
    {
      id: '3',
      name: 'שימוש גבוה ב-CPU',
      condition: 'avg(cpu_usage) > 80% למשך 5 דקות',
      recipients: ['infrastructure-team@company.com'],
      channels: ['Slack (#infrastructure)', 'PagerDuty'],
      status: 'active',
      type: 'prometheus',
    },
    {
      id: '4',
      name: 'התרעת עומק תור',
      condition: 'kafka_queue_depth > 1000 הודעות',
      recipients: ['data-team@company.com'],
      channels: ['Email', 'Slack (#data-alerts)'],
      status: 'active',
      type: 'prometheus',
    },
  ];

  const renderNotificationCard = (rule: NotificationRule) => (
    <Card 
      key={rule.id}
      style={{ 
        marginBottom: 16,
        backgroundColor: '#e8f4fd',
        border: '1px solid #bee5eb',
      }}
    >
      <Row justify="space-between" align="top">
        <Col flex="auto">
          <Title level={5}>{rule.name}</Title>
          <Text><strong>תנאי:</strong> {rule.condition}</Text>
          <br />
          <Text><strong>נמענים:</strong> {rule.recipients.join(', ')}</Text>
          <br />
          <Text><strong>ערוצים:</strong> {rule.channels.join(', ')}</Text>
        </Col>
        <Col>
          <Space direction="vertical" align="end">
            <Tag color={rule.status === 'active' ? 'green' : 'red'}>
              {rule.status === 'active' ? 'פעיל' : 'לא פעיל'}
            </Tag>
            <Button size="small" icon={<EditOutlined />}>
              ערוך
            </Button>
          </Space>
        </Col>
      </Row>
    </Card>
  );

  return (
    <div className="notifications-page">
      <div className="page-header">
        <Title level={2}>
          <BellOutlined /> כללי התרעות
        </Title>
        <Button type="primary" icon={<PlusOutlined />}>
          צור כלל התרעה
        </Button>
      </div>

      <Tabs activeKey={activeTab} onChange={setActiveTab}>
        <TabPane tab="התרעות אימות" key="validation-notifications">
          <div>
            {validationRules.map(renderNotificationCard)}
          </div>
        </TabPane>

        <TabPane tab="התרעות Prometheus" key="prometheus-alerts">
          <div>
            {prometheusRules.map(renderNotificationCard)}
          </div>
        </TabPane>

        <TabPane tab="התרעות מערכת" key="system-notifications">
          <Card>
            <Title level={4}>הגדרות התרעה גלובליות</Title>
            <Form form={form} layout="vertical">
              <Row gutter={16}>
                <Col span={12}>
                  <Form.Item
                    label="נמעני Email ברירת מחדל"
                    name="defaultEmails"
                  >
                    <Input defaultValue="admin@company.com, data-team@company.com" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="Slack Webhook URL"
                    name="slackWebhook"
                  >
                    <Input defaultValue="https://hooks.slack.com/services/..." />
                  </Form.Item>
                </Col>
              </Row>
              
              <Row gutter={16}>
                <Col span={12}>
                  <Form.Item
                    label="תדירות התרעות"
                    name="frequency"
                  >
                    <Select defaultValue="immediate">
                      <Option value="immediate">מיידי</Option>
                      <Option value="5min">כל 5 דקות</Option>
                      <Option value="15min">כל 15 דקות</Option>
                      <Option value="1hour">כל שעה</Option>
                    </Select>
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="הפעל התרעות"
                    name="enabled"
                    valuePropName="checked"
                  >
                    <Switch defaultChecked />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item>
                <Button type="primary">
                  שמור הגדרות
                </Button>
              </Form.Item>
            </Form>
          </Card>

          {/* Email Templates */}
          <Card style={{ marginTop: 16 }}>
            <Title level={4}>תבניות התרעה</Title>
            <Row gutter={16}>
              <Col span={12}>
                <Card size="small" title="תבנית Email">
                  <div style={{ backgroundColor: '#f5f5f5', padding: 12, borderRadius: 4 }}>
                    <Text style={{ fontFamily: 'monospace', fontSize: '12px' }}>
                      נושא: [מערכת עיבוד נתונים] {'{alertName}'}<br />
                      <br />
                      שלום,<br />
                      <br />
                      התקבלה התרעה במערכת:<br />
                      כלל: {'{alertName}'}<br />
                      תנאי: {'{condition}'}<br />
                      זמן: {'{timestamp}'}<br />
                      <br />
                      בברכה,<br />
                      מערכת עיבוד נתונים
                    </Text>
                  </div>
                </Card>
              </Col>
              <Col span={12}>
                <Card size="small" title="תבנית Slack">
                  <div style={{ backgroundColor: '#f5f5f5', padding: 12, borderRadius: 4 }}>
                    <Text style={{ fontFamily: 'monospace', fontSize: '12px' }}>
                      🚨 *התרעה: {'{alertName}'}*<br />
                      📋 תנאי: {'{condition}'}<br />
                      ⏰ זמן: {'{timestamp}'}<br />
                      🔗 <button type="button" onClick={() => {}} style={{ background: 'none', border: 'none', color: '#1890ff', cursor: 'pointer', textDecoration: 'underline' }}>לינק לדשבורד</button>
                    </Text>
                  </div>
                </Card>
              </Col>
            </Row>
          </Card>
        </TabPane>
      </Tabs>

      <style>{`
        .notifications-page .page-header {
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

export default NotificationsManagement;
