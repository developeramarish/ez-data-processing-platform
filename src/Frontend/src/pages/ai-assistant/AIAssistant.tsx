import React, { useState } from 'react';
import { Button, Input, Typography, Avatar } from 'antd';
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
        <Title level={2} style={{ margin: 0 }}>
          <RobotOutlined /> עוזר AI - תובנות נתונים
        </Title>
      </div>

      {/* Chat Interface - Mockup Style */}
      <div className="chat-container">
        {/* Dark Sidebar */}
        <div className="chat-sidebar">
          <div>
            <h3>🤖 עוזר AI</h3>
            <p>שאל אותי על מקורות הנתונים, מדדים ותובנות שלך!</p>
          </div>

          <div className="quick-actions">
            <strong>פעולות מהירות:</strong>
            <div>
              {quickActions.map((action, index) => (
                <Button
                  key={index}
                  ghost
                  size="small"
                  onClick={() => handleQuickAction(action.label)}
                >
                  {action.icon} {action.label}
                </Button>
              ))}
            </div>
          </div>

          <div className="connection-status">
            <strong>מחובר:</strong>
            <div>
              <div>✅ MongoDB</div>
              <div>✅ Grafana</div>
              <div>✅ Prometheus</div>
            </div>
          </div>
        </div>

        {/* Main Chat Area */}
        <div className="chat-main">
          {/* Messages */}
          <div className="chat-messages">
            {messages.map((message) => (
              <div key={message.id} className={`message ${message.type}`}>
                <div className="message-bubble">
                  {message.type === 'ai' && (
                    <div style={{ marginBottom: 8 }}>
                      <Avatar size="small" icon={<RobotOutlined />} />
                    </div>
                  )}
                  <div className="message-content">{message.content}</div>
                  <div className="message-timestamp">{message.timestamp}</div>
                </div>
              </div>
            ))}
          </div>

          {/* Chat Input */}
          <div className="chat-input">
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
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSendMessage}
              disabled={!inputValue.trim()}
            >
              שלח
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AIAssistant;
