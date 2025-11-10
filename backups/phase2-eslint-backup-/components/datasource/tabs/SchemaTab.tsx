import React from 'react';
import { Card, Alert } from 'antd';
import { SchemaBuilderNew } from '../../../pages/schema/SchemaBuilderNew';
import { type JSONSchema } from 'jsonjoy-builder';

interface SchemaTabProps {
  jsonSchema: JSONSchema;
  onChange: (schema: JSONSchema) => void;
}

export const SchemaTab: React.FC<SchemaTabProps> = ({ jsonSchema, onChange }) => {
  return (
    <>
      <Alert
        message="הגדר Schema לאימות נתונים"
        description="השתמש בעורך המקצועי של jsonjoy-builder לבניית JSON Schema. כולל תמיכה בעברית, regex helper, validation, ודוגמאות JSON."
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
      />

      <Card style={{ minHeight: '650px' }}>
        <SchemaBuilderNew
          initialSchema={jsonSchema}
          onChange={onChange}
          height="650px"
        />
      </Card>
    </>
  );
};
