// Consolidated export for all details tab components
// This file is created for documentation - actual detailed tab components 
// could be extracted to individual files later if needed

import React from 'react';
import { Descriptions, Tag, Typography, Alert, Space } from 'antd';
import { DataSource } from '../shared/types';
import { humanizeCron, getDelimiterLabel } from '../shared/helpers';
import { ClockCircleOutlined } from '@ant-design/icons';

const { Text } = Typography;

// Basic Info Details Tab
export const BasicInfoDetailsTab: React.FC<{ dataSource: DataSource; getCategoryLabel: (cat: string) => string }> = ({ dataSource, getCategoryLabel }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="שם מקור הנתונים" span={2}>
      <Text strong style={{ fontSize: '16px' }}>{dataSource.Name}</Text>
    </Descriptions.Item>
    <Descriptions.Item label="ספק הנתונים">{dataSource.SupplierName}</Descriptions.Item>
    <Descriptions.Item label="קטגוריה"><Tag color="blue">{getCategoryLabel(dataSource.Category)}</Tag></Descriptions.Item>
    <Descriptions.Item label="סטטוס">
      <Tag color={dataSource.IsActive ? 'green' : 'red'}>{dataSource.IsActive ? 'פעיל' : 'לא פעיל'}</Tag>
    </Descriptions.Item>
    <Descriptions.Item label="תקופת שמירה">{dataSource.AdditionalConfiguration?.RetentionDays || 30} ימים</Descriptions.Item>
    <Descriptions.Item label="תאריך יצירה">{new Date(dataSource.CreatedAt).toLocaleString('he-IL')}</Descriptions.Item>
    <Descriptions.Item label="עדכון אחרון">{new Date(dataSource.UpdatedAt).toLocaleString('he-IL')}</Descriptions.Item>
    <Descriptions.Item label="תיאור" span={2}>{dataSource.Description || 'לא הוגדר תיאור'}</Descriptions.Item>
  </Descriptions>
);

// Connection Details Tab
export const ConnectionDetailsTab: React.FC<{ connectionConfig: any; dataSource: DataSource }> = ({ connectionConfig, dataSource }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="סוג החיבור"><Tag color="purple">{connectionConfig.type || 'Local'}</Tag></Descriptions.Item>
    <Descriptions.Item label="נתיב/URL" span={1}>
      <Text code style={{ wordBreak: 'break-all' }}>
        {connectionConfig.path || connectionConfig.url || dataSource.FilePath || 'לא הוגדר'}
      </Text>
    </Descriptions.Item>
    {connectionConfig.host && (
      <>
        <Descriptions.Item label="שרת (Host)">{connectionConfig.host}</Descriptions.Item>
        <Descriptions.Item label="פורט">{connectionConfig.port || 'ברירת מחדל'}</Descriptions.Item>
        <Descriptions.Item label="שם משתמש">{connectionConfig.username || 'לא הוגדר'}</Descriptions.Item>
        <Descriptions.Item label="סיסמה">{connectionConfig.password ? '••••••••' : 'לא הוגדרה'}</Descriptions.Item>
      </>
    )}
    {connectionConfig.type === 'Kafka' && (
      <>
        <Descriptions.Item label="Kafka Brokers" span={2}>
          <Text code>{connectionConfig.brokers || 'לא הוגדר'}</Text>
        </Descriptions.Item>
        <Descriptions.Item label="Topic Name">
          <Tag color="blue">{connectionConfig.topic || 'לא הוגדר'}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Consumer Group">
          {connectionConfig.consumerGroup || 'לא הוגדר'}
        </Descriptions.Item>
        <Descriptions.Item label="Security Protocol">
          <Tag>{connectionConfig.securityProtocol || 'PLAINTEXT'}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Offset Reset">
          <Tag>{connectionConfig.offsetReset || 'latest'}</Tag>
        </Descriptions.Item>
        {connectionConfig.username && (
          <>
            <Descriptions.Item label="SASL Username">{connectionConfig.username}</Descriptions.Item>
            <Descriptions.Item label="SASL Password">••••••••</Descriptions.Item>
          </>
        )}
      </>
    )}
  </Descriptions>
);

