import React, { useState, useEffect } from 'react';
import { Table, Button, Space, Alert, Card, Tag, Descriptions, Spin, Typography, Empty } from 'antd';
import { PlusOutlined, EditOutlined, LineChartOutlined, BellOutlined, WarningOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { metricsApi, MetricConfiguration } from '../../../services/metrics-api-client';

const { Text } = Typography;

interface RelatedMetricsTabProps {
  dataSourceId: string;
}

const getSeverityColor = (severity?: string): string => {
  switch (severity?.toLowerCase()) {
    case 'critical': return 'red';
    case 'warning': return 'orange';
    case 'info': return 'blue';
    default: return 'default';
  }
};

const getStatusColor = (status: number): string => {
  switch (status) {
    case 0: return 'default'; // Draft
    case 1: return 'green'; // Active
    case 2: return 'orange'; // Inactive
    case 3: return 'red'; // Error
    default: return 'default';
  }
};

const getStatusLabel = (status: number): string => {
  switch (status) {
    case 0: return 'טיוטה';
    case 1: return 'פעיל';
    case 2: return 'לא פעיל';
    case 3: return 'שגיאה';
    default: return 'לא ידוע';
  }
};

export const RelatedMetricsTab: React.FC<RelatedMetricsTabProps> = ({ dataSourceId }) => {
  const navigate = useNavigate();
  const [metrics, setMetrics] = useState<MetricConfiguration[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchMetrics = React.useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const data = await metricsApi.getByDataSource(dataSourceId);
      setMetrics(data);
    } catch (err) {
      console.error('Error fetching metrics:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch metrics');
    } finally {
      setLoading(false);
    }
  }, [dataSourceId]);

  useEffect(() => {
    fetchMetrics();
  }, [fetchMetrics]);

  const handleCreateMetric = () => {
    // Navigate to metric wizard with datasource pre-selected
    navigate(`/metrics/new?dataSourceId=${dataSourceId}`);
  };

  const handleEditMetric = (metricId: string) => {
    navigate(`/metrics/edit/${metricId}?dataSourceId=${dataSourceId}`);
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
        <div style={{ marginTop: 16 }}>טוען metrics...</div>
      </div>
    );
  }

  if (error) {
    return (
      <Alert
        message="שגיאה בטעינת Metrics"
        description={error}
        type="error"
        showIcon
        action={
          <Button size="small" onClick={fetchMetrics}>
            נסה שוב
          </Button>
        }
      />
    );
  }

  const columns = [
    {
      title: 'שם Metric',
      dataIndex: 'displayName',
      key: 'displayName',
      render: (text: string, record: MetricConfiguration) => (
        <div>
          <Text strong>{text}</Text>
          <br />
          <Text type="secondary" style={{ fontSize: '12px' }}>{record.name}</Text>
        </div>
      ),
    },
    {
      title: 'קטגוריה',
      dataIndex: 'category',
      key: 'category',
      render: (text: string) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: 'סטטוס',
      dataIndex: 'status',
      key: 'status',
      render: (status: number) => (
        <Tag color={getStatusColor(status)}>{getStatusLabel(status)}</Tag>
      ),
    },
    {
      title: 'ערך אחרון',
      key: 'lastValue',
      render: (record: MetricConfiguration) => (
        record.lastValue !== undefined && record.lastValue !== null ? (
          <div>
            <Text strong>{record.lastValue.toFixed(2)}</Text>
            {record.lastCalculated && (
              <>
                <br />
                <Text type="secondary" style={{ fontSize: '11px' }}>
                  {new Date(record.lastCalculated).toLocaleString('he-IL')}
                </Text>
              </>
            )}
          </div>
        ) : (
          <Text type="secondary">-</Text>
        )
      ),
    },
    {
      title: 'פעולות',
      key: 'actions',
      render: (record: MetricConfiguration) => (
        <Space>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEditMetric(record.id)}
          >
            ערוך
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      {/* Summary and Actions */}
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <Alert
          message={
            metrics.length > 0
              ? `${metrics.length} Metrics מוגדרים למקור נתונים זה`
              : 'אין Metrics מוגדרים עדיין'
          }
          description={
            metrics.length > 0
              ? 'Metrics אלה עוקבים אחר שדות ונתונים מ מקור נתונים זה ויוצרים מדדים לניטור'
              : 'צור Metrics כדי לעקוב אחר נתונים חשובים ממקור נתונים זה'
          }
          type={metrics.length > 0 ? 'success' : 'info'}
          showIcon
          icon={<LineChartOutlined />}
        />

        <Space>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleCreateMetric}
          >
            צור Metric חדש למקור נתונים זה
          </Button>
          <Button onClick={() => navigate('/alerts')}>
            ניהול התראות
          </Button>
        </Space>

        {metrics.length === 0 ? (
          <Empty
            description="אין Metrics מוגדרים למקור נתונים זה"
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          >
            <Button type="primary" icon={<PlusOutlined />} onClick={handleCreateMetric}>
              צור Metric ראשון
            </Button>
          </Empty>
        ) : (
          <Table
            dataSource={metrics}
            columns={columns}
            rowKey="id"
            pagination={{ pageSize: 10 }}
            expandable={{
              expandedRowRender: (metric: MetricConfiguration) => (
                <Card style={{ backgroundColor: '#fafafa' }}>
                  <Descriptions column={2} bordered size="small">
                    <Descriptions.Item label="תיאור" span={2}>
                      {metric.description || 'אין תיאור'}
                    </Descriptions.Item>
                    
                    <Descriptions.Item label="Formula">
                      <Text code style={{ fontSize: '12px', wordBreak: 'break-all' }}>
                        {metric.formula}
                      </Text>
                    </Descriptions.Item>
                    
                    <Descriptions.Item label="Field Path">
                      {metric.fieldPath ? (
                        <Text code>{metric.fieldPath}</Text>
                      ) : (
                        <Text type="secondary">-</Text>
                      )}
                    </Descriptions.Item>
                    
                    {metric.prometheusType && (
                      <Descriptions.Item label="Prometheus Type">
                        <Tag>{metric.prometheusType}</Tag>
                      </Descriptions.Item>
                    )}
                    
                    {metric.labels && metric.labels.length > 0 && (
                      <Descriptions.Item label="Labels" span={2}>
                        <Space wrap>
                          {metric.labels.map((label, idx) => (
                            <Tag key={idx} color="geekblue">{label}</Tag>
                          ))}
                        </Space>
                      </Descriptions.Item>
                    )}
                    
                    {metric.retention && (
                      <Descriptions.Item label="Retention">
                        {metric.retention}
                      </Descriptions.Item>
                    )}
                    
                    <Descriptions.Item label="נוצר">
                      {new Date(metric.createdAt).toLocaleString('he-IL')}
                      <br />
                      <Text type="secondary" style={{ fontSize: '11px' }}>
                        על ידי: {metric.createdBy}
                      </Text>
                    </Descriptions.Item>
                    
                    <Descriptions.Item label="עודכן">
                      {new Date(metric.updatedAt).toLocaleString('he-IL')}
                      <br />
                      <Text type="secondary" style={{ fontSize: '11px' }}>
                        על ידי: {metric.updatedBy}
                      </Text>
                    </Descriptions.Item>
                  </Descriptions>

                  {/* Alert Rules Section */}
                  {(metric as any).alertRules && (metric as any).alertRules.length > 0 && (
                    <div style={{ marginTop: 16 }}>
                      <Alert
                        message={
                          <Space>
                            <BellOutlined />
                            <Text strong>
                              {(metric as any).alertRules.length} Alert Rules מוגדרים
                            </Text>
                          </Space>
                        }
                        type="warning"
                        showIcon
                        icon={<WarningOutlined />}
                        style={{ marginBottom: 12 }}
                      />
                      
                      <Space direction="vertical" style={{ width: '100%' }} size="small">
                        {(metric as any).alertRules.map((alert: any, idx: number) => (
                          <Card
                            key={idx}
                            size="small"
                            style={{ backgroundColor: '#fff' }}
                            title={
                              <Space>
                                <BellOutlined />
                                <Text>{alert.name || `Alert ${idx + 1}`}</Text>
                                <Tag color={getSeverityColor(alert.severity)}>
                                  {alert.severity || 'INFO'}
                                </Tag>
                              </Space>
                            }
                          >
                            <Descriptions column={2} size="small">
                              <Descriptions.Item label="Condition" span={2}>
                                <Text code style={{ fontSize: '11px' }}>
                                  {alert.condition || alert.expression}
                                </Text>
                              </Descriptions.Item>
                              
                              {alert.threshold !== undefined && (
                                <Descriptions.Item label="Threshold">
                                  <Tag>{alert.threshold}</Tag>
                                </Descriptions.Item>
                              )}
                              
                              {alert.duration && (
                                <Descriptions.Item label="Duration">
                                  {alert.duration}
                                </Descriptions.Item>
                              )}
                              
                              {alert.notificationRecipients && alert.notificationRecipients.length > 0 && (
                                <Descriptions.Item label="Recipients" span={2}>
                                  <Space wrap>
                                    {alert.notificationRecipients.map((email: string, i: number) => (
                                      <Tag key={i} icon="📧">{email}</Tag>
                                    ))}
                                  </Space>
                                </Descriptions.Item>
                              )}
                              
                              {alert.message && (
                                <Descriptions.Item label="Message" span={2}>
                                  {alert.message}
                                </Descriptions.Item>
                              )}
                            </Descriptions>
                          </Card>
                        ))}
                      </Space>
                    </div>
                  )}
                </Card>
              ),
              rowExpandable: () => true,
            }}
          />
        )}
      </Space>
    </div>
  );
};
