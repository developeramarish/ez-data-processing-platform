import React from 'react';
import { Form, Input, Select, Switch, Alert, Row, Col } from 'antd';
import { FormInstance } from 'antd/es/form';
import { CSV_DELIMITER_OPTIONS, ENCODING_OPTIONS, FILE_TYPE_OPTIONS } from '../shared/constants';

const { Option } = Select;

interface FileSettingsTabProps {
  form: FormInstance;
  t: (key: string) => string;
  fileType: string;
}

export const FileSettingsTab: React.FC<FileSettingsTabProps> = ({ form, t, fileType }) => {
  return (
    <>
      <Alert
        message="הגדרות פורמט הקובץ"
        description="הגדר את סוג הקובץ והגדרות ספציפיות לפורמט"
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Form.Item
        name="fileType"
        label="סוג הקובץ"
        rules={[{ required: true, message: t('errors.required') }]}
      >
        <Select placeholder="בחר סוג קובץ">
          {FILE_TYPE_OPTIONS.map(option => (
            <Option key={option.value} value={option.value}>{option.label}</Option>
          ))}
        </Select>
      </Form.Item>

      {/* CSV specific fields */}
      {fileType === 'CSV' && (
        <>
          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="csvDelimiter"
                label="מפריד (Delimiter)"
                tooltip="התו המפריד בין עמודות"
              >
                <Select>
                  {CSV_DELIMITER_OPTIONS.map(option => (
                    <Option key={option.value} value={option.value}>{option.label}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="hasHeaders"
                valuePropName="checked"
                label="שורת כותרות"
              >
                <Switch 
                  checkedChildren="יש כותרות" 
                  unCheckedChildren="אין כותרות"
                />
              </Form.Item>
            </Col>
          </Row>
        </>
      )}

      {/* Excel specific fields */}
      {fileType === 'Excel' && (
        <>
          <Row gutter={16}>
            <Col xs={24} lg={12}>
              <Form.Item
                name="excelSheet"
                label="שם הגיליון"
                tooltip="שם הגיליון בקובץ Excel (השאר ריק עבור הגיליון הראשון)"
              >
                <Input placeholder="לדוגמה: Sheet1 או נתונים" />
              </Form.Item>
            </Col>
            <Col xs={24} lg={12}>
              <Form.Item
                name="hasHeaders"
                valuePropName="checked"
                label="שורת כותרות"
              >
                <Switch 
                  checkedChildren="יש כותרות" 
                  unCheckedChildren="אין כותרות"
                />
              </Form.Item>
            </Col>
          </Row>
        </>
      )}

      {/* Common encoding field */}
      <Form.Item
        name="encoding"
        label="קידוד תווים (Encoding)"
        tooltip="קידוד התווים של הקובץ - חשוב במיוחד לטקסט עברי"
      >
        <Select>
          {ENCODING_OPTIONS.map(option => (
            <Option key={option.value} value={option.value}>{option.label}</Option>
          ))}
        </Select>
      </Form.Item>
    </>
  );
};
