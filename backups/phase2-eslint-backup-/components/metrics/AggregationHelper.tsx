import React, { useState } from 'react';
import { Card, Form, Select, Input, Radio, Space, Typography, Divider, InputNumber, Switch, Alert } from 'antd';
import { BarChartOutlined, InfoCircleOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

interface AggregationSettings {
  timeWindow: string;
  customDuration?: string;
  groupingField?: string;
  maxGroups?: number;
  showOthers?: boolean;
  sortBy?: string;
  rollingWindow?: boolean;
  lookbackPeriod?: number;
  alignment?: string;
}

interface AggregationHelperProps {
  onSettingsChange?: (settings: AggregationSettings) => void;
  initialSettings?: AggregationSettings;
}

const AggregationHelper: React.FC<AggregationHelperProps> = ({
  onSettingsChange,
  initialSettings,
}) => {
  const { t } = useTranslation();
  
  const [timeWindow, setTimeWindow] = useState(initialSettings?.timeWindow || 'daily');
  const [customDuration, setCustomDuration] = useState(initialSettings?.customDuration || '');
  const [groupingField, setGroupingField] = useState(initialSettings?.groupingField || '');
  const [maxGroups, setMaxGroups] = useState(initialSettings?.maxGroups || 10);
  const [showOthers, setShowOthers] = useState(initialSettings?.showOthers !== false);
  const [sortBy, setSortBy] = useState(initialSettings?.sortBy || 'value_desc');
  const [rollingWindow, setRollingWindow] = useState(initialSettings?.rollingWindow || false);
  const [lookbackPeriod, setLookbackPeriod] = useState(initialSettings?.lookbackPeriod || 7);
  const [alignment, setAlignment] = useState(initialSettings?.alignment || 'calendar');

  // Update parent when settings change
  const updateParent = () => {
    if (onSettingsChange) {
      onSettingsChange({
        timeWindow,
        customDuration,
        groupingField,
        maxGroups,
        showOthers,
        sortBy,
        rollingWindow,
        lookbackPeriod,
        alignment,
      });
    }
  };

  React.useEffect(() => {
    updateParent();
  }, [timeWindow, customDuration, groupingField, maxGroups, showOthers, sortBy, rollingWindow, lookbackPeriod, alignment]);

  return (
    <Card>
      <Title level={4}>
        <BarChartOutlined /> {t('metrics.aggregation.title')}
      </Title>
      <Paragraph type="secondary">
        {t('metrics.aggregation.subtitle')}
      </Paragraph>

      <Form layout="vertical">
        {/* Time Window Selection */}
        <Form.Item label={<Text strong>{t('metrics.aggregation.timeWindow')}</Text>}>
          <Radio.Group value={timeWindow} onChange={(e) => setTimeWindow(e.target.value)}>
            <Space direction="vertical">
              <Radio value="realtime">{t('metrics.aggregation.realTime')}</Radio>
              <Radio value="hourly">{t('metrics.aggregation.hourly')}</Radio>
              <Radio value="daily">{t('metrics.aggregation.daily')}</Radio>
              <Radio value="weekly">{t('metrics.aggregation.weekly')}</Radio>
              <Radio value="monthly">{t('metrics.aggregation.monthly')}</Radio>
              <Radio value="quarterly">{t('metrics.aggregation.quarterly')}</Radio>
              <Radio value="yearly">{t('metrics.aggregation.yearly')}</Radio>
              <Radio value="custom">{t('metrics.aggregation.custom')}</Radio>
            </Space>
          </Radio.Group>

          {timeWindow === 'custom' && (
            <Input
              placeholder="1h, 6h, 1d, 7d, 30d"
              value={customDuration}
              onChange={(e) => setCustomDuration(e.target.value)}
              style={{ marginTop: '8px' }}
            />
          )}
        </Form.Item>

        {/* Advanced Time Window Settings */}
        {timeWindow !== 'realtime' && (
          <>
            <Divider style={{ margin: '16px 0' }} />
            <div style={{ background: '#f5f5f5', padding: '16px', borderRadius: '6px' }}>
              <Text strong>הגדרות מתקדמות</Text>

              {/* Rolling Window */}
              <div style={{ marginTop: '12px' }}>
                <Space>
                  <Switch
                    checked={rollingWindow}
                    onChange={setRollingWindow}
                  />
                  <Text>{t('metrics.aggregation.rollingWindow')}</Text>
                </Space>
                {rollingWindow && (
                  <div style={{ marginTop: '8px', marginRight: '24px' }}>
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                      {t('metrics.aggregation.lookbackPeriod')}:
                    </Text>
                    <InputNumber
                      min={1}
                      max={365}
                      value={lookbackPeriod}
                      onChange={(val) => setLookbackPeriod(val || 7)}
                      style={{ marginRight: '8px', width: '80px' }}
                    />
                    <Text type="secondary" style={{ fontSize: '12px' }}>ימים</Text>
                  </div>
                )}
              </div>

              {/* Alignment */}
              <div style={{ marginTop: '12px' }}>
                <Text type="secondary" style={{ fontSize: '12px' }}>
                  {t('metrics.aggregation.alignment')}:
                </Text>
                <Radio.Group value={alignment} onChange={(e) => setAlignment(e.target.value)} size="small" style={{ marginRight: '8px' }}>
                  <Radio.Button value="calendar">{t('metrics.aggregation.calendarBoundaries')}</Radio.Button>
                  <Radio.Button value="sliding">{t('metrics.aggregation.slidingWindow')}</Radio.Button>
                </Radio.Group>
              </div>
            </div>
          </>
        )}

        <Divider />

        {/* Grouping Field */}
        <Form.Item label={<Text strong>{t('metrics.aggregation.groupingField')}</Text>}>
          <Select
            value={groupingField}
            onChange={setGroupingField}
            placeholder="בחר שדה לקיבוץ (אופציונלי)"
            allowClear
          >
            <Option value="">ללא קיבוץ</Option>
            <Option value="category">קטגוריה</Option>
            <Option value="status">סטטוס</Option>
            <Option value="region">אזור</Option>
            <Option value="payment_method">אמצעי תשלום</Option>
            <Option value="error_type">סוג שגיאה</Option>
            <Option value="data_source">מקור נתונים</Option>
          </Select>
        </Form.Item>

        {/* Grouping Options */}
        {groupingField && (
          <div style={{ background: '#f5f5f5', padding: '16px', borderRadius: '6px', marginTop: '8px' }}>
            <Space direction="vertical" style={{ width: '100%' }}>
              {/* Max Groups */}
              <div>
                <Text strong>{t('metrics.aggregation.maxGroups')}:</Text>
                <InputNumber
                  min={1}
                  max={100}
                  value={maxGroups}
                  onChange={(val) => setMaxGroups(val || 10)}
                  style={{ marginRight: '8px', width: '100px' }}
                />
              </div>

              {/* Show Others */}
              <div>
                <Space>
                  <Switch
                    checked={showOthers}
                    onChange={setShowOthers}
                  />
                  <Text>{t('metrics.aggregation.showOthers')}</Text>
                </Space>
                <div>
                  <Text type="secondary" style={{ fontSize: '11px' }}>
                    הצג קטגוריית "אחרים" לקבוצות שלא בטופ {maxGroups}
                  </Text>
                </div>
              </div>

              {/* Sort By */}
              <div>
                <Text strong>{t('metrics.aggregation.sortBy')}:</Text>
                <Radio.Group value={sortBy} onChange={(e) => setSortBy(e.target.value)} size="small">
                  <Space direction="vertical">
                    <Radio value="value_desc">{t('metrics.aggregation.valueDescending')}</Radio>
                    <Radio value="value_asc">{t('metrics.aggregation.valueAscending')}</Radio>
                    <Radio value="name_asc">{t('metrics.aggregation.nameAscending')}</Radio>
                    <Radio value="name_desc">{t('metrics.aggregation.nameDescending')}</Radio>
                  </Space>
                </Radio.Group>
              </div>
            </Space>
          </div>
        )}

        <Divider />

        {/* Preview Distribution (Mock) */}
        {groupingField && (
          <div>
            <Text strong>{t('metrics.aggregation.previewDistribution')}:</Text>
            <div style={{ background: '#f5f5f5', padding: '16px', borderRadius: '6px', marginTop: '8px' }}>
              <Space direction="vertical" style={{ width: '100%' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Category A:</Text>
                  <Text strong>45% (1,234 רשומות)</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Category B:</Text>
                  <Text strong>30% (821 רשומות)</Text>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text>Category C:</Text>
                  <Text strong>15% (410 רשומות)</Text>
                </div>
                {showOthers && (
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <Text type="secondary">Others / אחרים:</Text>
                    <Text type="secondary">10% (273 רשומות)</Text>
                  </div>
                )}
              </Space>
            </div>
          </div>
        )}

        {/* Info Alert */}
        <Alert
          message={
            <Space>
              <InfoCircleOutlined />
              <Text>
                {timeWindow === 'realtime' && 'נתונים בזמן אמת ללא צבירה'}
                {timeWindow === 'hourly' && 'נתונים יצברו לפי שעות'}
                {timeWindow === 'daily' && 'נתונים יצברו לפי ימים'}
                {timeWindow === 'weekly' && 'נתונים יצברו לפי שבועות'}
                {timeWindow === 'monthly' && 'נתונים יצברו לפי חודשים'}
                {timeWindow === 'quarterly' && 'נתונים יצברו לפי רבעונים'}
                {timeWindow === 'yearly' && 'נתונים יצברו לפי שנים'}
                {timeWindow === 'custom' && 'נתונים יצברו לפי משך זמן מותאם אישית'}
              </Text>
            </Space>
          }
          type="info"
          showIcon={false}
          style={{ marginTop: '16px', fontSize: '12px' }}
        />
      </Form>
    </Card>
  );
};

export default AggregationHelper;
