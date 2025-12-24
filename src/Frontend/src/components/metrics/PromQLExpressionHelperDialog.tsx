import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { Modal, Tabs, Space, Typography, Card, Input, Button, Tag, Alert, Divider, Collapse, Badge } from 'antd';
import { CodeOutlined, BookOutlined, ThunderboltOutlined, FunctionOutlined, CheckCircleOutlined, CloseCircleOutlined, SearchOutlined, DatabaseOutlined, SettingOutlined, BarChartOutlined } from '@ant-design/icons';

const { Text } = Typography;
const { TextArea } = Input;

// Cursor position tracking for inserting text at cursor or replacing selection
interface CursorState {
  start: number;
  end: number;
}

export interface AvailableMetric {
  name: string;
  displayName: string;
  description?: string;
  prometheusType?: string;
  category?: string;
  labels?: string[];
}

interface PromQLExpressionHelperDialogProps {
  visible: boolean;
  onClose: () => void;
  onSelect: (expression: string) => void;
  currentExpression?: string;
  metricName?: string;
  /** Optional array of available datasource-specific metrics */
  availableMetrics?: AvailableMetric[];
  /** Optional array of system metrics (infrastructure metrics like CPU, memory, etc.) */
  systemMetrics?: AvailableMetric[];
  /** Whether to show the built-in system metrics section (if no selected metrics provided) */
  showSystemMetrics?: boolean;
  /** Selected business metrics to show (if provided, only these will be shown) */
  selectedBusinessMetrics?: AvailableMetric[];
  /** Selected system metrics to show (if provided, only these will be shown) */
  selectedSystemMetrics?: AvailableMetric[];
}

// Global business metrics from BusinessMetrics.cs - operational metrics for all datasources
// Exported for use in other components (e.g., AlertsManagement)
export const GLOBAL_BUSINESS_METRICS: AvailableMetric[] = [
  // Records Metrics
  { name: 'business_records_processed_total', displayName: 'Records Processed Total', description: 'סה״כ רשומות שעברו עיבוד מוצלח במערכת', prometheusType: 'counter', category: 'records', labels: ['datasource_id', 'datasource_name', 'job_id'] },
  { name: 'business_invalid_records_total', displayName: 'Invalid Records Total', description: 'רשומות שנכשלו בולידציה או עיבוד', prometheusType: 'counter', category: 'records', labels: ['datasource_id', 'error_type'] },
  { name: 'business_records_skipped_total', displayName: 'Records Skipped Total', description: 'רשומות שדולגו עקב כללי סינון', prometheusType: 'counter', category: 'records', labels: ['datasource_id', 'reason'] },
  { name: 'business_output_records_total', displayName: 'Output Records Total', description: 'רשומות שנשלחו בהצלחה ליעד הפלט', prometheusType: 'counter', category: 'records', labels: ['datasource_id', 'output_type'] },
  { name: 'business_dead_letter_records_total', displayName: 'Dead Letter Records Total', description: 'רשומות שנכשלו ונשלחו לתור הודעות מתות', prometheusType: 'counter', category: 'records', labels: ['datasource_id', 'error_type'] },
  // Files Metrics
  { name: 'business_files_processed_total', displayName: 'Files Processed Total', description: 'סה״כ קבצים שעברו עיבוד מלא', prometheusType: 'counter', category: 'files', labels: ['datasource_id', 'file_type'] },
  { name: 'business_files_pending', displayName: 'Files Pending', description: 'מספר קבצים הממתינים לעיבוד', prometheusType: 'gauge', category: 'files', labels: ['datasource_id'] },
  { name: 'business_file_size_bytes', displayName: 'File Size (Bytes)', description: 'התפלגות גדלי קבצים שעובדו (בבייטים)', prometheusType: 'histogram', category: 'files', labels: ['datasource_id'] },
  // Bytes/Volume Metrics
  { name: 'business_bytes_processed_total', displayName: 'Bytes Processed Total', description: 'סה״כ נפח נתונים שעובד (בבייטים)', prometheusType: 'counter', category: 'volume', labels: ['datasource_id'] },
  { name: 'business_output_bytes_total', displayName: 'Output Bytes Total', description: 'סה״כ נפח נתונים שנשלח לפלט', prometheusType: 'counter', category: 'volume', labels: ['datasource_id', 'output_type'] },
  // Jobs Metrics
  { name: 'business_active_jobs', displayName: 'Active Jobs', description: 'מספר עבודות עיבוד פעילות כרגע', prometheusType: 'gauge', category: 'jobs', labels: ['datasource_id', 'job_type'] },
  { name: 'business_jobs_completed_total', displayName: 'Jobs Completed Total', description: 'סה״כ עבודות שהסתיימו בהצלחה', prometheusType: 'counter', category: 'jobs', labels: ['datasource_id'] },
  { name: 'business_jobs_failed_total', displayName: 'Jobs Failed Total', description: 'סה״כ עבודות שנכשלו', prometheusType: 'counter', category: 'jobs', labels: ['datasource_id', 'error_type'] },
  { name: 'business_batches_processed_total', displayName: 'Batches Processed Total', description: 'סה״כ אצוות רשומות שעובדו', prometheusType: 'counter', category: 'jobs', labels: ['datasource_id'] },
  // Latency/Duration Metrics
  { name: 'business_processing_duration_seconds', displayName: 'Processing Duration (sec)', description: 'זמן עיבוד רשומה בודדת (שניות)', prometheusType: 'histogram', category: 'latency', labels: ['datasource_id', 'operation'] },
  { name: 'business_end_to_end_latency_seconds', displayName: 'End-to-End Latency (sec)', description: 'זמן מלא מקבלת נתון ועד פלט (שניות)', prometheusType: 'histogram', category: 'latency', labels: ['datasource_id'] },
  { name: 'business_queue_wait_time_seconds', displayName: 'Queue Wait Time (sec)', description: 'זמן שרשומה ממתינה בתור לעיבוד', prometheusType: 'histogram', category: 'latency', labels: ['datasource_id', 'queue_name'] },
  { name: 'business_validation_latency_seconds', displayName: 'Validation Latency (sec)', description: 'זמן ביצוע ולידציה על רשומה', prometheusType: 'histogram', category: 'latency', labels: ['datasource_id', 'validation_type'] },
  // Validation Metrics
  { name: 'business_validation_error_rate', displayName: 'Validation Error Rate', description: 'אחוז רשומות שנכשלו בולידציה', prometheusType: 'histogram', category: 'validation', labels: ['datasource_id', 'rule_name'] },
  // Retry Metrics
  { name: 'business_retry_attempts_total', displayName: 'Retry Attempts Total', description: 'סה״כ ניסיונות חוזרים לעיבוד כושל', prometheusType: 'counter', category: 'errors', labels: ['datasource_id', 'operation'] },
  // Migrated from Legacy DataProcessingMetrics
  { name: 'business_validation_errors_total', displayName: 'Validation Errors Total', description: 'סה״כ שגיאות ולידציה לפי סוג ודרגת חומרה', prometheusType: 'counter', category: 'validation', labels: ['data_source', 'service', 'error_type', 'severity'] },
  { name: 'business_active_datasources_total', displayName: 'Active Datasources Total', description: 'מספר מקורות נתונים פעילים כרגע', prometheusType: 'gauge', category: 'datasources', labels: [] },
  { name: 'business_messages_sent_total', displayName: 'Messages Sent Total', description: 'סה״כ הודעות שנשלחו דרך אפיק ההודעות', prometheusType: 'counter', category: 'messaging', labels: ['message_type', 'service', 'status'] },
  { name: 'business_messages_received_total', displayName: 'Messages Received Total', description: 'סה״כ הודעות שהתקבלו מאפיק ההודעות', prometheusType: 'counter', category: 'messaging', labels: ['message_type', 'service', 'status'] },
  // Queue & Output Metrics (Phase 4 additions)
  { name: 'business_queue_depth', displayName: 'Queue Depth', description: 'עומק נוכחי של תורי עיבוד (הודעות/רשומות ממתינות)', prometheusType: 'gauge', category: 'queues', labels: ['queue_name', 'service', 'priority'] },
  { name: 'business_output_destination_errors_total', displayName: 'Output Destination Errors', description: 'סה״כ שגיאות בכתיבה ליעדי פלט', prometheusType: 'counter', category: 'errors', labels: ['data_source', 'service', 'output_destination', 'error_type'] },
];

