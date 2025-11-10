import React, { useState, useEffect } from 'react';
import { type JSONSchema, JsonSchemaEditor, TranslationContext, JsonValidator } from 'jsonjoy-builder';
import 'jsonjoy-builder/styles.css';
import './SchemaBuilderNew.css';
import { he } from '../../i18n/jsonjoy-hebrew';
import { Button, Card, Modal, Alert, Space, Tag } from 'antd';
import { PlayCircleOutlined, ToolOutlined, CheckCircleOutlined, CloseCircleOutlined, ExpandOutlined, ShrinkOutlined, ApartmentOutlined } from '@ant-design/icons';
import { loader } from '@monaco-editor/react';
import RegexHelperDialog from '../../components/schema/RegexHelperDialog';
import SchemaTemplateLibrary from '../../components/schema/SchemaTemplateLibrary';
import { generateExample } from '../../utils/schemaExampleGenerator';
import { validateJsonAgainstSchema, getErrorSummary } from '../../utils/schemaValidator';

// Configure Monaco custom theme for green keys and blue values
loader.init().then((monaco) => {
  monaco.editor.defineTheme('jsonGreenBlue', {
    base: 'vs',
    inherit: true,
    rules: [
      { token: 'string.key.json', foreground: '22863a' }, // Green for keys
      { token: 'string.value.json', foreground: '0366d6' }, // Blue for string values
      { token: 'number.json', foreground: '005cc5' }, // Blue for numbers
    ],
    colors: {}
  });
});

