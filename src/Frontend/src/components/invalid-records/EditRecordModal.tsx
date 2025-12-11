import React, { useState, useEffect } from 'react';
import { Modal, Form, Input, Button, message, Descriptions, Typography, Space, Tag } from 'antd';
import { InvalidRecord } from '../../services/invalidrecords-api-client';

const { Text, Title } = Typography;
const { TextArea } = Input;

interface EditRecordModalProps {
  visible: boolean;
  record: InvalidRecord | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface FieldError {
  fieldName: string;
  originalValue: any;
  errorMessage: string;
}

const EditRecordModal: React.FC<EditRecordModalProps> = ({
  visible,
  record,
  onClose,
  onSuccess,
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [failedFields, setFailedFields] = useState<FieldError[]>([]);

  useEffect(() => {
    if (record && visible) {
      // Extract failed fields from validation errors
      const fields = extractFailedFields(record);
      setFailedFields(fields);

      // Pre-populate form with current values
      const initialValues: Record<string, any> = {};
      fields.forEach(field => {
        initialValues[field.fieldName] = field.originalValue;
      });
      form.setFieldsValue(initialValues);
    } else {
      setFailedFields([]);
      form.resetFields();
    }
  }, [record, visible, form]);

  // Extract field names from error messages
  const extractFailedFields = (record: InvalidRecord): FieldError[] => {
    const fields: FieldError[] = [];
    const seenFields = new Set<string>();

    record.errors.forEach(error => {
      let fieldName = error.field;

      // If field is "Unknown", try to extract from message
      // Example: "Path '[2].Amount'" → "Amount"
      if (!fieldName || fieldName === 'Unknown') {
        const pathMatch = error.message.match(/Path '\[?\d*\]?\.?(\w+)'/);
        if (pathMatch) {
          fieldName = pathMatch[1];
        }
      }

      if (fieldName && fieldName !== 'Unknown' && !seenFields.has(fieldName)) {
        seenFields.add(fieldName);

        // Get original value from record.originalData
        const originalValue = record.originalData[fieldName];

        fields.push({
          fieldName,
          originalValue,
          errorMessage: error.message,
        });
      }
    });

    return fields;
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      if (!record) return;

      // Merge corrected values with original data
      const correctedData: any = {
        ...record.originalData,
        ...values,
      };

      // Convert Amount to number if it's a string (schema expects number)
      if (correctedData.Amount && typeof correctedData.Amount === 'string') {
        const amountNum = parseFloat(correctedData.Amount);
        if (!isNaN(amountNum)) {
          correctedData.Amount = amountNum;
        }
      }

      // Call the API to correct and reprocess
      const response = await fetch(
        `http://localhost:5007/api/v1/invalid-records/${record.id}/correct`,
        {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            correctedData,
            correctedBy: 'User',
            autoReprocess: true,
          }),
        }
      );

      const result = await response.json();

      if (result.isSuccess) {
        form.resetFields();
        onClose();

        // Show persistent processing message
        message.loading({
          content: 'נשלח לאימות מחדש... ממתין לתוצאות',
          key: 'reprocess',
          duration: 0,
        });

        // Wait 4 seconds for ValidationService to process, then refresh
        setTimeout(async () => {
          await onSuccess();

          message.info({
            content: 'עיבוד מחדש הושלם. אם הרשומה נעלמה - האימות עבר בהצלחה. אם נשארה - יש עדיין שגיאות.',
            key: 'reprocess',
            duration: 5,
          });
        }, 4000);
      } else {
        message.error(result.error?.message || 'שגיאה בתיקון הרשומה');
      }
    } catch (error: any) {
      message.error(error.message || 'Failed to correct record');
    } finally {
      setLoading(false);
    }
  };

  if (!record) return null;

  return (
    <Modal
      title={
        <Space direction="vertical" size="small">
          <Title level={4} style={{ margin: 0 }}>תיקון רשומה לא תקינה</Title>
          <Text type="secondary">תקן את השדות שנכשלו באימות</Text>
        </Space>
      }
      open={visible}
      onCancel={onClose}
      width={700}
      footer={[
        <Button key="cancel" onClick={onClose}>
          ביטול
        </Button>,
        <Button
          key="submit"
          type="primary"
          loading={loading}
          onClick={handleSubmit}
        >
          תקן ושלח לעיבוד מחדש
        </Button>,
      ]}
    >
      {/* Record Context */}
      <Descriptions bordered size="small" column={1} style={{ marginBottom: 16 }}>
        <Descriptions.Item label="מקור נתונים">{record.dataSourceName}</Descriptions.Item>
        <Descriptions.Item label="קובץ">{record.fileName}</Descriptions.Item>
        <Descriptions.Item label="שורה">{record.lineNumber || 'N/A'}</Descriptions.Item>
      </Descriptions>

      {/* Edit Form - Only Failed Fields */}
      <Form
        form={form}
        layout="vertical"
        style={{ marginTop: 16 }}
      >
        <Text strong style={{ color: '#ff4d4f', marginBottom: 16, display: 'block' }}>
          שדות שנכשלו באימות ({failedFields.length}):
        </Text>

        {failedFields.map(field => (
          <Form.Item
            key={field.fieldName}
            name={field.fieldName}
            label={
              <Space direction="vertical" size={0}>
                <Text strong>{field.fieldName}</Text>
                <Text type="secondary" style={{ fontSize: '12px' }}>
                  ערך מקורי: {String(field.originalValue || 'ריק')}
                </Text>
              </Space>
            }
            rules={[
              { required: true, message: 'שדה חובה' },
            ]}
            extra={
              <Text type="danger" style={{ fontSize: '12px' }}>
                {field.errorMessage}
              </Text>
            }
          >
            <Input placeholder={`הכנס ערך תקין עבור ${field.fieldName}`} />
          </Form.Item>
        ))}

        {failedFields.length === 0 && (
          <Text type="secondary">
            לא נמצאו שדות ספציפיים לתיקון. אנא צור קשר עם מנהל המערכת.
          </Text>
        )}
      </Form>

      {/* Non-Failed Fields (Read-Only Context) */}
      {Object.keys(record.originalData).filter(
        key => !failedFields.find(f => f.fieldName === key)
      ).length > 0 && (
        <>
          <Text strong style={{ display: 'block', marginTop: 24, marginBottom: 8 }}>
            שדות תקינים (לקריאה בלבד):
          </Text>
          <div style={{
            backgroundColor: '#f5f5f5',
            padding: 12,
            borderRadius: 4,
            maxHeight: '150px',
            overflowY: 'auto',
          }}>
            {Object.entries(record.originalData)
              .filter(([key]) => !failedFields.find(f => f.fieldName === key))
              .map(([key, value]) => (
                <div key={key} style={{ marginBottom: 4 }}>
                  <Text strong>{key}:</Text> <Text>{String(value)}</Text>
                </div>
              ))}
          </div>
        </>
      )}
    </Modal>
  );
};

export default EditRecordModal;
