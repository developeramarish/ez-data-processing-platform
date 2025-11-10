# Monaco Editor Folding Fix - Complete

## Date
October 28, 2025

## Problem Statement
Monaco editor folding wasn't working correctly for three modes in JSON Schema Builder:
- **Tree View**: Should show structure with properties/required keys visible but contents folded
- **Expand All**: Should show everything fully expanded
- **Collapse All**: Should collapse everything maximally

## Root Cause Analysis (Using Sequential Thinking)

### Issues Identified
1. **Stale Editor Reference**: Monaco editor was captured once in useEffect, but could be unavailable or become stale
2. **Wrong Fold Level**: Used `foldLevel1` instead of `foldLevel2` for tree view
3. **Timing Issues**: No proper sequencing between folding operations

### Understanding Monaco Fold Levels
```
JSON Schema Structure:
{ // Level 0 (root)
  "$schema": "...",     // Level 1
  "type": "object",      // Level 1
  "properties": {        // Level 1 (key) -> Level 2 (contents)
    "field1": {          // Level 2 (key) -> Level 3 (contents)
      "type": "string"   // Level 3
    }
  },
  "required": [          // Level 1 (key) -> Level 2 (contents)
    "field1"             // Level 2
  ]
}
```

- `foldLevel1`: Folds everything at level 1+ (hides properties/required completely)
- `foldLevel2`: Folds everything at level 2+ (shows keys, hides contents) ✓ CORRECT FOR TREE VIEW
- `foldAll`: Maximum folding
- `unfoldAll`: No folding

## Solution Implemented

### 1. Dynamic Editor Retrieval
```typescript
// Get Monaco editor dynamically (fresh each time)
const getMonacoEditor = () => {
  try {
    return (window as any).monaco?.editor?.getEditors?.()?.[0];
  } catch (e) {
    console.warn('Could not get Monaco editor:', e);
    return null;
  }
};
```

### 2. Tree View Button (תצוגת עץ)
```typescript
const handleExpandTreeOnly = () => {
  // ... visual editor handling ...
  
  // Monaco: Unfold all first, then fold to level 2
  const editor = getMonacoEditor();
  if (editor) {
    try {
      editor.getAction('editor.unfoldAll')?.run();
      setTimeout(() => {
        editor.getAction('editor.foldLevel2')?.run();
      }, 150);
    } catch (e) {
      console.warn('Monaco tree view folding failed:', e);
    }
  }
};
```
**Result**: Shows `"properties": {...}` and `"required": [...]` with contents collapsed

### 3. Expand All Button (הרחב הכל)
```typescript
const handleExpandAll = () => {
  // ... visual editor handling ...
  
  const editor = getMonacoEditor();
  if (editor) {
    try {
      editor.getAction('editor.unfoldAll')?.run();
    } catch (e) {
      console.warn('Monaco expand all failed:', e);
    }
  }
};
```
**Result**: Everything fully expanded

### 4. Collapse All Button (כווץ הכל)
```typescript
const handleCollapseAll = () => {
  // ... visual editor handling ...
  
  const editor = getMonacoEditor();
  if (editor) {
    try {
      editor.getAction('editor.foldAll')?.run();
    } catch (e) {
      console.warn('Monaco collapse all failed:', e);
    }
  }
};
```
**Result**: Maximum folding, minimal view

## Files Modified
- `src/Frontend/src/pages/schema/SchemaBuilderNew.tsx`

## Key Changes
1. **Removed**: `monacoEditor` state and useEffect for capturing editor
2. **Added**: `getMonacoEditor()` helper function for dynamic retrieval
3. **Fixed**: Tree view now uses `foldLevel2` instead of `foldLevel1`
4. **Improved**: All three buttons now get fresh Monaco reference each time
5. **Added**: Proper error handling for all Monaco operations
6. **Improved**: Better timing with 150ms delay for tree view folding sequence

## Technical Insights
- `foldLevel2` is the correct choice for tree view - it folds everything at level 2 and deeper
- This keeps level 0-1 visible (root properties like "properties", "required")
- But hides their contents (level 2+)
- Dynamic editor retrieval ensures we always have a valid reference
- Sequential unfold→fold approach with timeout ensures clean state
- Error handling prevents crashes if Monaco API changes or isn't available

## Testing Recommendations
1. Open Schema Builder (SchemaBuilderNew)
2. Create a schema with properties and required fields
3. Test Tree View button: Should show structure with collapsed contents
4. Test Expand All button: Should show everything expanded
5. Test Collapse All button: Should show minimal view
6. Verify Monaco editor and visual editor stay in sync
7. Test with complex nested schemas

## Status
✅ **COMPLETE** - Ready for testing
