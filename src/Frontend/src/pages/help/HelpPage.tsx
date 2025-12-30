import React, { useEffect, useState } from 'react';
import { Card, Spin, Alert } from 'antd';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import './HelpPage.css';

const HelpPage: React.FC = () => {
  const [content, setContent] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Load Hebrew user guide
    fetch('/docs/USER-GUIDE-HE.md')
      .then(response => {
        if (!response.ok) throw new Error('Failed to load user guide');
        return response.text();
      })
      .then(text => {
        setContent(text);
        setLoading(false);
      })
      .catch(err => {
        console.error('Error loading user guide:', err);
        setError('שגיאה בטעינת המדריך');
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <div style={{ padding: '50px', textAlign: 'center' }}>
        <Spin size="large" tip="טוען מדריך למשתמש..." />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '24px' }}>
        <Alert
          message="שגיאה בטעינת המדריך"
          description={error}
          type="error"
          showIcon
        />
      </div>
    );
  }

  return (
    <div className="help-page">
      <Card>
        <div className="markdown-content">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>
            {content}
          </ReactMarkdown>
        </div>
      </Card>
    </div>
  );
};

export default HelpPage;
