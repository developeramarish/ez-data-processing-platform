import React, { useState, useEffect, useRef } from 'react';
import {
  Typography,
  Card,
  Button,
  Space,
  Tabs,
  Form,
  Input,
  Select,
  InputNumber,
  Switch,
  Row,
  Col,
  List,
  Tag,
  Modal,
  Divider,
  Alert,
  Tooltip,
  Collapse,
  message
} from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { createJSONEditor } from 'vanilla-jsoneditor';
import 'vanilla-jsoneditor/themes/jse-theme-dark.css';
import './SchemaBuilder.css';
import {
  PlusOutlined,
  DeleteOutlined,
  EditOutlined,
  SaveOutlined,
  ArrowLeftOutlined,
  CodeOutlined,
  EyeOutlined,
  CheckCircleOutlined,
  CopyOutlined,
  FileTextOutlined,
  CloseOutlined
} from '@ant-design/icons';
import RegexHelperDialog from '../../components/schema/RegexHelperDialog';

const { Title, Paragraph, Text } = Typography;
const { Option } = Select;
const { TextArea } = Input;
const { Panel } = Collapse;

// Helper functions
const formatJsonSchema = (jsonString: string): string => {
  try {
    const parsed = JSON.parse(jsonString);
    return JSON.stringify(parsed, null, 2);
  } catch (error) {
    return jsonString;
  }
};

const validateJsonSyntax = (jsonString: string): { isValid: boolean; error?: string } => {
  try {
    JSON.parse(jsonString);
    return { isValid: true };
  } catch (error) {
    return { 
      isValid: false, 
      error: error instanceof Error ? error.message : 'Invalid JSON syntax'
    };
  }
};

// TypeScript interfaces
interface SchemaField {
  id: string;
  name: string;
  displayName: string;
  type: 'string' | 'number' | 'integer' | 'boolean' | 'array' | 'object' | 'null';
  required: boolean;
  description?: string;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
  format?: string;
  enum?: string[];
  minimum?: number;
  maximum?: number;
  exclusiveMinimum?: boolean;
  exclusiveMaximum?: boolean;
  multipleOf?: number;
  minItems?: number;
  maxItems?: number;
  uniqueItems?: boolean;
  items?: SchemaField;
  properties?: SchemaField[];
  additionalProperties?: boolean;
  defaultValue?: any;
  examples?: any[];
}

