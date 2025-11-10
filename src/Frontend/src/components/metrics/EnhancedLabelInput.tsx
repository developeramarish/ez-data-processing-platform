import React, { useState, useEffect } from 'react';
import { Card, Input, Space, Typography, Tag, Alert, Button, Row, Col } from 'antd';
import { TagsOutlined, PlusOutlined, DeleteOutlined, CheckCircleOutlined } from '@ant-design/icons';

const { Text, Title } = Typography;

interface LabelWithValue {
  name: string;
  value: string; // Can be "$labelName" for variable or a fixed value
  isVariable: boolean;
}

interface EnhancedLabelInputProps {
  value?: string;
  onChange?: (labelNames: string, promqlExpression: string, labels: LabelWithValue[]) => void;
}

/**
 * Validate label name according to Prometheus naming rules
 */
const validateLabelName = (name: string): boolean => {
  return /^[a-zA-Z_][a-zA-Z0-9_]*$/.test(name);
};

/**
 * Generate PromQL expression from labels with values
 */
const generatePromQLExpression = (labels: LabelWithValue[]): string => {
  if (labels.length === 0) return '';
  
  const promqlParts = labels.map(label => `${label.name}="${label.value}"`);
  return `{${promqlParts.join(', ')}}`;
};

const EnhancedLabelInput: React.FC<EnhancedLabelInputProps> = ({
  value = '',
  onChange
}) => {
  const [labels, setLabels] = useState<LabelWithValue[]>([]);
  const [newLabelName, setNewLabelName] = useState('');
  const [newLabelValue, setNewLabelValue] = useState('');
  const [promqlExpression, setPromqlExpression] = useState('');

  // Initialize from value
  useEffect(() => {
    if (value && labels.length === 0) {
      const parts = value.split(',').map(l => l.trim()).filter(Boolean);
      const initialLabels: LabelWithValue[] = parts.map(name => ({
        name,
        value: `$${name}`,
        isVariable: true
      }));
      setLabels(initialLabels);
    }
  }, [value, labels.length]);

  // Update PromQL when labels change
  useEffect(() => {
    const expression = generatePromQLExpression(labels);
    setPromqlExpression(expression);
    
    const labelNames = labels.map(l => l.name).join(', ');
    onChange?.(labelNames, expression, labels);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [labels]); // Only depend on labels, not onChange to prevent infinite loop

  const handleAddLabel = () => {
    if (!newLabelName) return;
    
    if (!validateLabelName(newLabelName)) {
      return;
    }

    const labelValue = newLabelValue || `$${newLabelName}`;
    const isVariable = labelValue.startsWith('$');

    setLabels([...labels, {
      name: newLabelName,
      value: labelValue,
      isVariable
    }]);

    setNewLabelName('');
    setNewLabelValue('');
  };

  const handleDeleteLabel = (index: number) => {
    setLabels(labels.filter((_, i) => i !== index));
  };

  const handleLabelValueChange = (index: number, value: string) => {
    const updatedLabels = [...labels];
    updatedLabels[index].value = value;
    updatedLabels[index].isVariable = value.startsWith('$');
    setLabels(updatedLabels);
  };

  return (
    <Card size="small" style={{ marginBottom: 16 }}>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Space>
            <TagsOutlined style={{ fontSize: 18, color: '#1890ff' }} />
            <Title level={5} style={{ margin: 0 }}>
              תוויות (Labels) עם ערכים
            </Title>
          </Space>
          <Text type="secondary" style={{ fontSize: 12 }}>
            הגדר תוויות עם ערכים. השתמש ב-$ לערכים דינמיים או הזן ערך קבוע
          </Text>
        </div>

        {/* Add New Label */}
        <Card size="small" type="inner" title="הוסף תווית">
          <Row gutter={[8, 8]}>
            <Col span={8}>
              <Input
                className="ltr-field"
                placeholder="שם תווית (status)"
                value={newLabelName}
                onChange={(e) => setNewLabelName(e.target.value)}
                status={newLabelName && !validateLabelName(newLabelName) ? 'error' : ''}
              />
            </Col>
            <Col span={12}>
              <Input
                className="ltr-field"
                placeholder="ערך ($status או approved)"
                value={newLabelValue}
                onChange={(e) => setNewLabelValue(e.target.value)}
              />
              <Text type="secondary" style={{ fontSize: 10 }}>
                השאר ריק ל-${newLabelName || 'label'}
              </Text>
            </Col>
            <Col span={4}>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={handleAddLabel}
                disabled={!newLabelName || !validateLabelName(newLabelName)}
                block
              >
                הוסף
              </Button>
            </Col>
          </Row>
        </Card>

        {/* Labels List */}
        {labels.length > 0 && (
          <div>
            <Text strong>תוויות ({labels.length}):</Text>
            <Space direction="vertical" style={{ width: '100%', marginTop: 8 }} size="small">
              {labels.map((label, index) => (
                <Card key={index} size="small" type="inner">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Space size="middle" style={{ flex: 1 }}>
                      <Tag color="blue" icon={<CheckCircleOutlined />}>
                        {label.name}
                      </Tag>
                      <Input
                        className="ltr-field"
                        size="small"
                        style={{ width: 200 }}
                        value={label.value}
                        onChange={(e) => handleLabelValueChange(index, e.target.value)}
                        addonBefore="="
                      />
                      {label.isVariable ? (
                        <Tag color="green">משתנה</Tag>
                      ) : (
                        <Tag color="orange">ערך קבוע</Tag>
                      )}
                    </Space>
                    <Button
                      type="text"
                      danger
                      size="small"
                      icon={<DeleteOutlined />}
                      onClick={() => handleDeleteLabel(index)}
                    />
                  </div>
                </Card>
              ))}
            </Space>
          </div>
        )}

        {/* Generated PromQL */}
        {promqlExpression && (
          <div>
            <Text strong>ביטוי PromQL שנוצר:</Text>
            <Card 
              size="small" 
              style={{ 
                marginTop: 8, 
                backgroundColor: '#f6f8fa',
                border: '1px solid #d1d5da'
              }}
            >
              <pre style={{ 
                margin: 0, 
                fontFamily: 'Monaco, Consolas, monospace',
                fontSize: 13,
                color: '#24292e',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}>
                {promqlExpression}
              </pre>
            </Card>
          </div>
        )}

        <Alert
          message="דוגמאות"
          description={
            <div style={{ fontSize: 12 }}>
              <div><strong>משתנה:</strong> status = $status → <code>{`status="$status"`}</code></div>
              <div><strong>קבוע:</strong> region = us-east-1 → <code>{`region="us-east-1"`}</code></div>
              <div><strong>מעורב:</strong> <code>{`{status="$status", env="production"}`}</code></div>
            </div>
          }
          type="info"
          showIcon
          style={{ fontSize: 12 }}
        />
      </Space>
    </Card>
  );
};

export default EnhancedLabelInput;
export type { LabelWithValue };
