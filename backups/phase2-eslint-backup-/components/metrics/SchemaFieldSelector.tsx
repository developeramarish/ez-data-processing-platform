import React, { useState, useEffect, useCallback } from 'react';
import { Select, Spin, Alert, Tag, Typography, Space, Card, Row, Col, Collapse, Badge } from 'antd';
import { InfoCircleOutlined, DatabaseOutlined } from '@ant-design/icons';
import { schemaApiClient } from '../../services/schema-api-client';
import type { Schema } from '../../types/schema-api';
import { SchemaStatus } from '../../types/schema-api';
import metricsApi, { MetricConfiguration } from '../../services/metrics-api-client';

const { Option } = Select;
const { Text } = Typography;

interface ParsedField {
  name: string;
  displayName: string;
  type: string;
  description: string;
  isNumeric: boolean;
  enumValues?: string[];
}

interface SchemaFieldSelectorProps {
  dataSourceId: string | null;
  prometheusType: string;
  onFieldSelect: (fieldName: string, field: ParsedField) => void;
  onLabelsSelect: (labels: string[]) => void;
  selectedField?: string;
  selectedLabels?: string[];
  showLabels?: boolean; // Control whether to show labels section
}

const SchemaFieldSelector: React.FC<SchemaFieldSelectorProps> = ({
  dataSourceId,
  prometheusType,
  onFieldSelect,
  onLabelsSelect,
  selectedField,
  selectedLabels = [],
  showLabels = true, // Default to true for backward compatibility
}) => {
  const [loading, setLoading] = useState(false);
  const [schema, setSchema] = useState<Schema | null>(null);
  const [fields, setFields] = useState<ParsedField[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [schemaMetrics, setSchemaMetrics] = useState<MetricConfiguration[]>([]);
  const [loadingMetrics, setLoadingMetrics] = useState(false);

  const fetchSchema = useCallback(async () => {
    if (!dataSourceId) return;

    setLoading(true);
    setError(null);

    try {
      console.log('SchemaFieldSelector: Fetching schema for dataSourceId:', dataSourceId);
      
      // Get schemas for this data source
      const response = await schemaApiClient.getSchemas({
        dataSourceId,
        status: SchemaStatus.Active,
        size: 1,
      });

      console.log('SchemaFieldSelector: API response:', response);

      // Fix: Access data correctly from SchemaListResponse structure
      if (response.isSuccess && response.data) {
        console.log('SchemaFieldSelector: Response data:', response.data);
        
        // Check if data is an array or has a nested data property
        const schemasArray = Array.isArray(response.data) ? response.data : (response.data as any).data || [];
        
        console.log('SchemaFieldSelector: Schemas array:', schemasArray);

        if (schemasArray.length > 0) {
          const fetchedSchema = schemasArray[0];
          setSchema(fetchedSchema);
          console.log('SchemaFieldSelector: Setting schema:', fetchedSchema);
          console.log('SchemaFieldSelector: Schema keys:', Object.keys(fetchedSchema));
          console.log('SchemaFieldSelector: jsonSchemaContent:', fetchedSchema.jsonSchemaContent);
          console.log('SchemaFieldSelector: jsonSchema:', (fetchedSchema as any).jsonSchema);

          // Fix: API returns PascalCase, TypeScript expects camelCase
          const jsonSchemaContent = (fetchedSchema as any).JsonSchemaContent || 
                                   fetchedSchema.jsonSchemaContent || 
                                   (fetchedSchema as any).jsonSchema || 
                                   (fetchedSchema as any).schema;

          console.log('SchemaFieldSelector: Using JsonSchemaContent:', jsonSchemaContent);

          // Parse JSON Schema to extract fields
          const parsedFields = parseJsonSchemaFields(jsonSchemaContent);
          console.log('SchemaFieldSelector: Parsed fields:', parsedFields);
          setFields(parsedFields);
        } else {
          console.warn('SchemaFieldSelector: No schemas found in response');
          setError('לא נמצאה סכמה פעילה עבור מקור נתונים זה');
          setFields([]);
        }
      } else {
        console.warn('SchemaFieldSelector: Response not successful or no data');
        setError('לא נמצאה סכמה פעילה עבור מקור נתונים זה');
        setFields([]);
      }
    } catch (err) {
      console.error('SchemaFieldSelector: Error fetching schema:', err);
      setError('שגיאה בטעינת סכמת הנתונים');
      setFields([]);
    } finally {
      setLoading(false);
    }
  }, [dataSourceId]);

  const parseJsonSchemaFields = (jsonSchemaContent: string | any): ParsedField[] => {
    try {
      // Handle both string and object formats
      let jsonSchema;
      if (typeof jsonSchemaContent === 'string') {
        jsonSchema = JSON.parse(jsonSchemaContent);
      } else {
        jsonSchema = jsonSchemaContent;
      }
      
      console.log('SchemaFieldSelector: Parsing JSON Schema:', jsonSchema);
      const properties = jsonSchema.properties || {};
      console.log('SchemaFieldSelector: Schema properties:', properties);
      
      const parsedFields: ParsedField[] = [];

      // Recursive function to parse nested fields
      const parseProperties = (props: any, prefix: string = '') => {
        for (const [fieldName, fieldDef] of Object.entries(props)) {
          const def = fieldDef as any;
          const type = def.type || 'string';
          const fullName = prefix ? `${prefix}.${fieldName}` : fieldName;
          const isNumeric = type === 'number' || type === 'integer';

          // Add the current field
          parsedFields.push({
            name: fullName,
            displayName: def.title || def.description || fullName,
            type,
            description: def.description || '',
            isNumeric,
            enumValues: def.enum,
          });

          // If this is an object with nested properties, parse them too
          if (type === 'object' && def.properties) {
            parseProperties(def.properties, fullName);
          }
        }
      };

      parseProperties(properties);

      console.log('SchemaFieldSelector: Successfully parsed fields:', parsedFields);
      return parsedFields;
    } catch (err) {
      console.error('SchemaFieldSelector: Error parsing JSON Schema:', err);
      console.error('SchemaFieldSelector: JSON Schema content was:', jsonSchemaContent);
      return [];
    }
  };

  // Filter fields based on Prometheus type
  const getFilteredFields = (): ParsedField[] => {
    if (!prometheusType) return fields;

    // Counter, Gauge, Histogram require numeric fields
    if (['counter', 'gauge', 'histogram'].includes(prometheusType)) {
      return fields.filter(f => f.isNumeric);
    }

    // Summary can work with any numeric field
    if (prometheusType === 'summary') {
      return fields.filter(f => f.isNumeric);
    }

    return fields;
  };

  // Get all fields for labels (string fields are preferred)
  const getLabelFields = (): ParsedField[] => {
    return fields.filter(f => f.type === 'string' || f.enumValues);
  };

  const fetchSchemaMetrics = useCallback(async () => {
    if (!dataSourceId) return;

    setLoadingMetrics(true);
    try {
      console.log('SchemaFieldSelector: Fetching metrics for dataSourceId:', dataSourceId);
      const metrics = await metricsApi.getByDataSource(dataSourceId);
      console.log('SchemaFieldSelector: Found metrics:', metrics);
      setSchemaMetrics(metrics);
    } catch (error) {
      console.error('SchemaFieldSelector: Error fetching metrics:', error);
      // Don't show error to user - metrics list is optional
      setSchemaMetrics([]);
    } finally {
      setLoadingMetrics(false);
    }
  }, [dataSourceId]);

  useEffect(() => {
    if (dataSourceId) {
      fetchSchema();
      fetchSchemaMetrics();
    } else {
      setSchema(null);
      setFields([]);
      setError(null);
      setSchemaMetrics([]);
    }
  }, [dataSourceId, fetchSchema, fetchSchemaMetrics]);

  const filteredFields = getFilteredFields();
  const labelFields = getLabelFields();

  if (loading) {
    return (
      <Card size="small">
        <Space>
          <Spin />
          <Text>טוען סכמת נתונים...</Text>
        </Space>
      </Card>
    );
  }

  if (error) {
    return (
      <Alert
        message="שגיאה"
        description={error}
        type="warning"
        showIcon
        closable
        onClose={() => setError(null)}
      />
    );
  }

  if (!dataSourceId) {
    return (
      <Alert
        message="בחר מקור נתונים"
        description="יש לבחור מקור נתונים כדי לראות את השדות הזמינים"
        type="info"
        showIcon
      />
    );
  }

  if (!schema || fields.length === 0) {
    return (
      <Alert
        message="אין סכמה זמינה"
        description="לא נמצאה סכמה פעילה עבור מקור נתונים זה"
        type="warning"
        showIcon
      />
    );
  }

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      {/* Schema Info with Metrics Count */}
      <Card size="small" type="inner">
        <Space direction="vertical" size="small" style={{ width: '100%' }}>
          <Text strong>סכמה: {schema.displayName}</Text>
          <Text type="secondary" style={{ fontSize: '12px' }}>
            {schema.description}
          </Text>
          <Space wrap>
            <Tag color="blue">{fields.length} שדות</Tag>
            <Tag color="green">{filteredFields.length} מתאימים למדד</Tag>
            <Badge count={schemaMetrics.length} style={{ backgroundColor: '#52c41a' }}>
              <Tag color="purple" icon={<DatabaseOutlined />}>מדדים משתמשים בסכמה</Tag>
            </Badge>
          </Space>
          
          {/* Metrics List Collapsible */}
          {schemaMetrics.length > 0 && (
            <Collapse
              size="small"
              style={{ marginTop: '8px' }}
              items={[
                {
                  key: 'metrics',
                  label: (
                    <Text strong>
                      <DatabaseOutlined /> הצג {schemaMetrics.length} מדדים המשתמשים בסכמה זו
                    </Text>
                  ),
                  children: (
                    <Space direction="vertical" size="small" style={{ width: '100%' }}>
                      {schemaMetrics.map((metric) => (
                        <Card key={metric.id} size="small" type="inner">
                          <Space direction="vertical" size={4} style={{ width: '100%' }}>
                            <Text strong>{metric.displayName}</Text>
                            <Text type="secondary" style={{ fontSize: '12px' }}>
                              {metric.name}
                            </Text>
                            {metric.description && (
                              <Text type="secondary" style={{ fontSize: '11px' }}>
                                {metric.description}
                              </Text>
                            )}
                            <Space wrap size={4}>
                              <Tag color="blue">{metric.prometheusType || 'N/A'}</Tag>
                              <Tag color={metric.status === 1 ? 'green' : 'default'}>
                                {metric.status === 1 ? 'פעיל' : 'לא פעיל'}
                              </Tag>
                              {metric.fieldPath && (
                                <Tag color="cyan">שדה: {metric.fieldPath}</Tag>
                              )}
                              {metric.labels && metric.labels.length > 0 && (
                                <Tag color="purple">{metric.labels.length} תוויות</Tag>
                              )}
                            </Space>
                          </Space>
                        </Card>
                      ))}
                    </Space>
                  ),
                },
              ]}
            />
          )}
          {schemaMetrics.length === 0 && !loadingMetrics && (
            <Alert
              message="אין מדדים"
              description="עדיין לא נוצרו מדדים המשתמשים בסכמה זו"
              type="info"
              showIcon
              style={{ marginTop: '8px', fontSize: '12px' }}
            />
          )}
        </Space>
      </Card>

      {/* Field to Measure */}
      <div>
        <Space direction="vertical" size="small" style={{ width: '100%' }}>
          <Space>
            <Text strong>שדה למדידה:</Text>
            <InfoCircleOutlined style={{ color: '#1890ff' }} />
          </Space>
          <Text type="secondary" style={{ fontSize: '12px' }}>
            {prometheusType === 'counter' && 'בחר שדה מספרי שרק עולה (לדוגמה: כמות עסקאות, מספר שגיאות)'}
            {prometheusType === 'gauge' && 'בחר שדה מספרי שיכול לעלות ולרדת (לדוגמה: יתרה, מלאי)'}
            {prometheusType === 'histogram' && 'בחר שדה מספרי להתפלגות (לדוגמה: זמן תגובה, סכום עסקה)'}
            {prometheusType === 'summary' && 'בחר שדה מספרי לחישוב אחוזונים (לדוגמה: זמן עיבוד)'}
            {!prometheusType && 'בחר תחילה סוג מדד Prometheus'}
          </Text>

          <Select
            showSearch
            placeholder="בחר שדה מספרי"
            style={{ width: '100%' }}
            value={selectedField}
            onChange={(value) => {
              const field = fields.find(f => f.name === value);
              if (field) {
                onFieldSelect(value, field);
              }
            }}
            disabled={!prometheusType || filteredFields.length === 0}
            filterOption={(input, option) => {
              const label = option?.label as string;
              return label?.toLowerCase().includes(input.toLowerCase()) || false;
            }}
          >
            {filteredFields.map(field => (
              <Option key={field.name} value={field.name}>
                <Space direction="vertical" size={0}>
                  <Text strong>{field.displayName}</Text>
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    {field.name} • {field.type}
                  </Text>
                </Space>
              </Option>
            ))}
          </Select>

          {filteredFields.length === 0 && prometheusType && (
            <Alert
              message="אין שדות מתאימים"
              description={`לא נמצאו שדות מספריים מתאימים למדד מסוג ${prometheusType}`}
              type="warning"
              showIcon
              style={{ marginTop: '8px' }}
            />
          )}
        </Space>
      </div>

      {/* Prometheus Labels - Only show if showLabels is true */}
      {showLabels && (
        <div>
          <Space direction="vertical" size="small" style={{ width: '100%' }}>
            <Space>
              <Text strong>תוויות (Labels):</Text>
              <InfoCircleOutlined style={{ color: '#1890ff' }} />
            </Space>
            <Text type="secondary" style={{ fontSize: '12px' }}>
              בחר שדות לסימון המדד (לדוגמה: סוג_תשלום, סטטוס, עיר). תוויות מאפשרות סינון ו-aggregation ב-PromQL
            </Text>

            <Select
              mode="multiple"
              placeholder="בחר שדות לתוויות"
              style={{ width: '100%' }}
              value={selectedLabels}
              onChange={onLabelsSelect}
              maxTagCount="responsive"
            >
              {labelFields.map(field => (
                <Option key={field.name} value={field.name}>
                  <Space direction="vertical" size={0}>
                    <Text strong>{field.displayName}</Text>
                    <Text type="secondary" style={{ fontSize: '12px' }}>
                      {field.name} • {field.type}
                      {field.enumValues && ` • ${field.enumValues.length} ערכים`}
                    </Text>
                  </Space>
                </Option>
              ))}
            </Select>

            {selectedLabels.length > 0 && (
              <Card size="small" type="inner" style={{ marginTop: '8px' }}>
                <Text type="secondary" style={{ fontSize: '12px' }}>
                  תוויות נבחרות: {selectedLabels.map(l => `{${l}}`).join(', ')}
                </Text>
              </Card>
            )}
          </Space>
        </div>
      )}

      {/* Field Details Preview */}
      {selectedField && (
        <Card size="small" type="inner" title="פרטי שדה נבחר">
          <Row gutter={[16, 8]}>
            {(() => {
              const field = fields.find(f => f.name === selectedField);
              return field ? (
                <>
                  <Col span={8}>
                    <Text type="secondary">שם:</Text>
                  </Col>
                  <Col span={16}>
                    <Text strong>{field.name}</Text>
                  </Col>
                  <Col span={8}>
                    <Text type="secondary">כותרת:</Text>
                  </Col>
                  <Col span={16}>
                    <Text>{field.displayName}</Text>
                  </Col>
                  <Col span={8}>
                    <Text type="secondary">סוג:</Text>
                  </Col>
                  <Col span={16}>
                    <Tag color={field.isNumeric ? 'blue' : 'default'}>{field.type}</Tag>
                  </Col>
                  {field.description && (
                    <>
                      <Col span={8}>
                        <Text type="secondary">תיאור:</Text>
                      </Col>
                      <Col span={16}>
                        <Text>{field.description}</Text>
                      </Col>
                    </>
                  )}
                  {field.enumValues && (
                    <>
                      <Col span={8}>
                        <Text type="secondary">ערכים:</Text>
                      </Col>
                      <Col span={16}>
                        <Space size={4} wrap>
                          {field.enumValues.map(val => (
                            <Tag key={val}>{val}</Tag>
                          ))}
                        </Space>
                      </Col>
                    </>
                  )}
                </>
              ) : null;
            })()}
          </Row>
        </Card>
      )}
    </Space>
  );
};

export default SchemaFieldSelector;
