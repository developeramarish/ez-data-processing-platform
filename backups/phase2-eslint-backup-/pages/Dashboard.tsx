import React from 'react';
import { Row, Col, Card, Statistic, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import {
  FileTextOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons';

const { Title, Paragraph } = Typography;

const Dashboard: React.FC = () => {
  const { t } = useTranslation();

  // Mock data - in real app, this would come from API
  const stats = {
    totalFiles: 1247,
    validRecords: 23456,
    invalidRecords: 234,
    errorRate: 1.0,
  };

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
            value={stats.totalFiles}
            prefix={<FileTextOutlined />}
            valueStyle={{ color: '#1890ff' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.validRecords')}
            value={stats.validRecords}
            prefix={<CheckCircleOutlined />}
            valueStyle={{ color: '#52c41a' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.invalidRecords')}
            value={stats.invalidRecords}
            prefix={<ExclamationCircleOutlined />}
            valueStyle={{ color: '#ff4d4f' }}
          />
        </Card>
        <Card>
          <Statistic
            title={t('dashboard.errorRate')}
            value={stats.errorRate}
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
