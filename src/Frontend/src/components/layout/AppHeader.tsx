import React from 'react';
import { Layout, Button, Switch, Space, Typography, Divider, Tooltip } from 'antd';
import {
  GlobalOutlined,
  BellOutlined,
  UserOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  QuestionCircleOutlined
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

const { Header } = Layout;
const { Text } = Typography;

interface AppHeaderProps {
  collapsed?: boolean;
  onCollapse?: () => void;
}

const AppHeader: React.FC<AppHeaderProps> = ({ collapsed = false, onCollapse }) => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isRTL = i18n.language === 'he';

  const toggleLanguage = () => {
    const newLanguage = i18n.language === 'he' ? 'en' : 'he';
    i18n.changeLanguage(newLanguage);
    
    // Update document direction
    document.documentElement.dir = newLanguage === 'he' ? 'rtl' : 'ltr';
    document.documentElement.lang = newLanguage;
  };

  const MenuIcon = collapsed ? MenuUnfoldOutlined : MenuFoldOutlined;

  const titleSection = (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      flex: 1,
      justifyContent: 'center'
    }}>
      {onCollapse && (
        <>
          <Button
            type="text"
            icon={<MenuIcon />}
            onClick={onCollapse}
            style={{
              fontSize: '16px',
              width: 48,
              height: 48,
              marginRight: isRTL ? 0 : 16,
              marginLeft: isRTL ? 16 : 0,
              position: 'absolute',
              [isRTL ? 'right' : 'left']: 0
            }}
          />
          <Divider type="vertical" style={{ height: '32px', position: 'absolute', [isRTL ? 'right' : 'left']: 48 }} />
        </>
      )}

      <div className="logo">
        <Text strong style={{ fontSize: '20px', color: '#1890ff' }}>
          {t('app.title')}
        </Text>
      </div>
    </div>
  );

  const actionsSection = (
    <div className="header-actions">
      <Space size="middle">
        {/* Language Toggle */}
        <Space>
          <GlobalOutlined style={{ color: '#8c8c8c' }} />
          <Switch
            checkedChildren="עב"
            unCheckedChildren="EN"
            checked={isRTL}
            onChange={toggleLanguage}
            size="small"
          />
        </Space>

        {/* Help */}
        <Tooltip title={isRTL ? 'עזרה' : 'Help'}>
          <Button
            type="text"
            icon={<QuestionCircleOutlined />}
            onClick={() => navigate('/help')}
            style={{
              fontSize: '16px',
              width: 40,
              height: 40,
            }}
          />
        </Tooltip>

        {/* Notifications */}
        <Button
          type="text"
          icon={<BellOutlined />}
          style={{
            fontSize: '16px',
            width: 40,
            height: 40,
          }}
        />

        {/* User Profile */}
        <Button
          type="text"
          icon={<UserOutlined />}
          style={{
            fontSize: '16px',
            width: 40,
            height: 40,
          }}
        />
      </Space>
    </div>
  );

  return (
    <Header className="app-header">
      {titleSection}
      {actionsSection}
    </Header>
  );
};

export default AppHeader;