const SchemaBuilder: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [form] = Form.useForm();
  
  // State
  const [schemaName, setSchemaName] = useState<string>('');
  const [schemaDisplayName, setSchemaDisplayName] = useState<string>('');
  const [schemaDescription, setSchemaDescription] = useState<string>('');
  const [fields, setFields] = useState<SchemaField[]>([]);
  const [isFieldModalVisible, setIsFieldModalVisible] = useState<boolean>(false);
  const [editingField, setEditingField] = useState<SchemaField | null>(null);
  const [activeTab, setActiveTab] = useState<string>('visual');
  const [jsonSchema, setJsonSchema] = useState<string>('');
  const [isRegexHelperVisible, setIsRegexHelperVisible] = useState<boolean>(false);
  const [isJsonPreviewVisible, setIsJsonPreviewVisible] = useState<boolean>(false);
  const [editorMode, setEditorMode] = useState<'tree' | 'text' | 'table'>('tree');
  
  // Refs for vanilla-jsoneditor instances
  const jsonEditorRef = useRef<HTMLDivElement>(null);
  const previewEditorRef = useRef<HTMLDivElement>(null);
  const validationEditorRef = useRef<HTMLDivElement>(null);
  const [jsonEditorInstance, setJsonEditorInstance] = useState<any>(null);
  const [previewEditorInstance, setPreviewEditorInstance] = useState<any>(null);
  const [validationEditorInstance, setValidationEditorInstance] = useState<any>(null);

  // Generate JSON Schema from fields - memoized with useCallback
  const generateJsonSchema = React.useCallback((): string => {
    const schema: any = {
      "$schema": "https://json-schema.org/draft/2020-12/schema",
      "title": schemaDisplayName || schemaName,
      "description": schemaDescription,
      "type": "object",
      "properties": {},
      "required": []
    };

    fields.forEach(field => {
      const property: any = {
        type: field.type,
        description: field.description
      };

      if (field.type === 'string') {
        if (field.minLength) property.minLength = field.minLength;
        if (field.maxLength) property.maxLength = field.maxLength;
        if (field.pattern) property.pattern = field.pattern;
        if (field.format) property.format = field.format;
        if (field.enum && field.enum.length > 0) property.enum = field.enum;
      }

      if (field.type === 'number' || field.type === 'integer') {
        if (field.minimum !== undefined) property.minimum = field.minimum;
        if (field.maximum !== undefined) property.maximum = field.maximum;
        if (field.exclusiveMinimum) property.exclusiveMinimum = field.minimum;
        if (field.exclusiveMaximum) property.exclusiveMaximum = field.maximum;
        if (field.multipleOf) property.multipleOf = field.multipleOf;
      }

      if (field.type === 'array') {
        if (field.minItems) property.minItems = field.minItems;
        if (field.maxItems) property.maxItems = field.maxItems;
        if (field.uniqueItems) property.uniqueItems = field.uniqueItems;
        if (field.items) {
          property.items = { type: field.items.type };
        }
      }

      if (field.defaultValue !== undefined) property.default = field.defaultValue;
      if (field.examples && field.examples.length > 0) property.examples = field.examples;

      schema.properties[field.name] = property;
      
      if (field.required) {
        schema.required.push(field.name);
      }
    });

    return JSON.stringify(schema, null, 2);
  }, [fields, schemaName, schemaDisplayName, schemaDescription]);

  // Load schema data when editing
  useEffect(() => {
    if (id) {
      loadSchemaData(id);
    }
  }, [id]);

  const loadSchemaData = async (schemaId: string) => {
    try {
      const response = await fetch(`http://localhost:5001/api/v1/schema/${schemaId}`);
      const apiResponse = await response.json();
      
      if (apiResponse.isSuccess && apiResponse.data) {
        const schema = apiResponse.data;
        setSchemaName(schema.Name);
        setSchemaDisplayName(schema.DisplayName);
        setSchemaDescription(schema.Description);
        setJsonSchema(schema.JsonSchemaContent);
        
        try {
          const parsedSchema = JSON.parse(schema.JsonSchemaContent);
          const properties = parsedSchema.properties || {};
          const required = parsedSchema.required || [];
          
          const extractedFields: SchemaField[] = Object.entries(properties).map(([name, prop]: [string, any], index) => ({
            id: `field_${index + 1}`,
            name,
            displayName: prop.description || name,
            type: prop.type || 'string',
            required: required.includes(name),
            description: prop.description,
            minLength: prop.minLength,
            maxLength: prop.maxLength,
            pattern: prop.pattern,
            format: prop.format,
            enum: prop.enum,
            minimum: prop.minimum,
            maximum: prop.maximum,
            examples: prop.examples
          }));
          
          setFields(extractedFields);
        } catch (jsonError) {
          console.warn('Could not parse JSON schema for visual editor:', jsonError);
        }
      }
    } catch (error) {
      console.error('Error loading schema:', error);
    }
  };

  // Initialize JSON Editor (main editable)
  useEffect(() => {
    if (!jsonEditorRef.current || activeTab !== 'json') return;

    try {
      const content = jsonSchema ? JSON.parse(jsonSchema) : {};
      const editor = createJSONEditor({
        target: jsonEditorRef.current,
        props: {
          content: { json: content },
          mode: editorMode,
          onChange: (updatedContent: any) => {
            const jsonString = updatedContent.text || JSON.stringify(updatedContent.json, null, 2);
            setJsonSchema(jsonString);
          },
          readOnly: false,
          mainMenuBar: true,
          navigationBar: true,
          statusBar: true,
          askToFormat: false
        }
      });

      setJsonEditorInstance(editor);

      return () => {
        editor.destroy();
        setJsonEditorInstance(null);
      };
    } catch (error) {
      console.error('Error initializing JSON editor:', error);
    }
  }, [activeTab]);

  // Update editor content when jsonSchema changes
  useEffect(() => {
    if (jsonEditorInstance && jsonSchema && activeTab === 'json') {
      try {
        jsonEditorInstance.set({ json: JSON.parse(jsonSchema) });
      } catch (e) {
        jsonEditorInstance.set({ text: jsonSchema });
      }
    }
  }, [jsonSchema]);

  // Initialize Preview Editor
  useEffect(() => {
    if (!previewEditorRef.current || !isJsonPreviewVisible) return;

    try {
      const content = jsonSchema ? JSON.parse(jsonSchema) : {};
      const editor = createJSONEditor({
        target: previewEditorRef.current,
        props: {
          content: { json: content },
          mode: 'text',
          readOnly: true,
          mainMenuBar: false,
          navigationBar: false,
          statusBar: false
        }
      });

      setPreviewEditorInstance(editor);

      return () => {
        editor.destroy();
        setPreviewEditorInstance(null);
      };
    } catch (error) {
      console.error('Error initializing preview editor:', error);
    }
  }, [isJsonPreviewVisible]);

  // Update preview when jsonSchema changes
  useEffect(() => {
    if (previewEditorInstance && jsonSchema) {
      try {
        previewEditorInstance.set({ json: JSON.parse(jsonSchema) });
      } catch (e) {
        previewEditorInstance.set({ text: jsonSchema });
      }
    }
  }, [jsonSchema, previewEditorInstance]);

  // Update JSON preview when fields change - ONLY for new schemas
  useEffect(() => {
    if (id) {
      return; // Don't regenerate when editing existing schema
    }
    
    const json = generateJsonSchema();
    setJsonSchema(json);
  }, [id, generateJsonSchema]);

  // Mode switcher
  const handleModeChange = (mode: 'tree' | 'text' | 'table') => {
    if (jsonEditorInstance) {
      jsonEditorInstance.updateProps({ mode });
      setEditorMode(mode);
    }
  };

  // Field handlers
  const handleAddField = () => {
    setEditingField(null);
    form.resetFields();
    setIsFieldModalVisible(true);
  };

  const handleEditField = (field: SchemaField) => {
    setEditingField(field);
    form.setFieldsValue(field);
    setIsFieldModalVisible(true);
  };

  const handleDeleteField = (fieldId: string) => {
    setFields(fields.filter(f => f.id !== fieldId));
  };

  const handleSaveField = (values: any) => {
    const newField: SchemaField = {
      id: editingField?.id || `field_${Date.now()}`,
      name: values.name,
      displayName: values.displayName,
      type: values.type,
      required: values.required || false,
      description: values.description,
      minLength: values.minLength,
      maxLength: values.maxLength,
      pattern: values.pattern,
      format: values.format,
      enum: values.enum ? values.enum.split(',').map((v: string) => v.trim()) : undefined,
      minimum: values.minimum,
      maximum: values.maximum,
      exclusiveMinimum: values.exclusiveMinimum,
      exclusiveMaximum: values.exclusiveMaximum,
      multipleOf: values.multipleOf,
      minItems: values.minItems,
      maxItems: values.maxItems,
      uniqueItems: values.uniqueItems,
      defaultValue: values.defaultValue,
      examples: values.examples ? values.examples.split(',').map((v: string) => v.trim()) : undefined
    };

    if (editingField) {
      setFields(fields.map(f => f.id === editingField.id ? newField : f));
    } else {
      setFields([...fields, newField]);
    }

    setEditingField(null);
    setIsFieldModalVisible(false);
  };

  // Field types
  const fieldTypes = [
    { value: 'string', label: 'מחרוזת (String)', icon: '📝' },
    { value: 'number', label: 'מספר (Number)', icon: '🔢' },
    { value: 'integer', label: 'מספר שלם (Integer)', icon: '💯' },
    { value: 'boolean', label: 'בוליאני (Boolean)', icon: '✅' },
    { value: 'array', label: 'מערך (Array)', icon: '📚' },
    { value: 'object', label: 'אובייקט (Object)', icon: '📦' },
    { value: 'null', label: 'Null', icon: '∅' }
  ];

  const stringFormats = [
    'date-time', 'date', 'time', 'email', 'hostname', 'ipv4', 'ipv6', 'uri', 'uuid'
  ];

  const fieldType = Form.useWatch('type', form);
  const formatValue = Form.useWatch('format', form);

  // Format to Regex mapping
  const formatToRegex: Record<string, string> = {
    'date-time': '^\\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\\d|3[01])T([01]\\d|2[0-3]):[0-5]\\d:[0-5]\\d(\\.\\d{3})?Z?$',
    'date': '^\\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\\d|3[01])$',
    'time': '^([01]\\d|2[0-3]):[0-5]\\d:[0-5]\\d$',
    'email': '^[a-zA-Z0-9.!#$%&\'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$',
    'hostname': '^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)*[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$',
    'ipv4': '^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$',
    'ipv6': '^(([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4})$',
    'uri': '^https?://[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}(/.*)?$',
    'uuid': '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$'
  };

  // Handle format selection - auto-populate pattern
  const handleFormatChange = (selectedFormat: string | undefined) => {
    if (selectedFormat && formatToRegex[selectedFormat]) {
      // Auto-fill pattern based on format
      form.setFieldsValue({ 
        format: selectedFormat,
        pattern: formatToRegex[selectedFormat]
      });
    } else if (!selectedFormat) {
      // Format cleared - keep existing pattern
      form.setFieldsValue({ format: undefined });
    }
  };

  return (
    <div>
      {/* Page Header */}
      <div className="page-header">
        <div>
          <Title level={2} style={{ margin: 0 }}>
            <FileTextOutlined /> בונה Schema
          </Title>
          <Paragraph className="page-subtitle">
            בנה JSON Schema 2020-12 באמצעות עורך חזותי או עורך קוד
          </Paragraph>
        </div>
        <Space>
          {activeTab === 'visual' && (
            <Button
              type={isJsonPreviewVisible ? "primary" : "default"}
              icon={<CodeOutlined />}
              onClick={() => setIsJsonPreviewVisible(!isJsonPreviewVisible)}
            >
              {isJsonPreviewVisible ? 'הסתר JSON' : 'הצג JSON'}
            </Button>
          )}
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/schema')}>
            {t('common.back')}
          </Button>
          <Button type="primary" icon={<SaveOutlined />}>
            {t('common.save')}
          </Button>
        </Space>
      </div>

      {/* Schema Basic Info */}
      <Card style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col span={8}>
            <Form.Item label="שם Schema (אנגלית)">
              <Input
                value={schemaName}
                onChange={(e) => setSchemaName(e.target.value)}
                placeholder="sales_transaction"
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item label="שם תצוגה (עברית)">
              <Input
                value={schemaDisplayName}
                onChange={(e) => setSchemaDisplayName(e.target.value)}
                placeholder="עסקאות מכירה"
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item label="גרסה">
              <Input defaultValue="v1.0" />
            </Form.Item>
          </Col>
        </Row>
        <Form.Item label="תיאור">
          <TextArea
            value={schemaDescription}
            onChange={(e) => setSchemaDescription(e.target.value)}
            rows={2}
            placeholder="תאר את מטרת Schema זה"
          />
        </Form.Item>
      </Card>

      {/* Main Content */}
      <Row gutter={16}>
        <Col span={isJsonPreviewVisible ? 14 : 24}>
          <Card>
            <Tabs activeKey={activeTab} onChange={setActiveTab} type="card">
              
              {/* Visual Editor Tab */}
              <Tabs.TabPane tab={<span><EyeOutlined /> עורך חזותי</span>} key="visual">
                <Alert
                  message="בונה שדות חזותי"
                  description="הוסף שדות ל-Schema שלך. כל שדה יכול להכיל כללי אימות מותאמים אישית."
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                />

                <Button
                  type="dashed"
                  size="large"
                  icon={<PlusOutlined />}
                  onClick={handleAddField}
                  style={{ width: '100%', marginBottom: 16 }}
                >
                  הוסף שדה חדש
                </Button>

                {fields.length === 0 ? (
                  <div style={{ textAlign: 'center', padding: '40px 0', color: '#999' }}>
                    <FileTextOutlined style={{ fontSize: 48, marginBottom: 16 }} />
                    <div>אין שדות עדיין. לחץ "הוסף שדה חדש" להתחלה.</div>
                  </div>
                ) : (
                  <List
                    dataSource={fields}
                    renderItem={(field) => (
                      <List.Item
                        actions={[
                          <Button type="link" icon={<EditOutlined />} onClick={() => handleEditField(field)}>ערוך</Button>,
                          <Button type="link" danger icon={<DeleteOutlined />} onClick={() => handleDeleteField(field.id)}>מחק</Button>
                        ]}
                        style={{ padding: '16px', backgroundColor: '#fafafa', marginBottom: '8px', borderRadius: '8px', border: '1px solid #d9d9d9' }}
                      >
                        <List.Item.Meta
                          title={
                            <Space>
                              <Text strong code>{field.name}</Text>
                              <Text type="secondary">({field.displayName})</Text>
                              <Tag color="blue">{field.type}</Tag>
                              {field.required && <Tag color="red">חובה</Tag>}
                            </Space>
                          }
                          description={
                            <div>
                              {field.description && <Text type="secondary">{field.description}</Text>}
                              <Space size="small" wrap style={{ marginTop: 4 }}>
                                {field.minLength && <Tag>Min: {field.minLength}</Tag>}
                                {field.maxLength && <Tag>Max: {field.maxLength}</Tag>}
                                {field.pattern && <Tag>Pattern: {field.pattern}</Tag>}
                                {field.format && <Tag>Format: {field.format}</Tag>}
                                {field.enum && <Tag>Enum: {field.enum.length} values</Tag>}
                                {field.minimum !== undefined && <Tag>Min: {field.minimum}</Tag>}
                                {field.maximum !== undefined && <Tag>Max: {field.maximum}</Tag>}
                              </Space>
                            </div>
                          }
                        />
                      </List.Item>
                    )}
                  />
                )}

                {fields.length > 0 && (
                  <Alert
                    message={
                      <Space size="large">
                        <Text><Text strong>{fields.length}</Text> שדות</Text>
                        <Text><Text strong>{fields.filter(f => f.required).length}</Text> חובה</Text>
                        <Text><Text strong>{fields.filter(f => f.pattern || f.format).length}</Text> עם אימות</Text>
                      </Space>
                    }
                    type="success"
                    showIcon
                    style={{ marginTop: 16 }}
                  />
                )}
              </Tabs.TabPane>

              {/* JSON Editor Tab with vanilla-jsoneditor */}
              <Tabs.TabPane tab={<span><CodeOutlined /> עורך JSON</span>} key="json">
                <Alert
                  message="JSON Schema 2020-12 - עורך מקצועי"
                  description="ערוך JSON Schema עם עורך עץ (Tree), קוד (Text) או טבלה (Table)"
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                />

                {/* Mode Switcher */}
                <div style={{ marginBottom: 16 }}>
                  <Button.Group>
                    <Button 
                      type={editorMode === 'tree' ? 'primary' : 'default'}
                      onClick={() => handleModeChange('tree')}
                    >
                      עץ (Tree)
                    </Button>
                    <Button 
                      type={editorMode === 'text' ? 'primary' : 'default'}
                      onClick={() => handleModeChange('text')}
                    >
                      קוד (Text)
                    </Button>
                  </Button.Group>
                  
                  <Space style={{ float: 'left', marginLeft: 16 }}>
                    <Button
                      size="small"
                      icon={<CopyOutlined />}
                      onClick={() => {
                        navigator.clipboard.writeText(jsonSchema);
                        message.success('JSON Schema הועתק ללוח');
                      }}
                    >
                      העתק
                    </Button>
                    <Button
                      size="small"
                      onClick={() => {
                        if (jsonEditorInstance) {
                          try {
                            const content = jsonEditorInstance.get();
                            const jsonString = content.text || JSON.stringify(content.json, null, 2);
                            const formatted = formatJsonSchema(jsonString);
                            setJsonSchema(formatted);
                            jsonEditorInstance.set({ text: formatted });
                            message.success('JSON עוצב בהצלחה');
                          } catch (error) {
                            message.error('שגיאה בעיצוב JSON');
                          }
                        }
                      }}
                    >
                      עצב
                    </Button>
                    <Button
                      size="small"
                      icon={<CheckCircleOutlined />}
                      onClick={() => {
                        if (jsonEditorInstance) {
                          try {
                            const content = jsonEditorInstance.get();
                            const jsonString = content.text || JSON.stringify(content.json);
                            const result = validateJsonSyntax(jsonString);
                            if (result.isValid) {
                              message.success('JSON Schema תקין!');
                            } else {
                              message.error(`JSON לא תקין: ${result.error}`);
                            }
                          } catch (error) {
                            message.error('שגיאה באימות JSON');
                          }
                        }
                      }}
                    >
                      אמת
                    </Button>
                  </Space>
                </div>

                {/* vanilla-jsoneditor Container */}
                <div 
                  ref={jsonEditorRef} 
                  style={{ 
                    height: '500px',
                    border: '1px solid #d9d9d9',
                    borderRadius: '6px',
                    overflow: 'hidden'
                  }} 
                />
              </Tabs.TabPane>

              {/* Validation Tab */}
              <Tabs.TabPane tab={<span><CheckCircleOutlined /> אימות</span>} key="validation">
                <Alert
                  message="בדוק את Schema שלך"
                  description="הזן נתוני דוגמה לבדיקת אימות"
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                />

                <Form.Item label="נתוני בדיקה (JSON)">
                  <div 
                    ref={validationEditorRef}
                    style={{ 
                      height: '300px',
                      border: '1px solid #d9d9d9',
                      borderRadius: '6px'
                    }} 
                  />
                </Form.Item>

                <Space>
                  <Button type="primary" icon={<CheckCircleOutlined />}>
                    אמת נתונים
                  </Button>
                  <Button>נקה</Button>
                </Space>

                <Divider />

                <div style={{ padding: '16px', backgroundColor: '#f0f9ff', borderRadius: '8px', border: '1px solid #bae7ff' }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <Text strong>תוצאת אימות:</Text>
                    <Tag color="success" icon={<CheckCircleOutlined />}>
                      הנתונים תקינים ועוברים את כל כללי האימות
                    </Tag>
                  </Space>
                </div>
              </Tabs.TabPane>
            </Tabs>
          </Card>
        </Col>

        {/* JSON Preview Pane */}
        {isJsonPreviewVisible && (
          <Col span={10}>
            <Card
              title={
                <Space>
                  <CodeOutlined />
                  <span>JSON Schema Preview</span>
                  <Tag color="blue">Real-time</Tag>
                </Space>
              }
              extra={
                <Tooltip title="סגור תצוגה">
                  <Button size="small" icon={<CloseOutlined />} onClick={() => setIsJsonPreviewVisible(false)} />
                </Tooltip>
              }
              style={{ height: '80vh' }}
              bodyStyle={{ padding: 0, height: 'calc(80vh - 60px)', overflow: 'hidden' }}
            >
              <div style={{ padding: '12px 16px', background: '#f8f9fa', borderBottom: '1px solid #e9ecef', direction: 'rtl' }}>
                <Space size="small">
                  <Text strong>שדות: </Text>
                  <Tag color="blue">{fields.length}</Tag>
                  <Text strong>חובה: </Text>
                  <Tag color="red">{fields.filter(f => f.required).length}</Tag>
                </Space>
              </div>

              <div 
                ref={previewEditorRef}
                style={{ height: 'calc(100% - 50px)' }} 
              />
            </Card>
          </Col>
        )}
      </Row>

      {/* Field Configuration Modal */}
      <Modal
        title={editingField ? 'ערוך שדה' : 'הוסף שדה חדש'}
        open={isFieldModalVisible}
        onCancel={() => setIsFieldModalVisible(false)}
        footer={null}
        width={800}
        destroyOnClose
      >
        <Form form={form} layout="vertical" onFinish={handleSaveField} initialValues={{ required: false, type: 'string' }}>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="name" label="שם שדה (אנגלית)" rules={[{ required: true, message: 'שדה חובה' }, { pattern: /^[a-z_][a-z0-9_]*$/, message: 'רק אותיות אנגליות קטנות, מספרים וקו תחתון' }]}>
                <Input placeholder="transaction_id" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="displayName" label="שם תצוגה (עברית)" rules={[{ required: true, message: 'שדה חובה' }]}>
                <Input placeholder="מזהה עסקה" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={16}>
              <Form.Item name="type" label="סוג השדה" rules={[{ required: true, message: 'שדה חובה' }]}>
                <Select>
                  {fieldTypes.map(type => (
                    <Option key={type.value} value={type.value}>{type.icon} {type.label}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="required" valuePropName="checked" label="שדה חובה">
                <Switch checkedChildren="כן" unCheckedChildren="לא" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="description" label="תיאור השדה">
            <TextArea rows={2} placeholder="תאר את מטרת השדה והמידע שהוא מכיל" />
          </Form.Item>

          <Divider orientation="right">הגדרות אימות</Divider>

          {fieldType === 'string' && (
            <Collapse defaultActiveKey={['basic']} ghost>
              <Panel header="אימות מחרוזת" key="basic">
                <Row gutter={16}>
                  <Col span={8}>
                    <Form.Item name="minLength" label="אורך מינימלי">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="maxLength" label="אורך מקסימלי">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="format" label="פורמט (מילוי אוטומטי)">
                      <Select 
                        placeholder="בחר פורמט" 
                        allowClear
                        onChange={handleFormatChange}
                      >
                        {stringFormats.map(fmt => <Option key={fmt} value={fmt}>{fmt}</Option>)}
                      </Select>
                    </Form.Item>
                  </Col>
                </Row>

                {formatValue && (
                  <Alert
                    message="פורמט נבחר"
                    description={`התבנית Regex מולאה אוטומטית עבור ${formatValue}. ניתן לערוך או לנקות את הפורמט לשימוש בתבנית מותאמת אישית.`}
                    type="info"
                    showIcon
                    style={{ marginBottom: 16 }}
                    action={
                      <Button size="small" onClick={() => {
                        form.setFieldsValue({ format: undefined });
                      }}>
                        נקה Format
                      </Button>
                    }
                  />
                )}

                <Form.Item name="pattern" label="תבנית Regex (אוטומטי או מותאם אישית)">
                  <Input
                    className="ltr-field"
                    placeholder="^[A-Z]{3}[0-9]{4}$ או השתמש בפורמט למילוי אוטומטי"
                    suffix={
                      <Button
                        size="small"
                        type="link"
                        onClick={() => setIsRegexHelperVisible(true)}
                      >
                        עזרה
                      </Button>
                    }
                  />
                </Form.Item>

                <Form.Item name="enum" label="ערכים אפשריים (מופרדים בפסיקים)">
                  <Input placeholder="אדום,כחול,ירוק" />
                </Form.Item>
              </Panel>
            </Collapse>
          )}

          {(fieldType === 'number' || fieldType === 'integer') && (
            <Collapse defaultActiveKey={['basic']} ghost>
              <Panel header="אימות מספרים" key="basic">
                <Row gutter={16}>
                  <Col span={8}>
                    <Form.Item name="minimum" label="ערך מינימלי">
                      <InputNumber style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="maximum" label="ערך מקסימלי">
                      <InputNumber style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="multipleOf" label="כפולה של">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                </Row>

                <Row gutter={16}>
                  <Col span={12}>
                    <Form.Item name="exclusiveMinimum" valuePropName="checked" label="מינימום בלעדי">
                      <Switch checkedChildren="כן" unCheckedChildren="לא" />
                    </Form.Item>
                  </Col>
                  <Col span={12}>
                    <Form.Item name="exclusiveMaximum" valuePropName="checked" label="מקסימום בלעדי">
                      <Switch checkedChildren="כן" unCheckedChildren="לא" />
                    </Form.Item>
                  </Col>
                </Row>
              </Panel>
            </Collapse>
          )}

          {fieldType === 'array' && (
            <Collapse defaultActiveKey={['basic']} ghost>
              <Panel header="אימות מערכים" key="basic">
                <Row gutter={16}>
                  <Col span={8}>
                    <Form.Item name="minItems" label="מספר פריטים מינימלי">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="maxItems" label="מספר פריטים מקסימלי">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col span={8}>
                    <Form.Item name="uniqueItems" valuePropName="checked" label="פריטים ייחודיים">
                      <Switch checkedChildren="כן" unCheckedChildren="לא" />
                    </Form.Item>
                  </Col>
                </Row>
              </Panel>
            </Collapse>
          )}

          <Divider orientation="right">הגדרות נוספות</Divider>

          <Form.Item name="defaultValue" label="ערך ברירת מחדל">
            <Input placeholder="ערך שיוצג כברירת מחדל" />
          </Form.Item>

          <Form.Item name="examples" label="דוגמאות (מופרדות בפסיקים)">
            <Input placeholder="דוגמה1,דוגמה2,דוגמה3" />
          </Form.Item>

          <Form.Item style={{ marginTop: 24, marginBottom: 0 }}>
            <Space>
              <Button type="primary" htmlType="submit">
                {editingField ? 'עדכן שדה' : 'הוסף שדה'}
              </Button>
              <Button onClick={() => setIsFieldModalVisible(false)}>
                ביטול
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Regex Helper Dialog */}
      <RegexHelperDialog
        visible={isRegexHelperVisible}
        onClose={() => setIsRegexHelperVisible(false)}
        onSelect={(pattern) => {
          form.setFieldsValue({ pattern });
          setIsRegexHelperVisible(false);
        }}
      />
    </div>
  );
};

export default SchemaBuilder;
