import React from 'react';
import { Typography } from 'antd';
import { useTranslation } from 'react-i18next';

const { Title, Paragraph } = Typography;

const ValidationResults: React.FC = () => {
  const { t } = useTranslation();

  return (
    <div>
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            {t('validation.title')}
          </Title>
          <Paragraph className="page-subtitle">
            {t('validation.subtitle')}
          </Paragraph>
        </div>
      </div>

      <div style={{ textAlign: 'center', padding: '60px 0' }}>
        <Paragraph>Validation Results - Coming Soon</Paragraph>
      </div>
    </div>
  );
};

export default ValidationResults;
