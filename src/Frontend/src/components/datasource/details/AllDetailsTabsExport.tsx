// Consolidated export for all details tab components
// This file is created for documentation - actual detailed tab components 
// could be extracted to individual files later if needed

import React from 'react';
import { Descriptions, Tag, Typography, Alert, Space, Table, Card } from 'antd';
import { DataSource, OutputConfiguration, OutputDestination } from '../shared/types';
import { humanizeCron, getDelimiterLabel } from '../shared/helpers';
import { ClockCircleOutlined, CloudServerOutlined, FolderOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';

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

// Output Details Tab
export const OutputDetailsTab: React.FC<{ outputConfig: OutputConfiguration | null }> = ({ outputConfig }) => {
  if (!outputConfig || !outputConfig.destinations || outputConfig.destinations.length === 0) {
    return (
      <Alert
        message="אין הגדרות פלט"
        description="לא הוגדרו יעדי פלט למקור נתונים זה. ניתן להוסיף יעדי פלט בעריכת מקור הנתונים."
        type="info"
        showIcon
      />
    );
  }

  const columns = [
    {
      title: 'סוג',
      dataIndex: 'type',
      key: 'type',
      width: 120,
      render: (type: string) => (
        <Space>
          {type === 'kafka' && <CloudServerOutlined style={{ color: '#1890ff' }} />}
          {type === 'folder' && <FolderOutlined style={{ color: '#faad14' }} />}
          <Text>{type.toUpperCase()}</Text>
        </Space>
      )
    },
    {
      title: 'שם',
      dataIndex: 'name',
      key: 'name',
      width: 150
    },
    {
      title: 'תיאור',
      dataIndex: 'description',
      key: 'description',
      width: 200,
      render: (description: string | undefined) => (
        <Text type="secondary">{description || '-'}</Text>
      )
    },
    {
      title: 'תצורה',
      key: 'config',
      render: (_: any, dest: OutputDestination) => {
        if (dest.type === 'kafka' && dest.kafkaConfig) {
          return (
            <Space direction="vertical" size="small">
              <Text type="secondary">Topic: <Text code>{dest.kafkaConfig.topic}</Text></Text>
              {dest.kafkaConfig.brokerServer && (
                <Text type="secondary">Broker: <Text code>{dest.kafkaConfig.brokerServer}</Text></Text>
              )}
            </Space>
          );
        } else if (dest.type === 'folder' && dest.folderConfig) {
          return (
            <Space direction="vertical" size="small">
              <Text type="secondary">Path: <Text code>{dest.folderConfig.path}</Text></Text>
              {dest.folderConfig.createSubfolders && dest.folderConfig.subfolderPattern && (
                <Text type="secondary">Subfolders: <Text code>{dest.folderConfig.subfolderPattern}</Text></Text>
              )}
            </Space>
          );
        }
        return <Text type="secondary">לא מוגדר</Text>;
      }
    },
    {
      title: 'פורמט',
      dataIndex: 'outputFormat',
      key: 'outputFormat',
      width: 100,
      render: (format: string | null | undefined) => (
        <Tag>{format || outputConfig.defaultOutputFormat || 'original'}</Tag>
      )
    },
    {
      title: 'כולל שגויות',
      dataIndex: 'includeInvalidRecords',
      key: 'includeInvalidRecords',
      width: 120,
      render: (include: boolean | null | undefined, dest: OutputDestination) => {
        const value = include !== null && include !== undefined ? include : outputConfig.includeInvalidRecords;
        return (
          <Tag color={value ? 'orange' : 'green'} icon={value ? <CheckCircleOutlined /> : <CloseCircleOutlined />}>
            {value ? 'כן' : 'לא'}
          </Tag>
        );
      }
    },
    {
      title: 'סטטוס',
      dataIndex: 'enabled',
      key: 'enabled',
      width: 100,
      render: (enabled: boolean) => (
        <Tag color={enabled ? 'green' : 'red'}>
          {enabled ? 'מופעל' : 'מושבת'}
        </Tag>
      )
    }
  ];

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      {/* Global Settings */}
      <Card title="הגדרות ברירת מחדל" size="small">
        <Descriptions column={2} bordered>
          <Descriptions.Item label="פורמט פלט ברירת מחדל">
            <Tag color="blue">{outputConfig.defaultOutputFormat || 'original'}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="כולל רשומות שגויות">
            <Tag color={outputConfig.includeInvalidRecords ? 'orange' : 'green'}>
              {outputConfig.includeInvalidRecords ? 'כן' : 'לא'}
            </Tag>
          </Descriptions.Item>
        </Descriptions>
      </Card>

      {/* Destinations Table */}
      <Card title={`יעדי פלט (${outputConfig.destinations.length})`} size="small">
        <Table
          dataSource={outputConfig.destinations}
          columns={columns}
          rowKey="id"
          pagination={false}
          size="small"
        />
      </Card>

      <Alert
        message="אודות יעדי פלט"
        description={
          <ul style={{ margin: 0, paddingRight: 20 }}>
            <li><strong>Kafka:</strong> שליחת נתונים ל-Message Queue לעיבוד Real-Time</li>
            <li><strong>Folder:</strong> שמירת קבצים בתיקייה (מקומית או רשת)</li>
            <li><strong>מרובה:</strong> כל קובץ יישלח לכל היעדים המופעלים במקביל</li>
            <li><strong>דריסה:</strong> כל יעד יכול לדרוס את הגדרות ברירת המחדל (פורמט, כולל שגויות)</li>
          </ul>
        }
        type="info"
        showIcon
      />
    </Space>
  );
};
