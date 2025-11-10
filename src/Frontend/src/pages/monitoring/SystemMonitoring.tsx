import React from 'react';
import { Typography } from 'antd';
import { useTranslation } from 'react-i18next';

const { Title, Paragraph } = Typography;

const SystemMonitoring: React.FC = () => {
  const { t } = useTranslation();

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            {t('monitoring.title')}
          </Title>
          <Paragraph className="page-subtitle">
            {t('monitoring.subtitle')}
          </Paragraph>
        </div>
      </div>

      <div style={{ textAlign: 'center', padding: '60px 0' }}>
        <Paragraph>System Monitoring - Coming Soon</Paragraph>
      </div>
    </div>
  );
};

export default SystemMonitoring;
