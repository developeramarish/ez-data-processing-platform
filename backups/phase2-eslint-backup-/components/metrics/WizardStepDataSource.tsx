import React, { useState, useEffect } from 'react';
import { Tabs, Alert, Select, Space, Typography } from 'antd';
import { GlobalOutlined, DatabaseOutlined } from '@ant-design/icons';
import type { WizardData } from '../../pages/metrics/MetricConfigurationWizard';

const { Text } = Typography;
const { Option } = Select;

interface WizardStepDataSourceProps {
  value: WizardData;
  onChange: (data: Partial<WizardData>) => void;
}

interface DataSource {
  ID: string;
  Name: string;
  Category: string;
  IsActive: boolean;
}

interface ApiResponse<T> {
  IsSuccess: boolean;
  Data: T;
  Error: any;
}

interface PagedResult<T> {
  Items: T[];
  TotalItems: number;
}

const WizardStepDataSource: React.FC<WizardStepDataSourceProps> = ({ value, onChange }) => {
  const [dataSources, setDataSources] = useState<DataSource[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadDataSources();
  }, []);

  const loadDataSources = async () => {
    setLoading(true);
    try {
      const response = await fetch('http://localhost:5001/api/v1/datasource?page=1&size=100');
      const data: ApiResponse<PagedResult<DataSource>> = await response.json();
      if (data.IsSuccess && data.Data) {
        setDataSources(data.Data.Items || []);
      }
    } catch (error) {
      console.error('Error loading data sources:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleScopeChange = (key: string) => {
    onChange({
      scope: key as 'global' | 'datasource-specific',
      dataSourceId: null,
      dataSourceName: null
    });
  };

  const handleDataSourceSelect = (dataSourceId: string) => {
    const dataSource = dataSources.find(ds => ds.ID === dataSourceId);
    onChange({
      dataSourceId,
      dataSourceName: dataSource?.Name || null
    });
  };

  return (
    <Tabs
      activeKey={value.scope}
      onChange={handleScopeChange}
      items={[
        {
          key: 'global',
          label: (
            <span>
              <GlobalOutlined /> מדדים כלליים
            </span>
          ),
          children: (
            <Alert
              message="מדד כללי"
              description="מדד כללי חל על כל מקורות הנתונים במערכת. השתמש במדדים כלליים למדדים שצריכים לעבוד על פני כל מקורות הנתונים."
              type="info"
              showIcon
              style={{ marginTop: 16 }}
            />
          )
        },
        {
          key: 'datasource-specific',
          label: (
            <span>
              <DatabaseOutlined /> מדדים פרטניים
            </span>
          ),
          children: (
            <Space direction="vertical" style={{ width: '100%' }} size="middle">
              <Alert
                message="מדד פרטני"
                description="מדד פרטני קשור למקור נתונים ספציפי אחד. השתמש במדדים פרטניים כאשר המדד תלוי בשדות או מבנה ספציפיים של מקור נתונים מסוים."
                type="info"
                showIcon
                style={{ marginTop: 16 }}
              />
              
              <div>
                <Text strong>בחר מקור נתונים:</Text>
                <Text type="danger"> *</Text>
                <Select
                  style={{ width: '100%', marginTop: 8 }}
                  placeholder="בחר מקור נתונים"
                  value={value.dataSourceId || undefined}
                  onChange={handleDataSourceSelect}
                  loading={loading}
                  showSearch
                  filterOption={(input, option) => {
                    const label = option?.label;
                    if (typeof label === 'string') {
                      return label.toLowerCase().includes(input.toLowerCase());
                    }
                    return false;
                  }}
                >
                  {dataSources.map(ds => (
                    <Option key={ds.ID} value={ds.ID} label={ds.Name}>
                      {ds.Name} ({ds.Category})
                    </Option>
                  ))}
                </Select>
              </div>
            </Space>
          )
        }
      ]}
    />
  );
};

export default WizardStepDataSource;
