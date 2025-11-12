// OutputTab.tsx - Multi-Destination Output Configuration
// Task-27: Enhanced Output Tab Component
// Version: 1.0
// Date: November 12, 2025

import React, { useState } from 'react';
import { Card, Button, Table, Space, Switch, Select, Form, Typography } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, CloudServerOutlined, FolderOutlined } from '@ant-design/icons';
import type { OutputConfiguration, OutputDestination } from '../../types/datasource';

const { Text } = Typography;

interface OutputTabProps {
  output?: OutputConfiguration;
  onChange: (output: OutputConfiguration) => void;
}

export const OutputTab: React.FC<OutputTabProps> = ({ output, onChange }) => {
  const [editingDestination, setEditingDestination] = useState<OutputDestination | null>(null);
  const [showEditor, setShowEditor] = useState(false);

  const destinations = output?.destinations || [];

  const handleAddDestination = () => {
    setEditingDestination({
      id: crypto.randomUUID(),
      name: '',
      type: 'kafka',
      enabled: true,
      outputFormat: null,
      includeInvalidRecords: null
    });
    setShowEditor(true);
  };

  const handleEditDestination = (destination: OutputDestination) => {
    setEditingDestination({ ...destination });
    setShowEditor(true);
  };

  const handleSaveDestination = (destination: OutputDestination) => {
    const updated = [...destinations];
    const index = updated.findIndex(d => d.id === destination.id);
    
    if (index >= 0) {
      updated[index] = destination;
    } else {
      updated.push(destination);
    }
    
    onChange({ ...output, destinations: updated });
    setShowEditor(false);
    setEditingDestination(null);
  };

  const handleDeleteDestination = (id: string) => {
    const updated = destinations.filter(d => d.id !== id);
    onChange({ ...output, destinations: updated });
  };

  const handleToggleEnabled = (id: string) => {
    const updated = destinations.map(d =>
      d.id === id ? { ...d, enabled: !d.enabled } : d
    );
    onChange({ ...output, destinations: updated });
  };

  const columns = [
    {
      title: 'סוג',
      dataIndex: 'type',
      key: 'type',
      width: 120,
      render: (type: string) => (
        <Space>
          {type === 'kafka' && <CloudServerOutlined style={{ color: '#1890ff' }} />}
          {type === 'folder' && <FolderOutlined style={{ color: '#faad14' }} />}
          <Text>{type.toUpperCase()}</Text>
        </Space>
      )
    },
    {
      title: 'שם',
      dataIndex: 'name',
      key: 'name',
      width: 200
    },
    {
      title: 'תצורה',
      key: 'config',
      render: (_: any, dest: OutputDestination) => {
        if (dest.type === 'kafka' && dest.kafkaConfig) {
          return <Text type="secondary">{dest.kafkaConfig.topic}</Text>;
        } else if (dest.type === 'folder' && dest.folderConfig) {
          return <Text type="secondary">{dest.folderConfig.path}</Text>;
        }
        return <Text type="secondary">לא מוגדר</Text>;
      }
    },
    {
      title: 'פורמט',
      dataIndex: 'outputFormat',
      key: 'outputFormat',
      width: 100,
      render: (format: string | null) => (
        <Text>{format || 'ברירת מחדל'}</Text>
      )
    },
    {
      title: 'מופעל',
      dataIndex: 'enabled',
      key: 'enabled',
      width: 80,
      render: (enabled: boolean, dest: OutputDestination) => (
        <Switch
          checked={enabled}
          onChange={() => handleToggleEnabled(dest.id)}
        />
      )
    },
    {
      title: 'פעולות',
      key: 'actions',
      width: 100,
      render: (_: any, dest: OutputDestination) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEditDestination(dest)}
          />
          <Button
            type="link"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleDeleteDestination(dest.id)}
          />
        </Space>
      )
    }
  ];

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      {/* Global Settings */}
      <Card title="הגדרות ברירת מחדל">
        <Form layout="vertical">
          <Form.Item label="פורמט פלט ברירת מחדל">
            <Select
              value={output?.defaultOutputFormat || 'original'}
              onChange={(value) => onChange({ ...output, defaultOutputFormat: value })}
              style={{ width: 300 }}
            >
              <Select.Option value="original">פורמט מקורי</Select.Option>
              <Select.Option value="json">JSON</Select.Option>
              <Select.Option value="csv">CSV</Select.Option>
              <Select.Option value="xml">XML</Select.Option>
            </Select>
          </Form.Item>

          <Form.Item>
            <Space>
              <Switch
                checked={output?.includeInvalidRecords}
                onChange={(checked) => onChange({ ...output, includeInvalidRecords: checked })}
              />
              <Text>כולל רשומות שגויות בפלט (ברירת מחדל)</Text>
            </Space>
          </Form.Item>
        </Form>
      </Card>

      {/* Destinations List */}
      <Card title="יעדי פלט">
        <Space direction="vertical" style={{ width: '100%' }}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAddDestination}
          >
            הוסף יעד פלט
          </Button>

          <Table
            dataSource={destinations}
            columns={columns}
            rowKey="id"
            locale={{
              emptyText: 'לא הוגדרו יעדי פלט. לחץ "הוסף יעד פלט" כדי להתחיל.'
            }}
            pagination={false}
          />
        </Space>
      </Card>

      {/* TODO: Add DestinationEditorModal component */}
      {/* <DestinationEditorModal
        visible={showEditor}
        destination={editingDestination}
        onSave={handleSaveDestination}
        onCancel={() => {
          setShowEditor(false);
          setEditingDestination(null);
        }}
      /> */}
    </Space>
  );
};