// Category labels for business metrics (Hebrew)
export const BUSINESS_METRIC_CATEGORIES: Record<string, string> = {
  records: 'רשומות',
  files: 'קבצים',
  volume: 'נפח נתונים',
  jobs: 'עבודות',
  latency: 'זמני תגובה',
  validation: 'ולידציה',
  errors: 'שגיאות',
  datasources: 'מקורות נתונים',
  messaging: 'הודעות'
};

// Predefined system/infrastructure metrics for system alerts
// Exported for use in other components (e.g., AlertsManagement)
export const SYSTEM_METRICS: AvailableMetric[] = [
  // CPU Metrics
  { name: 'process_cpu_seconds_total', displayName: 'Process CPU Seconds', description: 'סה״כ שניות CPU שנצרכו על ידי התהליך', prometheusType: 'counter', category: 'cpu', labels: ['instance', 'job'] },
  { name: 'process_cpu_usage', displayName: 'Process CPU Usage', description: 'אחוז ניצול CPU נוכחי של התהליך', prometheusType: 'gauge', category: 'cpu', labels: ['instance', 'job'] },
  { name: 'node_cpu_seconds_total', displayName: 'Node CPU Seconds', description: 'סה״כ זמן CPU של השרת', prometheusType: 'counter', category: 'cpu', labels: ['instance', 'cpu', 'mode'] },
  // Memory Metrics
  { name: 'process_resident_memory_bytes', displayName: 'Process Resident Memory', description: 'זיכרון פיזי בשימוש התהליך (בייטים)', prometheusType: 'gauge', category: 'memory', labels: ['instance', 'job'] },
  { name: 'process_virtual_memory_bytes', displayName: 'Process Virtual Memory', description: 'זיכרון וירטואלי בשימוש', prometheusType: 'gauge', category: 'memory', labels: ['instance', 'job'] },
  { name: 'dotnet_gc_heap_size_bytes', displayName: '.NET GC Heap Size', description: 'גודל ה-Heap של .NET GC', prometheusType: 'gauge', category: 'memory', labels: ['instance', 'generation'] },
  { name: 'node_memory_MemAvailable_bytes', displayName: 'Node Memory Available', description: 'זיכרון זמין בשרת', prometheusType: 'gauge', category: 'memory', labels: ['instance'] },
  { name: 'node_memory_MemTotal_bytes', displayName: 'Node Memory Total', description: 'סה״כ זיכרון בשרת', prometheusType: 'gauge', category: 'memory', labels: ['instance'] },
  // Disk Metrics
  { name: 'node_filesystem_avail_bytes', displayName: 'Filesystem Available', description: 'שטח דיסק פנוי', prometheusType: 'gauge', category: 'disk', labels: ['instance', 'device', 'mountpoint'] },
  { name: 'node_filesystem_size_bytes', displayName: 'Filesystem Size', description: 'גודל דיסק כולל', prometheusType: 'gauge', category: 'disk', labels: ['instance', 'device', 'mountpoint'] },
  // Network Metrics
  { name: 'node_network_receive_bytes_total', displayName: 'Network Receive Bytes', description: 'סה״כ בייטים שהתקבלו ברשת', prometheusType: 'counter', category: 'network', labels: ['instance', 'device'] },
  { name: 'node_network_transmit_bytes_total', displayName: 'Network Transmit Bytes', description: 'סה״כ בייטים שנשלחו ברשת', prometheusType: 'counter', category: 'network', labels: ['instance', 'device'] },
  // HTTP/Request Metrics
  { name: 'http_requests_total', displayName: 'HTTP Requests Total', description: 'סה״כ בקשות HTTP שהתקבלו', prometheusType: 'counter', category: 'http', labels: ['instance', 'method', 'status', 'path'] },
  { name: 'http_request_duration_seconds', displayName: 'HTTP Request Duration', description: 'התפלגות זמני תגובה לבקשות HTTP', prometheusType: 'histogram', category: 'http', labels: ['instance', 'method', 'path'] },
  { name: 'http_requests_in_progress', displayName: 'HTTP Requests In Progress', description: 'מספר בקשות HTTP בטיפול כעת', prometheusType: 'gauge', category: 'http', labels: ['instance', 'method'] },
  // Kubernetes Metrics
  { name: 'kube_pod_status_phase', displayName: 'Pod Status Phase', description: 'שלב נוכחי של Pod בקלאסטר', prometheusType: 'gauge', category: 'kubernetes', labels: ['namespace', 'pod', 'phase'] },
  { name: 'kube_deployment_status_replicas_available', displayName: 'Deployment Replicas Available', description: 'מספר רפליקות זמינות ב-Deployment', prometheusType: 'gauge', category: 'kubernetes', labels: ['namespace', 'deployment'] },
  { name: 'container_cpu_usage_seconds_total', displayName: 'Container CPU Usage', description: 'שימוש CPU של קונטיינר', prometheusType: 'counter', category: 'kubernetes', labels: ['namespace', 'pod', 'container'] },
  { name: 'container_memory_usage_bytes', displayName: 'Container Memory Usage', description: 'שימוש זיכרון של קונטיינר', prometheusType: 'gauge', category: 'kubernetes', labels: ['namespace', 'pod', 'container'] },
  // Service Health
  { name: 'up', displayName: 'Service Up', description: 'האם השירות פעיל (1) או לא (0)', prometheusType: 'gauge', category: 'health', labels: ['instance', 'job'] },
  { name: 'scrape_duration_seconds', displayName: 'Scrape Duration', description: 'זמן שלקח לאסוף מדדים מהשירות', prometheusType: 'gauge', category: 'health', labels: ['instance', 'job'] },
];

