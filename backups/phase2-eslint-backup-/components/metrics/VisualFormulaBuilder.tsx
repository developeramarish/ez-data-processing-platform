import React, { useState, useEffect } from 'react';
import { Card, Form, Select, Input, Radio, Space, Typography, Button, Tag, Divider, Alert } from 'antd';
import { ThunderboltOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

// Aggregation functions
const aggregationFunctions = [
  { value: 'SUM', label: 'סכום (Sum)', hebrewLabel: 'סכום', icon: 'Σ', type: 'number' },
  { value: 'AVG', label: 'ממוצע (Average)', hebrewLabel: 'ממוצע', icon: 'x̄', type: 'number' },
  { value: 'COUNT', label: 'ספירה (Count)', hebrewLabel: 'ספירה', icon: '#', type: 'any' },
  { value: 'MIN', label: 'מינימום (Minimum)', hebrewLabel: 'מינימום', icon: '↓', type: 'number' },
  { value: 'MAX', label: 'מקסימום (Maximum)', hebrewLabel: 'מקסימום', icon: '↑', type: 'number' },
  { value: 'COUNT_DISTINCT', label: 'ספירה ייחודית (Count Distinct)', hebrewLabel: 'ספירה ייחודית', icon: '#!', type: 'any' },
  { value: 'MEDIAN', label: 'חציון (Median)', hebrewLabel: 'חציון', icon: 'M', type: 'number' },
  { value: 'PERCENTILE', label: 'אחוזון (Percentile)', hebrewLabel: 'אחוזון', icon: '%ile', type: 'number' },
];

// Time window options
const timeWindows = [
  { value: 'realtime', label: 'זמן אמת (Real-time)', duration: null },
  { value: 'hourly', label: 'שעתי (Hourly)', duration: '1h' },
  { value: 'daily', label: 'יומי (Daily)', duration: '1d' },
  { value: 'weekly', label: 'שבועי (Weekly)', duration: '7d' },
  { value: 'monthly', label: 'חודשי (Monthly)', duration: '30d' },
  { value: 'custom', label: 'מותאם אישית (Custom)', duration: null },
];

interface VisualFormulaBuilderProps {
  dataSourceId?: string;
  onFormulaGenerated?: (formula: string, metadata: any) => void;
  initialFormula?: string;
}

const VisualFormulaBuilder: React.FC<VisualFormulaBuilderProps> = ({
  dataSourceId,
  onFormulaGenerated,
  initialFormula,
}) => {
  const { t } = useTranslation();
  const [form] = Form.useForm();
  
  // State
  const [selectedFunction, setSelectedFunction] = useState<string>('SUM');
  const [selectedField, setSelectedField] = useState<string>('');
  const [timeWindow, setTimeWindow] = useState<string>('daily');
  const [customWindow, setCustomWindow] = useState<string>('');
  const [groupByEnabled, setGroupByEnabled] = useState<boolean>(true);
  const [groupByField, setGroupByField] = useState<string>('');
  const [whereCondition, setWhereCondition] = useState<string>('');
  const [generatedFormula, setGeneratedFormula] = useState<string>('');
  const [availableFields, setAvailableFields] = useState<string[]>([]);

  // Mock fields - in production, fetch from schema based on dataSourceId
  const mockFields = [
    'amount',
    'quantity',
    'price',
    'status',
    'timestamp',
    'created_at',
    'updated_at',
    'customer_id',
    'transaction_id',
    'category',
    'payment_method',
  ];

  useEffect(() => {
    // TODO: Fetch actual fields from schema API based on dataSourceId
    setAvailableFields(mockFields);
  }, [dataSourceId]);

  // Generate formula whenever inputs change
  useEffect(() => {
    generateFormula();
  }, [selectedFunction, selectedField, timeWindow, customWindow, groupByEnabled, groupByField, whereCondition]);

  const generateFormula = () => {
    if (!selectedField) {
      setGeneratedFormula('');
      return;
    }

    let formula = '';

    // Build aggregation part
    if (selectedFunction === 'PERCENTILE') {
      formula = `PERCENTILE(${selectedField}, 95)`; // Default to 95th percentile
    } else if (selectedFunction === 'COUNT_DISTINCT') {
      formula = `COUNT(DISTINCT ${selectedField})`;
    } else {
      formula = `${selectedFunction}(${selectedField})`;
    }

    // Add WHERE clause if specified
    if (whereCondition) {
      formula += ` WHERE ${whereCondition}`;
    }

    // Add GROUP BY if enabled
    if (groupByEnabled && groupByField) {
      if (groupByField === 'date' || groupByField === 'timestamp' || groupByField === 'created_at') {
        formula += ` GROUP BY DATE(${groupByField})`;
      } else {
        formula += ` GROUP BY ${groupByField}`;
      }
    }

    // Add time window
    if (timeWindow !== 'realtime') {
      const windowDuration = timeWindows.find(w => w.value === timeWindow)?.duration;
      if (windowDuration) {
        formula += ` WINDOW ${windowDuration}`;
      } else if (timeWindow === 'custom' && customWindow) {
        formula += ` WINDOW ${customWindow}`;
      }
    }

    setGeneratedFormula(formula);

    if (onFormulaGenerated) {
      onFormulaGenerated(formula, {
        function: selectedFunction,
        field: selectedField,
        timeWindow,
        groupBy: groupByEnabled ? groupByField : null,
        whereCondition,
      });
    }
  };

  const handleUseFormula = () => {
    if (onFormulaGenerated) {
      onFormulaGenerated(generatedFormula, {
        function: selectedFunction,
        field: selectedField,
        timeWindow,
        groupBy: groupByEnabled ? groupByField : null,
        whereCondition,
      });
    }
  };

  const selectedFunctionInfo = aggregationFunctions.find(f => f.value === selectedFunction);

  return (
    <Card>
      <Title level={4}>
        <ThunderboltOutlined /> {t('metrics.formula.visualBuilder')}
      </Title>
      <Paragraph type="secondary">
        {t('metrics.formula.subtitle')}
      </Paragraph>

      <Form form={form} layout="vertical">
        {/* Function Selection */}
        <Form.Item label={<Text strong>{t('metrics.formula.function')}</Text>}>
          <Select
            size="large"
            value={selectedFunction}
            onChange={setSelectedFunction}
            placeholder={t('metrics.formula.selectFunction')}
          >
            {aggregationFunctions.map(func => (
              <Option key={func.value} value={func.value}>
                <Space>
                  <Tag>{func.icon}</Tag>
                  <span>{func.label}</span>
                </Space>
              </Option>
            ))}
          </Select>
          {selectedFunctionInfo && (
            <div style={{ marginTop: '8px' }}>
              <Alert
                message={
                  <Space>
                    <Tag color="blue">{selectedFunctionInfo.type}</Tag>
                    <Text type="secondary">
                      {selectedFunction === 'SUM' && 'מחבר את כל הערכים יחד'}
                      {selectedFunction === 'AVG' && 'מחשב ערך ממוצע'}
                      {selectedFunction === 'COUNT' && 'סופר מספר רשומות'}
                      {selectedFunction === 'MIN' && 'מוצא את הערך הנמוך ביותר'}
                      {selectedFunction === 'MAX' && 'מוצא את הערך הגבוה ביותר'}
                      {selectedFunction === 'COUNT_DISTINCT' && 'סופר ערכים ייחודיים'}
                      {selectedFunction === 'MEDIAN' && 'מוצא את הערך האמצעי'}
                      {selectedFunction === 'PERCENTILE' && 'מחשב אחוזון מסוים'}
                    </Text>
                  </Space>
                }
                type="info"
                showIcon={false}
                style={{ padding: '4px 8px', fontSize: '12px' }}
              />
            </div>
          )}
        </Form.Item>

        {/* Field Selection */}
        <Form.Item label={<Text strong>{t('metrics.formula.field')}</Text>}>
          <Select
            size="large"
            value={selectedField}
            onChange={setSelectedField}
            placeholder={t('metrics.formula.selectField')}
            showSearch
            filterOption={(input, option) =>
              String(option?.children || '').toLowerCase().includes(input.toLowerCase())
            }
          >
            {availableFields.map(field => (
              <Option key={field} value={field}>
                {field}
              </Option>
            ))}
          </Select>
        </Form.Item>

        {/* WHERE Condition (Optional) */}
        <Form.Item label={<Text strong>תנאי WHERE (אופציונלי)</Text>}>
          <Input
            size="large"
            value={whereCondition}
            onChange={(e) => setWhereCondition(e.target.value)}
            placeholder='status="completed"'
          />
          <Text type="secondary" style={{ fontSize: '11px' }}>
            דוגמאות: status="active" | amount &gt; 1000 | category IN ("A", "B")
          </Text>
        </Form.Item>

        <Divider />

        {/* Time Window */}
        <Form.Item label={<Text strong>{t('metrics.aggregation.timeWindow')}</Text>}>
          <Radio.Group value={timeWindow} onChange={(e) => setTimeWindow(e.target.value)}>
            <Space direction="vertical">
              {timeWindows.map(window => (
                <Radio key={window.value} value={window.value}>
                  {window.label}
                </Radio>
              ))}
            </Space>
          </Radio.Group>
          {timeWindow === 'custom' && (
            <Input
              size="large"
              value={customWindow}
              onChange={(e) => setCustomWindow(e.target.value)}
              placeholder="1h, 6h, 2d, 7d, 30d"
              style={{ marginTop: '8px' }}
            />
          )}
        </Form.Item>

        <Divider />

        {/* Group By */}
        <Form.Item label={<Text strong>{t('metrics.aggregation.groupBy')}</Text>}>
          <Radio.Group
            value={groupByEnabled}
            onChange={(e) => setGroupByEnabled(e.target.value)}
          >
            <Space direction="vertical">
              <Radio value={false}>{t('metrics.aggregation.noAggregation')}</Radio>
              <Radio value={true}>קיבוץ לפי שדה</Radio>
            </Space>
          </Radio.Group>
          {groupByEnabled && (
            <Select
              size="large"
              value={groupByField}
              onChange={setGroupByField}
              placeholder="בחר שדה לקיבוץ"
              style={{ marginTop: '8px', width: '100%' }}
            >
              <Option value="date">תאריך (DATE)</Option>
              <Option value="timestamp">חותמת זמן (TIMESTAMP)</Option>
              <Option value="category">קטגוריה</Option>
              <Option value="status">סטטוס</Option>
              <Option value="customer_id">מזהה לקוח</Option>
              {availableFields.map(field => (
                <Option key={field} value={field}>
                  {field}
                </Option>
              ))}
            </Select>
          )}
        </Form.Item>
      </Form>

      <Divider />

      {/* Generated Formula */}
      <div style={{ marginTop: '24px' }}>
        <Text strong>{t('metrics.formula.generatedFormula')}:</Text>
        <div
          style={{
            background: '#f5f5f5',
            padding: '16px',
            borderRadius: '6px',
            marginTop: '8px',
            border: generatedFormula ? '2px solid #52c41a' : '2px solid #d9d9d9',
            position: 'relative',
          }}
        >
          {generatedFormula ? (
            <>
              <div style={{ position: 'absolute', top: '8px', right: '8px' }}>
                <CheckCircleOutlined style={{ color: '#52c41a', fontSize: '16px' }} />
              </div>
              <pre style={{
                margin: 0,
                fontFamily: 'Monaco, Consolas, monospace',
                fontSize: '14px',
                lineHeight: '1.6',
                color: '#000',
              }}>
                {generatedFormula}
              </pre>
            </>
          ) : (
            <div style={{ textAlign: 'center', padding: '24px' }}>
              <CloseCircleOutlined style={{ fontSize: '32px', color: '#d9d9d9' }} />
              <div style={{ marginTop: '8px' }}>
                <Text type="secondary">בחר פונקציה ושדה כדי ליצור נוסחה</Text>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Explanation */}
      {generatedFormula && (
        <div style={{ marginTop: '16px' }}>
          <Alert
            message="הסבר הנוסחה"
            description={
              <div>
                <p>
                  הנוסחה תחשב <strong>{selectedFunctionInfo?.hebrewLabel}</strong> של השדה{' '}
                  <code>{selectedField}</code>
                  {whereCondition && (
                    <> עבור רשומות שמקיימות את התנאי: <code>{whereCondition}</code></>
                  )}
                  .
                </p>
                {groupByEnabled && groupByField && (
                  <p>
                    הנתונים יקובצו לפי <code>{groupByField}</code>.
                  </p>
                )}
                {timeWindow !== 'realtime' && (
                  <p>
                    חלון זמן:{' '}
                    <strong>
                      {timeWindows.find(w => w.value === timeWindow)?.label}
                    </strong>
                  </p>
                )}
              </div>
            }
            type="info"
            showIcon
          />
        </div>
      )}

      {/* Action Buttons */}
      <div style={{ marginTop: '24px', textAlign: 'left' }}>
        <Space>
          <Button
            type="primary"
            size="large"
            disabled={!generatedFormula}
            onClick={handleUseFormula}
          >
            {t('metrics.formula.useTemplate')}
          </Button>
          <Button
            size="large"
            onClick={() => {
              form.resetFields();
              setSelectedFunction('SUM');
              setSelectedField('');
              setWhereCondition('');
              setGroupByField('');
              setTimeWindow('daily');
              setGeneratedFormula('');
            }}
          >
            {t('common.reset')}
          </Button>
        </Space>
      </div>
    </Card>
  );
};

export default VisualFormulaBuilder;
