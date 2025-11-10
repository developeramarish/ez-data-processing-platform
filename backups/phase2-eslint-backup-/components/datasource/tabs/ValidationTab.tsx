import React from 'react';
import { Form, Switch, InputNumber, Alert, Row, Col } from 'antd';
import { FormInstance } from 'antd/es/form';

interface ValidationTabProps {
  form: FormInstance;
  t: (key: string) => string;
}

export const ValidationTab: React.FC<ValidationTabProps> = ({ form, t }) => {
  return (
    <>
      <Alert
        message="כללי אימות נתונים"
        description="הגדר כיצד המערכת תטפל ברשומות שאינן עוברות אימות"
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Row gutter={16}>
        <Col xs={24} lg={12}>
          <Form.Item
            name="skipInvalidRecords"
            valuePropName="checked"
            label="דלג על רשומות לא תקינות"
            tooltip="אם מופעל, המערכת תדלג על רשומות שלא עוברות אימות ותמשיך בעיבוד"
          >
            <Switch 
              checkedChildren="דלג" 
              unCheckedChildren="עצור"
            />
          </Form.Item>
        </Col>
        <Col xs={24} lg={12}>
          <Form.Item
            name="maxErrorsAllowed"
            label="מקסימום שגיאות מותרות"
            tooltip="מספר השגיאות המקסימלי לפני עצירת העיבוד (0 = ללא הגבלה)"
          >
            <InputNumber 
              min={0} 
              max={10000} 
              placeholder="100" 
              style={{ width: '100%' }}
            />
          </Form.Item>
        </Col>
      </Row>
    </>
  );
};