// Category labels for system metrics (Hebrew)
export const SYSTEM_METRIC_CATEGORIES: Record<string, string> = {
  cpu: 'מעבד (CPU)',
  memory: 'זיכרון',
  disk: 'אחסון',
  network: 'רשת',
  http: 'HTTP/API',
  kubernetes: 'Kubernetes',
  health: 'בריאות שירות'
};

const PROMQL_FUNCTIONS = [
  {
    category: 'צבירה (Aggregation)',
    functions: [
      { name: 'sum', syntax: 'sum(metric)', description: 'סכום כל הערכים', example: 'sum(http_requests_total)', detail: 'מחשב את הסכום של כל הערכים. שימושי לצבירת בקשות, שגיאות וכו\'' },
      { name: 'avg', syntax: 'avg(metric)', description: 'ממוצע', example: 'avg(cpu_usage)', detail: 'מחשב ממוצע של כל הערכים' },
      { name: 'min', syntax: 'min(metric)', description: 'מינימום', example: 'min(response_time)', detail: 'מוצא את הערך המינימלי' },
      { name: 'max', syntax: 'max(metric)', description: 'מקסימום', example: 'max(memory_usage)', detail: 'מוצא את הערך המקסימלי' },
      { name: 'count', syntax: 'count(metric)', description: 'ספירה', example: 'count(up == 1)', detail: 'סופר כמה ערכים יש' },
      { name: 'sum by', syntax: 'sum(metric) by (label)', description: 'סכום לפי תווית', example: 'sum(requests) by (status)', detail: 'צובר לפי קבוצות של תוויות' },
      { name: 'avg by', syntax: 'avg(metric) by (label)', description: 'ממוצע לפי תווית', example: 'avg(latency) by (endpoint)', detail: 'ממוצע לכל קבוצה' },
      { name: 'topk', syntax: 'topk(5, metric)', description: 'K הגבוהים ביותר', example: 'topk(10, cpu_usage)', detail: 'מחזיר את ה-K ערכים הגבוהים ביותר' },
      { name: 'bottomk', syntax: 'bottomk(5, metric)', description: 'K הנמוכים ביותר', example: 'bottomk(5, free_memory)', detail: 'מחזיר את ה-K ערכים הנמוכים ביותר' }
    ]
  },
  {
    category: 'זמן (Time)',
    functions: [
      { name: 'rate', syntax: 'rate(metric[5m])', description: 'קצב שינוי לשנייה', example: 'rate(requests_total[5m])', detail: 'מחשב קצב שינוי לשנייה עבור counter. חובה למדדי counter' },
      { name: 'irate', syntax: 'irate(metric[5m])', description: 'קצב מיידי', example: 'irate(errors_total[1m])', detail: 'קצב מיידי - רגיש יותר לשינויים פתאומיים' },
      { name: 'increase', syntax: 'increase(metric[1h])', description: 'עלייה בפרק זמן', example: 'increase(sales_total[1h])', detail: 'כמה עלה המונה בפרק הזמן הנתון' },
      { name: 'avg_over_time', syntax: 'avg_over_time(metric[5m])', description: 'ממוצע על פני זמן', example: 'avg_over_time(cpu[10m])', detail: 'ממוצע של כל הערכים בחלון הזמן' },
      { name: 'max_over_time', syntax: 'max_over_time(metric[5m])', description: 'מקסימום על פני זמן', example: 'max_over_time(latency[1h])', detail: 'הערך המקסימלי בחלון הזמן' },
      { name: 'min_over_time', syntax: 'min_over_time(metric[5m])', description: 'מינימום על פני זמן', example: 'min_over_time(free_memory[30m])', detail: 'הערך המינימלי בחלון הזמן' },
      { name: 'sum_over_time', syntax: 'sum_over_time(metric[5m])', description: 'סכום על פני זמן', example: 'sum_over_time(errors[1h])', detail: 'סכום כל הערכים בחלון הזמן' },
      { name: 'changes', syntax: 'changes(metric[5m])', description: 'מספר שינויים', example: 'changes(version[1h])', detail: 'כמה פעמים הערך השתנה' },
      { name: 'deriv', syntax: 'deriv(metric[5m])', description: 'נגזרת', example: 'deriv(disk_usage[1h])', detail: 'קצב השינוי של gauge - נגזרת לפי זמן' }
    ]
  },
  {
    category: 'פעולות חשבון (Math)',
    functions: [
      { name: 'abs', syntax: 'abs(metric)', description: 'ערך מוחלט', example: 'abs(temperature)', detail: 'מחזיר ערך מוחלט (ללא סימן)' },
      { name: 'ceil', syntax: 'ceil(metric)', description: 'עיגול למעלה', example: 'ceil(cpu_usage)', detail: 'עיגול כלפי מעלה למספר שלם' },
      { name: 'floor', syntax: 'floor(metric)', description: 'עיגול למטה', example: 'floor(response_time)', detail: 'עיגול כלפי מטה למספר שלם' },
      { name: 'round', syntax: 'round(metric)', description: 'עיגול', example: 'round(memory_percent)', detail: 'עיגול רגיל למספר השלם הקרוב' },
      { name: 'clamp_max', syntax: 'clamp_max(metric, 100)', description: 'הגבל מקסימום', example: 'clamp_max(cpu, 100)', detail: 'מגביל ערך מקסימלי' },
      { name: 'clamp_min', syntax: 'clamp_min(metric, 0)', description: 'הגבל מינימום', example: 'clamp_min(temp, 0)', detail: 'מגביל ערך מינימלי' },
      { name: 'sqrt', syntax: 'sqrt(metric)', description: 'שורש ריבועי', example: 'sqrt(variance)', detail: 'מחשב שורש ריבועי' },
      { name: 'ln', syntax: 'ln(metric)', description: 'לוגריתם טבעי', example: 'ln(growth_rate)', detail: 'לוגריתם בבסיס e' },
      { name: 'log2', syntax: 'log2(metric)', description: 'לוגריתם בסיס 2', example: 'log2(data_size)', detail: 'לוגריתם בבסיס 2' },
      { name: 'log10', syntax: 'log10(metric)', description: 'לוגריתם בסיס 10', example: 'log10(population)', detail: 'לוגריתם בבסיס 10' }
    ]
  },
  {
    category: 'השוואות (Comparison)',
    functions: [
      { name: 'greater', syntax: 'metric > 10', description: 'גדול מ (>)', example: 'cpu_usage > 80' },
      { name: 'less', syntax: 'metric < 10', description: 'קטן מ (<)', example: 'free_memory < 1000' },
      { name: 'equals', syntax: 'metric == 1', description: 'שווה ל (==)', example: 'up == 1' },
      { name: 'not_equals', syntax: 'metric != 0', description: 'לא שווה ל (!=)', example: 'errors != 0' },
      { name: 'absent', syntax: 'absent(metric)', description: 'בדוק היעדרות', example: 'absent(heartbeat)' }
    ]
  },
  {
    category: 'אופרטורים לוגיים (Logical)',
    functions: [
      { name: 'and', syntax: 'metric1 and metric2', description: 'וגם', example: 'up == 1 and cpu < 90' },
      { name: 'or', syntax: 'metric1 or metric2', description: 'או', example: 'errors > 10 or latency > 5' },
      { name: 'unless', syntax: 'metric1 unless metric2', description: 'אלא אם כן', example: 'alerts unless maintenance' }
    ]
  }
];

