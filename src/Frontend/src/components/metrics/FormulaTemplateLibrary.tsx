import React, { useState } from 'react';
import { Card, Row, Col, Tag, Typography, Space, Button, Collapse, Tooltip } from 'antd';
import {
  ThunderboltOutlined,
  DashboardOutlined,
  BarChartOutlined,
  LineChartOutlined,
  CopyOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const { Title, Text, Paragraph } = Typography;
const { Panel } = Collapse;

// Prometheus metric type definitions
export type PrometheusMetricType = 'Counter' | 'Gauge' | 'Histogram' | 'Summary';

export interface FormulaParameter {
  name: string;
  type: 'string' | 'number' | 'field' | 'datetime';
  description: string;
  descriptionHebrew: string;
  defaultValue?: any;
  required: boolean;
}

export interface FormulaTemplate {
  id: string;
  name: string;
  nameHebrew: string;
  category: string;
  prometheusType: PrometheusMetricType;
  formula: string;
  description: string;
  descriptionHebrew: string;
  parameters: FormulaParameter[];
  openTelemetryExample: string;
  promQLQuery: string;
  useCases: string[];
  useCasesHebrew: string[];
  labels?: string[];
}

// Formula templates based on Prometheus metric types
export const formulaTemplates: FormulaTemplate[] = [
  // ========== COUNTER TEMPLATES ==========
  {
    id: 'counter_daily_sum',
    name: 'Daily Sum',
    nameHebrew: 'סכום יומי',
    category: 'aggregation',
    prometheusType: 'Counter',
    formula: 'SUM({field}) GROUP BY DATE({timestamp}) WINDOW 1d',
    description: 'Calculate daily sum of a numeric field (Counter - only increases)',
    descriptionHebrew: 'חישוב סכום יומי של שדה מספרי (Counter - רק עולה)',
    parameters: [
      {
        name: 'field',
        type: 'field',
        description: 'Numeric field to sum',
        descriptionHebrew: 'שדה מספרי לסיכום',
        required: true,
      },
      {
        name: 'timestamp',
        type: 'datetime',
        description: 'Timestamp field for grouping',
        descriptionHebrew: 'שדה תאריך לקיבוץ',
        required: true,
      },
    ],
    openTelemetryExample: `var counter = meter.CreateCounter<long>("business_daily_sales_total");
counter.Add(amount, 
  new KeyValuePair<string, object>("data_source", "ds001"),
  new KeyValuePair<string, object>("date", date.ToString("yyyy-MM-dd"))
);`,
    promQLQuery: 'rate(business_daily_sales_total[5m])',
    useCases: [
      'Total daily sales',
      'Number of processed files per day',
      'Total transactions per day',
    ],
    useCasesHebrew: [
      'סך מכירות יומי',
      'מספר קבצים מעובדים ליום',
      'סך עסקאות ליום',
    ],
    labels: ['data_source', 'date'],
  },
  {
    id: 'counter_total_count',
    name: 'Total Count',
    nameHebrew: 'ספירה כוללת',
    category: 'aggregation',
    prometheusType: 'Counter',
    formula: 'COUNT(*) WHERE {condition}',
    description: 'Count total number of records (Counter - monotonically increasing)',
    descriptionHebrew: 'ספירת מספר רשומות כולל (Counter - רק עולה)',
    parameters: [
      {
        name: 'condition',
        type: 'string',
        description: 'Filter condition',
        descriptionHebrew: 'תנאי סינון',
        defaultValue: 'status="completed"',
        required: false,
      },
    ],
    openTelemetryExample: `var counter = meter.CreateCounter<long>("business_records_processed_total");
counter.Add(1,
  new KeyValuePair<string, object>("data_source", dataSource),
  new KeyValuePair<string, object>("status", "completed")
);`,
    promQLQuery: 'rate(business_records_processed_total[1m])',
    useCases: [
      'Total HTTP requests',
      'Total processed records',
      'Total errors occurred',
    ],
    useCasesHebrew: [
      'סך בקשות HTTP',
      'סך רשומות מעובדות',
      'סך שגיאות שהתרחשו',
    ],
    labels: ['data_source', 'status'],
  },
  {
    id: 'counter_success_rate',
    name: 'Success Rate',
    nameHebrew: 'שיעור הצלחה',
    category: 'percentage',
    prometheusType: 'Counter',
    formula: '(COUNT(*) WHERE {success_condition}) / COUNT(*) * 100',
    description: 'Calculate percentage of successful operations (uses Counter)',
    descriptionHebrew: 'חישוב אחוז פעולות מוצלחות (משתמש ב-Counter)',
    parameters: [
      {
        name: 'success_condition',
        type: 'string',
        description: 'Condition for success',
        descriptionHebrew: 'תנאי להצלחה',
        defaultValue: 'status="success"',
        required: true,
      },
    ],
    openTelemetryExample: `// Record both success and total
var successCounter = meter.CreateCounter<long>("business_operations_success_total");
var totalCounter = meter.CreateCounter<long>("business_operations_total");

successCounter.Add(isSuccess ? 1 : 0);
totalCounter.Add(1);

// PromQL calculates rate: rate(success) / rate(total) * 100`,
    promQLQuery: '(rate(business_operations_success_total[5m]) / rate(business_operations_total[5m])) * 100',
    useCases: [
      'Processing success rate',
      'Payment success rate',
      'Validation success rate',
    ],
    useCasesHebrew: [
      'שיעור הצלחת עיבוד',
      'שיעור הצלחת תשלומים',
      'שיעור הצלחת אימות',
    ],
    labels: ['data_source', 'operation_type'],
  },

  // ========== GAUGE TEMPLATES ==========
  {
    id: 'gauge_current_value',
    name: 'Current Value',
    nameHebrew: 'ערך נוכחי',
    category: 'snapshot',
    prometheusType: 'Gauge',
    formula: 'LAST({field})',
    description: 'Get current value of a metric (Gauge - can go up or down)',
    descriptionHebrew: 'קבלת ערך נוכחי של מדד (Gauge - יכול לעלות או לרדת)',
    parameters: [
      {
        name: 'field',
        type: 'field',
        description: 'Field to measure',
        descriptionHebrew: 'שדה למדידה',
        required: true,
      },
    ],
    openTelemetryExample: `var gauge = meter.CreateObservableGauge<double>("business_queue_depth", 
  () => new Measurement<double>(GetCurrentQueueDepth(),
    new KeyValuePair<string, object>("queue_name", "processing")
  )
);`,
    promQLQuery: 'business_queue_depth{queue_name="processing"}',
    useCases: [
      'Current queue depth',
      'Active connections',
      'Current temperature',
      'Available memory',
    ],
    useCasesHebrew: [
      'עומק תור נוכחי',
      'חיבורים פעילים',
      'טמפרטורה נוכחית',
      'זיכרון פנוי',
    ],
    labels: ['resource_name'],
  },
  {
    id: 'gauge_avg_last_n',
    name: 'Average (Last N)',
    nameHebrew: 'ממוצע (N אחרונים)',
    category: 'statistical',
    prometheusType: 'Gauge',
    formula: 'AVG({field}) OVER (ORDER BY {timestamp} ROWS {window} PRECEDING)',
    description: 'Calculate average over last N records (Gauge)',
    descriptionHebrew: 'חישוב ממוצע על N רשומות אחרונות (Gauge)',
    parameters: [
      {
        name: 'field',
        type: 'field',
        description: 'Numeric field',
        descriptionHebrew: 'שדה מספרי',
        required: true,
      },
      {
        name: 'timestamp',
        type: 'datetime',
        description: 'Time field for ordering',
        descriptionHebrew: 'שדה זמן למיון',
        required: true,
      },
      {
        name: 'window',
        type: 'number',
        description: 'Window size (number of records)',
        descriptionHebrew: 'גודל חלון (מספר רשומות)',
        defaultValue: 10,
        required: true,
      },
    ],
    openTelemetryExample: `var gauge = meter.CreateObservableGauge<double>("business_avg_processing_time",
  () => new Measurement<double>(CalculateAvgLast10(),
    new KeyValuePair<string, object>("data_source", "ds001")
  )
);`,
    promQLQuery: 'avg_over_time(business_avg_processing_time[5m])',
    useCases: [
      'Average response time (last 10 requests)',
      'Average CPU usage (last hour)',
      'Moving average of sales',
    ],
    useCasesHebrew: [
      'זמן תגובה ממוצע (10 בקשות אחרונות)',
      'שימוש CPU ממוצע (שעה אחרונה)',
      'ממוצע נע של מכירות',
    ],
    labels: ['data_source', 'metric_type'],
  },

  // ========== HISTOGRAM TEMPLATES ==========
  {
    id: 'histogram_duration',
    name: 'Duration/Latency Distribution',
    nameHebrew: 'התפלגות זמן/לטנסי',
    category: 'time',
    prometheusType: 'Histogram',
    formula: 'DATEDIFF(second, {start_time}, {end_time})',
    description: 'Measure duration distribution (Histogram - with buckets)',
    descriptionHebrew: 'מדידת התפלגות זמנים (Histogram - עם buckets)',
    parameters: [
      {
        name: 'start_time',
        type: 'datetime',
        description: 'Process start time',
        descriptionHebrew: 'זמן התחלה',
        required: true,
      },
      {
        name: 'end_time',
        type: 'datetime',
        description: 'Process end time',
        descriptionHebrew: 'זמן סיום',
        required: true,
      },
    ],
    openTelemetryExample: `var histogram = meter.CreateHistogram<double>("business_file_processing_duration_seconds");
var duration = (endTime - startTime).TotalSeconds;
histogram.Record(duration,
  new KeyValuePair<string, object>("data_source", "ds001"),
  new KeyValuePair<string, object>("file_type", "csv")
);

// Prometheus auto-creates _bucket, _sum, _count`,
    promQLQuery: 'histogram_quantile(0.95, rate(business_file_processing_duration_seconds_bucket[5m]))',
    useCases: [
      'HTTP request duration',
      'File processing time',
      'Database query latency',
      'API response time',
    ],
    useCasesHebrew: [
      'זמן בקשת HTTP',
      'זמן עיבוד קובץ',
      'לטנסי שאילתת DB',
      'זמן תגובת API',
    ],
    labels: ['data_source', 'operation', 'status'],
  },
  {
    id: 'histogram_size',
    name: 'Size Distribution',
    nameHebrew: 'התפלגות גדלים',
    category: 'measurement',
    prometheusType: 'Histogram',
    formula: 'SIZE({field})',
    description: 'Measure size/amount distribution (Histogram)',
    descriptionHebrew: 'מדידת התפלגות גודל/כמות (Histogram)',
    parameters: [
      {
        name: 'field',
        type: 'field',
        description: 'Field with size/amount data',
        descriptionHebrew: 'שדה עם נתוני גודל/כמות',
        required: true,
      },
    ],
    openTelemetryExample: `var histogram = meter.CreateHistogram<long>("business_transaction_amount_bytes");
histogram.Record(transactionAmount,
  new KeyValuePair<string, object>("currency", "ILS"),
  new KeyValuePair<string, object>("payment_method", "credit_card")
);`,
    promQLQuery: 'histogram_quantile(0.99, rate(business_transaction_amount_bytes_bucket[5m]))',
    useCases: [
      'Request/Response body size',
      'File size distribution',
      'Transaction amount ranges',
    ],
    useCasesHebrew: [
      'גודל בקשה/תגובה',
      'התפלגות גודל קבצים',
      'טווחי סכומי עסקאות',
    ],
    labels: ['type', 'category'],
  },

  // ========== SUMMARY (ADVANCED) ==========
  {
    id: 'summary_percentiles',
    name: 'Pre-calculated Percentiles',
    nameHebrew: 'אחוזונים מחושבים מראש',
    category: 'statistical',
    prometheusType: 'Summary',
    formula: 'PERCENTILE({field}, {percentile_value})',
    description: 'Calculate percentiles on client-side (Summary)',
    descriptionHebrew: 'חישוב אחוזונים בצד הלקוח (Summary)',
    parameters: [
      {
        name: 'field',
        type: 'field',
        description: 'Numeric field',
        descriptionHebrew: 'שדה מספרי',
        required: true,
      },
      {
        name: 'percentile_value',
        type: 'number',
        description: 'Percentile (0-100)',
        descriptionHebrew: 'אחוזון (0-100)',
        defaultValue: 95,
        required: true,
      },
    ],
    openTelemetryExample: `// Note: Summary is less common in OpenTelemetry
// Usually Histogram is preferred
var summary = meter.CreateHistogram<double>("business_response_time_summary");
summary.Record(responseTime);

// Query specific quantiles from Prometheus`,
    promQLQuery: 'business_response_time_summary{quantile="0.95"}',
    useCases: [
      '95th percentile response time',
      '99th percentile latency',
      'Median processing time',
    ],
    useCasesHebrew: [
      'אחוזון 95 זמן תגובה',
      'אחוזון 99 לטנסי',
      'חציון זמן עיבוד',
    ],
    labels: ['service', 'endpoint'],
  },
];

interface FormulaTemplateLibraryProps {
  onTemplateSelect?: (template: FormulaTemplate) => void;
}

const FormulaTemplateLibrary: React.FC<FormulaTemplateLibraryProps> = ({
  onTemplateSelect,
}) => {
  const { t } = useTranslation();
  const [selectedMetricType, setSelectedMetricType] = useState<PrometheusMetricType | null>(null);

  const metricTypes: PrometheusMetricType[] = ['Counter', 'Gauge', 'Histogram', 'Summary'];

  // Filter templates
  const filteredTemplates = formulaTemplates.filter(template => {
    if (selectedMetricType && template.prometheusType !== selectedMetricType) return false;
    return true;
  });

  // Get metric type icon and color
  const getMetricTypeConfig = (type: PrometheusMetricType) => {
    switch (type) {
      case 'Counter':
        return { icon: <ThunderboltOutlined />, color: 'blue', description: 'Only increases (e.g., requests, errors)' };
      case 'Gauge':
        return { icon: <DashboardOutlined />, color: 'green', description: 'Can go up or down (e.g., memory, connections)' };
      case 'Histogram':
        return { icon: <BarChartOutlined />, color: 'orange', description: 'Distribution with buckets (e.g., latency, sizes)' };
      case 'Summary':
        return { icon: <LineChartOutlined />, color: 'purple', description: 'Pre-calculated percentiles' };
    }
  };

  const handleTemplateUse = (template: FormulaTemplate) => {
    if (onTemplateSelect) {
      onTemplateSelect(template);
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Title level={3}>{t('metrics.templates.title')}</Title>
      <Paragraph type="secondary">
        {t('metrics.templates.subtitle')}
      </Paragraph>

      {/* Filters */}
      <Space size="middle" style={{ marginBottom: '24px' }}>
        <div>
          <Text strong>Prometheus Metric Type:</Text>
          <div style={{ marginTop: '8px' }}>
            <Space>
              <Button
                size="small"
                type={selectedMetricType === null ? 'primary' : 'default'}
                onClick={() => setSelectedMetricType(null)}
              >
                All Types
              </Button>
              {metricTypes.map(type => {
                const config = getMetricTypeConfig(type);
                return (
                  <Tooltip key={type} title={config.description}>
                    <Button
                      size="small"
                      type={selectedMetricType === type ? 'primary' : 'default'}
                      icon={config.icon}
                      onClick={() => setSelectedMetricType(type)}
                    >
                      {type}
                    </Button>
                  </Tooltip>
                );
              })}
            </Space>
          </div>
        </div>
      </Space>

      {/* Templates Grid */}
      <Row gutter={[16, 16]}>
        {filteredTemplates.map(template => {
          const metricConfig = getMetricTypeConfig(template.prometheusType);
          return (
            <Col xs={24} sm={12} lg={8} key={template.id}>
              <Card
                hoverable
                title={
                  <Space>
                    {metricConfig.icon}
                    <span>{template.nameHebrew}</span>
                  </Space>
                }
                extra={
                  <Tag color={metricConfig.color}>{template.prometheusType}</Tag>
                }
                actions={[
                  <Button
                    type="link"
                    icon={<CopyOutlined />}
                    onClick={() => handleTemplateUse(template)}
                  >
                    {t('metrics.formula.useTemplate')}
                  </Button>,
                ]}
              >
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    {template.descriptionHebrew}
                  </Text>
                  
                  <div>
                    <Text strong style={{ fontSize: '11px' }}>Formula:</Text>
                    <pre style={{
                      background: '#f5f5f5',
                      padding: '8px',
                      borderRadius: '4px',
                      fontSize: '11px',
                      margin: '4px 0',
                      overflow: 'auto',
                    }}>
                      {template.formula}
                    </pre>
                  </div>

                  <Collapse ghost size="small">
                    <Panel header={<Text type="secondary" style={{ fontSize: '11px' }}>OpenTelemetry Example</Text>} key="1">
                      <pre style={{
                        background: '#f9f9f9',
                        padding: '8px',
                        borderRadius: '4px',
                        fontSize: '10px',
                        overflow: 'auto',
                        maxHeight: '150px',
                      }}>
                        {template.openTelemetryExample}
                      </pre>
                    </Panel>
                    <Panel header={<Text type="secondary" style={{ fontSize: '11px' }}>PromQL Query</Text>} key="2">
                      <pre style={{
                        background: '#f9f9f9',
                        padding: '8px',
                        borderRadius: '4px',
                        fontSize: '10px',
                        overflow: 'auto',
                      }}>
                        {template.promQLQuery}
                      </pre>
                    </Panel>
                    <Panel header={<Text type="secondary" style={{ fontSize: '11px' }}>Use Cases</Text>} key="3">
                      <ul style={{ margin: 0, paddingLeft: '20px', fontSize: '11px' }}>
                        {template.useCasesHebrew.map((useCase, index) => (
                          <li key={index}>{useCase}</li>
                        ))}
                      </ul>
                    </Panel>
                  </Collapse>

                  {template.labels && template.labels.length > 0 && (
                    <div style={{ marginTop: '8px' }}>
                      <Text type="secondary" style={{ fontSize: '11px' }}>Labels: </Text>
                      {template.labels.map(label => (
                        <Tag key={label} style={{ fontSize: '10px' }}>{label}</Tag>
                      ))}
                    </div>
                  )}
                </Space>
              </Card>
            </Col>
          );
        })}
      </Row>

      {filteredTemplates.length === 0 && (
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Text type="secondary">No templates match the selected filters</Text>
        </div>
      )}
    </div>
  );
};

export default FormulaTemplateLibrary;
