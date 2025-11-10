/**
 * Schema Template Library
 * Pre-defined schema templates for common use cases
 */

import React, { useState } from 'react';
import { Modal, List, Card, Tag, Input, Space, Button } from 'antd';
import { SearchOutlined, FileTextOutlined, CheckOutlined } from '@ant-design/icons';
import { type JSONSchema } from 'jsonjoy-builder';

interface SchemaTemplate {
  id: string;
  name: string;
  nameHebrew: string;
  description: string;
  descriptionHebrew: string;
  category: string;
  categoryHebrew: string;
  tags: string[];
  schema: JSONSchema;
}

const templates: SchemaTemplate[] = [
  {
    id: 'customer-profile',
    name: 'Customer Profile',
    nameHebrew: 'פרופיל לקוח',
    description: 'Basic customer information',
    descriptionHebrew: 'מידע בסיסי על לקוח',
    category: 'CRM',
    categoryHebrew: 'ניהול לקוחות',
    tags: ['customer', 'לקוח', 'profile'],
    schema: {
      type: 'object',
      required: ['customerId', 'name', 'email'],
      properties: {
        customerId: {
          type: 'string',
          description: 'מזהה לקוח ייחודי',
          minLength: 1
        },
        name: {
          type: 'string',
          description: 'שם מלא',
          minLength: 2
        },
        email: {
          type: 'string',
          format: 'email',
          description: 'כתובת אימייל'
        },
        phone: {
          type: 'string',
          description: 'מספר טלפון',
          pattern: '^05\\d{8}$'
        },
        address: {
          type: 'object',
          properties: {
            street: { type: 'string', description: 'רחוב' },
            city: { type: 'string', description: 'עיר' },
            postalCode: { type: 'string', description: 'מיקוד' }
          }
        }
      }
    }
  },
  {
    id: 'israeli-id',
    name: 'Israeli ID Validation',
    nameHebrew: 'אימות תעודת זהות',
    description: 'Israeli ID number with validation',
    descriptionHebrew: 'מספר תעודת זהות עם אימות',
    category: 'Validation',
    categoryHebrew: 'אימות',
    tags: ['id', 'תעודת זהות', 'israeli'],
    schema: {
      type: 'object',
      required: ['idNumber', 'firstName', 'lastName'],
      properties: {
        idNumber: {
          type: 'string',
          description: 'מספר תעודת זהות',
          pattern: '^\\d{9}$',
          minLength: 9,
          maxLength: 9
        },
        firstName: {
          type: 'string',
          description: 'שם פרטי',
          minLength: 2
        },
        lastName: {
          type: 'string',
          description: 'שם משפחה',
          minLength: 2
        },
        birthDate: {
          type: 'string',
          format: 'date',
          description: 'תאריך לידה'
        }
      }
    }
  },
  {
    id: 'product-catalog',
    name: 'Product Catalog Item',
    nameHebrew: 'פריט בקטלוג',
    description: 'Product with pricing and inventory',
    descriptionHebrew: 'מוצר עם מחיר ומלאי',
    category: 'E-Commerce',
    categoryHebrew: 'מסחר אלקטרוני',
    tags: ['product', 'מוצר', 'inventory'],
    schema: {
      type: 'object',
      required: ['sku', 'name', 'price'],
      properties: {
        sku: {
          type: 'string',
          description: 'מק"ט',
          minLength: 1
        },
        name: {
          type: 'string',
          description: 'שם המוצר',
          minLength: 2
        },
        price: {
          type: 'number',
          description: 'מחיר',
          minimum: 0,
          multipleOf: 0.01
        },
        quantity: {
          type: 'integer',
          description: 'כמות במלאי',
          minimum: 0
        },
        category: {
          type: 'string',
          description: 'קטגוריה'
        },
        inStock: {
          type: 'boolean',
          description: 'האם במלאי'
        }
      }
    }
  },
  {
    id: 'order-invoice',
    name: 'Order/Invoice',
    nameHebrew: 'הזמנה/חשבונית',
    description: 'Order with line items and total',
    descriptionHebrew: 'הזמנה עם פריטים וסכום',
    category: 'Finance',
    categoryHebrew: 'כספים',
    tags: ['order', 'הזמנה', 'invoice', 'חשבונית'],
    schema: {
      type: 'object',
      required: ['orderId', 'customerId', 'orderDate', 'items', 'total'],
      properties: {
        orderId: {
          type: 'string',
          description: 'מזהה הזמנה'
        },
        customerId: {
          type: 'string',
          description: 'מזהה לקוח'
        },
        orderDate: {
          type: 'string',
          format: 'date',
          description: 'תאריך הזמנה'
        },
        items: {
          type: 'array',
          description: 'פריטי ההזמנה',
          minItems: 1,
          items: {
            type: 'object',
            required: ['sku', 'quantity', 'price'],
            properties: {
              sku: { type: 'string', description: 'מק"ט' },
              quantity: { type: 'integer', minimum: 1, description: 'כמות' },
              price: { type: 'number', minimum: 0, description: 'מחיר' }
            }
          }
        },
        total: {
          type: 'number',
          description: 'סכום כולל',
          minimum: 0
        },
        status: {
          type: 'string',
          enum: ['pending', 'processing', 'completed', 'cancelled'],
          description: 'סטטוס הזמנה'
        }
      }
    }
  },
  {
    id: 'employee-record',
    name: 'Employee Record',
    nameHebrew: 'רשומת עובד',
    description: 'Employee information',
    descriptionHebrew: 'מידע עובד',
    category: 'HR',
    categoryHebrew: 'משאבי אנוש',
    tags: ['employee', 'עובד', 'hr'],
    schema: {
      type: 'object',
      required: ['employeeId', 'firstName', 'lastName', 'hireDate'],
      properties: {
        employeeId: {
          type: 'string',
          description: 'מספר עובד'
        },
        firstName: {
          type: 'string',
          description: 'שם פרטי',
          minLength: 2
        },
        lastName: {
          type: 'string',
          description: 'שם משפחה',
          minLength: 2
        },
        email: {
          type: 'string',
          format: 'email',
          description: 'אימייל'
        },
        hireDate: {
          type: 'string',
          format: 'date',
          description: 'תאריך תחילת עבודה'
        },
        department: {
          type: 'string',
          description: 'מחלקה'
        },
        salary: {
          type: 'number',
          description: 'משכורת',
          minimum: 0
        }
      }
    }
  },
  {
    id: 'contact-form',
    name: 'Contact Form',
    nameHebrew: 'טופס יצירת קשר',
    description: 'Contact form submission',
    descriptionHebrew: 'שליחת טופס יצירת קשר',
    category: 'Forms',
    categoryHebrew: 'טפסים',
    tags: ['contact', 'יצירת קשר', 'form'],
    schema: {
      type: 'object',
      required: ['name', 'email', 'message'],
      properties: {
        name: {
          type: 'string',
          description: 'שם',
          minLength: 2
        },
        email: {
          type: 'string',
          format: 'email',
          description: 'אימייל'
        },
        phone: {
          type: 'string',
          description: 'טלפון'
        },
        subject: {
          type: 'string',
          description: 'נושא'
        },
        message: {
          type: 'string',
          description: 'הודעה',
          minLength: 10
        }
      }
    }
  }
];

