import React from 'react';
import { Card, Alert, Button, Space, Tag, Divider, Descriptions } from 'antd';
import { FileTextOutlined } from '@ant-design/icons';

interface SchemaDetailsTabProps {
  jsonSchema: any;
  dataSourceId?: string;
  onEdit: () => void;
}

export const SchemaDetailsTab: React.FC<SchemaDetailsTabProps> = ({ jsonSchema, dataSourceId, onEdit }) => {
  const hasSchema = jsonSchema && Object.keys(jsonSchema).length > 0;

  if (!hasSchema) {
    return (
      <Alert
        message="אין Schema מוגדר"
        description="מקור נתונים זה אינו כולל JSON Schema מוטמע. ניתן להוסיף Schema על ידי עריכת מקור הנתונים."
        type="info"
        showIcon
        action={
          <Button type="primary" size="small" onClick={onEdit}>
            ערוך ל-הוספת Schema
          </Button>
        }
      />
    );
  }

  return (
    <div>
      <Alert
        message="JSON Schema מוטמע"
        description="Schema זה מוגדר ישירות במקור הנתונים ומשמש לאימות הקבצים הנכנסים"
        type="success"
        showIcon
        style={{ marginBottom: 16 }}
      />
      
      <Card 
        title={
          <Space>
            <FileTextOutlined />
            <span>JSON Schema</span>
            {jsonSchema.title && <Tag color="blue">{jsonSchema.title}</Tag>}
          </Space>
        }
        style={{ backgroundColor: '#fafafa' }}
      >
        <pre style={{ 
          backgroundColor: '#fff',
          padding: '16px',
          borderRadius: '6px',
          border: '1px solid #d9d9d9',
          maxHeight: '600px',
          overflow: 'auto',
          fontSize: '13px',
          lineHeight: '1.6',
          direction: 'ltr',
          textAlign: 'left'
        }}>
          {JSON.stringify(jsonSchema, null, 2)}
        </pre>
        
        {jsonSchema.properties && (
          <div style={{ marginTop: 16 }}>
            <Divider orientation="right">סיכום שדות</Divider>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="מספר שדות">
                <Tag color="blue">{Object.keys(jsonSchema.properties).length}</Tag>
              </Descriptions.Item>
              {jsonSchema.required && jsonSchema.required.length > 0 && (
                <Descriptions.Item label="שדות חובה">
                  <Space wrap>
                    {jsonSchema.required.map((field: string) => (
                      <Tag key={field} color="red">{field}</Tag>
                    ))}
                  </Space>
                </Descriptions.Item>
              )}
              <Descriptions.Item label="שדות זמינים">
                <Space wrap>
                  {Object.keys(jsonSchema.properties).map((field: string) => (
                    <Tag key={field} color="geekblue">{field}</Tag>
                  ))}
                </Space>
              </Descriptions.Item>
            </Descriptions>
          </div>
        )}
      </Card>
    </div>
  );
};
