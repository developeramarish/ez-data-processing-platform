import React, { useEffect, useState } from 'react';
import { Row, Col, Card, Statistic, Typography, Spin, Alert } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  FileTextOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';
import { getDashboardOverview, DashboardOverview } from '../services/dashboard-api-client';

const { Title, Paragraph } = Typography;

const Dashboard: React.FC = () => {
  const { t } = useTranslation();
  const [stats, setStats] = useState<DashboardOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await getDashboardOverview();
        setStats(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch dashboard statistics');
        console.error('Error fetching dashboard stats:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
    
    // Refresh every 30 seconds
    const interval = setInterval(fetchStats, 30000);
    return () => clearInterval(interval);
  }, []);

  if (loading && !stats) {
    return (
      <div style={{ textAlign: 'center', padding: '100px 0' }}>
        <Spin size="large" />
        <Paragraph style={{ marginTop: 16 }}>{t('common.loading')}</Paragraph>
      </div>
    );
  }

  if (error) {
    return (
      <Alert
        message="Error Loading Dashboard"
        description={error}
        type="error"
        showIcon
        style={{ margin: 24 }}
      />
    );
  }

  if (!stats) {
    return null;
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            {t('dashboard.title')}
          </Title>
          <Paragraph className="page-subtitle">
            {t('dashboard.overview')}
          </Paragraph>
        </div>
      </div>

      <div className="stats-cards">
        <Card>
          <Statistic
            title={t('dashboard.totalFiles')}
            value={stats.TotalFiles}
            prefix={<FileTextOutlined />}
            valueStyle={{ color: '#1890ff' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.validRecords')}
            value={stats.ValidRecords}
            prefix={<CheckCircleOutlined />}
            valueStyle={{ color: '#52c41a' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.invalidRecords')}
            value={stats.InvalidRecords}
            prefix={<ExclamationCircleOutlined />}
            valueStyle={{ color: '#ff4d4f' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.errorRate')}
            value={stats.ErrorRate}
            suffix="%"
            prefix={<ClockCircleOutlined />}
            valueStyle={{ color: '#faad14' }}
            precision={1}
          />
        </Card>
      </div>

      <Row gutter={[24, 24]}>
        <Col span={12}>
          <Card title={t('dashboard.metrics.filesProcessed')}>
            <div style={{ textAlign: 'center', padding: '40px 0' }}>
              <Paragraph>{t('common.noData')}</Paragraph>
            </div>
          </Card>
        </Col>
        <Col span={12}>
          <Card title={t('dashboard.metrics.systemHealth')}>
            <div style={{ textAlign: 'center', padding: '40px 0' }}>
              <Paragraph>{t('common.noData')}</Paragraph>
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
