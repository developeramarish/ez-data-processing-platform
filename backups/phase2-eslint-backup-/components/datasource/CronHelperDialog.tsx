import React, { useState, useEffect, useCallback } from 'react';
import {
  Modal,
  Tabs,
  Card,
  List,
  Input,
  Button,
  Space,
  Typography,
  Row,
  Col,
  Alert,
  Divider,
  Collapse,
  message,
  Form,
  Select
} from 'antd';
import {
  CopyOutlined,
  ClockCircleOutlined,
  CalendarOutlined,
  HistoryOutlined
} from '@ant-design/icons';
import { humanizeCron } from '../datasource/shared/helpers';

const { Text, Title } = Typography;
const { Panel } = Collapse;
const { Option } = Select;

interface CronPattern {
  id: string;
  name: string;
  nameHebrew: string;
  expression: string;
  description: string;
  examples: string[];
  category: 'seconds' | 'minutes' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'custom';
}

interface CronHelperDialogProps {
  visible: boolean;
  onClose: () => void;
  onSelect: (expression: string) => void;
  currentValue?: string;
}

const CronHelperDialog: React.FC<CronHelperDialogProps> = ({ 
  visible, 
  onClose, 
  onSelect,
  currentValue 
}) => {
  const [selectedExpression, setSelectedExpression] = useState<string>('');
  const [testExpression, setTestExpression] = useState<string>('');
  const [activeTab, setActiveTab] = useState<string>('patterns');
  const [form] = Form.useForm();

  // Initialize with current value if provided
  useEffect(() => {
    if (currentValue) {
      setTestExpression(currentValue);
      setSelectedExpression(currentValue);
    }
  }, [currentValue, visible]);

  // Predefined cron patterns library (6-field Quartz format with seconds)
  const predefinedPatterns: CronPattern[] = [
    // Every N Seconds
    {
      id: 'every_5_sec',
      name: 'Every 5 Seconds',
      nameHebrew: 'כל 5 שניות',
      expression: '*/5 * * * * *',
      description: 'הרצה כל 5 שניות',
      examples: ['00:00:05', '00:00:10', '00:00:15'],
      category: 'seconds'
    },
    {
      id: 'every_10_sec',
      name: 'Every 10 Seconds',
      nameHebrew: 'כל 10 שניות',
      expression: '*/10 * * * * *',
      description: 'הרצה כל 10 שניות',
      examples: ['00:00:10', '00:00:20', '00:00:30'],
      category: 'seconds'
    },
    {
      id: 'every_15_sec',
      name: 'Every 15 Seconds',
      nameHebrew: 'כל 15 שניות',
      expression: '*/15 * * * * *',
      description: 'הרצה כל 15 שניות',
      examples: ['00:00:15', '00:00:30', '00:00:45'],
      category: 'seconds'
    },
    {
      id: 'every_30_sec',
      name: 'Every 30 Seconds',
      nameHebrew: 'כל 30 שניות',
      expression: '*/30 * * * * *',
      description: 'הרצה כל 30 שניות',
      examples: ['00:00:30', '00:01:00', '00:01:30'],
      category: 'seconds'
    },

    // Every N Minutes
    {
      id: 'every_1_min',
      name: 'Every Minute',
      nameHebrew: 'כל דקה',
      expression: '0 * * * * *',
      description: 'הרצה כל דקה',
      examples: ['00:00:00', '00:01:00', '00:02:00'],
      category: 'minutes'
    },
    {
      id: 'every_2_min',
      name: 'Every 2 Minutes',
      nameHebrew: 'כל שתי דקות',
      expression: '0 */2 * * * *',
      description: 'הרצה כל שתי דקות',
      examples: ['00:00:00', '00:02:00', '00:04:00'],
      category: 'minutes'
    },
    {
      id: 'every_5_min',
      name: 'Every 5 Minutes',
      nameHebrew: 'כל 5 דקות',
      expression: '0 */5 * * * *',
      description: 'הרצה כל 5 דקות',
      examples: ['00:05:00', '00:10:00', '00:15:00'],
      category: 'minutes'
    },
    {
      id: 'every_10_min',
      name: 'Every 10 Minutes',
      nameHebrew: 'כל 10 דקות',
      expression: '0 */10 * * * *',
      description: 'הרצה כל 10 דקות',
      examples: ['00:10:00', '00:20:00', '00:30:00'],
      category: 'minutes'
    },
    {
      id: 'every_15_min',
      name: 'Every 15 Minutes',
      nameHebrew: 'כל 15 דקות',
      expression: '0 */15 * * * *',
      description: 'הרצה כל 15 דקות',
      examples: ['00:15:00', '00:30:00', '00:45:00'],
      category: 'minutes'
    },
    {
      id: 'every_30_min',
      name: 'Every 30 Minutes',
      nameHebrew: 'כל 30 דקות',
      expression: '0 */30 * * * *',
      description: 'הרצה כל 30 דקות',
      examples: ['00:30:00', '01:00:00', '01:30:00'],
      category: 'minutes'
    },

    // Hourly Patterns
    {
      id: 'every_hour',
      name: 'Every Hour',
      nameHebrew: 'כל שעה',
      expression: '0 0 * * * *',
      description: 'הרצה בתחילת כל שעה',
      examples: ['00:00:00', '01:00:00', '02:00:00'],
      category: 'hourly'
    },
    {
      id: 'every_2_hours',
      name: 'Every 2 Hours',
      nameHebrew: 'כל שעתיים',
      expression: '0 0 */2 * * *',
      description: 'הרצה כל שעתיים',
      examples: ['00:00', '02:00', '04:00'],
      category: 'hourly'
    },
    {
      id: 'every_3_hours',
      name: 'Every 3 Hours',
      nameHebrew: 'כל 3 שעות',
      expression: '0 0 */3 * * *',
      description: 'הרצה כל 3 שעות',
      examples: ['00:00', '03:00', '06:00'],
      category: 'hourly'
    },
    {
      id: 'every_6_hours',
      name: 'Every 6 Hours',
      nameHebrew: 'כל 6 שעות',
      expression: '0 0 */6 * * *',
      description: 'הרצה כל 6 שעות',
      examples: ['00:00', '06:00', '12:00', '18:00'],
      category: 'hourly'
    },

    // Daily Patterns
    {
      id: 'daily_midnight',
      name: 'Daily at Midnight',
      nameHebrew: 'יומי בחצות',
      expression: '0 0 0 * * *',
      description: 'הרצה כל יום בחצות',
      examples: ['00:00:00'],
      category: 'daily'
    },
    {
      id: 'daily_8am',
      name: 'Daily at 8:00 AM',
      nameHebrew: 'יומי ב-08:00 בבוקר',
      expression: '0 0 8 * * *',
      description: 'הרצה כל יום בשעה 8:00',
      examples: ['08:00:00'],
      category: 'daily'
    },

    // Weekday Patterns
    {
      id: 'weekdays_8am',
      name: 'Weekdays at 8:00 AM',
      nameHebrew: 'בימי חול ב-08:00',
      expression: '0 0 8 * * 1-5',
      description: 'הרצה בימים א-ה בשעה 8:00',
      examples: ['שני 08:00', 'שלישי 08:00'],
      category: 'weekly'
    },

    // Monthly Patterns
    {
      id: 'monthly_first_day',
      name: 'Monthly on 1st',
      nameHebrew: 'חודשי ב-1 לחודש',
      expression: '0 0 0 1 * *',
      description: 'הרצה ב-1 לכל חודש בחצות',
      examples: ['1 לחודש'],
      category: 'monthly'
    }
  ];

  const allPatterns = [...predefinedPatterns];

  const handleSelectPattern = (pattern: CronPattern) => {
    setSelectedExpression(pattern.expression);
    setTestExpression(pattern.expression);
  };

  const handleCopyToClipboard = useCallback(() => {
    const expressionToCopy = testExpression || selectedExpression;
    if (!expressionToCopy) {
      message.warning('אין ביטוי Cron לשימוש');
      return;
    }

    navigator.clipboard.writeText(expressionToCopy);
    message.success('ביטוי Cron הועתק ללוח!', 3);
  }, [testExpression, selectedExpression]);

  const renderPatternItem = (pattern: CronPattern) => (
    <List.Item
      key={pattern.id}
      actions={[
        <Button
          type="link"
          icon={<CopyOutlined />}
          onClick={() => {
            navigator.clipboard.writeText(pattern.expression);
            message.success('ביטוי Cron הועתק ללוח');
          }}
        >
          העתק
        </Button>,
        <Button
          type="primary"
          size="small"
          onClick={() => handleSelectPattern(pattern)}
        >
          בחר
        </Button>
      ]}
      style={{ backgroundColor: '#f9f9f9', padding: 12, marginBottom: 8, borderRadius: 6 }}
    >
      <List.Item.Meta
        title={
          <Space>
            <Text strong>{pattern.nameHebrew}</Text>
            <Text type="secondary">({pattern.name})</Text>
          </Space>
        }
        description={
          <div>
            <Text type="secondary">{pattern.description}</Text>
            <div style={{ marginTop: 8 }}>
              <Text code style={{ fontSize: 11, backgroundColor: '#2d3748', color: '#68d391', padding: '2px 6px', direction: 'ltr', fontFamily: 'monospace' }}>
                {pattern.expression}
              </Text>
            </div>
            {pattern.examples.length > 0 && (
              <div style={{ marginTop: 4 }}>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  דוגמאות זמן: <span style={{ direction: 'ltr', fontFamily: 'monospace' }}>{pattern.examples.join(', ')}</span>
                </Text>
              </div>
            )}
          </div>
        }
      />
    </List.Item>
  );

  // Build 6-field cron expression
  const handleBuildExpression = (values: any) => {
    const { second, minute, hour, dayOfMonth, month, dayOfWeek } = values;
    const cronExpression = `${second || '0'} ${minute || '*'} ${hour || '*'} ${dayOfMonth || '*'} ${month || '*'} ${dayOfWeek || '*'}`;
    setTestExpression(cronExpression);
    message.success('ביטוי Cron נוצר בהצלחה');
  };

  return (
    <Modal
      title={<Title level={4}>עזרת Cron - תזמון משימות (תמיכה בשניות)</Title>}
      open={visible}
      onCancel={onClose}
      width={950}
      footer={[
        <Button key="close" onClick={onClose}>
          סגור
        </Button>,
        <Button
          key="copy"
          type="default"
          icon={<CopyOutlined />}
          onClick={handleCopyToClipboard}
          disabled={!testExpression && !selectedExpression}
        >
          העתק ללוח
        </Button>,
        <Button
          key="use"
          type="primary"
          onClick={() => {
            const expr = testExpression || selectedExpression;
            if (expr) {
              onSelect(expr);
              onClose();
            }
          }}
          disabled={!testExpression && !selectedExpression}
        >
          השתמש בביטוי
        </Button>
      ]}
    >
      <Tabs activeKey={activeTab} onChange={setActiveTab}>
        <Tabs.TabPane tab="תבניות נפוצות" key="patterns">
          <Alert
            message="ספריית ביטויי Cron - פורמט 6 שדות (כולל שניות)"
            description="בחר ביטוי Cron מוכן. פורמט: שנייה דקה שעה יוםבחודש חודש יוםבשבוע"
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <Collapse defaultActiveKey={['seconds', 'minutes']} accordion={false}>
            <Panel header={<span>⚡ כל מספר שניות</span>} key="seconds">
              <List
                dataSource={allPatterns.filter(p => p.category === 'seconds')}
                renderItem={renderPatternItem}
              />
            </Panel>

            <Panel header={<span><ClockCircleOutlined /> כל מספר דקות</span>} key="minutes">
              <List
                dataSource={allPatterns.filter(p => p.category === 'minutes')}
                renderItem={renderPatternItem}
              />
            </Panel>

            <Panel header={<span><HistoryOutlined /> כל מספר שעות</span>} key="hourly">
              <List
                dataSource={allPatterns.filter(p => p.category === 'hourly')}
                renderItem={renderPatternItem}
              />
            </Panel>

            <Panel header={<span><CalendarOutlined /> יומי</span>} key="daily">
              <List
                dataSource={allPatterns.filter(p => p.category === 'daily')}
                renderItem={renderPatternItem}
              />
            </Panel>

            <Panel header={<span><CalendarOutlined /> שבועי</span>} key="weekly">
              <List
                dataSource={allPatterns.filter(p => p.category === 'weekly')}
                renderItem={renderPatternItem}
              />
            </Panel>

            <Panel header={<span><CalendarOutlined /> חודשי</span>} key="monthly">
              <List
                dataSource={allPatterns.filter(p => p.category === 'monthly')}
                renderItem={renderPatternItem}
              />
            </Panel>
          </Collapse>
        </Tabs.TabPane>

        <Tabs.TabPane tab="בונה ויזואלי" key="builder">
          <Alert
            message="בנה ביטוי Cron עם תמיכה בשניות"
            description="פורמט 6 שדות: שנייה דקה שעה יום חודש יוםבשבוע"
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <Form
            form={form}
            layout="vertical"
            onFinish={handleBuildExpression}
            initialValues={{
              second: '0',
              minute: '*',
              hour: '*',
              dayOfMonth: '*',
              month: '*',
              dayOfWeek: '*'
            }}
          >
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item name="second" label="שנייה (0-59)">
                  <Select>
                    <Option value="0">0 (בתחילת הדקה)</Option>
                    <Option value="*">* (כל שנייה)</Option>
                    <Option value="*/5">*/5 (כל 5 שניות)</Option>
                    <Option value="*/10">*/10 (כל 10 שניות)</Option>
                    <Option value="*/15">*/15 (כל 15 שניות)</Option>
                    <Option value="*/30">*/30 (כל 30 שניות)</Option>
                  </Select>
                </Form.Item>
              </Col>

              <Col span={12}>
                <Form.Item name="minute" label="דקה (0-59)">
                  <Select>
                    <Option value="*">* (כל דקה)</Option>
                    <Option value="0">0 (בתחילת השעה)</Option>
                    <Option value="*/2">*/2 (כל שתי דקות)</Option>
                    <Option value="*/5">*/5 (כל 5 דקות)</Option>
                    <Option value="*/10">*/10 (כל 10 דקות)</Option>
                    <Option value="*/15">*/15 (כל 15 דקות)</Option>
                    <Option value="*/30">*/30 (כל 30 דקות)</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={8}>
                <Form.Item name="hour" label="שעה (0-23)">
                  <Select>
                    <Option value="*">* (כל שעה)</Option>
                    <Option value="0">0 (חצות)</Option>
                    <Option value="8">8 (בוקר)</Option>
                    <Option value="12">12 (צהריים)</Option>
                    <Option value="18">18 (ערב)</Option>
                    <Option value="*/2">*/2 (כל שעתיים)</Option>
                    <Option value="*/6">*/6 (כל 6 שעות)</Option>
                  </Select>
                </Form.Item>
              </Col>

              <Col span={8}>
                <Form.Item name="dayOfMonth" label="יום בחודש (1-31)">
                  <Select>
                    <Option value="*">* (כל יום)</Option>
                    <Option value="1">1 (ראשון לחודש)</Option>
                    <Option value="15">15 (אמצע חודש)</Option>
                  </Select>
                </Form.Item>
              </Col>

              <Col span={8}>
                <Form.Item name="month" label="חודש (1-12)">
                  <Select>
                    <Option value="*">* (כל חודש)</Option>
                    <Option value="1">1 (ינואר)</Option>
                    <Option value="6">6 (יוני)</Option>
                    <Option value="12">12 (דצמבר)</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={24}>
                <Form.Item name="dayOfWeek" label="יום בשבוע (0-6 או *)">
                  <Select>
                    <Option value="*">* (כל יום)</Option>
                    <Option value="0">0 (ראשון)</Option>
                    <Option value="1">1 (שני)</Option>
                    <Option value="1-5">1-5 (שני-שישי)</Option>
                    <Option value="0,6">0,6 (סופ"ש)</Option>
                  </Select>
                </Form.Item>
              </Col>
            </Row>

            <Form.Item>
              <Button type="primary" htmlType="submit" block>
                בנה ביטוי Cron
              </Button>
            </Form.Item>
          </Form>

          <Divider />

          <div>
            <Text strong>ביטוי Cron נוכחי:</Text>
            <div style={{ marginTop: 8, padding: 12, backgroundColor: '#2d3748', borderRadius: 6 }}>
              <Text code style={{ color: '#68d391', fontSize: 14, direction: 'ltr', fontFamily: 'monospace' }}>
                {testExpression || '(ריק)'}
              </Text>
            </div>
          </div>
        </Tabs.TabPane>

        <Tabs.TabPane tab="הזנה ידנית" key="manual">
          <Alert
            message="הזן ביטוי Cron עם תמיכה בשניות"
            description="פורמט 6 שדות: שנייה דקה שעה יום חודש יוםבשבוע"
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <div style={{ marginBottom: 16 }}>
            <Text strong>ביטוי Cron (6 שדות):</Text>
            <Input
              className="ltr-field"
              value={testExpression}
              onChange={(e) => setTestExpression(e.target.value)}
              placeholder="*/30 * * * * * (כל 30 שניות)"
              style={{ marginTop: 8 }}
              size="large"
            />
          </div>

          <Alert
            message="פורמט ביטוי Cron (Quartz - 6 שדות)"
            description={
              <div style={{ 
                fontFamily: '"Courier New", Consolas, monospace', 
                direction: 'ltr', 
                fontSize: '13px', 
                lineHeight: '1.8',
                backgroundColor: '#1e1e1e',
                color: '#d4d4d4',
                padding: '16px',
                borderRadius: '6px',
                overflow: 'auto'
              }}>
                <pre style={{ margin: 0, color: '#d4d4d4' }}>
{`┌───────────── Second (0-59)
│ ┌─────────── Minute (0-59)
│ │ ┌───────── Hour (0-23)
│ │ │ ┌─────── Day of Month (1-31)
│ │ │ │ ┌───── Month (1-12)
│ │ │ │ │ ┌─── Day of Week (0-6, 0=Sunday)
│ │ │ │ │ │
* * * * * *`}
                </pre>
                <div style={{ marginTop: '12px', fontSize: '11px', color: '#858585' }}>
                  <div>דוגמה: <span style={{ color: '#4ec9b0' }}>*/30 * * * * *</span> = כל 30 שניות</div>
                  <div>דוגמה: <span style={{ color: '#4ec9b0' }}>0 */5 * * * *</span> = כל 5 דקות</div>
                  <div>דוגמה: <span style={{ color: '#4ec9b0' }}>0 0 8 * * 1-5</span> = ימי חול ב-08:00</div>
                </div>
              </div>
            }
            type="info"
          />
        </Tabs.TabPane>
      </Tabs>

      {(selectedExpression || testExpression) && (
        <Card size="small" style={{ marginTop: 16, backgroundColor: '#f0fdf4', border: '1px solid #86efac' }}>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Text strong>ביטוי נבחר:</Text>
            <Text code style={{ fontSize: 13, backgroundColor: '#2d3748', color: '#68d391', padding: '4px 8px', direction: 'ltr', fontFamily: 'monospace' }}>
              {testExpression || selectedExpression}
            </Text>
            <Alert
              message={humanizeCron(testExpression || selectedExpression)}
              type="success"
              showIcon
              icon={<ClockCircleOutlined />}
              style={{ marginTop: 8 }}
            />
          </Space>
        </Card>
      )}
    </Modal>
  );
};

export default CronHelperDialog;
