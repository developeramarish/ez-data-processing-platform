import React, { useState, useEffect } from 'react';
import { Card, Button, Space, message, Tag, Empty, Typography, Spin, Modal, Alert } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, EyeOutlined, BellOutlined, ReloadOutlined } from '@ant-design/icons';
import metricsApi, { type MetricConfiguration } from '../../../services/metrics-api-client';

const { Text } = Typography;

interface MetricsTabProps {
  dataSourceId: string;
  dataSourceName: string;
  onCreateMetric: () => void;
  onEditMetric: (metricId: string) => void;
}

export const MetricsTab: React.FC<MetricsTabProps> = ({
  dataSourceId,
  dataSourceName,
  onCreateMetric,
  onEditMetric
}) => {
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<MetricConfiguration[]>([]);
  const [viewingMetric, setViewingMetric] = useState<MetricConfiguration | null>(null);

  const loadMetrics = async () => {
    if (!dataSourceId) return;

    setLoading(true);
    try {
      const data = await metricsApi.getByDataSource(dataSourceId);
      setMetrics(data);
    } catch (error) {
      console.error('Error loading metrics:', error);
      message.error('שגיאה בטעינת מדדים');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMetrics();
  }, [dataSourceId]);

  const handleDelete = async (id: string, name: string) => {
    try {
      await metricsApi.delete(id);
      message.success(`המדד "${name}" נמחק בהצלחה`);
      loadMetrics();
    } catch (error) {
      message.error('שגיאה במחיקת המדד');
      console.error('Error deleting metric:', error);
    }
  };

  const getStatusTag = (status: number) => {
    const statusMap: Record<number, { text: string; color: string }> = {
      0: { text: 'טיוטה', color: 'default' },
      1: { text: 'פעיל', color: 'green' },
      2: { text: 'לא פעיל', color: 'red' },
      3: { text: 'שגיאה', color: 'orange' }
    };
    const info = statusMap[status] || { text: 'לא ידוע', color: 'default' };
    return <Tag color={info.color}>{info.text}</Tag>;
  };

  const getPrometheusTypeTag = (type?: string) => {
    if (!type) return null;
    const typeMap: Record<string, string> = {
      gauge: 'Gauge',
      counter: 'Counter',
      histogram: 'Histogram',
      summary: 'Summary'
    };
    return <Tag color="blue">{typeMap[type] || type}</Tag>;
  };

  const renderMetricCard = (metric: MetricConfiguration) => {
    const alertRules = (metric as any).alertRules || [];
    const hasAlerts = alertRules.length > 0;

    return (
      <Card
        key={metric.id}
        size="small"
        style={{ marginBottom: 8 }}
        bodyStyle={{ padding: '12px 16px' }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Space direction="vertical" size={4} style={{ flex: 1 }}>
            <Space size="small" wrap>
              <Text strong>{metric.displayName}</Text>
              {getStatusTag(metric.status)}
              {getPrometheusTypeTag(metric.prometheusType)}
              {metric.category && <Tag color="purple">{metric.category}</Tag>}
              {hasAlerts && (
                <Tag color="orange" icon={<BellOutlined />}>
                  {alertRules.length} התראות
                </Tag>
              )}
            </Space>
            <Space size="small" wrap>
              <Tag style={{ fontFamily: 'monospace', fontSize: 11 }}>{metric.name}</Tag>
              {metric.fieldPath && <Tag color="cyan">{metric.fieldPath}</Tag>}
              {metric.labels && metric.labels.length > 0 && (
                <Text type="secondary" style={{ fontSize: 11 }}>
                  תוויות: {metric.labels.join(', ')}
                </Text>
              )}
            </Space>
            {metric.description && (
              <Text type="secondary" style={{ fontSize: 12 }}>{metric.description}</Text>
            )}
          </Space>
          <Space size="small">
            <Button
              type="text"
              size="small"
              icon={<EyeOutlined />}
              onClick={() => setViewingMetric(metric)}
              title="צפה"
            />
            <Button
              type="text"
              size="small"
              icon={<EditOutlined />}
              onClick={() => onEditMetric(metric.id)}
              title="ערוך"
            />
            <Button
              type="text"
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={() => {
                if (window.confirm(`האם למחוק את המדד "${metric.displayName}"?`)) {
                  handleDelete(metric.id, metric.displayName);
                }
              }}
              title="מחק"
            />
          </Space>
        </div>
      </Card>
    );
  };

  return (
    <>
      <Alert
        message="מדדי חילוץ שדות"
        description="הגדר מדדים שיחלצו ערכים משדות ברשומות המעובדות ממקור נתונים זה. המדדים יישלחו ל-Prometheus ויהיו זמינים בדשבורד Grafana."
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Text strong>מדדים מוגדרים ({metrics.length})</Text>
        <Space>
          <Button
            icon={<ReloadOutlined />}
            onClick={loadMetrics}
            loading={loading}
            size="small"
          >
            רענן
          </Button>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={onCreateMetric}
          >
            יצירת מדד
          </Button>
        </Space>
      </div>

      <Spin spinning={loading}>
        {metrics.length === 0 ? (
          <Empty
            description="אין מדדים מוגדרים למקור נתונים זה"
            style={{ padding: '40px 0' }}
          >
            <Button type="primary" icon={<PlusOutlined />} onClick={onCreateMetric}>
              יצירת מדד ראשון
            </Button>
          </Empty>
        ) : (
          <Space direction="vertical" style={{ width: '100%' }}>
            {metrics.map(renderMetricCard)}
          </Space>
        )}
      </Spin>

      {/* View Metric Details Modal */}
      <Modal
        title={viewingMetric ? viewingMetric.displayName : ''}
        open={!!viewingMetric}
        onCancel={() => setViewingMetric(null)}
        footer={[
          <Button key="close" onClick={() => setViewingMetric(null)}>
            סגור
          </Button>,
          <Button
            key="edit"
            type="primary"
            icon={<EditOutlined />}
            onClick={() => {
              if (viewingMetric) {
                setViewingMetric(null);
                onEditMetric(viewingMetric.id);
              }
            }}
          >
            ערוך
          </Button>
        ]}
        width={600}
      >
        {viewingMetric && (
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            <Card size="small" title="פרטי מדד">
              <Space direction="vertical" size="small" style={{ width: '100%' }}>
                <div>
                  <Text type="secondary">שם Prometheus:</Text><br />
                  <Tag style={{ fontFamily: 'monospace' }}>{viewingMetric.name}</Tag>
                </div>
                <div>
                  <Text type="secondary">סוג:</Text><br />
                  {getPrometheusTypeTag(viewingMetric.prometheusType)}
                </div>
                <div>
                  <Text type="secondary">סטטוס:</Text><br />
                  {getStatusTag(viewingMetric.status)}
                </div>
                {viewingMetric.description && (
                  <div>
                    <Text type="secondary">תיאור:</Text><br />
                    <Text>{viewingMetric.description}</Text>
                  </div>
                )}
                {viewingMetric.fieldPath && (
                  <div>
                    <Text type="secondary">שדה למדידה:</Text><br />
                    <Tag color="cyan">{viewingMetric.fieldPath}</Tag>
                  </div>
                )}
                {viewingMetric.category && (
                  <div>
                    <Text type="secondary">קטגוריה:</Text><br />
                    <Tag color="purple">{viewingMetric.category}</Tag>
                  </div>
                )}
                {viewingMetric.labels && viewingMetric.labels.length > 0 && (
                  <div>
                    <Text type="secondary">תוויות:</Text><br />
                    <Space wrap>
                      {viewingMetric.labels.map((label, idx) => (
                        <Tag key={idx}>{label}</Tag>
                      ))}
                    </Space>
                  </div>
                )}
              </Space>
            </Card>

            {/* Alert Rules */}
            {(viewingMetric as any).alertRules && (viewingMetric as any).alertRules.length > 0 && (
              <Card size="small" title={
                <Space>
                  <BellOutlined />
                  <span>כללי התראה ({(viewingMetric as any).alertRules.length})</span>
                </Space>
              }>
                <Space direction="vertical" style={{ width: '100%' }} size="small">
                  {(viewingMetric as any).alertRules.map((rule: any, idx: number) => (
                    <Card key={idx} size="small" type="inner">
                      <Space direction="vertical" size={4} style={{ width: '100%' }}>
                        <Space>
                          <Text strong>{rule.name}</Text>
                          <Tag color={rule.severity === 'critical' ? 'red' : rule.severity === 'warning' ? 'orange' : 'blue'}>
                            {rule.severity}
                          </Tag>
                          {!rule.isEnabled && <Tag>מושבת</Tag>}
                        </Space>
                        <Text type="secondary" style={{ fontSize: 11 }}>{rule.description}</Text>
                        <Card size="small" style={{ backgroundColor: '#f6f8fa' }}>
                          <pre style={{ margin: 0, fontSize: 10, fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                            {rule.expression}
                          </pre>
                        </Card>
                      </Space>
                    </Card>
                  ))}
                </Space>
              </Card>
            )}
          </Space>
        )}
      </Modal>
    </>
  );
};

export default MetricsTab;
