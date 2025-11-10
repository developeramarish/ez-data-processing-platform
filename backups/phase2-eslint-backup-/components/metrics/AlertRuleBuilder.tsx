import React, { useState, useEffect } from 'react';
import { 
  Card, Space, Typography, Select, Input, InputNumber, 
  Button, List, Tag, Alert, Divider, Row, Col, Switch 
} from 'antd';
import { 
  BellOutlined, PlusOutlined, DeleteOutlined, EditOutlined,
  CheckCircleOutlined, CloseCircleOutlined, WarningOutlined, InfoCircleOutlined, CodeOutlined
} from '@ant-design/icons';
import { 
  ALERT_EXPRESSION_TEMPLATES, 
  AlertExpressionTemplate, 
  generateExpression,
  getTemplateCategories 
} from './AlertExpressionTemplates';
import PromQLExpressionHelperDialog from './PromQLExpressionHelperDialog';

const { Text, Title } = Typography;
const { TextArea } = Input;
const { Option } = Select;

interface AlertRule {
  id?: string;
  name: string;
  description: string;
  expression: string;
  for?: string;
  keepFiringFor?: string;
  severity: 'critical' | 'warning' | 'info';
  templateId?: string;
  templateParameters?: Record<string, string>;
  isEnabled: boolean;
}

interface AlertRuleBuilderProps {
  value?: AlertRule[];
  onChange?: (rules: AlertRule[]) => void;
  availableMetrics?: string[]; // Metric names for selector
  dataSourceId?: string; // For fetching metrics
}

/**
 * AlertRuleBuilder Component
 * Builds Prometheus alert rules using templates and parameters
 */
