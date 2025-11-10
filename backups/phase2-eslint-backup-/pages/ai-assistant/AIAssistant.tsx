import React, { useState } from 'react';
import { Card, Button, Input, Typography, Row, Col, Avatar } from 'antd';
import {
  RobotOutlined,
  SendOutlined,
  BarChartOutlined,
  DatabaseOutlined,
  AlertOutlined,
} from '@ant-design/icons';

const { Title, Text } = Typography;
const { TextArea } = Input;

interface Message {
  id: string;
  type: 'user' | 'ai';
  content: string;
  timestamp: string;
}

const AIAssistant: React.FC = () => {
  const [inputValue, setInputValue] = useState('');
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      type: 'ai',
      content: '👋 שלום! אני העוזר הדיגיטלי שלך לתובנות נתונים. אני יכול לעזור לך לנתח מקורות הנתונים שלך, ליצור דשבורדים ולחקור בעיות. מה תרצה לדעת?',
      timestamp: '2025-09-17 10:30:00',
    },
    {
      id: '2',
      type: 'user',
      content: 'הצג לי את מגמות שגיאות האימות לשבוע האחרון',
      timestamp: '2025-09-17 10:31:15',
    },
    {
      id: '3',
      type: 'ai',
      content: `ניתחתי את שגיאות האימות לשבוע האחרון. הנה מה שמצאתי:

📊 **סיכום שגיאות:**
• סך שגיאות: 1,247 (↓ 15% מהשבוע הקודם)
• הנפוצות ביותר: שדות חובה חסרים (45%)
• שעת שיא שגיאות: יום שני 9-11 בבוקר
• מקור ביצועים גרועים ביותר: טפסי משוב לקוחות

תרצה שאצור דשבורד Grafana לניטור המגמות האלה?`,
      timestamp: '2025-09-17 10:32:00',
    },
    {
      id: '4',
      type: 'user',
      content: 'למה לטפסי משוב לקוחות יש כל כך הרבה שגיאות?',
      timestamp: '2025-09-17 10:33:20',
    },
    {
      id: '5',
      type: 'ai',
      content: `חקרתי את מקור הנתונים טפסי משוב לקוחות. הבעיות העיקריות הן:

🔍 **סיבות שורש:**
• Schema עודכן ב-15 בספטמבר, אבל פורמט ישן עדיין מתקבל
• שדה חובה חדש 'satisfaction_score' חסר ב-67% מהרשומות
• תבנית אימות אימייל השתנתה, גורמת לשגיאות פורמט

**פעולות מומלצות:**
1. פנה ל-Survey Monkey לעדכון פורמט הייצוא שלהם
2. שקול להפוך את 'satisfaction_score' לא חובה זמנית
3. בדוק תבנית regex לאימות אימייל`,
      timestamp: '2025-09-17 10:34:10',
    },
  ]);

  const quickActions = [
    { label: 'הצג מדדי היום', icon: <BarChartOutlined /> },
    { label: 'צור דשבורד שגיאות', icon: <AlertOutlined /> },
    { label: 'נתח מגמות איכות נתונים', icon: <DatabaseOutlined /> },
  ];

  const handleSendMessage = () => {
    if (inputValue.trim()) {
      const newMessage: Message = {
        id: Date.now().toString(),
        type: 'user',
        content: inputValue,
        timestamp: new Date().toLocaleString('he-IL'),
      };
      setMessages([...messages, newMessage]);
      setInputValue('');

      // Simulate AI response
      setTimeout(() => {
        const aiResponse: Message = {
          id: (Date.now() + 1).toString(),
          type: 'ai',
          content: 'תודה על שאלתך! אני עובד על הנתונים ואחזור אליך עם תשובה מפורטת בקרוב.',
          timestamp: new Date().toLocaleString('he-IL'),
        };
        setMessages(prev => [...prev, aiResponse]);
      }, 1000);
    }
  };

  const handleQuickAction = (action: string) => {
    setInputValue(action);
  };

  return (
    <div className="ai-assistant-page">
      <div className="page-header">
        <Title level={2}>
          <RobotOutlined /> עוזר AI - תובנות נתונים
        </Title>
      </div>

      <Row gutter={24} style={{ height: 'calc(100vh - 200px)' }}>
        {/* Chat Sidebar */}
        <Col span={6}>
          <Card 
            title="🤖 עוזר AI" 
            style={{ height: '100%', backgroundColor: '#2c3e50', color: 'white' }}
            headStyle={{ color: 'white', borderBottom: '1px solid #34495e' }}
            bodyStyle={{ color: 'white' }}
          >
            <Text style={{ color: 'white', fontSize: '12px', display: 'block', marginBottom: 20 }}>
              שאל אותי על מקורות הנתונים, מדדים ותובנות שלך!
            </Text>

            <div style={{ marginBottom: 20 }}>
              <Text strong style={{ color: 'white' }}>פעולות מהירות:</Text>
              <div style={{ marginTop: 10 }}>
                {quickActions.map((action, index) => (
                  <Button
                    key={index}
                    ghost
                    block
                    size="small"
                    style={{ marginBottom: 5, textAlign: 'right' }}
                    onClick={() => handleQuickAction(action.label)}
                  >
                    {action.icon} {action.label}
                  </Button>
                ))}
              </div>
            </div>

            <div>
              <Text strong style={{ color: 'white' }}>מחובר:</Text>
              <div style={{ fontSize: '12px', marginTop: 5 }}>
                <div>✅ MongoDB</div>
                <div>✅ Grafana</div>
                <div>✅ Prometheus</div>
              </div>
            </div>
          </Card>
        </Col>

        {/* Chat Main Area */}
        <Col span={18}>
          <Card style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            {/* Chat Messages */}
            <div style={{ flex: 1, overflowY: 'auto', marginBottom: 16 }}>
              {messages.map((message) => (
                <div
                  key={message.id}
                  style={{
                    display: 'flex',
                    justifyContent: message.type === 'user' ? 'flex-end' : 'flex-start',
                    marginBottom: 16,
                  }}
                >
                  <div
                    style={{
                      maxWidth: '70%',
                      padding: '12px 16px',
                      borderRadius: 18,
                      backgroundColor: message.type === 'user' 
                        ? 'linear-gradient(135deg, #3498db, #2980b9)' 
                        : '#f8f9fa',
                      color: message.type === 'user' ? 'white' : '#333',
                      border: message.type === 'ai' ? '1px solid #dee2e6' : 'none',
                    }}
                  >
                    {message.type === 'ai' && (
                      <Avatar size="small" icon={<RobotOutlined />} style={{ marginLeft: 8 }} />
                    )}
                    <div style={{ whiteSpace: 'pre-wrap', textAlign: 'right' }}>
                      {message.content}
                    </div>
                    <div 
                      style={{ 
                        fontSize: '10px', 
                        opacity: 0.7, 
                        marginTop: 4,
                        textAlign: 'right',
                      }}
                    >
                      {message.timestamp}
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Chat Input */}
            <div style={{ borderTop: '1px solid #f0f0f0', paddingTop: 16 }}>
              <Row gutter={8}>
                <Col flex="auto">
                  <TextArea
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    placeholder="שאל על הנתונים, מדדים או בקש תובנות..."
                    autoSize={{ minRows: 1, maxRows: 3 }}
                    onPressEnter={(e) => {
                      if (!e.shiftKey) {
                        e.preventDefault();
                        handleSendMessage();
                      }
                    }}
                  />
                </Col>
                <Col>
                  <Button
                    type="primary"
                    icon={<SendOutlined />}
                    onClick={handleSendMessage}
                    disabled={!inputValue.trim()}
                  >
                    שלח
                  </Button>
                </Col>
              </Row>
            </div>
          </Card>
        </Col>
      </Row>

      <style>{`
        .ai-assistant-page .page-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 24px;
          padding-bottom: 16px;
          border-bottom: 2px solid #e9ecef;
        }
      `}</style>
    </div>
  );
};

export default AIAssistant;