interface SchemaTemplateLibraryProps {
  visible: boolean;
  onClose: () => void;
  onSelect: (schema: JSONSchema, templateName: string) => void;
}

export const SchemaTemplateLibrary: React.FC<SchemaTemplateLibraryProps> = ({
  visible,
  onClose,
  onSelect
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  const categories = Array.from(new Set(templates.map(t => t.categoryHebrew)));

  const filteredTemplates = templates.filter(template => {
    const matchesSearch = !searchTerm || 
      template.nameHebrew.includes(searchTerm) ||
      template.descriptionHebrew.includes(searchTerm) ||
      template.tags.some(tag => tag.includes(searchTerm));
    
    const matchesCategory = !selectedCategory || template.categoryHebrew === selectedCategory;
    
    return matchesSearch && matchesCategory;
  });

  const handleSelect = (template: SchemaTemplate) => {
    onSelect(template.schema, template.nameHebrew);
    onClose();
  };

  return (
    <Modal
      title="ספריית תבניות Schema"
      open={visible}
      onCancel={onClose}
      footer={null}
      width={900}
    >
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        {/* Search and Filter */}
        <Space>
          <Input
            placeholder="חפש תבנית..."
            prefix={<SearchOutlined />}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{ width: 300 }}
            allowClear
          />
          <Space>
            {categories.map(category => (
              <Tag
                key={category}
                color={selectedCategory === category ? 'blue' : 'default'}
                style={{ cursor: 'pointer' }}
                onClick={() => setSelectedCategory(selectedCategory === category ? null : category)}
              >
                {category}
              </Tag>
            ))}
          </Space>
        </Space>

        {/* Template List */}
        <List
          grid={{ gutter: 16, xs: 1, sm: 1, md: 2, lg: 2, xl: 2, xxl: 3 }}
          dataSource={filteredTemplates}
          renderItem={(template) => (
            <List.Item>
              <Card
                hoverable
                onClick={() => handleSelect(template)}
                actions={[
                  <Button 
                    type="link" 
                    icon={<CheckOutlined />}
                    onClick={() => handleSelect(template)}
                  >
                    השתמש בתבנית
                  </Button>
                ]}
              >
                <Card.Meta
                  avatar={<FileTextOutlined style={{ fontSize: 24, color: '#1890ff' }} />}
                  title={template.nameHebrew}
                  description={
                    <Space direction="vertical" size="small" style={{ width: '100%' }}>
                      <div>{template.descriptionHebrew}</div>
                      <div>
                        <Tag color="blue">{template.categoryHebrew}</Tag>
                        {template.tags.slice(0, 2).map(tag => (
                          <Tag key={tag}>{tag}</Tag>
                        ))}
                      </div>
                    </Space>
                  }
                />
              </Card>
            </List.Item>
          )}
        />

        {filteredTemplates.length === 0 && (
          <div style={{ textAlign: 'center', padding: '40px', color: '#999' }}>
            לא נמצאו תבניות התואמות לחיפוש
          </div>
        )}
      </Space>
    </Modal>
  );
};

export default SchemaTemplateLibrary;
