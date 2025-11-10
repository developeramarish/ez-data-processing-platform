// Constants and options for DataSource forms

// Hebrew categories synchronized with DemoDataGenerator
export const DATASOURCE_CATEGORIES = [
  "מכירות",
  "כספים",
  "משאבי אנוש",
  "מלאי",
  "שירות לקוחות",
  "שיווק",
  "לוגיסטיקה",
  "תפעול",
  "מחקר ופיתוח",
  "רכש"
] as const;

export const SCHEDULE_FREQUENCY_OPTIONS = [
  { value: 'Manual', label: 'ידני - רק עם הפעלה ידנית' },
  { value: 'Every5Minutes', label: '🕐 כל 5 דקות' },
  { value: 'Every10Minutes', label: '🕐 כל 10 דקות' },
  { value: 'Every15Minutes', label: '🕐 כל 15 דקות' },
  { value: 'Every30Minutes', label: '🕐 כל 30 דקות' },
  { value: 'Hourly', label: '🕐 כל שעה' },
  { value: 'Every2Hours', label: '🕑 כל שעתיים' },
  { value: 'Every3Hours', label: '🕒 כל 3 שעות' },
  { value: 'Every6Hours', label: '🕕 כל 6 שעות' },
  { value: 'Daily', label: '🌙 יומי - כל יום בחצות' },
  { value: 'Daily8AM', label: '☀️ יומי - כל יום ב-08:00' },
  { value: 'DailyNoon', label: '☀️ יומי - כל יום בצהריים' },
  { value: 'Weekdays8AM', label: '💼 ימי חול ב-08:00' },
  { value: 'Weekly', label: '📅 שבועי - פעם בשבוע' },
  { value: 'Monthly', label: '📆 חודשי - ראשון לחודש' },
  { value: 'Custom', label: '⚙️ מותאם אישית (Cron)' }
];

export const FILE_TYPE_OPTIONS = [
  { value: 'CSV', label: 'CSV - Comma Separated Values' },
  { value: 'Excel', label: 'Excel - גיליונות אלקטרוניים' },
  { value: 'JSON', label: 'JSON - JavaScript Object Notation' },
  { value: 'XML', label: 'XML - Extensible Markup Language' }
];

export const CONNECTION_TYPE_OPTIONS = [
  { value: 'Local', label: 'Local - תיקייה מקומית', icon: 'FileOutlined' },
  { value: 'SFTP', label: 'SFTP - Secure FTP', icon: 'ApiOutlined' },
  { value: 'FTP', label: 'FTP - File Transfer Protocol', icon: 'ApiOutlined' },
  { value: 'HTTP', label: 'HTTP/HTTPS - Web API', icon: 'ApiOutlined' },
  { value: 'Kafka', label: 'Kafka - Message Queue', icon: 'ApiOutlined' }
];

export const KAFKA_SECURITY_PROTOCOLS = [
  { value: 'PLAINTEXT', label: 'PLAINTEXT - ללא הצפנה' },
  { value: 'SSL', label: 'SSL - הצפנה מלאה' },
  { value: 'SASL_PLAINTEXT', label: 'SASL_PLAINTEXT - אימות בלבד' },
  { value: 'SASL_SSL', label: 'SASL_SSL - אימות והצפנה' }
];

export const KAFKA_OFFSET_RESET = [
  { value: 'earliest', label: 'Earliest - מההתחלה' },
  { value: 'latest', label: 'Latest - מהסוף' }
];

export const CSV_DELIMITER_OPTIONS = [
  { value: ',', label: 'פסיק (,)' },
  { value: ';', label: 'נקודה-פסיק (;)' },
  { value: '\t', label: 'Tab' },
  { value: '|', label: 'Pipe (|)' }
];

export const ENCODING_OPTIONS = [
  { value: 'UTF-8', label: 'UTF-8 (מומלץ)' },
  { value: 'Windows-1255', label: 'Windows-1255 (עברית)' },
  { value: 'ISO-8859-8', label: 'ISO-8859-8 (עברית)' },
  { value: 'UTF-16', label: 'UTF-16' }
];

export const DEFAULT_FORM_VALUES = {
  isActive: true,
  category: 'financial',
  connectionType: 'Local',
  fileType: 'CSV',
  encoding: 'UTF-8',
  hasHeaders: true,
  csvDelimiter: ',',
  scheduleFrequency: 'Manual',
  scheduleEnabled: false,
  skipInvalidRecords: false,
  notifyOnSuccess: false,
  notifyOnFailure: true,
  retentionDays: 30
};
