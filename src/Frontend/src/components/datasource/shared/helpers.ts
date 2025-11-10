// Helper functions for DataSource forms
import cronstrue from 'cronstrue/i18n';

/**
 * Humanizes a cron expression into Hebrew description using cronstrue library
 */
export const humanizeCron = (cronExpr: string): string => {
  if (!cronExpr) return '';
  
  try {
    const description = cronstrue.toString(cronExpr.trim(), { 
      locale: 'he',
      use24HourTimeFormat: true,
      throwExceptionOnParseError: false
    });
    return `⏰ ${description}`;
  } catch (error) {
    return '⏰ ביטוי מותאם';
  }
};

/**
 * Maps a schedule frequency to a cron expression
 */
export const frequencyToCron = (frequency: string): string | undefined => {
  const mapping: Record<string, string> = {
    'Every5Minutes': '*/5 * * * *',
    'Every10Minutes': '*/10 * * * *',
    'Every15Minutes': '*/15 * * * *',
    'Every30Minutes': '*/30 * * * *',
    'Hourly': '0 * * * *',
    'Every2Hours': '0 */2 * * *',
    'Every3Hours': '0 */3 * * *',
    'Every6Hours': '0 */6 * * *',
    'Daily': '0 0 * * *',
    'Daily6AM': '0 6 * * *',
    'Daily8AM': '0 8 * * *',
    'DailyNoon': '0 12 * * *',
    'Daily6PM': '0 18 * * *',
    'Weekdays8AM': '0 8 * * 1-5',
    'Weekdays6PM': '0 18 * * 1-5',
    'Weekly': '0 0 * * 0',
    'Monthly': '0 0 1 * *'
  };
  return mapping[frequency];
};

/**
 * Builds a connection string from form values
 */
export const buildConnectionString = (values: any): string => {
  switch (values.connectionType) {
    case 'SFTP':
    case 'FTP':
      return `${values.connectionType.toLowerCase()}://${values.connectionHost}:${values.connectionPort}${values.connectionPath}`;
    case 'HTTP':
      return values.connectionUrl;
    case 'Local':
      return values.connectionPath;
    case 'Kafka':
      return `kafka://${values.kafkaBrokers}/${values.kafkaTopic}`;
    default:
      return '';
  }
};

/**
 * Extracts file type from FilePattern (e.g., "*.csv" -> "CSV")
 */
export const extractFileTypeFromPattern = (filePattern?: string): string => {
  if (!filePattern) return 'CSV';
  
  const match = filePattern.match(/\*\.([a-zA-Z]+)/);
  if (match) {
    const extension = match[1].toLowerCase();
    switch (extension) {
      case 'csv': return 'CSV';
      case 'xls':
      case 'xlsx': return 'Excel';
      case 'json': return 'JSON';
      case 'xml': return 'XML';
      default: return 'CSV';
    }
  }
  return 'CSV';
};

/**
 * Gets display label for delimiter character
 */
export const getDelimiterLabel = (delimiter?: string): string => {
  if (!delimiter) return 'פסיק (,)';
  
  switch (delimiter) {
    case ',': return 'פסיק (,)';
    case ';': return 'נקודה-פסיק (;)';
    case '\t': return 'Tab';
    case '|': return 'Pipe (|)';
    default: return delimiter;
  }
};