const COMMON_PATTERNS = [
  {
    name: 'שיעור שגיאות',
    expression: '(sum(rate(errors_total[5m])) / sum(rate(requests_total[5m]))) * 100',
    description: 'חישוב אחוז שגיאות מתוך כלל הבקשות',
    category: 'errors',
    useCase: 'מעקב אחר איכות השירות - מתריע כאשר שיעור השגיאות גבוה מדי'
  },
  {
    name: 'ממוצע זמן תגובה P95',
    expression: 'histogram_quantile(0.95, rate(response_time_bucket[5m]))',
    description: 'חישוב percentile 95 לזמן תגובה',
    category: 'performance',
    useCase: '95% מהבקשות מסתיימות תחת זמן זה - מדד טוב לביצועים'
  },
  {
    name: 'שימוש CPU ממוצע',
    expression: 'avg(rate(cpu_seconds_total[5m])) * 100',
    description: 'שימוש ממוצע ב-CPU באחוזים',
    category: 'resources',
    useCase: 'מעקב אחר עומס CPU על השרתים'
  },
  {
    name: 'קצב בקשות לשנייה',
    expression: 'sum(rate(http_requests_total[5m]))',
    description: 'מספר בקשות HTTP לשנייה',
    category: 'throughput',
    useCase: 'מדידת throughput של האפליקציה'
  },
  {
    name: 'זיהוי שירותים לא פעילים',
    expression: 'up == 0',
    description: 'מציאת שירותים שאינם זמינים',
    category: 'availability',
    useCase: 'התראה מיידית על שירות שנפל'
  },
  {
    name: 'שימוש זיכרון באחוזים',
    expression: '(1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100',
    description: 'חישוב אחוז שימוש בזיכרון',
    category: 'resources',
    useCase: 'מעקב אחר צריכת זיכרון'
  },
  {
    name: 'תפוסת דיסק באחוזים',
    expression: '(1 - (node_filesystem_avail_bytes / node_filesystem_size_bytes)) * 100',
    description: 'חישוב אחוז תפוסת דיסק',
    category: 'resources',
    useCase: 'התראה על מחסור בשטח אחסון'
  },
  {
    name: 'קצב עלייה חד',
    expression: '(metric - metric offset 1h) > 1000',
    description: 'זיהוי עלייה חדה במדד',
    category: 'anomaly',
    useCase: 'גילוי חריגות - עלייה משמעותית במדד'
  },
  {
    name: 'שינוי באחוזים',
    expression: '((metric - metric offset 1h) / metric offset 1h) * 100',
    description: 'אחוז השינוי לעומת שעה קודמת',
    category: 'trends',
    useCase: 'מעקב אחר מגמות ושינויים'
  },
  {
    name: 'זמינות שירות',
    expression: 'avg_over_time(up[24h]) * 100',
    description: 'אחוז זמינות שירות ב-24 שעות אחרונות',
    category: 'availability',
    useCase: 'מדידת SLA - זמינות השירות'
  }
];

