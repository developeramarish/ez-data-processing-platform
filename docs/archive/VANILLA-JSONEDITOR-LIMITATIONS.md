# vanilla-jsoneditor Library Limitations

## Critical Discovery

After implementing vanilla-jsoneditor and researching its API, we discovered **major limitations** that prevent full customization:

### ❌ NOT SUPPORTED by vanilla-jsoneditor

#### 1. Disabling Table Mode
**Problem**: The library does NOT provide an API to disable specific modes.

**What doesn't work**:
```typescript
// This property does NOT exist in vanilla-jsoneditor API
modeOptions: {
  modes: ['tree', 'text']  // ❌ NOT SUPPORTED
}
```

**Only solution**: CSS to hide table mode button
- Attempted in `SchemaBuilder.css`
- May not fully work due to dynamic DOM rendering
- Unreliable across updates

#### 2. Hebrew Translations
**Problem**: The library does NOT support internationalization/localization.

**What doesn't work**:
```typescript
// This property does NOT exist in vanilla-jsoneditor API
translations: {
  'tree': 'עץ',
  'text': 'טקסט'
  // ❌ NOT SUPPORTED
}
```

**Impact**: ALL editor UI strings will ALWAYS be in English:
- Mode labels: "Tree", "Text", "Table" (always English)
- Menu items: "Sort", "Transform", "Search" (always English)
- Buttons: "Undo", "Redo", "Copy", "Paste" (always English)
- Data types: "Array", "Object", "String" (always English)
- Status messages: "Invalid", "Repair" (always English)

---

## Why vanilla-jsoneditor Was Chosen (Despite Limitations)

### ✅ Advantages
1. **Fixed Monaco's blank screen issue** - Primary goal achieved
2. **Tree mode for hierarchical editing** - Better UX than plain text
3. **Built-in syntax highlighting** - Works automatically
4. **Professional JSON editing** - Industry-standard features
5. **Active maintenance** - Regular updates

### ❌ Disadvantages  
1. **English UI only** - No localization support
2. **Cannot disable table mode programmatically** - Can only hide with CSS
3. **No control over internal menu** - Limited customization

---

## Alternative Solutions

### Option 1: Use Monaco Editor (Original)
**Pros**:
- Can be fully customized
- Supports any language
- Full control over UI

**Cons**:
- ❌ **Blank screen issues** (the problem we're trying to fix!)
- Complex AMD loader setup
- More configuration needed

### Option 2: Use CodeMirror 6 Directly
**Pros**:
- Fully customizable
- Can add Hebrew translations
- Modern, well-maintained
- No blank screen issues

**Cons**:
- No built-in Tree mode
- More work to implement
- Have to build JSON-specific features ourselves

### Option 3: Use react-json-view
**Pros**:
- React-specific, easy integration
- Tree view for JSON
- Can customize

**Cons**:
- Read-only by default (editing requires extra work)
- Not as feature-rich as vanilla-jsoneditor
- No text mode

### Option 4: Keep vanilla-jsoneditor with workarounds
**Pros**:
- Works now, fixes Monaco blank screen
- Professional features out of the box
- Our custom buttons provide Hebrew (עצב, אמת, העתק)

**Cons**:
- Editor internal UI stays in English
- Table mode cannot be fully disabled

---

## Recommendation

### Short-term (Current Implementation)
**Keep vanilla-jsoneditor** with:
1. ✅ CSS to attempt hiding table mode
2. ✅ Our own Hebrew buttons (עצב, אמת, העתק) 
3. ✅ Left-justified JSON (works)
4. ✅ Working helper functionality
5. ⚠️ Accept English UI in editor itself

**Reasoning**:
- Primary goal achieved: Monaco blank screen FIXED
- Tree mode provides much better UX
- Most important actions (format, validate, copy) have Hebrew buttons
- Internal editor UI in English is acceptable compromise

### Long-term (Future Enhancement)
**Consider CodeMirror 6 implementation**:
1. Full Hebrew localization possible
2. Custom Tree view can be built
3. Complete control over all features
4. No blank screen issues
5. Modern, actively maintained

**Effort**: Medium-High (2-3 days development)

---

## Current Status Summary

| Feature | Status | Notes |
|---------|--------|-------|
| Fix Monaco Blank Screen | ✅ SOLVED | vanilla-jsoneditor works visually |
| Tree Mode | ✅ WORKING | Hierarchical JSON view available |
| Text Mode | ✅ WORKING | Code editing with syntax highlighting |
| Table Mode Removal | ⚠️ CSS ONLY | Cannot fully disable, can only hide |
| Hebrew UI - Our Buttons | ✅ WORKING | עצב, אמת, העתק all in Hebrew |
| Hebrew UI - Editor Internal | ❌ NOT POSSIBLE | Library limitation |
| Left-Justified JSON | ✅ WORKING | CSS forces LTR |
| Syntax Highlighting | ✅ WORKING | Built-in |
| Format Button | ✅ WORKING | Reformats JSON |
| Validate Button | ✅ WORKING | Shows errors |

---

## User Impact

**What Users Will See**:
- ✅ Schema Builder page header and buttons: All Hebrew
- ✅ Visual Editor tab: All Hebrew
- ✅ Our custom buttons (עצב, אמת, העתק): Hebrew
- ❌ Editor's internal menu bar: English (Tree, Text, Table)
- ❌ Editor's context menu: English (Sort, Copy, Paste, etc.)
- ❌ Editor's search dialog: English
- ❌ Editor's transform dialog: English

**Is This Acceptable?**
- For internal tools: Usually YES
- For customer-facing: Might be NO
- Depends on user expectations and requirements

---

## Decision Required

Do you want to:

**A) Keep vanilla-jsoneditor** (Current)
- Fast, works now
- English editor UI
- Easy to maintain

**B) Switch to CodeMirror 6**
- 2-3 days work
- Full Hebrew support
- More control
- Need to build tree view

**C) Keep Monaco and fix blank screen**
- Investigate root cause
- More debugging needed
- May still have issues