// File Details Tab
export const FileDetailsTab: React.FC<{ fileConfig: any; getFileTypeDisplay: () => string }> = ({ fileConfig, getFileTypeDisplay }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="סוג הקובץ"><Tag color="cyan">{getFileTypeDisplay()}</Tag></Descriptions.Item>
    <Descriptions.Item label="קידוד (Encoding)">{fileConfig.encoding || 'UTF-8'}</Descriptions.Item>
    {fileConfig.type === 'CSV' && (
      <>
        <Descriptions.Item label="מפריד (Delimiter)"><Tag>{getDelimiterLabel(fileConfig.delimiter)}</Tag></Descriptions.Item>
        <Descriptions.Item label="שורת כותרות">
          <Tag color={fileConfig.hasHeaders !== false ? 'green' : 'orange'}>{fileConfig.hasHeaders !== false ? 'כן' : 'לא'}</Tag>
        </Descriptions.Item>
      </>
    )}
    {fileConfig.type === 'Excel' && (
      <>
        <Descriptions.Item label="שם הגיליון">{fileConfig.sheetName || 'ברירת מחדל (גיליון ראשון)'}</Descriptions.Item>
        <Descriptions.Item label="שורת כותרות">
          <Tag color={fileConfig.hasHeaders !== false ? 'green' : 'orange'}>{fileConfig.hasHeaders !== false ? 'כן' : 'לא'}</Tag>
        </Descriptions.Item>
      </>
    )}
  </Descriptions>
);

// Schedule Details Tab
export const ScheduleDetailsTab: React.FC<{ schedule: any }> = ({ schedule }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="תדירות"><Tag color="blue">{schedule.frequency || 'Manual'}</Tag></Descriptions.Item>
    <Descriptions.Item label="סטטוס תזמון">
      <Tag color={schedule.enabled ? 'green' : 'orange'}>{schedule.enabled ? 'מופעל' : 'מושבת'}</Tag>
    </Descriptions.Item>
    {schedule.cronExpression && (
      <Descriptions.Item label="ביטוי Cron" span={2}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Text code style={{ fontSize: '13px' }}>{schedule.cronExpression}</Text>
          <Alert message={humanizeCron(schedule.cronExpression)} type="info" showIcon icon={<ClockCircleOutlined />} />
        </Space>
      </Descriptions.Item>
    )}
  </Descriptions>
);

// Validation Details Tab
export const ValidationDetailsTab: React.FC<{ validationRules: any; schemas: any[]; dataSourceId?: string; loadingSchemas: boolean }> = ({ validationRules, schemas, dataSourceId, loadingSchemas }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="Schema מקושר" span={2}>
      {loadingSchemas ? (
        <Text type="secondary">טוען...</Text>
      ) : (
        (() => {
          const linkedSchema = schemas.find(s => s.DataSourceId === dataSourceId);
          return linkedSchema ? (
            <div>
              <Tag color="green" style={{ fontSize: '14px', padding: '4px 12px' }}>
                {linkedSchema.DisplayName || linkedSchema.Name} v{linkedSchema.Version || '1.0'}
              </Tag>
              {linkedSchema.Description && (
                <div style={{ marginTop: 8, fontSize: '12px', color: '#666' }}>{linkedSchema.Description}</div>
              )}
            </div>
          ) : (
            <div>
              <Text type="secondary">אין Schema מקושר</Text>
              <div style={{ marginTop: 8, fontSize: '12px', color: '#666' }}>
                💡 לקישור Schema, השתמש ב<a href="/schema" target="_blank">ניהול Schemas</a> ובחר את מקור הנתונים זה בעמודת "מקור נתונים".
              </div>
            </div>
          );
        })()
      )}
    </Descriptions.Item>
    <Descriptions.Item label="מקסימום שגיאות">{validationRules.maxErrorsAllowed || 'ללא הגבלה'}</Descriptions.Item>
    <Descriptions.Item label="טיפול ברשומות שגויות">
      <Tag color={validationRules.skipInvalidRecords ? 'orange' : 'red'}>
        {validationRules.skipInvalidRecords ? 'דלג על רשומות שגויות' : 'עצור בשגיאה'}
      </Tag>
    </Descriptions.Item>
  </Descriptions>
);

// Notifications Details Tab
export const NotificationsDetailsTab: React.FC<{ notificationSettings: any }> = ({ notificationSettings }) => (
  <Descriptions column={2} bordered>
    <Descriptions.Item label="התראה בהצלחה">
      <Tag color={notificationSettings.onSuccess ? 'green' : 'default'}>
        {notificationSettings.onSuccess ? 'מופעל' : 'מושבת'}
      </Tag>
    </Descriptions.Item>
    <Descriptions.Item label="התראה בכשלון">
      <Tag color={notificationSettings.onFailure ? 'red' : 'default'}>
        {notificationSettings.onFailure ? 'מופעל' : 'מושבת'}
      </Tag>
    </Descriptions.Item>
    <Descriptions.Item label="נמעני התראות" span={2}>
      {notificationSettings.recipients && notificationSettings.recipients.length > 0 ? (
        <Space wrap>
          {notificationSettings.recipients.map((email: string, idx: number) => (
            <Tag key={idx} icon="📧">{email}</Tag>
          ))}
        </Space>
      ) : (
        <Text type="secondary">לא הוגדרו נמענים</Text>
      )}
    </Descriptions.Item>
  </Descriptions>
);
