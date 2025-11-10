import React from 'react';
import { Form, Input, Switch, Alert, Row, Col } from 'antd';
import { FormInstance } from 'antd/es/form';

interface NotificationsTabProps {
  form: FormInstance;
  t: (key: string) => string;
}

export const NotificationsTab: React.FC<NotificationsTabProps> = ({ form, t }) => {
  return (
    <>
      <Alert
        message="הגדרות התראות"
        description="הגדר מתי לקבל התראות על עיבוד מקור נתונים זה"
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Row gutter={16}>
        <Col xs={24} lg={12}>
          <Form.Item
            name="notifyOnSuccess"
            valuePropName="checked"
            label="התראה בהצלחה"
          >
            <Switch 
              checkedChildren="מופעל" 
              unCheckedChildren="מושבת"
            />
          </Form.Item>
        </Col>
        <Col xs={24} lg={12}>
          <Form.Item
            name="notifyOnFailure"
            valuePropName="checked"
            label="התראה בכשלון"
          >
            <Switch 
              checkedChildren="מופעל" 
              unCheckedChildren="מושבת"
            />
          </Form.Item>
        </Col>
      </Row>

      <Form.Item
        name="notificationRecipients"
        label="נמעני התראות"
        tooltip="רשימת כתובות מייל מופרדות בפסיקים"
      >
        <Input 
          placeholder="user1@example.com, user2@example.com"
          addonBefore="📧"
        />
      </Form.Item>
    </>
  );
};
