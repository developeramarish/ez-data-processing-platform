import React, { useState, useEffect } from 'react';
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
import metricsApi from '../../services/metrics-api-client';
import type { MetricConfiguration as ApiMetricConfiguration } from '../../services/metrics-api-client';

const { Title, Text } = Typography;
const { confirm } = Modal;

// UI model - maps status number to string for display
interface MetricConfiguration {
  id: string;
  name: string;
  displayName: string;
  description: string;
  category: string;
  scope: 'global' | 'datasource-specific';
  dataSourceId: string | null;
  dataSourceName?: string;
  formula: string;
  fieldPath?: string;
  labels?: string[];
  retention?: string;
  status: 'active' | 'inactive' | 'draft' | 'error';
  lastValue?: number;
  lastCalculated?: string;
  createdAt: string;
  updatedAt: string;
}

// Convert backend status number to frontend string
const mapStatusToString = (status: number): 'active' | 'inactive' | 'draft' | 'error' => {
  switch (status) {
    case 1: return 'active';
    case 2: return 'inactive';
    case 0: return 'draft';
    case 3: return 'error';
    default: return 'draft';
  }
};

// Convert frontend status string to backend number
const mapStatusToNumber = (status: string): number => {
  switch (status) {
    case 'active': return 1;
    case 'inactive': return 2;
    case 'draft': return 0;
    case 'error': return 3;
    default: return 0;
  }
};

  // Convert API response to UI model - with null safety
  const mapApiToUi = (apiMetric: ApiMetricConfiguration): MetricConfiguration => ({
    id: apiMetric.id,
    name: apiMetric.name,
    displayName: apiMetric.displayName,
    description: apiMetric.description,
    category: apiMetric.category,
    scope: apiMetric.scope,
    dataSourceId: apiMetric.dataSourceId,
    dataSourceName: apiMetric.dataSourceName,
    formula: apiMetric.formula,
    fieldPath: apiMetric.fieldPath,
    labels: apiMetric.labels,
    retention: apiMetric.retention,
    status: mapStatusToString(apiMetric.status),
    lastValue: apiMetric.lastValue,
    lastCalculated: apiMetric.lastCalculated || undefined,
    createdAt: apiMetric.createdAt || new Date().toISOString(),
    updatedAt: apiMetric.updatedAt || new Date().toISOString(),
  });

const MetricsConfigurationListConnected: React.FC = () => {
  const { t } = useTranslation();
  const [searchText, setSearchText] = useState('');
  const [loading, setLoading] = useState(false);
  const [metrics, setMetrics] = useState<MetricConfiguration[]>([]);

  // Fetch metrics from API on mount
  useEffect(() => {
    fetchMetrics();
  }, []);

  const fetchMetrics = async () => {
    setLoading(true);
    try {
      const apiMetrics = await metricsApi.getAll();
      const uiMetrics = apiMetrics.map(mapApiToUi);
      setMetrics(uiMetrics);
    } catch (error) {
      message.error('שגיאה בטעינת מדדים');
      console.error('Error fetching metrics:', error);
    } finally {
      setLoading(false);
    }
  };

  // Handle metric activation/deactivation with API persistence
  const handleToggleStatus = async (metric: MetricConfiguration) => {
    const newStatus = metric.status === 'active' ? 'inactive' : 'active';
    const newStatusNumber = mapStatusToNumber(newStatus);
    
    // Optimistic update
    setMetrics(metrics.map(m => 
      m.id === metric.id 
        ? { ...m, status: newStatus, updatedAt: new Date().toISOString() }
        : m
    ));
    
    try {
      await metricsApi.update(metric.id, {
        displayName: metric.displayName,
        description: metric.description,
        category: metric.category,
        scope: metric.scope,
        dataSourceId: metric.dataSourceId,
        dataSourceName: metric.dataSourceName,
        formula: metric.formula,
        fieldPath: metric.fieldPath,
        labels: metric.labels,
        retention: metric.retention,
        status: newStatusNumber,
        updatedBy: 'User',
      });
      
      message.success(
        newStatus === 'active'
          ? t('metrics.messages.metricActivated')
          : t('metrics.messages.metricDeactivated')
      );
    } catch (error) {
      message.error('שגיאה בעדכון סטטוס');
      // Revert optimistic update
      await fetchMetrics();
    }
  };

  // Handle metric deletion with API persistence
  const handleDelete = async (metric: MetricConfiguration) => {
    try {
      await metricsApi.delete(metric.id);
      setMetrics(metrics.filter(m => m.id !== metric.id));
      message.success(t('metrics.messages.metricDeleted'));
    } catch (error) {
      message.error('שגיאה במחיקת מדד');
      console.error('Error deleting metric:', error);
    }
  };

  // Handle metric duplication with API persistence
  const handleDuplicate = async (metric: MetricConfiguration) => {
    try {
      const duplicated = await metricsApi.duplicate(metric.id, {
        name: `${metric.name}_copy`,
        displayName: `${metric.displayName} (עותק)`,
        description: `עותק של ${metric.description}`,
        createdBy: 'User',
      });
      
      const uiMetric = mapApiToUi(duplicated);
      setMetrics([uiMetric, ...metrics]);
      message.success(t('metrics.messages.metricCreated'));
    } catch (error) {
      message.error('שגיאה בשכפול מדד');
      console.error('Error duplicating metric:', error);
    }
  };

  // Handle view history
  const handleViewHistory = (metric: MetricConfiguration) => {
    message.info('History view - coming soon');
  };

  // Handle create new metric
  const handleCreateMetric = () => {
    window.location.href = '/metrics/create';
  };

  // Create dropdown menu for each row
  const createActionMenu = (record: MetricConfiguration) => ({
    items: [
      {
        key: 'edit',
        label: t('metrics.configuration.edit'),
        icon: <EditOutlined />,
        onClick: () => {
          window.location.href = `/metrics/${record.id}/edit`;
        },
      },
      {
        key: 'duplicate',
        label: t('metrics.configuration.duplicate'),
        icon: <CopyOutlined />,
        onClick: () => {
          handleDuplicate(record);
        },
      },
      {
        key: 'history',
        label: t('metrics.configuration.viewHistory'),
        icon: <LineChartOutlined />,
        onClick: () => {
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
      render: (value: number | undefined | null) => {
        // Check for both null and undefined (MongoDB returns null, not undefined)
        if (value == null) {
          return <Text type="secondary">-</Text>;
        }
        try {
          return <Text strong>{value.toLocaleString()}</Text>;
        } catch {
          return <Text type="secondary">-</Text>;
        }
      },
    },
    {
      title: t('metrics.fields.lastCalculated'),
      dataIndex: 'lastCalculated',
      key: 'lastCalculated',
      width: 180,
      render: (date: string | undefined | null) => {
        // Explicitly check for null and undefined to avoid toLocaleString error
        if (!date || date === null) {
          return <Text type="secondary">-</Text>;
        }
        try {
          return (
            <Text type="secondary">
              {new Date(date).toLocaleString('he-IL')}
            </Text>
          );
        } catch {
          return <Text type="secondary">-</Text>;
        }
      },
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
            <Button onClick={fetchMetrics} loading={loading}>
              {t('common.refresh')}
            </Button>
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

export default MetricsConfigurationListConnected;