const PromQLExpressionHelperDialog: React.FC<PromQLExpressionHelperDialogProps> = ({
  visible,
  onClose,
  onSelect,
  currentExpression = '',
  metricName = 'metric',
  availableMetrics = [],
  systemMetrics,
  showSystemMetrics = true,
  selectedBusinessMetrics,
  selectedSystemMetrics
}) => {
  // Determine which metrics to show:
  // - If selectedBusinessMetrics provided, show only those (for alert dialog)
  // - Otherwise, show all GLOBAL_BUSINESS_METRICS (for standalone use)
  const effectiveBusinessMetrics = selectedBusinessMetrics !== undefined
    ? selectedBusinessMetrics
    : GLOBAL_BUSINESS_METRICS;

  // Same logic for system metrics
  const effectiveSystemMetrics = selectedSystemMetrics !== undefined
    ? selectedSystemMetrics
    : (systemMetrics || (showSystemMetrics ? SYSTEM_METRICS : []));
  const [expression, setExpression] = useState(currentExpression);
  const [validationResult, setValidationResult] = useState<{ valid: boolean; message: string } | null>(null);
  const [cursorState, setCursorState] = useState<CursorState>({ start: 0, end: 0 });
  const textAreaRef = useRef<HTMLTextAreaElement | null>(null);
  const [metricsFilter, setMetricsFilter] = useState('');

  // Filter metrics based on search
  const filteredDatasourceMetrics = useMemo(() =>
    availableMetrics.filter(m =>
      m.name.toLowerCase().includes(metricsFilter.toLowerCase()) ||
      m.displayName.toLowerCase().includes(metricsFilter.toLowerCase())
    ), [availableMetrics, metricsFilter]);

  const filteredBusinessMetrics = useMemo(() =>
    effectiveBusinessMetrics.filter(m =>
      m.name.toLowerCase().includes(metricsFilter.toLowerCase()) ||
      m.displayName.toLowerCase().includes(metricsFilter.toLowerCase())
    ), [effectiveBusinessMetrics, metricsFilter]);

  const filteredSystemMetrics = useMemo(() =>
    effectiveSystemMetrics.filter(m =>
      m.name.toLowerCase().includes(metricsFilter.toLowerCase()) ||
      m.displayName.toLowerCase().includes(metricsFilter.toLowerCase())
    ), [effectiveSystemMetrics, metricsFilter]);

  useEffect(() => {
    if (visible) {
      setExpression(currentExpression);
      // Reset cursor to end when dialog opens
      setCursorState({ start: currentExpression.length, end: currentExpression.length });
    }
  }, [visible, currentExpression]);

  // Track cursor position and selection on the textarea
  const handleSelectionChange = useCallback(() => {
    const textArea = textAreaRef.current;
    if (textArea) {
      setCursorState({
        start: textArea.selectionStart || 0,
        end: textArea.selectionEnd || 0
      });
    }
  }, []);

  // Insert text at cursor position or replace selection
  const insertAtCursor = useCallback((textToInsert: string) => {
    const { start, end } = cursorState;
    const before = expression.substring(0, start);
    const after = expression.substring(end);

    // Determine if we need a space before the inserted text
    const needsSpaceBefore = before.length > 0 && !before.endsWith(' ') && !before.endsWith('(');
    const prefix = needsSpaceBefore ? ' ' : '';

    const newExpression = before + prefix + textToInsert + after;
    setExpression(newExpression);

    // Update cursor position to end of inserted text
    const newCursorPos = start + prefix.length + textToInsert.length;
    setCursorState({ start: newCursorPos, end: newCursorPos });

    // Focus back on textarea and set cursor position
    setTimeout(() => {
      const textArea = textAreaRef.current;
      if (textArea) {
        textArea.focus();
        textArea.setSelectionRange(newCursorPos, newCursorPos);
      }
    }, 0);
  }, [expression, cursorState]);

  // Replace 'metric' placeholder with actual metric name in syntax
  const getActualSyntax = (syntax: string): string => {
    if (!metricName || metricName === 'metric') return syntax;
    return syntax.replace(/\bmetric\b/g, metricName);
  };

  // Basic PromQL validation
  useEffect(() => {
    if (!expression) {
      setValidationResult(null);
      return;
    }

    // Basic validation rules
    const squareBrackets = expression.match(/[[\]]/g);
    const curlyBrackets = expression.match(/[{}]/g);
    const openParens = expression.match(/[(]/g);
    const closeParens = expression.match(/[)]/g);
    
    const checks = [
      { test: expression.length > 0, message: 'ביטוי לא יכול להיות ריק' },
      { test: !expression.includes('  '), message: 'הסר רווחים כפולים' },
      { test: !squareBrackets || squareBrackets.length % 2 === 0, message: 'סוגריים מרובעות לא מאוזנות' },
      { test: !curlyBrackets || curlyBrackets.length % 2 === 0, message: 'סוגריים מסולסלות לא מאוזנות' },
      { test: (openParens?.length || 0) === (closeParens?.length || 0), message: 'סוגריים עגולות לא מאוזנות' }
    ];

    const failedCheck = checks.find(c => !c.test);
    if (failedCheck) {
      setValidationResult({ valid: false, message: failedCheck.message });
    } else {
      setValidationResult({ valid: true, message: 'הביטוי נראה תקין' });
    }
  }, [expression]);

  const handleInsertFunction = (syntax: string) => {
    insertAtCursor(syntax);
  };

  const handleUsePattern = (pattern: string) => {
    setExpression(pattern);
  };

  const handleApply = () => {
    onSelect(expression);
    onClose();
  };

  return (
    <Modal
      title={
        <Space>
          <CodeOutlined />
          <span>עוזר ביטויי PromQL - בונה ביטויים אינטראקטיבי</span>
        </Space>
      }
      open={visible}
      onCancel={onClose}
      width={1000}
      style={{ top: 20 }}
      footer={[
        <Button key="cancel" onClick={onClose}>
          ביטול
        </Button>,
        <Button 
          key="apply" 
          type="primary" 
          onClick={handleApply}
          disabled={validationResult?.valid === false}
          icon={validationResult?.valid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
        >
          השתמש בביטוי
        </Button>
      ]}
    >
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        {/* Metrics Search and Collapsible Sections */}
        <Card size="small">
          <Space direction="vertical" style={{ width: '100%' }} size="small">
            <Input
              prefix={<SearchOutlined />}
              placeholder="חפש מדדים לפי שם..."
              value={metricsFilter}
              onChange={(e) => setMetricsFilter(e.target.value)}
              allowClear
              style={{ marginBottom: 8 }}
            />
            <Collapse
              defaultActiveKey={availableMetrics.length > 0 ? ['datasource'] : ['business']}
              size="small"
              items={[
                // Datasource-Specific Metrics
                ...(availableMetrics.length > 0 ? [{
                  key: 'datasource',
                  label: (
                    <Space>
                      <DatabaseOutlined style={{ color: '#1890ff' }} />
                      <span>Datasource Metrics - מדדים ספציפיים</span>
                      <Badge count={filteredDatasourceMetrics.length} style={{ backgroundColor: '#1890ff' }} />
                    </Space>
                  ),
                  children: (
                    <Space direction="vertical" style={{ width: '100%' }} size="small">
                      {filteredDatasourceMetrics.length > 0 ? (
                        <Space wrap size={6}>
                          {filteredDatasourceMetrics.map((metric, idx) => (
                            <Button
                              key={idx}
                              size="small"
                              type="dashed"
                              onClick={() => insertAtCursor(metric.name)}
                              style={{ fontFamily: 'monospace', fontSize: 11 }}
                            >
                              <Space size={4}>
                                <span>{metric.name}</span>
                                {metric.prometheusType && (
                                  <Tag color="blue" style={{ fontSize: 9, margin: 0 }}>{metric.prometheusType}</Tag>
                                )}
                              </Space>
                            </Button>
                          ))}
                        </Space>
                      ) : (
                        <Text type="secondary">לא נמצאו תוצאות</Text>
                      )}
                    </Space>
                  ),
                  style: { backgroundColor: '#f0f7ff' }
                }] : []),
                // Business Metrics - shows selected or all depending on context
                ...(effectiveBusinessMetrics.length > 0 ? [{
                  key: 'business',
                  label: (
                    <Space>
                      <BarChartOutlined style={{ color: '#52c41a' }} />
                      <span>{selectedBusinessMetrics !== undefined ? 'מדדים עסקיים שנבחרו' : 'מדדים עסקיים גלובליים'}</span>
                      <Badge count={filteredBusinessMetrics.length} style={{ backgroundColor: '#52c41a' }} />
                    </Space>
                  ),
                  children: (
                    <Space direction="vertical" style={{ width: '100%' }} size="small">
                      {filteredBusinessMetrics.length > 0 ? (
                        <Space wrap size={6}>
                          {filteredBusinessMetrics.map((metric, idx) => (
                            <Button
                              key={idx}
                              size="small"
                              type="dashed"
                              onClick={() => insertAtCursor(metric.name)}
                              style={{ fontFamily: 'monospace', fontSize: 11 }}
                              title={`${metric.displayName}${metric.description ? ` - ${metric.description}` : ''}`}
                            >
                              <Space size={4}>
                                <span>{metric.name}</span>
                                {metric.prometheusType && (
                                  <Tag color="green" style={{ fontSize: 9, margin: 0 }}>{metric.prometheusType}</Tag>
                                )}
                              </Space>
                            </Button>
                          ))}
                        </Space>
                      ) : (
                        <Text type="secondary">לא נמצאו תוצאות</Text>
                      )}
                    </Space>
                  ),
                  style: { backgroundColor: '#f6ffed' }
                }] : []),
                // System Metrics - shows selected or all depending on context
                ...(effectiveSystemMetrics.length > 0 ? [{
                  key: 'system',
                  label: (
                    <Space>
                      <SettingOutlined style={{ color: '#fa8c16' }} />
                      <span>{selectedSystemMetrics !== undefined ? 'מדדי מערכת שנבחרו' : 'מדדי מערכת'}</span>
                      <Badge count={filteredSystemMetrics.length} style={{ backgroundColor: '#fa8c16' }} />
                    </Space>
                  ),
                  children: (
                    <Space direction="vertical" style={{ width: '100%' }} size="small">
                      {filteredSystemMetrics.length > 0 ? (
                        <Space wrap size={6}>
                          {filteredSystemMetrics.map((metric, idx) => (
                            <Button
                              key={idx}
                              size="small"
                              type="dashed"
                              onClick={() => insertAtCursor(metric.name)}
                              style={{ fontFamily: 'monospace', fontSize: 11 }}
                              title={`${metric.displayName}${metric.description ? ` - ${metric.description}` : ''}`}
                            >
                              <Space size={4}>
                                <span>{metric.name}</span>
                                {metric.prometheusType && (
                                  <Tag color="orange" style={{ fontSize: 9, margin: 0 }}>{metric.prometheusType}</Tag>
                                )}
                              </Space>
                            </Button>
                          ))}
                        </Space>
                      ) : (
                        <Text type="secondary">לא נמצאו תוצאות</Text>
                      )}
                    </Space>
                  ),
                  style: { backgroundColor: '#fff7e6' }
                }] : [])
              ]}
            />
            {availableMetrics.length === 0 && effectiveBusinessMetrics.length === 0 && effectiveSystemMetrics.length === 0 && (
              <Alert
                type="info"
                message="לא נבחרו מדדים"
                description="בחר מדדים בדיאלוג יצירת ההתרעה כדי להציג אותם כאן. עדיין ניתן להשתמש בפונקציות ובתבניות מהטאבים למטה."
                showIcon
                style={{ marginTop: 8 }}
              />
            )}
            {(availableMetrics.length > 0 || effectiveBusinessMetrics.length > 0 || effectiveSystemMetrics.length > 0) && (
              <Text type="secondary" style={{ fontSize: 11 }}>
                לחץ על מדד להוספה במיקום הסמן. השתמש בחיפוש לסינון מהיר.
              </Text>
            )}
          </Space>
        </Card>

        {/* Expression Editor */}
        <Card size="small">
          <Space direction="vertical" style={{ width: '100%' }} size="small">
            <Text strong>ביטוי PromQL:</Text>
            <TextArea
              ref={(el) => { textAreaRef.current = el?.resizableTextArea?.textArea || null; }}
              className="ltr-field"
              rows={5}
              value={expression}
              onChange={(e) => setExpression(e.target.value)}
              onSelect={handleSelectionChange}
              onClick={handleSelectionChange}
              onKeyUp={handleSelectionChange}
              placeholder="הזן ביטוי PromQL או השתמש בפונקציות ובתבניות למטה&#10;דוגמה: rate(http_requests_total[5m]) > 100"
              style={{
                fontSize: 13,
                backgroundColor: '#f6f8fa'
              }}
              status={validationResult?.valid === false ? 'error' : validationResult?.valid ? '' : ''}
            />
            {validationResult && (
              <Alert
                message={validationResult.valid ? 'תקין' : 'שגיאה'}
                description={validationResult.message}
                type={validationResult.valid ? 'success' : 'error'}
                showIcon
                icon={validationResult.valid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
              />
            )}
          </Space>
        </Card>

        <Divider style={{ margin: '12px 0' }} />

        <Tabs
          items={[
            {
              key: 'functions',
              label: (
                <Space>
                  <FunctionOutlined />
                  <span>פונקציות</span>
                </Space>
              ),
              children: (
                <div>
                  {PROMQL_FUNCTIONS.map((category, idx) => (
                    <div key={idx} style={{ marginBottom: 16 }}>
                      <Text strong>{category.category}</Text>
                      <Space wrap style={{ marginTop: 8 }}>
                        {category.functions.map((func, fidx) => (
                          <Card
                            key={fidx}
                            size="small"
                            hoverable
                            onClick={() => handleInsertFunction(getActualSyntax(func.syntax))}
                            style={{ cursor: 'pointer', width: '100%', marginBottom: 8 }}
                          >
                            <Space direction="vertical" size={2} style={{ width: '100%' }}>
                              <Space>
                                <Tag color="blue" style={{ fontFamily: 'monospace' }}>
                                  {func.name}
                                </Tag>
                                <Text strong style={{ fontSize: 12 }}>{func.description}</Text>
                              </Space>
                              <Text type="secondary" style={{ fontSize: 10 }}>
                                {(func as any).detail}
                              </Text>
                              <Card size="small" style={{ backgroundColor: '#f0f0f0', marginTop: 4 }}>
                                <Text type="secondary" style={{ fontSize: 10, fontFamily: 'monospace', direction: 'ltr' }}>
                                  {getActualSyntax(func.example)}
                                </Text>
                              </Card>
                            </Space>
                          </Card>
                        ))}
                      </Space>
                    </div>
                  ))}
                </div>
              )
            },
            {
              key: 'patterns',
              label: (
                <Space>
                  <ThunderboltOutlined />
                  <span>תבניות נפוצות</span>
                </Space>
              ),
              children: (
                <Space direction="vertical" style={{ width: '100%' }} size="small">
                  {COMMON_PATTERNS.map((pattern, idx) => (
                    <Card
                      key={idx}
                      size="small"
                      hoverable
                      onClick={() => handleUsePattern(pattern.expression)}
                      style={{ cursor: 'pointer', marginBottom: 8 }}
                    >
                      <Space direction="vertical" size={6} style={{ width: '100%' }}>
                        <Space>
                          <Text strong style={{ fontSize: 14 }}>{pattern.name}</Text>
                          <Tag color="purple">{(pattern as any).category}</Tag>
                        </Space>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          {pattern.description}
                        </Text>
                        <Text style={{ fontSize: 11, color: '#666' }}>
                          <strong>שימוש:</strong> {(pattern as any).useCase}
                        </Text>
                        <Card size="small" style={{ backgroundColor: '#f6f8fa', marginTop: 4 }}>
                          <pre style={{ 
                            margin: 0, 
                            fontFamily: 'Monaco, Consolas, monospace',
                            fontSize: 11,
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word',
                            direction: 'ltr',
                            textAlign: 'left'
                          }}>
                            {pattern.expression}
                          </pre>
                        </Card>
                        <Button size="small" type="dashed" block>
                          לחץ להשתמש בתבנית זו
                        </Button>
                      </Space>
                    </Card>
                  ))}
                </Space>
              )
            },
            {
              key: 'docs',
              label: (
                <Space>
                  <BookOutlined />
                  <span>עזרה</span>
                </Space>
              ),
              children: (
                <Space direction="vertical" style={{ width: '100%' }} size="middle">
                  <Alert
                    message="תחביר PromQL"
                    description={
                      <div style={{ fontSize: 12 }}>
                        <div><strong>בחירת מדד:</strong> <code>metric_name</code></div>
                        <div><strong>עם תוויות:</strong> <code>{`metric_name{label="value"}`}</code></div>
                        <div><strong>טווח זמן:</strong> <code>metric_name[5m]</code></div>
                        <div><strong>פעולות חשבון:</strong> <code>metric1 + metric2</code></div>
                        <div><strong>צבירה:</strong> <code>sum(metric) by (label)</code></div>
                      </div>
                    }
                    type="info"
                    showIcon
                  />
                  
                  <Card size="small" title="יחידות זמן">
                    <Space wrap>
                      <Tag>s = שניות</Tag>
                      <Tag>m = דקות</Tag>
                      <Tag>h = שעות</Tag>
                      <Tag>d = ימים</Tag>
                      <Tag>w = שבועות</Tag>
                      <Tag>y = שנים</Tag>
                    </Space>
                  </Card>

                  <Card size="small" title="אופרטורים">
                    <Space wrap>
                      <Tag>+ - * / %</Tag>
                      <Tag>{'== != > < >= <='}</Tag>
                      <Tag>and or unless</Tag>
                    </Space>
                  </Card>

                  <Alert
                    message="דוגמאות נוספות"
                    description={
                      <pre style={{ fontSize: 11, fontFamily: 'monospace', margin: 0 }}>
                        {`rate(metric[5m]) > 0.1
sum(metric) by (instance)
metric{job="app"} / 1024
avg(metric) without (instance)`}
                      </pre>
                    }
                    type="info"
                  />
                </Space>
              )
            }
          ]}
        />
      </Space>
    </Modal>
  );
};

export default PromQLExpressionHelperDialog;
