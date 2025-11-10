import React, { useState } from 'react';
import {
  Card,
  Table,
  Button,
  Input,
  Tag,
  Space,
  Dropdown,
  Typography,
  Empty,
  Badge,
  Tooltip,
  Modal,
  message,
  Popconfirm,
} from 'antd';
import {
  PlusOutlined,
  SearchOutlined,
  MoreOutlined,
  EditOutlined,
  DeleteOutlined,
  CopyOutlined,
  PlayCircleOutlined,
  PauseCircleOutlined,
  LineChartOutlined,
  ExclamationCircleOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { ColumnsType } from 'antd/es/table';

const { Title, Text } = Typography;
const { confirm } = Modal;

// Data model interface with data source relationships
interface MetricConfiguration {
  id: string;
  name: string;
  displayName: string;
  description: string;
  category: string;
  scope: 'global' | 'datasource-specific';  // Global applies to all, specific to one
  dataSourceId: string | null;  // null for global, specific ID for custom
  dataSourceName?: string;  // Display name
  formula: string;
  fieldPath?: string;  // JSON path like $.amount for data extraction
  labels?: string[];  // For grouping: data_source, status, etc.
  retention?: string;  // How long to keep data: 7d, 30d, 90d
  status: 'active' | 'inactive' | 'draft' | 'error';
  lastValue?: number;
  lastCalculated?: string;
  createdAt: string;
  updatedAt: string;
}

// Mock data with both global and custom metrics
const mockMetrics: MetricConfiguration[] = [
  {
    id: 'metric_global_001',
    name: 'files_processed_count',
    displayName: 'ספירת קבצים מעובדים',
    description: 'ספירת קבצים שעובדו לכל מחזור polling (כל מקורות הנתונים)',
    category: 'performance',
    scope: 'global',
    dataSourceId: null,
    formula: 'COUNT(*) WHERE status="processed"',
    labels: ['data_source', 'status'],
    retention: '30d',
    status: 'active',
    lastValue: 1234,
    lastCalculated: new Date().toISOString(),
    createdAt: '2025-10-01T10:00:00Z',
    updatedAt: '2025-10-16T10:00:00Z',
  },
  {
    id: 'metric_global_002',
    name: 'processing_duration_avg',
    displayName: 'ממוצע זמן עיבוד',
    description: 'זמן ממוצע שנדרש לעיבוד קבצים (כל מקורות הנתונים)',
    category: 'performance',
    scope: 'global',
    dataSourceId: null,
    formula: 'AVG(processing_time_seconds)',
    labels: ['data_source'],
    retention: '7d',
    status: 'active',
    lastValue: 45.3,
    lastCalculated: new Date().toISOString(),
    createdAt: '2025-10-01T10:00:00Z',
    updatedAt: '2025-10-16T10:00:00Z',
  },
  {
    id: 'metric_ds001_001',
    name: 'transaction_amount_total',
    displayName: 'סכום עסקאות כולל',
    description: 'סכום כולל של עסקאות מעובדות',
    category: 'financial',
    scope: 'datasource-specific',
    dataSourceId: 'ds001',
    dataSourceName: 'בנק לאומי - עסקאות',
    formula: 'SUM(amount) WHERE status="completed"',
    fieldPath: '$.amount',
    labels: ['payment_method'],
    retention: '30d',
    status: 'active',
    lastValue: 1250000,
    lastCalculated: new Date().toISOString(),
    createdAt: '2025-10-05T10:00:00Z',
    updatedAt: '2025-10-16T10:00:00Z',
  },
  {
    id: 'metric_ds002_001',
    name: 'validation_error_rate',
    displayName: 'שיעור שגיאות אימות',
    description: 'אחוז רשומות עם שגיאות אימות',
    category: 'quality',
    scope: 'datasource-specific',
    dataSourceId: 'ds002',
    dataSourceName: 'מערכת CRM - לקוחות',
    formula: '(COUNT(*) WHERE has_errors=true / COUNT(*)) * 100',
    labels: ['error_type'],
    retention: '7d',
    status: 'active',
    lastValue: 2.5,
    lastCalculated: new Date().toISOString(),
    createdAt: '2025-10-10T10:00:00Z',
    updatedAt: '2025-10-16T10:00:00Z',
  },
];

const MetricsConfigurationList: React.FC = () => {
  const { t } = useTranslation();
  const [searchText, setSearchText] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<MetricConfiguration[]>(mockMetrics);

  // Handle metric activation/deactivation with state update
  const handleToggleStatus = (metric: MetricConfiguration) => {
    const newStatus = metric.status === 'active' ? 'inactive' : 'active';
    
    // Update state immediately (optimistic update)
    setMetrics(metrics.map(m => 
      m.id === metric.id 
        ? { ...m, status: newStatus, updatedAt: new Date().toISOString() }
        : m
    ));
    
    message.success(
      newStatus === 'active'
        ? t('metrics.messages.metricActivated')
        : t('metrics.messages.metricDeactivated')
    );
    // TODO: API call to toggle status
  };

  // Handle metric deletion - simplified for Popconfirm
  const handleDelete = (metric: MetricConfiguration) => {
    console.log('Deleting metric:', metric.id);
    message.success(t('metrics.messages.metricDeleted'));
    setMetrics(metrics.filter(m => m.id !== metric.id));
    // TODO: API call to delete
  };

  // Handle metric duplication - direct without extra modal
  const handleDuplicate = (metric: MetricConfiguration) => {
    console.log('Duplicating metric:', metric.id);
    // Generate new ID with timestamp to ensure uniqueness
    const newId = `${metric.scope === 'global' ? 'metric_global' : `metric_${metric.dataSourceId}`}_${Date.now()}`;
    const duplicatedMetric: MetricConfiguration = {
      ...metric,
      id: newId,
      name: `${metric.name}_copy`,
      displayName: `${metric.displayName} (עותק)`,
      status: 'draft',  // New duplicated metrics start as draft
      lastValue: undefined,
      lastCalculated: undefined,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    
    // Add to metrics list at TOP (UI updates immediately)
    setMetrics([duplicatedMetric, ...metrics]);
    message.success(t('metrics.messages.metricCreated'));
    // TODO: API call to persist
  };

  // Handle view history
  const handleViewHistory = (metric: MetricConfiguration) => {
    // TODO: Navigate to history view
    message.info('History view - coming soon');
  };

  // Handle create new metric
  const handleCreateMetric = () => {
    // TODO: Navigate to create form
    window.location.href = '/metrics/create';
  };

  // Create dropdown menu for each row - with FIXED handlers (no nested modals)
  const createActionMenu = (record: MetricConfiguration) => ({
    items: [
      {
        key: 'edit',
        label: t('metrics.configuration.edit'),
        icon: <EditOutlined />,
        onClick: () => {
          console.log('Edit clicked');
          window.location.href = `/metrics/${record.id}/edit`;
        },
      },
      {
        key: 'duplicate',
        label: t('metrics.configuration.duplicate'),
        icon: <CopyOutlined />,
        onClick: () => {
          console.log('Duplicate clicked');
          handleDuplicate(record);
        },
      },
      {
        key: 'history',
        label: t('metrics.configuration.viewHistory'),
        icon: <LineChartOutlined />,
        onClick: () => {
          console.log('History clicked');
          handleViewHistory(record);
        },
      },
      {
        type: 'divider' as const,
      },
      {
        key: 'delete-item',
        label: (
          <Popconfirm
            title={t('metrics.configuration.delete')}
            description={t('metrics.messages.confirmDelete')}
            onConfirm={(e) => {
              e?.stopPropagation();
              handleDelete(record);
            }}
            onCancel={(e) => e?.stopPropagation()}
            okText={t('common.yes')}
            cancelText={t('common.no')}
            okType="danger"
          >
            <span style={{ color: '#ff4d4f' }}>
              <DeleteOutlined /> {t('metrics.configuration.delete')}
            </span>
          </Popconfirm>
        ),
        danger: true,
      },
    ],
  });

  // Table columns definition
  const columns: ColumnsType<MetricConfiguration> = [
    {
      title: t('metrics.fields.name'),
      dataIndex: 'displayName',
      key: 'displayName',
      width: 250,
      render: (text: string, record: MetricConfiguration) => (
        <Space direction="vertical" size={0}>
          <Text strong>{text}</Text>
          <Text type="secondary" style={{ fontSize: '12px' }}>
            {record.name}
          </Text>
        </Space>
      ),
      sorter: (a, b) => a.displayName.localeCompare(b.displayName),
    },
    {
      title: t('metrics.fields.category'),
      dataIndex: 'category',
      key: 'category',
      width: 120,
      render: (category: string) => (
        <Tag color="blue">{t(`metrics.categories.${category}`)}</Tag>
      ),
      filters: [
        { text: t('metrics.categories.performance'), value: 'performance' },
        { text: t('metrics.categories.quality'), value: 'quality' },
        { text: t('metrics.categories.efficiency'), value: 'efficiency' },
        { text: t('metrics.categories.financial'), value: 'financial' },
        { text: t('metrics.categories.operations'), value: 'operations' },
        { text: t('metrics.categories.customer'), value: 'customer' },
        { text: t('metrics.categories.custom'), value: 'custom' },
      ],
      onFilter: (value, record) => record.category === value,
    },
    {
      title: 'היקף / Data Source',
      key: 'scope',
      width: 200,
      render: (_: any, record: MetricConfiguration) => (
        <Space direction="vertical" size={0}>
          <Tag color={record.scope === 'global' ? 'gold' : 'green'}>
            {record.scope === 'global' ? 'כללי (Global)' : 'ספציפי (Custom)'}
          </Tag>
          {record.scope === 'datasource-specific' && record.dataSourceName && (
            <Text type="secondary" style={{ fontSize: '11px' }}>
              {record.dataSourceName}
            </Text>
          )}
          {record.scope === 'global' && (
            <Text type="secondary" style={{ fontSize: '11px' }}>
              כל מקורות הנתונים
            </Text>
          )}
        </Space>
      ),
      filters: [
        { text: 'כללי (Global)', value: 'global' },
        { text: 'ספציפי (Custom)', value: 'datasource-specific' },
      ],
      onFilter: (value, record) => record.scope === value,
    },
    {
      title: t('metrics.fields.status'),
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (status: string) => {
        const statusConfig = {
          active: { color: 'success', text: t('metrics.status.active') },
          inactive: { color: 'default', text: t('metrics.status.inactive') },
          draft: { color: 'warning', text: t('metrics.status.draft') },
          error: { color: 'error', text: t('metrics.status.error') },
        };
        const config = statusConfig[status as keyof typeof statusConfig];
        return <Badge status={config.color as any} text={config.text} />;
      },
      filters: [
        { text: t('metrics.status.active'), value: 'active' },
        { text: t('metrics.status.inactive'), value: 'inactive' },
        { text: t('metrics.status.draft'), value: 'draft' },
        { text: t('metrics.status.error'), value: 'error' },
      ],
      onFilter: (value, record) => record.status === value,
    },
    {
      title: t('metrics.fields.lastValue'),
      dataIndex: 'lastValue',
      key: 'lastValue',
      width: 120,
      align: 'right',
      render: (value: number | undefined) => (
        value !== undefined ? (
          <Text strong>{value.toLocaleString()}</Text>
        ) : (
          <Text type="secondary">-</Text>
        )
      ),
    },
    {
      title: t('metrics.fields.lastCalculated'),
      dataIndex: 'lastCalculated',
      key: 'lastCalculated',
      width: 180,
      render: (date: string | undefined) => (
        date ? (
          <Text type="secondary">
            {new Date(date).toLocaleString('he-IL')}
          </Text>
        ) : (
          <Text type="secondary">-</Text>
        )
      ),
      sorter: (a, b) => {
        if (!a.lastCalculated) return 1;
        if (!b.lastCalculated) return -1;
        return new Date(a.lastCalculated).getTime() - new Date(b.lastCalculated).getTime();
      },
    },
    {
      title: t('common.actions'),
      key: 'actions',
      width: 120,
      align: 'center',
      render: (_: any, record: MetricConfiguration) => (
        <Space>
          <Tooltip title={record.status === 'active' ? t('metrics.tooltips.deactivateMetric') : t('metrics.tooltips.activateMetric')}>
            <Button
              type="text"
              size="small"
              icon={record.status === 'active' ? <PauseCircleOutlined /> : <PlayCircleOutlined />}
              onClick={() => handleToggleStatus(record)}
            />
          </Tooltip>
          <Dropdown
            menu={createActionMenu(record)}
            trigger={['click']}
          >
            <Button type="text" size="small" icon={<MoreOutlined />} />
          </Dropdown>
        </Space>
      ),
    },
  ];

  // Filter metrics based on search text
  const filteredMetrics = metrics.filter((metric) => {
    const searchLower = searchText.toLowerCase();
    return (
      metric.name.toLowerCase().includes(searchLower) ||
      metric.displayName.toLowerCase().includes(searchLower) ||
      metric.description.toLowerCase().includes(searchLower)
    );
  });

  return (
    <div style={{ padding: '24px' }}>
      <Card>
        {/* Header */}
        <div style={{ marginBottom: '24px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
            <div>
              <Title level={2} style={{ margin: 0 }}>
                {t('metrics.title')}
              </Title>
              <Text type="secondary">{t('metrics.subtitle')}</Text>
            </div>
            <Button
              type="primary"
              size="large"
              icon={<PlusOutlined />}
              onClick={handleCreateMetric}
            >
              {t('metrics.configuration.create')}
            </Button>
          </div>

          {/* Search and Filters */}
          <Space size="middle" style={{ width: '100%' }}>
            <Input
              placeholder={t('metrics.placeholders.searchMetrics')}
              prefix={<SearchOutlined />}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              style={{ width: '300px' }}
              allowClear
            />
          </Space>
        </div>

        {/* Table */}
        {filteredMetrics.length === 0 && !loading ? (
          <Empty
            description={
              searchText
                ? t('metrics.placeholders.noMetricsConfigured')
                : t('metrics.configuration.noMetrics')
            }
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          >
            {!searchText && (
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={handleCreateMetric}
              >
                {t('metrics.configuration.create')}
              </Button>
            )}
          </Empty>
        ) : (
          <Table
            columns={columns}
            dataSource={filteredMetrics}
            rowKey="id"
            loading={loading}
            pagination={{
              pageSize: 10,
              showSizeChanger: true,
              showTotal: (total, range) =>
                `${t('common.showing')} ${range[0]}-${range[1]} ${t('common.of')} ${total} ${t('common.items')}`,
            }}
            scroll={{ x: 1200 }}
          />
        )}
      </Card>
    </div>
  );
};

export default MetricsConfigurationList;
