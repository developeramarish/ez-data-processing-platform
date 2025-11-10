import React, { useState } from 'react';
import { Card, Select, Input, Button, Space, Typography, Tag, Divider, Radio, Alert } from 'antd';
import { PlusOutlined, DeleteOutlined, FilterOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

// Filter operators with Hebrew labels
const operators = [
  { value: '=', label: 'שווה ל (Equals)', symbol: '=' },
  { value: '!=', label: 'לא שווה ל (Not Equals)', symbol: '≠' },
  { value: '>', label: 'גדול מ (Greater Than)', symbol: '>' },
  { value: '>=', label: 'גדול או שווה ל (Greater or Equal)', symbol: '≥' },
  { value: '<', label: 'קטן מ (Less Than)', symbol: '<' },
  { value: '<=', label: 'קטן או שווה ל (Less or Equal)', symbol: '≤' },
  { value: 'CONTAINS', label: 'מכיל (Contains)', symbol: '⊃' },
  { value: 'NOT_CONTAINS', label: 'לא מכיל (Not Contains)', symbol: '⊅' },
  { value: 'STARTS_WITH', label: 'מתחיל ב (Starts With)', symbol: '^' },
  { value: 'ENDS_WITH', label: 'מסתיים ב (Ends With)', symbol: '$' },
  { value: 'IN', label: 'ברשימה (In List)', symbol: '∈' },
  { value: 'NOT_IN', label: 'לא ברשימה (Not In List)', symbol: '∉' },
  { value: 'IS_NULL', label: 'ריק (Is Null)', symbol: '∅' },
  { value: 'IS_NOT_NULL', label: 'לא ריק (Is Not Null)', symbol: '¬∅' },
];

// Common filter templates
const filterTemplates = [
  {
    name: 'שבוע אחרון',
    nameEnglish: 'Last Week',
    filters: [{ field: 'created_at', operator: '>=', value: 'NOW() - 7 DAYS' }],
  },
  {
    name: 'חודש אחרון',
    nameEnglish: 'Last Month',
    filters: [{ field: 'created_at', operator: '>=', value: 'NOW() - 30 DAYS' }],
  },
  {
    name: 'פעילים בלבד',
    nameEnglish: 'Active Only',
    filters: [{ field: 'is_active', operator: '=', value: 'true' }],
  },
  {
    name: 'ערך גבוה',
    nameEnglish: 'High Value',
    filters: [{ field: 'amount', operator: '>', value: '10000' }],
  },
  {
    name: 'שגיאות בלבד',
    nameEnglish: 'Errors Only',
    filters: [{ field: 'status', operator: '=', value: 'error' }],
  },
  {
    name: 'הושלם היום',
    nameEnglish: 'Completed Today',
    filters: [
      { field: 'completed_at', operator: '>=', value: 'TODAY()' },
      { field: 'status', operator: '=', value: 'completed' },
    ],
  },
];

interface FilterCondition {
  id: string;
  field: string;
  operator: string;
  value: string;
  logicOperator?: 'AND' | 'OR';
}

interface FilterConditionBuilderProps {
  availableFields?: string[];
  onFiltersChange?: (filters: FilterCondition[], filterString: string) => void;
  initialFilters?: FilterCondition[];
}

const FilterConditionBuilder: React.FC<FilterConditionBuilderProps> = ({
  availableFields = [],
  onFiltersChange,
  initialFilters = [],
}) => {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<FilterCondition[]>(
    initialFilters.length > 0
      ? initialFilters
      : [{ id: '1', field: '', operator: '=', value: '', logicOperator: 'AND' }]
  );

  // Mock fields if none provided
  const fields = availableFields.length > 0 ? availableFields : [
    'amount',
    'quantity',
    'status',
    'is_active',
    'created_at',
    'updated_at',
    'category',
    'payment_method',
    'error_type',
  ];

  // Add new filter condition
  const addFilter = () => {
    const newFilter: FilterCondition = {
      id: Date.now().toString(),
      field: '',
      operator: '=',
      value: '',
      logicOperator: 'AND',
    };
    const updatedFilters = [...filters, newFilter];
    setFilters(updatedFilters);
    updateParent(updatedFilters);
  };

  // Remove filter condition
  const removeFilter = (id: string) => {
    const updatedFilters = filters.filter(f => f.id !== id);
    setFilters(updatedFilters);
    updateParent(updatedFilters);
  };

  // Update filter field
  const updateFilter = (id: string, updates: Partial<FilterCondition>) => {
    const updatedFilters = filters.map(f =>
      f.id === id ? { ...f, ...updates } : f
    );
    setFilters(updatedFilters);
    updateParent(updatedFilters);
  };

  // Load template
  const loadTemplate = (templateIndex: number) => {
    const template = filterTemplates[templateIndex];
    const newFilters: FilterCondition[] = template.filters.map((f, index) => ({
      id: Date.now().toString() + index,
      field: f.field,
      operator: f.operator,
      value: f.value,
      logicOperator: index === 0 ? undefined : 'AND',
    }));
    setFilters(newFilters);
    updateParent(newFilters);
  };

  // Generate filter string
  const generateFilterString = (filterList: FilterCondition[]): string => {
    if (filterList.length === 0) return '';

    return filterList
      .filter(f => f.field && f.operator)
      .map((f, index) => {
        let condition = '';

        // Add logic operator if not first
        if (index > 0 && f.logicOperator) {
          condition = `${f.logicOperator} `;
        }

        // Build condition based on operator
        if (f.operator === 'IS_NULL') {
          condition += `${f.field} IS NULL`;
        } else if (f.operator === 'IS_NOT_NULL') {
          condition += `${f.field} IS NOT NULL`;
        } else if (f.operator === 'IN' || f.operator === 'NOT_IN') {
          const op = f.operator === 'IN' ? 'IN' : 'NOT IN';
          condition += `${f.field} ${op} (${f.value})`;
        } else if (f.operator === 'CONTAINS') {
          condition += `${f.field} LIKE '%${f.value}%'`;
        } else if (f.operator === 'NOT_CONTAINS') {
          condition += `${f.field} NOT LIKE '%${f.value}%'`;
        } else if (f.operator === 'STARTS_WITH') {
          condition += `${f.field} LIKE '${f.value}%'`;
        } else if (f.operator === 'ENDS_WITH') {
          condition += `${f.field} LIKE '%${f.value}'`;
        } else {
          // Standard operators: =, !=, >, <, >=, <=
          const valueNeedsQuotes = isNaN(Number(f.value)) && !f.value.startsWith('NOW(') && !f.value.startsWith('TODAY(');
          const formattedValue = valueNeedsQuotes ? `"${f.value}"` : f.value;
          condition += `${f.field} ${f.operator} ${formattedValue}`;
        }

        return condition;
      })
      .join(' ');
  };

  // Update parent component
  const updateParent = (updatedFilters: FilterCondition[]) => {
    const filterString = generateFilterString(updatedFilters);
    if (onFiltersChange) {
      onFiltersChange(updatedFilters, filterString);
    }
  };

  const filterString = generateFilterString(filters);
  const hasValidFilters = filters.some(f => f.field && f.operator);

  return (
    <Card>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <Title level={4}>
            <FilterOutlined /> {t('metrics.filters.title')}
          </Title>
          <Paragraph type="secondary">{t('metrics.filters.subtitle')}</Paragraph>
        </div>
        <Select
          placeholder="טען תבנית"
          style={{ width: 200 }}
          onChange={loadTemplate}
          allowClear
        >
          {filterTemplates.map((template, index) => (
            <Option key={index} value={index}>
              {template.name}
            </Option>
          ))}
        </Select>
      </div>

      <Divider />

      {/* Filter Conditions */}
      {filters.map((filter, index) => (
        <div key={filter.id} style={{ marginBottom: '16px' }}>
          {/* Logic Operator (AND/OR) for non-first filters */}
          {index > 0 && (
            <div style={{ marginBottom: '8px' }}>
              <Radio.Group
                value={filter.logicOperator}
                onChange={(e) => updateFilter(filter.id, { logicOperator: e.target.value })}
                size="small"
              >
                <Radio.Button value="AND">
                  <Tag color="blue">AND / וגם</Tag>
                </Radio.Button>
                <Radio.Button value="OR">
                  <Tag color="orange">OR / או</Tag>
                </Radio.Button>
              </Radio.Group>
            </div>
          )}

          <Space.Compact style={{ width: '100%' }}>
            {/* Field */}
            <Select
              style={{ width: '30%' }}
              value={filter.field}
              onChange={(value) => updateFilter(filter.id, { field: value })}
              placeholder={t('metrics.filters.field')}
              showSearch
            >
              {fields.map(field => (
                <Option key={field} value={field}>
                  {field}
                </Option>
              ))}
            </Select>

            {/* Operator */}
            <Select
              style={{ width: '30%' }}
              value={filter.operator}
              onChange={(value) => updateFilter(filter.id, { operator: value })}
              placeholder={t('metrics.filters.operator')}
            >
              {operators.map(op => (
                <Option key={op.value} value={op.value}>
                  <Space>
                    <span>{op.symbol}</span>
                    <span>{op.label}</span>
                  </Space>
                </Option>
              ))}
            </Select>

            {/* Value (hide for IS_NULL/IS_NOT_NULL) */}
            {filter.operator !== 'IS_NULL' && filter.operator !== 'IS_NOT_NULL' && (
              <Input
                style={{ width: '30%' }}
                value={filter.value}
                onChange={(e) => updateFilter(filter.id, { value: e.target.value })}
                placeholder={t('metrics.filters.value')}
              />
            )}

            {/* Remove button */}
            <Button
              danger
              icon={<DeleteOutlined />}
              onClick={() => removeFilter(filter.id)}
              disabled={filters.length === 1}
            />
          </Space.Compact>
        </div>
      ))}

      {/* Add Filter Button */}
      <Button
        type="dashed"
        block
        icon={<PlusOutlined />}
        onClick={addFilter}
        style={{ marginTop: '16px' }}
      >
        {t('metrics.filters.addFilter')}
      </Button>

      <Divider />

      {/* Generated Filter String Preview */}
      <div style={{ marginTop: '24px' }}>
        <Text strong>{t('metrics.filters.previewFilters')}:</Text>
        <div
          style={{
            background: '#f5f5f5',
            padding: '16px',
            borderRadius: '6px',
            marginTop: '8px',
            border: hasValidFilters ? '2px solid #52c41a' : '2px solid #d9d9d9',
            position: 'relative',
            minHeight: '60px',
          }}
        >
          {filterString ? (
            <>
              <div style={{ position: 'absolute', top: '8px', right: '8px' }}>
                <CheckCircleOutlined style={{ color: '#52c41a', fontSize: '16px' }} />
              </div>
              <pre style={{
                margin: 0,
                fontFamily: 'Monaco, Consolas, monospace',
                fontSize: '13px',
                lineHeight: '1.6',
                color: '#000',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              }}>
                {filterString}
              </pre>
            </>
          ) : (
            <div style={{ textAlign: 'center', padding: '16px' }}>
              <Text type="secondary">לא הוגדרו תנאי סינון</Text>
            </div>
          )}
        </div>
      </div>

      {/* Matching Records Estimate (mock) */}
      {hasValidFilters && (
        <div style={{ marginTop: '16px' }}>
          <Alert
            message={
              <Space>
                <Text strong>{t('metrics.filters.matchingRecords')}:</Text>
                <Text>~1,234 רשומות (הערכה)</Text>
              </Space>
            }
            type="success"
            showIcon
          />
        </div>
      )}

      {/* Action Buttons */}
      <div style={{ marginTop: '24px' }}>
        <Space>
          <Button
            type="primary"
            size="large"
            disabled={!hasValidFilters}
            icon={<CheckCircleOutlined />}
          >
            השתמש בסינון
          </Button>
          <Button
            size="large"
            onClick={() => {
              setFilters([{ id: '1', field: '', operator: '=', value: '', logicOperator: 'AND' }]);
              updateParent([]);
            }}
          >
            {t('metrics.filters.clearAllFilters')}
          </Button>
        </Space>
      </div>
    </Card>
  );
};

export default FilterConditionBuilder;