// Format to Regex mapping - defined outside component to avoid re-creation
const FORMAT_TO_REGEX: Record<string, string> = {
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

// Recursively auto-populate patterns for formats in the schema
const autoPopulatePatterns = (schema: any): any => {
  if (!schema || typeof schema !== 'object') return schema;

  const result = { ...schema };

  // Handle properties object
  if (result.properties) {
    const enhancedProperties: any = {};
    Object.keys(result.properties).forEach(key => {
      const prop = result.properties[key];
      enhancedProperties[key] = autoPopulatePatterns(prop);
      
      // If this property has a format and type is string, auto-update pattern
      if (prop.type === 'string' && prop.format && FORMAT_TO_REGEX[prop.format]) {
        // ALWAYS update pattern to match format (override existing pattern)
        enhancedProperties[key] = {
          ...enhancedProperties[key],
          pattern: FORMAT_TO_REGEX[prop.format]
        };
      }
    });
    result.properties = enhancedProperties;
  }

  // Handle array items
  if (result.items) {
    result.items = autoPopulatePatterns(result.items);
  }

  // Handle allOf, anyOf, oneOf
  if (result.allOf) result.allOf = result.allOf.map(autoPopulatePatterns);
  if (result.anyOf) result.anyOf = result.anyOf.map(autoPopulatePatterns);
  if (result.oneOf) result.oneOf = result.oneOf.map(autoPopulatePatterns);

  return result;
};

interface SchemaBuilderNewProps {
  initialSchema?: JSONSchema;
  onChange?: (schema: JSONSchema) => void;
  height?: string;
}

export const SchemaBuilderNew: React.FC<SchemaBuilderNewProps> = ({
  initialSchema = {},
  onChange,
  height = '600px'
}) => {
  const [schema, setSchema] = useState<JSONSchema>(initialSchema);
  const [showValidator, setShowValidator] = useState(false);
  const [showExampleJson, setShowExampleJson] = useState(false);
  const [showRegexHelper, setShowRegexHelper] = useState(false);
  const [showTemplateLibrary, setShowTemplateLibrary] = useState(false);
  const [generatedExample, setGeneratedExample] = useState<any>(null);
  const [exampleValidation, setExampleValidation] = useState<any>(null);

  // Tree View: Shows nested structure with fields visible but properties collapsed
  const handleExpandTreeOnly = () => {
    // Step 1: Collapse everything first using aria-label
    const collapseButtons = document.querySelectorAll('.jsonjoy button[aria-label="כווץ"]');
    collapseButtons.forEach((button: any) => {
      button.click();
    });
    
    // Step 2: Expand only Object/List types to show tree structure
    setTimeout(() => {
      const expandButtons = document.querySelectorAll('.jsonjoy button[aria-label="הרחב"]');
      expandButtons.forEach((button: any) => {
        const parent = button.closest('.json-field-row');
        if (parent) {
          const typeButton = parent.querySelector('button span');
          const typeText = typeButton?.textContent;
          
          if (typeText && (typeText.includes('Object') || typeText.includes('List') || typeText.includes('אובייקט') || typeText.includes('מערך'))) {
            button.click();
          }
        }
      });
    }, 150);
    
    // Note: Monaco editor stays permanently expanded (no folding)
  };

  // Expand All: Shows everything fully expanded
  const handleExpandAll = () => {
    // Visual editor: Recursively expand all collapsed items using aria-label
    const expandRecursively = () => {
      const expandButtons = document.querySelectorAll('.jsonjoy button[aria-label="הרחב"]');
      if (expandButtons.length > 0) {
        expandButtons.forEach((button: any) => {
          button.click();
        });
        setTimeout(expandRecursively, 150);
      }
    };
    
    expandRecursively();
    
    // Note: Monaco editor stays permanently expanded (no folding)
  };

  // Collapse All: Maximum folding, minimal view
  const handleCollapseAll = () => {
    // Only click collapse buttons using aria-label
    const collapseButtons = document.querySelectorAll('.jsonjoy button[aria-label="כווץ"]');
    collapseButtons.forEach((button: any) => {
      button.click();
    });
    
    // Note: Monaco editor stays permanently expanded (no folding)
  };

  // Update when initialSchema changes (e.g., when loaded from API)
  // Auto-populate patterns for any existing formats
  useEffect(() => {
    const enhancedSchema = autoPopulatePatterns(initialSchema);
    
    // Only update if actually different (deep comparison)
    const currentStr = JSON.stringify(schema);
    const newStr = JSON.stringify(enhancedSchema);
    
    if (currentStr !== newStr) {
      setSchema(enhancedSchema);
    }
  }, [initialSchema, schema]); // eslint-disable-line react-hooks/exhaustive-deps

  // Enhanced schema change handler with format auto-population
  const handleSchemaChange = (newSchema: JSONSchema) => {
    // Auto-populate pattern when format is set
    const enhancedSchema = autoPopulatePatterns(newSchema);
    
    // Only update if schema actually changed (deep comparison)
    const currentStr = JSON.stringify(schema);
    const newStr = JSON.stringify(enhancedSchema);
    
    if (currentStr !== newStr) {
      setSchema(enhancedSchema);
      onChange?.(enhancedSchema);
    }
  };

  // Handle template selection
  const handleTemplateSelect = (templateSchema: JSONSchema, templateName: string) => {
    handleSchemaChange(templateSchema);
  };

  // Handle showing example JSON modal
  const handleShowExample = () => {
    try {
      // Generate example using the smart generator
      const example = generateExample(schema);
      setGeneratedExample(example);
      
      // Validate the generated example
      const validation = validateJsonAgainstSchema(schema, example);
      setExampleValidation(validation);
      
      // Open modal
      setShowExampleJson(true);
    } catch (error) {
      console.error('Error generating example:', error);
      setGeneratedExample({ error: 'Failed to generate example' });
      setExampleValidation({ valid: false, errors: [], errorCount: 0 });
      setShowExampleJson(true);
    }
  };

  return (
    <TranslationContext.Provider value={he}>
      <div style={{ height: '100%', display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {/* Action Buttons */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'space-between', alignItems: 'center' }}>
          <Space>
            <Button 
              icon={<ApartmentOutlined />}
              onClick={handleExpandTreeOnly}
              size="small"
              type="primary"
            >
              תצוגת עץ
            </Button>
            <Button 
              icon={<ExpandOutlined />}
              onClick={handleExpandAll}
              size="small"
            >
              הרחב הכל
            </Button>
            <Button 
              icon={<ShrinkOutlined />}
              onClick={handleCollapseAll}
              size="small"
            >
              כווץ הכל
            </Button>
          </Space>
          <Space>
            <Button 
              onClick={() => setShowTemplateLibrary(true)}
            >
              תבניות Schema
            </Button>
            <Button 
              icon={<ToolOutlined />}
              onClick={() => setShowRegexHelper(true)}
            >
              עזרת Regex
            </Button>
            <Button 
              icon={<PlayCircleOutlined />}
              onClick={handleShowExample}
            >
              הצג JSON לדוגמה
            </Button>
            <Button onClick={() => setShowValidator(true)}>
              אמת JSON
            </Button>
          </Space>
        </div>

        {/* Schema Editor */}
        <div className="jsonjoy" style={{ flex: 1, minHeight: height }}>
          <JsonSchemaEditor 
            schema={schema} 
            setSchema={handleSchemaChange}
          />
        </div>

        {/* JSON Validator Modal */}
        <JsonValidator
          open={showValidator}
          onOpenChange={setShowValidator}
          schema={schema}
        />

        {/* Template Library */}
        <SchemaTemplateLibrary
          visible={showTemplateLibrary}
          onClose={() => setShowTemplateLibrary(false)}
          onSelect={handleTemplateSelect}
        />

        {/* Regex Helper Dialog */}
        <RegexHelperDialog
          visible={showRegexHelper}
          onClose={() => setShowRegexHelper(false)}
          onSelect={(pattern) => {
            // Pattern is now available - will be inserted if field is focused
            setShowRegexHelper(false);
          }}
        />

        {/* Example JSON Modal */}
        <Modal
          title="JSON לדוגמה המתאים ל-Schema"
          open={showExampleJson}
          onCancel={() => setShowExampleJson(false)}
          footer={null}
          width={800}
        >
          <Space direction="vertical" style={{ width: '100%' }} size="large">
            {/* Validation Status */}
            {exampleValidation && (
              <Alert
                type={exampleValidation.valid ? 'success' : 'error'}
                message={
                  <Space>
                    {exampleValidation.valid ? (
                      <><CheckCircleOutlined /> {getErrorSummary(exampleValidation)}</>
                    ) : (
                      <><CloseCircleOutlined /> {getErrorSummary(exampleValidation)}</>
                    )}
                  </Space>
                }
                description={
                  !exampleValidation.valid && exampleValidation.errors.length > 0 ? (
                    <div style={{ marginTop: '8px' }}>
                      {exampleValidation.errors.map((error: any, index: number) => (
                        <div key={index} style={{ marginBottom: '4px' }}>
                          <Tag color="error">{error.field}</Tag>
                          {error.messageHebrew}
                        </div>
                      ))}
                    </div>
                  ) : null
                }
                showIcon
              />
            )}

            {/* Generated JSON */}
            <Card title="JSON שנוצר">
              <pre style={{ 
                backgroundColor: '#f5f5f5', 
                padding: '16px', 
                borderRadius: '4px',
                overflow: 'auto',
                maxHeight: '500px',
                direction: 'ltr',
                textAlign: 'left',
                fontSize: '13px',
                fontFamily: 'monospace'
              }}>
                {generatedExample ? JSON.stringify(generatedExample, null, 2) : 'טוען...'}
              </pre>
            </Card>
          </Space>
        </Modal>
      </div>
    </TranslationContext.Provider>
  );
};

export default SchemaBuilderNew;