const AlertRuleBuilder: React.FC<AlertRuleBuilderProps> = ({
  value = [],
  onChange,
  availableMetrics = [],
  dataSourceId
}) => {
  const [rules, setRules] = useState<AlertRule[]>(value);
  const [isEditing, setIsEditing] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number>(-1);
  
  // Form state
  const [selectedTemplate, setSelectedTemplate] = useState<AlertExpressionTemplate | null>(null);
  const [alertName, setAlertName] = useState('');
  const [alertDescription, setAlertDescription] = useState('');
  const [parameters, setParameters] = useState<Record<string, string>>({});
  const [forDuration, setForDuration] = useState('5m');
  const [keepFiringFor, setKeepFiringFor] = useState('');
  const [severity, setSeverity] = useState<'critical' | 'warning' | 'info'>('warning');
  const [generatedExpression, setGeneratedExpression] = useState('');
  const [isEnabled, setIsEnabled] = useState(true);
  const [showPromQLHelper, setShowPromQLHelper] = useState(false);
  const [expressionValid, setExpressionValid] = useState(true);

  // Update parent when rules change
  useEffect(() => {
    onChange?.(rules);
  }, [rules, onChange]);

  // Generate expression when template or parameters change
  useEffect(() => {
    if (selectedTemplate) {
      const expression = generateExpression(selectedTemplate, parameters);
      setGeneratedExpression(expression);
      
      // Validate the generated expression
      const isValid = validatePromQLExpression(expression);
      setExpressionValid(isValid);
    }
  }, [selectedTemplate, parameters]);

  const validatePromQLExpression = (expr: string): boolean => {
    if (!expr || expr.trim().length === 0) return false;
    
    // Check for balanced brackets
    const squareBrackets = expr.match(/[\[\]]/g);
    const curlyBrackets = expr.match(/[{}]/g);
    const openParens = expr.match(/[(]/g);
    const closeParens = expr.match(/[)]/g);
    
    if (squareBrackets && squareBrackets.length % 2 !== 0) return false;
    if (curlyBrackets && curlyBrackets.length % 2 !== 0) return false;
    if ((openParens?.length || 0) !== (closeParens?.length || 0)) return false;
    
    // Check for common PromQL keywords
    const hasMetricOrFunction = /[a-zA-Z_][a-zA-Z0-9_]*/.test(expr);
    if (!hasMetricOrFunction) return false;
    
    // Check no double spaces
    if (expr.includes('  ')) return false;
    
    return true;
  };

  const handleTemplateSelect = (templateId: string) => {
    const template = ALERT_EXPRESSION_TEMPLATES.find(t => t.id === templateId);
    if (template) {
      setSelectedTemplate(template);
      
      // Initialize parameters with defaults
      const initParams: Record<string, string> = {};
      template.parameters.forEach(param => {
        if (param.defaultValue) {
          initParams[param.name] = param.defaultValue;
        }
      });
      setParameters(initParams);
    }
  };

  const handleParameterChange = (paramName: string, value: string) => {
    setParameters(prev => ({
      ...prev,
      [paramName]: value
    }));
  };

  const handleAddRule = () => {
    if (!selectedTemplate || !alertName || !alertDescription) {
      return;
    }

    const newRule: AlertRule = {
      id: isEditing ? rules[editingIndex]?.id : undefined,
      name: alertName,
      description: alertDescription,
      expression: generatedExpression,
      for: forDuration || undefined,
      keepFiringFor: keepFiringFor || undefined,
      severity,
      templateId: selectedTemplate.id,
      templateParameters: parameters,
      isEnabled
    };

    if (isEditing && editingIndex >= 0) {
      const updatedRules = [...rules];
      updatedRules[editingIndex] = newRule;
      setRules(updatedRules);
    } else {
      setRules([...rules, newRule]);
    }

    handleResetForm();
  };

  const handleEditRule = (index: number) => {
    const rule = rules[index];
    setIsEditing(true);
    setEditingIndex(index);
    
    setAlertName(rule.name);
    setAlertDescription(rule.description);
    setSeverity(rule.severity);
    setForDuration(rule.for || '5m');
    setKeepFiringFor(rule.keepFiringFor || '');
    setIsEnabled(rule.isEnabled);
    
    if (rule.templateId) {
      const template = ALERT_EXPRESSION_TEMPLATES.find(t => t.id === rule.templateId);
      if (template) {
        setSelectedTemplate(template);
        setParameters(rule.templateParameters || {});
      }
    }
  };

  const handleDeleteRule = (index: number) => {
    const updatedRules = rules.filter((_, i) => i !== index);
    setRules(updatedRules);
  };

  const handleResetForm = () => {
    setSelectedTemplate(null);
    setAlertName('');
    setAlertDescription('');
    setParameters({});
    setForDuration('5m');
    setKeepFiringFor('');
    setSeverity('warning');
    setGeneratedExpression('');
    setIsEnabled(true);
    setIsEditing(false);
    setEditingIndex(-1);
  };

  const validateAlertName = (name: string): boolean => {
    return /^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name);
  };

  const getSeverityIcon = (sev: string) => {
    switch (sev) {
      case 'critical': return <WarningOutlined style={{ color: '#ff4d4f' }} />;
      case 'warning': return <WarningOutlined style={{ color: '#faad14' }} />;
      case 'info': return <InfoCircleOutlined style={{ color: '#1890ff' }} />;
      default: return <InfoCircleOutlined />;
    }
  };

  const getSeverityColor = (sev: string): string => {
    switch (sev) {
      case 'critical': return 'red';
      case 'warning': return 'orange';
      case 'info': return 'blue';
      default: return 'default';
    }
  };

  return (
    <Card>
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <div>
          <Space>
            <BellOutlined style={{ fontSize: 20, color: '#1890ff' }} />
            <Title level={4} style={{ margin: 0 }}>
              כללי התראה (Alert Rules)
            </Title>
          </Space>
          <Text type="secondary">
            הוסף כללי התראה למדד זה. השתמש בתבניות מוכנות או צור ביטוי PromQL מותאם אישית
          </Text>
        </div>

        {/* Alert Rule Form */}
        <Card size="small" title={isEditing ? "עריכת כלל התראה" : "הוסף כלל התראה חדש"}>
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            {/* Template Selector */}
            <div>
              <Text strong>בחר תבנית התראה:</Text>
              <Select
                style={{ width: '100%', marginTop: 8 }}
                placeholder="בחר תבנית"
                value={selectedTemplate?.id}
                onChange={handleTemplateSelect}
                showSearch
                filterOption={(input, option) => {
                  const label = option?.label;
                  if (typeof label === 'string') {
                    return label.toLowerCase().includes(input.toLowerCase());
                  }
                  return false;
                }}
              >
                {getTemplateCategories().map(category => (
                  <Select.OptGroup key={category.key} label={category.labelHebrew}>
                    {ALERT_EXPRESSION_TEMPLATES
                      .filter(t => t.category === category.key)
                      .map(template => (
                        <Option 
                          key={template.id} 
                          value={template.id}
                          label={template.nameHebrew}
                        >
                          <Space>
                            <Text strong>{template.nameHebrew}</Text>
                            <Text type="secondary" style={{ fontSize: 11 }}>
                              {template.nameEnglish}
                            </Text>
                          </Space>
                        </Option>
                      ))}
                  </Select.OptGroup>
                ))}
              </Select>
            </div>

            {selectedTemplate && (
              <>
                {/* Template Description */}
                <Alert
                  message={selectedTemplate.descriptionHebrew}
                  description={
                    <div style={{ marginTop: 8 }}>
                      <Text strong style={{ fontSize: 11 }}>דוגמה:</Text>
                      <pre style={{ 
                        fontSize: 11, 
                        background: '#f6f8fa',
                        padding: 8,
                        marginTop: 4,
                        borderRadius: 4
                      }}>
                        {selectedTemplate.example}
                      </pre>
                    </div>
                  }
                  type="info"
                  showIcon
                />

                {/* PromQL Helper Button for Custom Expression */}
                {selectedTemplate.id === 'custom_expression' && (
                  <Button
                    icon={<CodeOutlined />}
                    onClick={() => setShowPromQLHelper(true)}
                    block
                  >
                    פתח עוזר ביטויי PromQL
                  </Button>
                )}

                {/* Dynamic Parameters */}
                <div>
                  <Text strong>פרמטרים:</Text>
                  <Row gutter={[16, 16]} style={{ marginTop: 8 }}>
                    {selectedTemplate.parameters.map(param => (
                      <Col span={12} key={param.name}>
                        <div>
                          <Text>{param.labelHebrew}</Text>
                          {param.required && <Text type="danger"> *</Text>}
                          {param.type === 'metric' ? (
                            <Select
                              style={{ width: '100%', marginTop: 4 }}
                              placeholder={param.placeholderHebrew}
                              value={parameters[param.name]}
                              onChange={(val) => handleParameterChange(param.name, val)}
                              showSearch
                            >
                              {availableMetrics.map(metric => (
                                <Option key={metric} value={metric}>
                                  {metric}
                                </Option>
                              ))}
                            </Select>
                          ) : param.type === 'number' ? (
                            <InputNumber
                              style={{ width: '100%', marginTop: 4 }}
                              placeholder={param.placeholderHebrew}
                              value={parameters[param.name]}
                              onChange={(val) => handleParameterChange(param.name, String(val || ''))}
                            />
                          ) : (
                            <Input
                              className="ltr-field"
                              style={{ marginTop: 4 }}
                              placeholder={param.placeholderHebrew}
                              value={parameters[param.name]}
                              onChange={(e) => handleParameterChange(param.name, e.target.value)}
                            />
                          )}
                          {param.description && (
                            <Text type="secondary" style={{ fontSize: 11 }}>
                              {param.description}
                            </Text>
                          )}
                        </div>
                      </Col>
                    ))}
                  </Row>
                </div>

                {/* Generated Expression Preview with Validation */}
                {generatedExpression && (
                  <div>
                    <Space>
                      <Text strong>ביטוי PromQL שנוצר:</Text>
                      {expressionValid ? (
                        <Tag color="green" icon={<CheckCircleOutlined />}>תקין</Tag>
                      ) : (
                        <Tag color="red" icon={<CloseCircleOutlined />}>שגיאה</Tag>
                      )}
                    </Space>
                    <Card 
                      size="small" 
                      style={{ 
                        marginTop: 8,
                        backgroundColor: expressionValid ? '#f6f8fa' : '#fff2f0',
                        border: expressionValid ? '1px solid #d1d5da' : '1px solid #ffccc7'
                      }}
                    >
                      <pre style={{ 
                        margin: 0,
                        fontFamily: 'Monaco, Consolas, monospace',
                        fontSize: 12,
                        whiteSpace: 'pre-wrap',
                        wordBreak: 'break-word'
                      }}>
                        {generatedExpression}
                      </pre>
                    </Card>
                    {!expressionValid && (
                      <Alert
                        message="ביטוי לא תקין"
                        description="בדוק סוגריים, תחביר ושמות מדדים"
                        type="error"
                        showIcon
                        style={{ marginTop: 8 }}
                      />
                    )}
                  </div>
                )}

                <Divider />

                {/* Alert Details */}
                <Row gutter={[16, 16]}>
                  <Col span={12}>
                    <div>
                      <Text strong>שם ההתראה:</Text>
                      <Text type="danger"> *</Text>
                      <Input
                        style={{ marginTop: 4 }}
                        placeholder="לדוגמה: HighErrorRate"
                        value={alertName}
                        onChange={(e) => setAlertName(e.target.value)}
                        status={alertName && !validateAlertName(alertName) ? 'error' : ''}
                      />
                      {alertName && !validateAlertName(alertName) && (
                        <Text type="danger" style={{ fontSize: 11 }}>
                          שם חייב להתחיל באות וכולל רק אותיות, מספרים וקו תחתון
                        </Text>
                      )}
                    </div>
                  </Col>
                  <Col span={12}>
                    <div>
                      <Text strong>חומרה (Severity):</Text>
                      <Select
                        style={{ width: '100%', marginTop: 4 }}
                        value={severity}
                        onChange={setSeverity}
                      >
                        <Option value="critical">
                          <Space>
                            <WarningOutlined style={{ color: '#ff4d4f' }} />
                            קריטי (Critical)
                          </Space>
                        </Option>
                        <Option value="warning">
                          <Space>
                            <WarningOutlined style={{ color: '#faad14' }} />
                            אזהרה (Warning)
                          </Space>
                        </Option>
                        <Option value="info">
                          <Space>
                            <InfoCircleOutlined style={{ color: '#1890ff' }} />
                            מידע (Info)
                          </Space>
                        </Option>
                      </Select>
                    </div>
                  </Col>
                </Row>

                <div>
                  <Text strong>תיאור:</Text>
                  <Text type="danger"> *</Text>
                  <TextArea
                    style={{ marginTop: 4 }}
                    rows={2}
                    placeholder="תאר מה ההתראה מזהה ומה צריך לעשות"
                    value={alertDescription}
                    onChange={(e) => setAlertDescription(e.target.value)}
                  />
                </div>

                <Row gutter={[16, 16]}>
                  <Col span={8}>
                    <div>
                      <Text strong>המתן לפני הפעלה (for):</Text>
                      <Input
                        style={{ marginTop: 4 }}
                        placeholder="לדוגמה: 5m, 10m"
                        value={forDuration}
                        onChange={(e) => setForDuration(e.target.value)}
                      />
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        כמה זמן להמתין לפני הפעלת ההתראה
                      </Text>
                    </div>
                  </Col>
                  <Col span={8}>
                    <div>
                      <Text strong>המשך הפעלה (keep_firing_for):</Text>
                      <Input
                        style={{ marginTop: 4 }}
                        placeholder="לדוגמה: 5m"
                        value={keepFiringFor}
                        onChange={(e) => setKeepFiringFor(e.target.value)}
                      />
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        כמה זמן להמשיך להפעיל לאחר פתרון
                      </Text>
                    </div>
                  </Col>
                  <Col span={8}>
                    <div>
                      <Text strong>מופעל:</Text>
                      <div style={{ marginTop: 8 }}>
                        <Switch
                          checked={isEnabled}
                          onChange={setIsEnabled}
                          checkedChildren="כן"
                          unCheckedChildren="לא"
                        />
                      </div>
                    </div>
                  </Col>
                </Row>

                {/* Action Buttons */}
                <Space>
                  <Button
                    type="primary"
                    icon={isEditing ? <EditOutlined /> : <PlusOutlined />}
                    onClick={handleAddRule}
                    disabled={
                      !alertName || 
                      !alertDescription || 
                      !validateAlertName(alertName) ||
                      !generatedExpression ||
                      !expressionValid
                    }
                  >
                    {isEditing ? 'עדכן כלל' : 'הוסף כלל'}
                  </Button>
                  {isEditing && (
                    <Button onClick={handleResetForm}>
                      ביטול
                    </Button>
                  )}
                </Space>
              </>
            )}
          </Space>
        </Card>

        {/* Alert Rules List */}
        {rules.length > 0 && (
          <div>
            <Text strong style={{ fontSize: 16 }}>
              כללי התראה ({rules.length})
            </Text>
            <List
              style={{ marginTop: 16 }}
              dataSource={rules}
              renderItem={(rule, index) => (
                <List.Item
                  actions={[
                    <Button
                      key="edit"
                      type="link"
                      icon={<EditOutlined />}
                      onClick={() => handleEditRule(index)}
                    >
                      ערוך
                    </Button>,
                    <Button
                      key="delete"
                      type="link"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => handleDeleteRule(index)}
                    >
                      מחק
                    </Button>
                  ]}
                >
                  <List.Item.Meta
                    avatar={getSeverityIcon(rule.severity)}
                    title={
                      <Space>
                        <Text strong>{rule.name}</Text>
                        <Tag color={getSeverityColor(rule.severity)}>
                          {rule.severity}
                        </Tag>
                        {!rule.isEnabled && <Tag color="default">מושבת</Tag>}
                        {rule.for && <Tag color="blue">for: {rule.for}</Tag>}
                      </Space>
                    }
                    description={
                      <Space direction="vertical" style={{ width: '100%' }}>
                        <Text>{rule.description}</Text>
                        <Card size="small" style={{ backgroundColor: '#fafafa' }}>
                          <pre style={{ 
                            margin: 0, 
                            fontSize: 11,
                            fontFamily: 'Monaco, Consolas, monospace',
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word'
                          }}>
                            {rule.expression}
                          </pre>
                        </Card>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </div>
        )}

        {rules.length === 0 && (
          <Alert
            message="לא הוגדרו כללי התראה"
            description="כללי התראה הם אופציונליים. באפשרותך להוסיף אותם מאוחר יותר."
            type="info"
            showIcon
          />
        )}

        {/* PromQL Expression Helper Dialog */}
        <PromQLExpressionHelperDialog
          visible={showPromQLHelper}
          onClose={() => setShowPromQLHelper(false)}
          onSelect={(expr) => {
            if (selectedTemplate?.id === 'custom_expression') {
              handleParameterChange('expression', expr);
              setShowPromQLHelper(false);
            }
          }}
          currentExpression={parameters['expression'] || ''}
        />
      </Space>
    </Card>
  );
};

export default AlertRuleBuilder;
export type { AlertRule };
