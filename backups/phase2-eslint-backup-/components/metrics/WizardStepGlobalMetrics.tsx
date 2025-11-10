import React from 'react';
import { Space, Alert, Select, Typography, Card, Tag } from 'antd';
import { InfoCircleOutlined, FileTextOutlined, DatabaseOutlined, ClockCircleOutlined } from '@ant-design/icons';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';

const { Text } = Typography;
const { Option } = Select;

interface WizardStepGlobalMetricsProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

/**
 * Predefined global metric options
 * These metrics are system-wide and apply to all data sources
 */
const GLOBAL_METRIC_OPTIONS = [
  {
    id: 'file_size',
    nameHebrew: 'גודל קובץ',
    nameEnglish: 'file_size_bytes',
    description: 'גודל קובץ בבייטים',
    type: 'gauge',
    category: 'file_metrics',
    icon: <FileTextOutlined />
  },
  {
    id: 'file_count',
    nameHebrew: 'מספר קבצים',
    nameEnglish: 'files_total',
    description: 'מספר כולל של קבצים שעובדו',
    type: 'counter',
    category: 'file_metrics',
    icon: <FileTextOutlined />
  },
  {
    id: 'record_count',
    nameHebrew: 'מספר רשומות',
    nameEnglish: 'records_total',
    description: 'מספר כולל של רשומות בקובץ',
    type: 'counter',
    category: 'data_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'valid_records',
    nameHebrew: 'רשומות תקינות',
    nameEnglish: 'valid_records_total',
    description: 'מספר רשומות שעברו ולידציה בהצלחה',
    type: 'counter',
    category: 'data_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'invalid_records',
    nameHebrew: 'רשומות שגויות',
    nameEnglish: 'invalid_records_total',
    description: 'מספר רשומות שנכשלו בולידציה',
    type: 'counter',
    category: 'data_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'processing_duration',
    nameHebrew: 'משך עיבוד',
    nameEnglish: 'processing_duration_seconds',
    description: 'זמן עיבוד קובץ בשניות',
    type: 'histogram',
    category: 'performance_metrics',
    icon: <ClockCircleOutlined />
  },
  {
    id: 'file_processing_rate',
    nameHebrew: 'קצב עיבוד קבצים',
    nameEnglish: 'file_processing_rate',
    description: 'מספר קבצים שעובדו לשעה',
    type: 'gauge',
    category: 'performance_metrics',
    icon: <ClockCircleOutlined />
  },
  {
    id: 'error_rate',
    nameHebrew: 'שיעור שגיאות',
    nameEnglish: 'error_rate_percentage',
    description: 'אחוז רשומות שגויות מתוך הכל',
    type: 'gauge',
    category: 'quality_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'data_completeness',
    nameHebrew: 'שלמות נתונים',
    nameEnglish: 'data_completeness_percentage',
    description: 'אחוז שדות מלאים (לא null)',
    type: 'gauge',
    category: 'quality_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'processing_errors',
    nameHebrew: 'שגיאות עיבוד',
    nameEnglish: 'processing_errors_total',
    description: 'מספר שגיאות במהלך העיבוד',
    type: 'counter',
    category: 'error_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'duplicate_records',
    nameHebrew: 'רשומות כפולות',
    nameEnglish: 'duplicate_records_total',
    description: 'מספר רשומות כפולות שזוהו',
    type: 'counter',
    category: 'quality_metrics',
    icon: <DatabaseOutlined />
  },
  {
    id: 'avg_record_size',
    nameHebrew: 'גודל ממוצע של רשומה',
    nameEnglish: 'avg_record_size_bytes',
    description: 'גודל ממוצע של רשומה בבייטים',
    type: 'gauge',
    category: 'data_metrics',
    icon: <DatabaseOutlined />
  }
];

const METRIC_CATEGORIES = [
  { key: 'file_metrics', labelHebrew: 'מדדי קבצים', color: 'blue' },
  { key: 'data_metrics', labelHebrew: 'מדדי נתונים', color: 'green' },
  { key: 'performance_metrics', labelHebrew: 'מדדי ביצועים', color: 'orange' },
  { key: 'quality_metrics', labelHebrew: 'מדדי איכות', color: 'purple' },
  { key: 'error_metrics', labelHebrew: 'מדדי שגיאות', color: 'red' }
];

