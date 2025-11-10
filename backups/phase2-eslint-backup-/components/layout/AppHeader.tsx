import React from 'react';
import { Layout, Button, Switch, Space, Typography, Divider } from 'antd';
import { 
  GlobalOutlined, 
  BellOutlined, 
  UserOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined 
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const { Header } = Layout;
const { Text } = Typography;

interface AppHeaderProps {
  collapsed?: boolean;
  onCollapse?: () => void;
}

const AppHeader: React.FC<AppHeaderProps> = ({ collapsed = false, onCollapse }) => {
  const { t, i18n } = useTranslation();
  const isRTL = i18n.language === 'he';

  const toggleLanguage = () => {
    const newLanguage = i18n.language === 'he' ? 'en' : 'he';
    i18n.changeLanguage(newLanguage);
    
    // Update document direction
    document.documentElement.dir = newLanguage === 'he' ? 'rtl' : 'ltr';
    document.documentElement.lang = newLanguage;
  };

  const MenuIcon = collapsed ? MenuUnfoldOutlined : MenuFoldOutlined;

  return (
    <Header className="app-header">
      <div style={{ display: 'flex', alignItems: 'center' }}>
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
              }}
            />
            <Divider type="vertical" style={{ height: '32px' }} />
          </>
        )}
        
        <div className="logo">
          <Text strong style={{ fontSize: '20px', color: '#1890ff' }}>
            {t('app.title')}
          </Text>
        </div>
      </div>

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
    </Header>
  );
};

export default AppHeader;
