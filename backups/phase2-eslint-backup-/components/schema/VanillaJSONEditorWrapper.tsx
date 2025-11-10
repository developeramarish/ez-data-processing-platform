import React, { useEffect, useRef } from 'react';
import { createJSONEditor } from 'vanilla-jsoneditor';
import 'vanilla-jsoneditor/themes/jse-theme-dark.css';

interface VanillaJSONEditorProps {
  content: string;
  onChange?: (content: string) => void;
  mode?: 'tree' | 'text' | 'table';
  readOnly?: boolean;
  height?: string;
  mainMenuBar?: boolean;
  navigationBar?: boolean;
  statusBar?: boolean;
}

const VanillaJSONEditorWrapper: React.FC<VanillaJSONEditorProps> = ({
  content,
  onChange,
  mode = 'tree',
  readOnly = false,
  height = '500px',
  mainMenuBar = true,
  navigationBar = true,
  statusBar = true
}) => {
  const editorRef = useRef<HTMLDivElement>(null);
  const editorInstanceRef = useRef<any>(null);

  // Initialize editor
  useEffect(() => {
    if (!editorRef.current) return;

    try {
      const jsonContent = content ? JSON.parse(content) : {};
      
      const editor = createJSONEditor({
        target: editorRef.current,
        props: {
          content: { json: jsonContent },
          mode,
          onChange: (updatedContent: any) => {
            if (onChange) {
              const jsonString = updatedContent.text || JSON.stringify(updatedContent.json, null, 2);
              onChange(jsonString);
            }
          },
          readOnly,
          mainMenuBar,
          navigationBar,
          statusBar
        }
      });

      editorInstanceRef.current = editor;

      return () => {
        editor.destroy();
        editorInstanceRef.current = null;
      };
    } catch (error) {
      console.error('Error initializing vanilla-jsoneditor:', error);
    }
  }, [mode, readOnly, mainMenuBar, navigationBar, statusBar]);

  // Update content when it changes
  useEffect(() => {
    if (editorInstanceRef.current && content) {
      try {
        const jsonContent = JSON.parse(content);
        editorInstanceRef.current.set({ json: jsonContent });
      } catch (e) {
        editorInstanceRef.current.set({ text: content });
      }
    }
  }, [content]);

  // Update mode
  useEffect(() => {
    if (editorInstanceRef.current) {
      editorInstanceRef.current.updateProps({ mode });
    }
  }, [mode]);

  return (
    <div 
      ref={editorRef} 
      style={{ 
        height,
        border: '1px solid #d9d9d9',
        borderRadius: '6px',
        overflow: 'hidden'
      }} 
    />
  );
};

export default VanillaJSONEditorWrapper;
