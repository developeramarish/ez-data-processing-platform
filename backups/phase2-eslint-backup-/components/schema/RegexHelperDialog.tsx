import React, { useState, useEffect, useCallback } from 'react';
import {
  Modal,
  Tabs,
  Card,
  List,
  Input,
  Button,
  Space,
  Tag,
  Typography,
  Row,
  Col,
  Alert,
  Divider,
  Collapse,
  message,
  Popconfirm,
  Form
} from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  CopyOutlined,
  SaveOutlined,
  DeleteOutlined,
  EditOutlined
} from '@ant-design/icons';

const { Text, Title } = Typography;
const { TextArea } = Input;
const { Panel } = Collapse;

interface RegexPattern {
  id: string;
  name: string;
  nameHebrew: string;
  pattern: string;
  description: string;
  examples: string[];
  category: 'israeli' | 'general' | 'financial' | 'banking' | 'government' | 'business' | 'personal' | 'validation' | 'custom';
  isCustom?: boolean;
  createdAt?: string;
}

interface RegexHelperDialogProps {
  visible: boolean;
  onClose: () => void;
  onSelect: (pattern: string) => void;
}

// LocalStorage key
const CUSTOM_PATTERNS_KEY = 'ez_custom_regex_patterns';

// Custom pattern management
const loadCustomPatterns = (): RegexPattern[] => {
  try {
    const stored = localStorage.getItem(CUSTOM_PATTERNS_KEY);
    return stored ? JSON.parse(stored) : [];
  } catch {
    return [];
  }
};

const saveCustomPatterns = (patterns: RegexPattern[]): void => {
  try {
    localStorage.setItem(CUSTOM_PATTERNS_KEY, JSON.stringify(patterns));
  } catch (error) {
    message.error('שגיאה בשמירת התבניות');
  }
};