const WizardStepGlobalMetrics: React.FC<WizardStepGlobalMetricsProps> = ({ value, onChange }) => {
  const [selectedOption, setSelectedOption] = React.useState<string | null>(null);

  React.useEffect(() => {
    // Try to find matching option if editing - only trigger when value.name changes
    if (value.name && value.scope === 'global') {
      const match = GLOBAL_METRIC_OPTIONS.find(opt => opt.nameEnglish === value.name);
      if (match && selectedOption !== match.id) {
        console.log('Pre-selecting global metric - value.name:', value.name, 'found:', match.id);
        setSelectedOption(match.id);
      } else if (!match) {
        console.log('No matching global metric found for:', value.name, 'Available options:', GLOBAL_METRIC_OPTIONS.map(o => o.nameEnglish));
      }
    }
  }, [value.name, value.scope, selectedOption]);

  const handleOptionSelect = (optionId: string) => {
    const option = GLOBAL_METRIC_OPTIONS.find(opt => opt.id === optionId);
    if (option) {
      setSelectedOption(optionId);
      onChange({
        name: option.nameEnglish,
        displayName: option.nameHebrew,
        description: option.description,
        prometheusType: option.type as any,
        category: option.category,
        fieldPath: '$.' + optionId // Set a generic field path for global metrics
      });
    }
  };

  const selectedMetric = selectedOption ? GLOBAL_METRIC_OPTIONS.find(opt => opt.id === selectedOption) : null;

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Alert
        message="מדדים גלובליים - מדדי מערכת"
        description="מדדים אלה חלים על כל מקורות הנתונים במערכת ומודדים היבטים כלליים של עיבוד הנתונים. בחר מדד מוגדר מראש או צור מדד מותאם אישית."
        type="info"
        showIcon
        icon={<InfoCircleOutlined />}
      />

      <div>
        <Text strong>בחר מדד גלובלי:</Text>
        <Text type="danger"> *</Text>
        <Select
          style={{ width: '100%', marginTop: 8 }}
          placeholder="בחר מדד מוגדר מראש"
          value={selectedOption}
          onChange={handleOptionSelect}
          showSearch
          optionLabelProp="label"
          filterOption={(input, option) => {
            const label = option?.label;
            if (typeof label === 'string') {
              return label.toLowerCase().includes(input.toLowerCase());
            }
            return false;
          }}
        >
          {METRIC_CATEGORIES.map(category => (
            <Select.OptGroup key={category.key} label={category.labelHebrew}>
              {GLOBAL_METRIC_OPTIONS
                .filter(opt => opt.category === category.key)
                .map(option => (
                  <Option 
                    key={option.id} 
                    value={option.id}
                    label={option.nameHebrew}
                  >
                    <Space>
                      {option.icon}
                      <div>
                        <div><Text strong>{option.nameHebrew}</Text></div>
                        <div>
                          <Text type="secondary" style={{ fontSize: 11 }}>
                            {option.nameEnglish}
                          </Text>
                        </div>
                      </div>
                    </Space>
                  </Option>
                ))}
            </Select.OptGroup>
          ))}
        </Select>
      </div>

      {selectedMetric && (
        <Card size="small" type="inner" title="פרטי מדד נבחר">
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <div>
              <Text type="secondary">שם לתצוגה:</Text>
              <br />
              <Text strong>{selectedMetric.nameHebrew}</Text>
            </div>
            <div>
              <Text type="secondary">שם Prometheus:</Text>
              <br />
              <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                {selectedMetric.nameEnglish}
              </Tag>
            </div>
            <div>
              <Text type="secondary">תיאור:</Text>
              <br />
              <Text>{selectedMetric.description}</Text>
            </div>
            <div>
              <Text type="secondary">סוג Prometheus:</Text>
              <br />
              <Tag color="green">{selectedMetric.type}</Tag>
            </div>
            <div>
              <Text type="secondary">קטגוריה:</Text>
              <br />
              <Tag color={METRIC_CATEGORIES.find(c => c.key === selectedMetric.category)?.color}>
                {METRIC_CATEGORIES.find(c => c.key === selectedMetric.category)?.labelHebrew}
              </Tag>
            </div>
          </Space>
        </Card>
      )}

      {!selectedOption && (
        <Alert
          message="נדרש בחירת מדד"
          description="יש לבחור מדד גלובלי מהרשימה כדי להמשיך."
          type="warning"
          showIcon
        />
      )}

      <Alert
        message="הערה חשובה"
        description="מדדים גלובליים מחושבים עבור כל מקור נתונים בנפרד, אך משתמשים באותו הגדרת מדד. לדוגמה: 'מספר קבצים' יחושב בנפרד לכל מקור נתונים."
        type="info"
        showIcon
      />
    </Space>
  );
};

export default WizardStepGlobalMetrics;
export { GLOBAL_METRIC_OPTIONS, METRIC_CATEGORIES };
