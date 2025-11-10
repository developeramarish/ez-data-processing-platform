import React from 'react';
import { Form, Input, Select, Switch, Alert, Row, Col, Tooltip } from 'antd';
import { FormInstance } from 'antd/es/form';
import { QuestionCircleOutlined } from '@ant-design/icons';
import { SCHEDULE_FREQUENCY_OPTIONS } from '../shared/constants';
import { humanizeCron } from '../shared/helpers';

const { Option } = Select;

interface ScheduleTabProps {
  form: FormInstance;
  t: (key: string) => string;
  scheduleFrequency: string;
  cronExpression?: string;
  onOpenCronHelper: () => void;
}

export const ScheduleTab: React.FC<ScheduleTabProps> = ({
  form,
  t,
  scheduleFrequency,
  cronExpression,
  onOpenCronHelper
}) => {
  return (
    <>
      <Alert
        message="הגדרות תזמון אוטומטי"
        description="קבע באיזו תדירות המערכת תבדוק קבצים חדשים למקור נתונים זה"
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Row gutter={16}>
        <Col xs={24} lg={16}>
          <Form.Item
            name="scheduleFrequency"
            label="תדירות בדיקה"
          >
            <Select>
              {SCHEDULE_FREQUENCY_OPTIONS.map(option => (
                <Option key={option.value} value={option.value}>{option.label}</Option>
              ))}
            </Select>
          </Form.Item>
        </Col>
        <Col xs={24} lg={8}>
          <Form.Item
            name="scheduleEnabled"
            valuePropName="checked"
            label="תזמון פעיל"
          >
            <Switch 
              checkedChildren="מופעל" 
              unCheckedChildren="מושבת"
            />
          </Form.Item>
        </Col>
      </Row>

      {scheduleFrequency === 'Custom' && (
        <>
          <Form.Item
            name="cronExpression"
            label="ביטוי Cron"
            tooltip="ביטוי Cron לתזמון מותאם אישית"
            rules={[
              { required: scheduleFrequency === 'Custom', message: 'נדרש ביטוי Cron' }
            ]}
          >
            <Input 
              className="ltr-field"
              placeholder="לדוגמה: 0 0 * * * (כל יום בחצות)"
              addonAfter={
                <Tooltip title="פתח עוזר Cron">
                  <QuestionCircleOutlined 
                    style={{ cursor: 'pointer' }}
                    onClick={onOpenCronHelper}
                  />
                </Tooltip>
              }
            />
          </Form.Item>

          {cronExpression && (
            <Alert
              message={humanizeCron(cronExpression)}
              type="success"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}
        </>
      )}

      <Alert
        message="דוגמאות תזמון"
        description={
          <ul style={{ margin: 0, paddingRight: 20 }}>
            <li>שעתי: בדיקה כל שעה</li>
            <li>יומי: בדיקה פעם ביום בשעה שנבחרה</li>
            <li>שבועי: בדיקה פעם בשבוע</li>
            <li>Cron מותאם: שליטה מלאה בזמני הבדיקה</li>
          </ul>
        }
        type="warning"
        showIcon
      />
    </>
  );
};