const RegexHelperDialog: React.FC<RegexHelperDialogProps> = ({ visible, onClose, onSelect }) => {
  const [selectedPattern, setSelectedPattern] = useState<string>('');
  const [testPattern, setTestPattern] = useState<string>('');
  const [testStrings, setTestStrings] = useState<string[]>(['']);
  const [activeTab, setActiveTab] = useState<string>('patterns');
  const [customPatterns, setCustomPatterns] = useState<RegexPattern[]>([]);
  const [editingCustomPattern, setEditingCustomPattern] = useState<RegexPattern | null>(null);
  const [isSaveModalVisible, setIsSaveModalVisible] = useState(false);
  const [form] = Form.useForm();

  // Load custom patterns on mount
  useEffect(() => {
    setCustomPatterns(loadCustomPatterns());
  }, []);

  // Expanded predefined patterns library (25+ patterns)
  const predefinedPatterns: RegexPattern[] = [
    // Israeli Patterns
    {
      id: 'israeli_id',
      name: 'Israeli ID',
      nameHebrew: 'תעודת זהות ישראלית',
      pattern: '^[0-9]{9}$',
      description: '9 ספרות - מספר תעודת זהות ישראלית (הערה: pattern זה מאמת פורמט בלבד, לא ספרת ביקורת)',
      examples: ['123456789', '305719018', '208566618'],
      category: 'israeli'
    },
    {
      id: 'israeli_phone',
      name: 'Israeli Phone',
      nameHebrew: 'מספר טלפון ישראלי',
      pattern: '^0(?:[2-4]|[8-9])[0-9]{7}$',
      description: 'מספר טלפון ישראלי (קווי 02-04, 08-09 - 8 ספרות כולל קידומת)',
      examples: ['025551234', '036667890', '0901234567'],
      category: 'israeli'
    },
    {
      id: 'israeli_mobile',
      name: 'Israeli Mobile',
      nameHebrew: 'טלפון נייד ישראלי',
      pattern: '^05[0-58][0-9]{7}$',
      description: 'מספר טלפון נייד ישראלי (050-055, 058)',
      examples: ['0501234567', '0523456789', '0545678901', '0587654321'],
      category: 'israeli'
    },
    {
      id: 'israeli_postal',
      name: 'Israeli Postal Code',
      nameHebrew: 'מיקוד ישראלי',
      pattern: '^[0-9]{5,7}$',
      description: 'מיקוד ישראלי (5-7 ספרות)',
      examples: ['12345', '1234567', '6100001'],
      category: 'israeli'
    },
    {
      id: 'hebrew_text',
      name: 'Hebrew Text',
      nameHebrew: 'טקסט עברי',
      pattern: '^[\u0590-\u05FF\\s]+$',
      description: 'אותיות עבריות ורווחים בלבד',
      examples: ['שלום עולם', 'ישראל', 'מערכת עיבוד נתונים'],
      category: 'israeli'
    },
    
    // Banking Patterns
    {
      id: 'israeli_bank_account',
      name: 'Israeli Bank Account',
      nameHebrew: 'מספר חשבון בנק',
      pattern: '^[0-9]{6,9}$',
      description: 'מספר חשבון בנק ישראלי (6-9 ספרות)',
      examples: ['123456', '1234567', '123456789'],
      category: 'banking'
    },
    {
      id: 'israeli_branch_code',
      name: 'Bank Branch Code',
      nameHebrew: 'קוד סניף בנק',
      pattern: '^[0-9]{3}$',
      description: 'קוד סניף בנק (3 ספרות)',
      examples: ['001', '123', '999'],
      category: 'banking'
    },
    {
      id: 'israeli_iban',
      name: 'Israeli IBAN',
      nameHebrew: 'IBAN ישראלי',
      pattern: '^IL[0-9]{2}[0-9]{3}[0-9]{3}[0-9]{13}$',
      description: 'מספר IBAN ישראלי (IL + 2 ספרות ביקורת + 3 בנק + 3 סניף + 13 חשבון)',
      examples: ['IL620108000000099999999', 'IL620108000000012345678'],
      category: 'banking'
    },
    {
      id: 'swift_code',
      name: 'SWIFT/BIC Code',
      nameHebrew: 'קוד SWIFT',
      pattern: '^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$',
      description: 'קוד SWIFT/BIC בינלאומי',
      examples: ['DEUTDEFF', 'DEUTDEFF500', 'BNPAFRPP'],
      category: 'banking'
    },
    
    // Government Patterns
    {
      id: 'israeli_passport',
      name: 'Israeli Passport',
      nameHebrew: 'מספר דרכון ישראלי',
      pattern: '^[0-9]{7,9}$',
      description: 'מספר דרכון ישראלי (7-9 ספרות)',
      examples: ['1234567', '12345678', '123456789'],
      category: 'government'
    },
    {
      id: 'israeli_drivers_license',
      name: 'Israeli Driver License',
      nameHebrew: 'רישיון נהיגה ישראלי',
      pattern: '^[0-9]{7,8}$',
      description: 'מספר רישיון נהיגה ישראלי',
      examples: ['1234567', '12345678'],
      category: 'government'
    },
    {
      id: 'israeli_license_plate',
      name: 'Israeli License Plate',
      nameHebrew: 'מספר רכב ישראלי',
      pattern: '^[0-9]{2}-[0-9]{3}-[0-9]{2}$',
      description: 'מספר רכב ישראלי (פורמט חדש)',
      examples: ['12-345-67', '99-888-77'],
      category: 'government'
    },
    {
      id: 'israeli_license_plate_old',
      name: 'Israeli License Plate (Old)',
      nameHebrew: 'מספר רכב ישראלי (ישן)',
      pattern: '^[0-9]{7,8}$',
      description: 'מספר רכב ישראלי (פורמט ישן)',
      examples: ['1234567', '12345678'],
      category: 'government'
    },
    
    // Business Patterns
    {
      id: 'israeli_business_number',
      name: 'Israeli Business Number',
      nameHebrew: 'מספר עוסק מורשה',
      pattern: '^5[0-9]{8}$',
      description: 'מספר עוסק מורשה (9 ספרות, מתחיל ב-5)',
      examples: ['512345678', '501234567', '599876543'],
      category: 'business'
    },
    {
      id: 'israeli_company_registration',
      name: 'Company Registration',
      nameHebrew: 'מספר רישום חברה',
      pattern: '^51-?[0-9]{6}-?[0-9]$',
      description: 'מספר רישום חברה ישראלית',
      examples: ['51-1234567', '511234567'],
      category: 'business'
    },
    {
      id: 'israeli_vat',
      name: 'Israeli VAT Number',
      nameHebrew: 'מספר עוסק למע"מ',
      pattern: '^[0-9]{9}$',
      description: 'מספר עוסק מורשה למע"מ',
      examples: ['512345678', '501234567'],
      category: 'business'
    },
    
    // Personal Patterns
    {
      id: 'hebrew_name',
      name: 'Hebrew Name',
      nameHebrew: 'שם בעברית',
      pattern: '^[\u0590-\u05FF]{2,20}$',
      description: 'שם פרטי או משפחה בעברית (2-20 תווים)',
      examples: ['דוד', 'משה', 'רחל', 'כהן'],
      category: 'personal'
    },
    {
      id: 'hebrew_full_name',
      name: 'Hebrew Full Name',
      nameHebrew: 'שם מלא בעברית',
      pattern: '^[\u0590-\u05FF\\s]{2,50}$',
      description: 'שם מלא בעברית (שם פרטי + משפחה)',
      examples: ['דוד כהן', 'רחל לוי', 'משה ישראלי'],
      category: 'personal'
    },
    {
      id: 'israeli_address',
      name: 'Israeli Street Address',
      nameHebrew: 'כתובת רחוב ישראלית',
      pattern: '^[\u0590-\u05FF\\s]+\\s[0-9]{1,4}$',
      description: 'כתובת רחוב בעברית + מספר בית',
      examples: ['הרצל 10', 'רוטשילד 123', 'בן יהודה 5'],
      category: 'personal'
    },
    
    // Validation Patterns
    {
      id: 'alphanumeric',
      name: 'Alphanumeric',
      nameHebrew: 'אלפאנומרי',
      pattern: '^[a-zA-Z0-9]+$',
      description: 'אותיות וספרות באנגלית בלבד',
      examples: ['abc123', 'Test99', 'USER001'],
      category: 'validation'
    },
    {
      id: 'alphanumeric_hebrew',
      name: 'Alphanumeric Hebrew',
      nameHebrew: 'אלפאנומרי עברי',
      pattern: '^[\u0590-\u05FFa-zA-Z0-9\\s]+$',
      description: 'אותיות עבריות, אנגליות וספרות',
      examples: ['test123', 'בדיקה123', 'Test בדיקה 99'],
      category: 'validation'
    },
    {
      id: 'decimal_number',
      name: 'Decimal Number',
      nameHebrew: 'מספר עשרוני',
      pattern: '^[0-9]+\\.?[0-9]*$',
      description: 'מספר עשרוני (עם נקודה)',
      examples: ['123', '123.45', '0.99', '1000.0'],
      category: 'validation'
    },
    {
      id: 'percentage',
      name: 'Percentage',
      nameHebrew: 'אחוז',
      pattern: '^(100(\\.0{1,2})?|[0-9]{1,2}(\\.[0-9]{1,2})?)$',
      description: 'אחוזים (0-100 עם עד 2 ספרות אחרי הנקודה)',
      examples: ['0', '50', '99.99', '100', '25.5'],
      category: 'validation'
    },
    {
      id: 'integer_positive',
      name: 'Positive Integer',
      nameHebrew: 'מספר שלם חיובי',
      pattern: '^[1-9][0-9]*$',
      description: 'מספר שלם חיובי (ללא אפס)',
      examples: ['1', '99', '1000', '999999'],
      category: 'validation'
    },
    
    // Financial Patterns
    {
      id: 'credit_card_visa',
      name: 'Visa Card',
      nameHebrew: 'כרטיס ויזה',
      pattern: '^4[0-9]{12}(?:[0-9]{3})?$',
      description: 'מספר כרטיס Visa (13 או 16 ספרות, מתחיל ב-4)',
      examples: ['4111111111111111', '4012888888881881'],
      category: 'financial'
    },
    {
      id: 'credit_card_mastercard',
      name: 'MasterCard',
      nameHebrew: 'מאסטרקארד',
      pattern: '^5[1-5][0-9]{14}$',
      description: 'מספר כרטיס MasterCard (16 ספרות, מתחיל ב-51-55)',
      examples: ['5555555555554444', '5105105105105100'],
      category: 'financial'
    },
    {
      id: 'credit_card_amex',
      name: 'American Express',
      nameHebrew: 'אמריקן אקספרס',
      pattern: '^3[47][0-9]{13}$',
      description: 'מספר כרטיס AmEx (15 ספרות, מתחיל ב-34 או 37)',
      examples: ['378282246310005', '371449635398431'],
      category: 'financial'
    },
    {
      id: 'credit_card',
      name: 'Credit Card (General)',
      nameHebrew: 'כרטיס אשראי (כללי)',
      pattern: '^[0-9]{13,19}$',
      description: 'מספר כרטיס אשראי כללי (13-19 ספרות)',
      examples: ['4111111111111111', '5500000000000004', '378282246310005'],
      category: 'financial'
    },
    {
      id: 'currency_amount',
      name: 'Currency Amount',
      nameHebrew: 'סכום כספי',
      pattern: '^[0-9]{1,10}(\\.[0-9]{1,2})?$',
      description: 'סכום כספי (עד 10 ספרות, עד 2 ספרות אחרי הנקודה)',
      examples: ['100', '1234.56', '99.99', '1000000.00'],
      category: 'financial'
    },
    
    // General Patterns
    {
      id: 'email',
      name: 'Email Address',
      nameHebrew: 'כתובת דוא"ל',
      pattern: '^[a-zA-Z0-9.!#$%&\'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$',
      description: 'כתובת אימייל תקנית (RFC 5322 compliant)',
      examples: ['user@example.com', 'test@domain.co.il', 'admin@company.com'],
      category: 'general'
    },
    {
      id: 'url',
      name: 'URL',
      nameHebrew: 'כתובת אתר (URL)',
      pattern: '^https?://[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}.*$',
      description: 'כתובת אתר אינטרנט (HTTP/HTTPS)',
      examples: ['https://example.com', 'http://site.co.il/page', 'https://api.example.com/v1'],
      category: 'general'
    },
    {
      id: 'date_iso',
      name: 'Date (ISO 8601)',
      nameHebrew: 'תאריך (ISO)',
      pattern: '^[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$',
      description: 'תאריך בפורמט YYYY-MM-DD עם אימות חודש (01-12) ויום (01-31)',
      examples: ['2025-01-15', '2024-12-31', '2023-06-30'],
      category: 'general'
    },
    {
      id: 'time_24h',
      name: 'Time (24h)',
      nameHebrew: 'שעה (24 שעות)',
      pattern: '^([01][0-9]|2[0-3]):[0-5][0-9](:[0-5][0-9])?$',
      description: 'שעה בפורמט 24 שעות HH:MM או HH:MM:SS',
      examples: ['09:30', '14:45:30', '23:59', '00:00'],
      category: 'general'
    },
    {
      id: 'ipv4',
      name: 'IPv4 Address',
      nameHebrew: 'כתובת IP (IPv4)',
      pattern: '^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$',
      description: 'כתובת IPv4',
      examples: ['192.168.1.1', '10.0.0.1', '172.16.0.1'],
      category: 'general'
    },
    {
      id: 'uuid',
      name: 'UUID',
      nameHebrew: 'מזהה ייחודי UUID',
      pattern: '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
      description: 'מזהה ייחודי UUID (RFC 4122 compliant)',
      examples: ['550e8400-e29b-41d4-a716-446655440000', '6ba7b810-9dad-11d1-80b4-00c04fd430c8'],
      category: 'general'
    }
  ];

  // Combine predefined and custom patterns
  const allPatterns = [...predefinedPatterns, ...customPatterns];

  // Test pattern against strings
  const testPatternAgainstStrings = (pattern: string, strings: string[]): Array<{string: string; matches: boolean; error?: boolean}> => {
    try {
      const regex = new RegExp(pattern);
      return strings.map(str => ({
        string: str,
        matches: regex.test(str)
      }));
    } catch {
      return strings.map(str => ({
        string: str,
        matches: false,
        error: true
      }));
    }
  };

  const handleSelectPattern = (pattern: RegexPattern) => {
    setSelectedPattern(pattern.pattern);
    setTestPattern(pattern.pattern);
    setTestStrings(pattern.examples);
  };

  // Copy pattern to clipboard
  const handleCopyToClipboard = useCallback(() => {
    const patternToCopy = testPattern || selectedPattern;
    if (!patternToCopy) {
      message.warning('אין תבנית לשימוש');
      return;
    }

    navigator.clipboard.writeText(patternToCopy);
    message.success(
      <div>
        <div>התבנית הועתקה ללוח!</div>
        <div style={{ fontSize: '12px', marginTop: '4px' }}>
          לחץ בשדה התבנית והדבק עם Ctrl+V
        </div>
      </div>,
      3
    );
  }, [testPattern, selectedPattern]);

  // Save current pattern as custom
  const handleSavePattern = () => {
    if (!testPattern) {
      message.warning('אין תבנית לשמירה');
      return;
    }
    
    setIsSaveModalVisible(true);
    form.setFieldsValue({
      pattern: testPattern,
      nameHebrew: '',
      name: '',
      description: '',
      examples: testStrings.filter(s => s.trim()).join(', ')
    });
  };

  const handleSaveCustomPattern = (values: any) => {
    const newPattern: RegexPattern = {
      id: `custom_${Date.now()}`,
      name: values.name || 'Custom Pattern',
      nameHebrew: values.nameHebrew,
      pattern: values.pattern,
      description: values.description || '',
      examples: values.examples ? values.examples.split(',').map((s: string) => s.trim()).filter(Boolean) : [],
      category: 'custom',
      isCustom: true,
      createdAt: new Date().toISOString()
    };

    const updatedCustomPatterns = [...customPatterns, newPattern];
    setCustomPatterns(updatedCustomPatterns);
    saveCustomPatterns(updatedCustomPatterns);
    
    message.success('התבנית נשמרה בהצלחה');
    setIsSaveModalVisible(false);
    form.resetFields();
  };

  // Delete custom pattern
  const handleDeleteCustomPattern = (patternId: string) => {
    const updatedPatterns = customPatterns.filter(p => p.id !== patternId);
    setCustomPatterns(updatedPatterns);
    saveCustomPatterns(updatedPatterns);
    message.success('התבנית נמחקה');
  };

  // Edit custom pattern
  const handleEditCustomPattern = (pattern: RegexPattern) => {
    setEditingCustomPattern(pattern);
    setIsSaveModalVisible(true);
    form.setFieldsValue({
      pattern: pattern.pattern,
      nameHebrew: pattern.nameHebrew,
      name: pattern.name,
      description: pattern.description,
      examples: pattern.examples.join(', ')
    });
  };

  const handleUpdateCustomPattern = (values: any) => {
    if (!editingCustomPattern) return;

    const updatedPattern: RegexPattern = {
      ...editingCustomPattern,
      name: values.name,
      nameHebrew: values.nameHebrew,
      pattern: values.pattern,
      description: values.description,
      examples: values.examples ? values.examples.split(',').map((s: string) => s.trim()).filter(Boolean) : []
    };

    const updatedPatterns = customPatterns.map(p => 
      p.id === editingCustomPattern.id ? updatedPattern : p
    );
    
    setCustomPatterns(updatedPatterns);
    saveCustomPatterns(updatedPatterns);
    
    message.success('התבנית עודכנה בהצלחה');
    setIsSaveModalVisible(false);
    setEditingCustomPattern(null);
    form.resetFields();
  };

  const results = testPattern ? testPatternAgainstStrings(testPattern, testStrings.filter(s => s.trim())) : [];

  // Render pattern list item
  const renderPatternItem = (pattern: RegexPattern) => (
    <List.Item
      key={pattern.id}
      actions={[
        <Button
          type="link"
          icon={<CopyOutlined />}
          onClick={() => {
            navigator.clipboard.writeText(pattern.pattern);
            message.success('התבנית הועתקה ללוח');
          }}
        >
          העתק
        </Button>,
        pattern.isCustom && (
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEditCustomPattern(pattern)}
          >
            ערוך
          </Button>
        ),
        pattern.isCustom && (
          <Popconfirm
            title="למחוק תבנית זו?"
            onConfirm={() => handleDeleteCustomPattern(pattern.id)}
            okText="כן"
            cancelText="לא"
          >
            <Button
              type="link"
              danger
              icon={<DeleteOutlined />}
            >
              מחק
            </Button>
          </Popconfirm>
        ),
        <Button
          type="primary"
          size="small"
          onClick={() => handleSelectPattern(pattern)}
        >
          בחר
        </Button>
      ].filter(Boolean)}
      style={{ backgroundColor: '#f9f9f9', padding: 12, marginBottom: 8, borderRadius: 6 }}
    >
      <List.Item.Meta
        title={
          <Space>
            <Text strong>{pattern.nameHebrew}</Text>
            <Text type="secondary">({pattern.name})</Text>
            {pattern.isCustom && <Tag color="purple">מותאם אישית</Tag>}
          </Space>
        }
        description={
          <div>
            <Text type="secondary">{pattern.description}</Text>
            <div style={{ marginTop: 8 }}>
              <Text code style={{ fontSize: 11, backgroundColor: '#2d3748', color: '#68d391', padding: '2px 6px', direction: 'ltr', fontFamily: 'monospace' }}>
                {pattern.pattern}
              </Text>
            </div>
            {pattern.examples.length > 0 && (
              <div style={{ marginTop: 4 }}>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  דוגמאות: <span style={{ direction: 'ltr', fontFamily: 'monospace' }}>{pattern.examples.join(', ')}</span>
                </Text>
              </div>
            )}
          </div>
        }
      />
    </List.Item>
  );

  return (
    <>
      <Modal
        title={<Title level={4}>עזרת Regex - תבניות נפוצות</Title>}
        open={visible}
        onCancel={onClose}
        width={950}
        footer={[
          <Button key="close" onClick={onClose}>
            סגור
          </Button>,
          <Button
            key="copy"
            type="default"
            icon={<CopyOutlined />}
            onClick={handleCopyToClipboard}
            disabled={!testPattern && !selectedPattern}
          >
            העתק ללוח
          </Button>,
          <Button
            key="use"
            type="primary"
            icon={<CopyOutlined />}
            onClick={() => {
              handleCopyToClipboard();
              onClose();
            }}
            disabled={!testPattern && !selectedPattern}
          >
            העתק וסגור
          </Button>
        ]}
      >
        <Tabs activeKey={activeTab} onChange={setActiveTab}>
          {/* Pattern Library Tab */}
          <Tabs.TabPane tab="תבניות נפוצות" key="patterns">
            <Alert
              message="ספריית תבניות Regex"
              description="בחר תבנית מוכנה מהרשימה. לחץ 'העתק ללוח' ואז הדבק בשדה התבנית עם Ctrl+V"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Collapse defaultActiveKey={['israeli']} accordion={false}>
              {/* Israeli Patterns */}
              <Panel header="🇮🇱 תבניות ישראליות" key="israeli">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'israeli')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Banking Patterns */}
              <Panel header="🏦 תבניות בנקאיות" key="banking">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'banking')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Government Patterns */}
              <Panel header="🏛️ תבניות ממשלתיות" key="government">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'government')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Business Patterns */}
              <Panel header="🏢 תבניות עסקיות" key="business">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'business')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Personal Patterns */}
              <Panel header="👤 תבניות אישיות" key="personal">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'personal')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Validation Patterns */}
              <Panel header="✅ תבניות ולידציה" key="validation">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'validation')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Financial Patterns */}
              <Panel header="💳 תבניות פיננסיות" key="financial">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'financial')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* General Patterns */}
              <Panel header="🌐 תבניות כלליות" key="general">
                <List
                  dataSource={allPatterns.filter(p => p.category === 'general')}
                  renderItem={renderPatternItem}
                />
              </Panel>

              {/* Custom Patterns */}
              {customPatterns.length > 0 && (
                <Panel header="⭐ התבניות שלי" key="custom">
                  <List
                    dataSource={allPatterns.filter(p => p.category === 'custom')}
                    renderItem={renderPatternItem}
                  />
                </Panel>
              )}
            </Collapse>
          </Tabs.TabPane>

          {/* Pattern Tester Tab */}
          <Tabs.TabPane tab="בודק תבניות" key="tester">
            <Alert
              message="בדוק תבנית Regex"
              description="הזן תבנית Regex ומחרוזות לבדיקה. לחץ 'העתק ללוח' והדבק בשדה התבנית עם Ctrl+V"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <div style={{ marginBottom: 16 }}>
              <Text strong>תבנית Regex:</Text>
              <Input
                className="ltr-field"
                value={testPattern}
                onChange={(e) => setTestPattern(e.target.value)}
                placeholder="^[0-9]{9}$"
                style={{ marginTop: 8 }}
                size="large"
              />
            </div>

            <Divider />

            <div>
              <Space style={{ marginBottom: 8 }}>
                <Text strong>מחרוזות לבדיקה:</Text>
                <Button
                  size="small"
                  onClick={() => setTestStrings([...testStrings, ''])}
                >
                  הוסף מחרוזת
                </Button>
                <Button
                  size="small"
                  icon={<SaveOutlined />}
                  onClick={handleSavePattern}
                  disabled={!testPattern}
                >
                  שמור כתבנית מותאמת
                </Button>
              </Space>

              {testStrings.map((str, index) => (
                <div key={index} style={{ marginBottom: 8 }}>
                  <Row gutter={8}>
                    <Col span={18}>
                      <Input
                        className="ltr-field"
                        value={str}
                        onChange={(e) => {
                          const newStrings = [...testStrings];
                          newStrings[index] = e.target.value;
                          setTestStrings(newStrings);
                        }}
                        placeholder="הזן מחרוזת לבדיקה..."
                      />
                    </Col>
                    <Col span={4}>
                      {results[index] && (
                        <Tag
                          icon={results[index].matches ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
                          color={results[index].matches ? 'success' : 'error'}
                          style={{ width: '100%', textAlign: 'center' }}
                        >
                          {results[index].matches ? 'תואם' : 'לא תואם'}
                        </Tag>
                      )}
                    </Col>
                    <Col span={2}>
                      {testStrings.length > 1 && (
                        <Button
                          danger
                          size="small"
                          onClick={() => {
                            const newStrings = testStrings.filter((_, i) => i !== index);
                            setTestStrings(newStrings);
                          }}
                        >
                          ✕
                        </Button>
                      )}
                    </Col>
                  </Row>
                </div>
              ))}
            </div>

            {testPattern && results.length > 0 && (
              <>
                <Divider />
                <Card size="small" style={{ backgroundColor: '#f0f9ff', border: '1px solid #bae7ff' }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <Text strong>תוצאות:</Text>
                    <Space wrap>
                      <Tag color="success">
                        {results.filter(r => r.matches).length} תואמים
                      </Tag>
                      <Tag color="error">
                        {results.filter(r => !r.matches && !r.error).length} לא תואמים
                      </Tag>
                      {results.some(r => r.error) && (
                        <Tag color="warning">שגיאה בתבנית</Tag>
                      )}
                    </Space>
                  </Space>
                </Card>
              </>
            )}
          </Tabs.TabPane>

          {/* Pattern Builder Tab */}
          <Tabs.TabPane tab="בונה תבניות" key="builder">
            <Alert
              message="בונה תבניות Regex ויזואלי"
              description="בנה תבנית Regex שלב אחר שלב"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Card title="אותיות וספרות" size="small" style={{ marginBottom: 12 }}>
              <Space wrap>
                <Button onClick={() => setTestPattern(testPattern + '[0-9]')}>ספרה (0-9)</Button>
                <Button onClick={() => setTestPattern(testPattern + '[a-z]')}>אות קטנה (a-z)</Button>
                <Button onClick={() => setTestPattern(testPattern + '[A-Z]')}>אות גדולה (A-Z)</Button>
                <Button onClick={() => setTestPattern(testPattern + '[א-ת]')}>אות עברית (א-ת)</Button>
                <Button onClick={() => setTestPattern(testPattern + '[a-zA-Z0-9]')}>אלפאנומרי</Button>
                <Button onClick={() => setTestPattern(testPattern + '\\s')}>רווח</Button>
              </Space>
            </Card>

            <Card title="כמויות" size="small" style={{ marginBottom: 12 }}>
              <Space wrap>
                <Button onClick={() => setTestPattern(testPattern + '+')}>+ (אחד או יותר)</Button>
                <Button onClick={() => setTestPattern(testPattern + '*')}>* (אפס או יותר)</Button>
                <Button onClick={() => setTestPattern(testPattern + '?')}>? (אפציונלי)</Button>
                <Button onClick={() => setTestPattern(testPattern + '{3}')}>{'לדוגמה{3} (בדיוק 3)'}</Button>
                <Button onClick={() => setTestPattern(testPattern + '{2,5}')}>{'לדוגמה{2,5} (2 עד 5)'}</Button>
              </Space>
            </Card>

            <Card title="עוגנים ומבנה" size="small" style={{ marginBottom: 12 }}>
              <Space wrap>
                <Button onClick={() => setTestPattern('^' + testPattern)}>^ (התחלה)</Button>
                <Button onClick={() => setTestPattern(testPattern + '$')}>$ (סוף)</Button>
                <Button onClick={() => setTestPattern(testPattern + '|')}>| (או)</Button>
                <Button onClick={() => setTestPattern(testPattern + '()')}>() (קבוצה)</Button>
              </Space>
            </Card>

            <Divider />

            <div>
              <Text strong>תבנית נוכחית:</Text>
              <div style={{ marginTop: 8, padding: 12, backgroundColor: '#2d3748', borderRadius: 6 }}>
                <Text code style={{ color: '#68d391', fontSize: 14, direction: 'ltr', fontFamily: 'monospace' }}>
                  {testPattern || '(ריק)'}
                </Text>
              </div>
              <Space style={{ marginTop: 8 }}>
                <Button
                  danger
                  size="small"
                  onClick={() => setTestPattern('')}
                >
                  נקה תבנית
                </Button>
                <Button
                  size="small"
                  icon={<SaveOutlined />}
                  onClick={handleSavePattern}
                  disabled={!testPattern}
                >
                  שמור כתבנית מותאמת
                </Button>
              </Space>
            </div>
          </Tabs.TabPane>

          {/* Help Tab */}
          <Tabs.TabPane tab="עזרה" key="help">
            <Title level={5}>מדריך מהיר ל-Regex</Title>

            <Collapse defaultActiveKey={['basic']} ghost>
              <Panel header="סימנים בסיסיים" key="basic">
                <List size="small">
                  <List.Item>
                    <Text code>.</Text> - כל תו (למעט שורה חדשה)
                  </List.Item>
                  <List.Item>
                    <Text code>\d</Text> - ספרה (0-9), שקול ל-[0-9]
                  </List.Item>
                  <List.Item>
                    <Text code>\w</Text> - תו מילה (a-z, A-Z, 0-9, _)
                  </List.Item>
                  <List.Item>
                    <Text code>\s</Text> - רווח לבן (רווח, טאב, שורה חדשה)
                  </List.Item>
                  <List.Item>
                    <Text code>^</Text> - התחלת המחרוזת
                  </List.Item>
                  <List.Item>
                    <Text code>$</Text> - סוף המחרוזת
                  </List.Item>
                </List>
              </Panel>

              <Panel header="כמויות" key="quantifiers">
                <List size="small">
                  <List.Item>
                    <Text code>*</Text> - אפס או יותר פעמים
                  </List.Item>
                  <List.Item>
                    <Text code>+</Text> - פעם אחת או יותר
                  </List.Item>
                  <List.Item>
                    <Text code>?</Text> - אפס או פעם אחת (אופציונלי)
                  </List.Item>
                  <List.Item>
                    <Text code>{'לדוגמה{n}'}</Text> - בדיוק n פעמים
                  </List.Item>
                  <List.Item>
                    <Text code>{'לדוגמה{n,m}'}</Text> - בין n ל-m פעמים
                  </List.Item>
                </List>
              </Panel>

              <Panel header="קבוצות ותווים" key="groups">
                <List size="small">
                  <List.Item>
                    <Text code>[abc]</Text> - אחד מהתווים a, b, או c
                  </List.Item>
                  <List.Item>
                    <Text code>[a-z]</Text> - כל אות קטנה
                  </List.Item>
                  <List.Item>
                    <Text code>[0-9]</Text> - כל ספרה
                  </List.Item>
                  <List.Item>
                    <Text code>[א-ת]</Text> - כל אות עברית
                  </List.Item>
                  <List.Item>
                    <Text code>(abc)</Text> - קבוצה שתתפס
                  </List.Item>
                  <List.Item>
                    <Text code>a|b</Text> - a או b
                  </List.Item>
                </List>
              </Panel>

              <Panel header="דוגמאות ישראליות" key="examples">
                <List size="small">
                  <List.Item>
                    <Text strong>תעודת זהות:</Text> <Text code>^[0-9]{'לדוגמה{9}'}$</Text>
                    <br />
                    <Text type="secondary">9 ספרות בדיוק</Text>
                  </List.Item>
                  <List.Item>
                    <Text strong>טלפון נייד:</Text> <Text code>^05[0-9]{'לדוגמה{8}'}$</Text>
                    <br />
                    <Text type="secondary">מתחיל ב-05 ואחריו 8 ספרות</Text>
                  </List.Item>
                  <List.Item>
                    <Text strong>טקסט עברי:</Text> <Text code>^[\u0590-\u05FF\s]+$</Text>
                    <br />
                    <Text type="secondary">אותיות עבריות ורווחים בלבד</Text>
                  </List.Item>
                  <List.Item>
                    <Text strong>מזהה עסקה:</Text> <Text code>^TXN-\d{'לדוגמה{8}'}$</Text>
                    <br />
                    <Text type="secondary">TXN- ואחריו 8 ספרות</Text>
                  </List.Item>
                </List>
              </Panel>
            </Collapse>
          </Tabs.TabPane>
        </Tabs>

        {selectedPattern && (
          <Card size="small" style={{ marginTop: 16, backgroundColor: '#f0fdf4', border: '1px solid #86efac' }}>
            <Space direction="vertical" style={{ width: '100%' }}>
              <Text strong>תבנית נבחרה:</Text>
              <Text code style={{ fontSize: 13, backgroundColor: '#2d3748', color: '#68d391', padding: '4px 8px', direction: 'ltr', fontFamily: 'monospace' }}>
                {selectedPattern}
              </Text>
              <Button
                type="primary"
                size="small"
                icon={<CopyOutlined />}
                onClick={() => {
                  navigator.clipboard.writeText(selectedPattern);
                  message.success('תבנית הועתקה ללוח');
                }}
              >
                העתק תבנית
              </Button>
            </Space>
          </Card>
        )}
      </Modal>

      {/* Save Pattern Modal */}
      <Modal
        title={editingCustomPattern ? 'ערוך תבנית מותאמת' : 'שמור תבנית מותאמת'}
        open={isSaveModalVisible}
        onCancel={() => {
          setIsSaveModalVisible(false);
          setEditingCustomPattern(null);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        okText={editingCustomPattern ? 'שמור שינויים' : 'שמור'}
        cancelText="ביטול"
        width={600}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={editingCustomPattern ? handleUpdateCustomPattern : handleSaveCustomPattern}
        >
          <Form.Item
            name="nameHebrew"
            label="שם בעברית"
            rules={[{ required: true, message: 'נא להזין שם בעברית' }]}
          >
            <Input placeholder="למשל: תעודת זהות מותאמת" />
          </Form.Item>

          <Form.Item
            name="name"
            label="שם באנגלית"
          >
            <Input placeholder="e.g., Custom ID Pattern" />
          </Form.Item>

          <Form.Item
            name="pattern"
            label="תבנית Regex"
            rules={[{ required: true, message: 'נא להזין תבנית' }]}
          >
            <Input
              className="ltr-field"
              placeholder="^[0-9]{9}$"
            />
          </Form.Item>

          <Form.Item
            name="description"
            label="תיאור"
          >
            <TextArea rows={2} placeholder="תיאור קצר של התבנית..." />
          </Form.Item>

          <Form.Item
            name="examples"
            label="דוגמאות (מופרדות בפסיקים)"
          >
            <Input placeholder="123456789, 987654321, 555666777" />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
};

export default RegexHelperDialog;
