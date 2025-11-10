# Regex Helper Dialog - Feature Enhancements

## Overview

The RegexHelperDialog has been significantly enhanced with new features to improve the regex pattern creation experience in the EZ Data Processing Platform. The component now includes 30 predefined patterns, custom pattern management, field insertion capabilities, and global accessibility.

## New Features

### 1. Expanded Pattern Library (30 Patterns)

The pattern library has been expanded from 12 to 30 predefined patterns, organized into 8 categories:

#### 🇮🇱 Israeli Patterns (5 patterns)
- **Israeli ID** - תעודת זהות ישראלית (9 digits)
- **Israeli Phone** - מספר טלפון ישראלי (landline or mobile)
- **Israeli Mobile** - טלפון נייד ישראלי (mobile only)
- **Israeli Postal Code** - מיקוד ישראלי (5-7 digits)
- **Hebrew Text** - טקסט עברי (Hebrew letters and spaces)

#### 🏦 Banking Patterns (4 patterns)
- **Israeli Bank Account** - מספר חשבון בנק (6-9 digits)
- **Bank Branch Code** - קוד סניף בנק (3 digits)
- **Israeli IBAN** - IBAN ישראלי (IL followed by 21 digits)
- **SWIFT/BIC Code** - קוד SWIFT (international banking code)

#### 🏛️ Government Patterns (4 patterns)
- **Israeli Passport** - מספר דרכון ישראלי (7-9 digits)
- **Israeli Driver License** - רישיון נהיגה ישראלי (7-8 digits)
- **Israeli License Plate** - מספר רכב ישראלי (new format: 12-345-67)
- **Israeli License Plate (Old)** - מספר רכב ישראלי (old format: 7-8 digits)

#### 🏢 Business Patterns (3 patterns)
- **Israeli Business Number** - מספר עוסק מורשה (9 digits)
- **Company Registration** - מספר רישום חברה (51-XXXXXXX)
- **Israeli VAT Number** - מספר עוסק למע"מ (9 digits)

#### 👤 Personal Patterns (3 patterns)
- **Hebrew Name** - שם בעברית (2-20 Hebrew characters)
- **Hebrew Full Name** - שם מלא בעברית (first + last name)
- **Israeli Street Address** - כתובת רחוב ישראלית (street name + number)

#### ✅ Validation Patterns (5 patterns)
- **Alphanumeric** - אלפאנומרי (English letters and digits)
- **Alphanumeric Hebrew** - אלפאנומרי עברי (Hebrew, English, digits)
- **Decimal Number** - מספר עשרוני (decimal with optional point)
- **Percentage** - אחוז (0-100 with up to 2 decimal places)
- **Positive Integer** - מספר שלם חיובי (positive whole number)

#### 💳 Financial Patterns (2 patterns)
- **Credit Card** - כרטיס אשראי (13-19 digits)
- **Currency Amount** - סכום כספי (up to 10 digits, 2 decimals)

#### 🌐 General Patterns (4 patterns)
- **Email Address** - כתובת דוא"ל
- **URL** - כתובת אתר (HTTP/HTTPS)
- **Date (ISO)** - תאריך (YYYY-MM-DD format)
- **Time (24h)** - שעה (24-hour format HH:MM)
- **IPv4 Address** - כתובת IP
- **UUID** - מזהה ייחודי UUID

### 2. Custom Pattern Management

Users can now save, edit, and delete their own custom regex patterns:

#### Save Custom Pattern
1. Create or test a regex pattern in the "Pattern Tester" tab
2. Click "שמור כתבנית מותאמת" (Save as Custom Pattern)
3. Fill in the form:
   - **Hebrew Name** (required): שם בעברית
   - **English Name** (optional): Name in English
   - **Regex Pattern** (required): The regex pattern
   - **Description** (optional): Brief description
   - **Examples** (optional): Comma-separated test examples
4. Click "שמור" (Save)

#### View Custom Patterns
- Custom patterns appear in a new category: "⭐ התבניות שלי" (My Patterns)
- Each custom pattern is tagged with "מותאם אישית" (Custom)

#### Edit Custom Pattern
1. Find your custom pattern in "התבניות שלי"
2. Click the "ערוך" (Edit) button
3. Modify the fields as needed
4. Click "עדכן" (Update)

#### Delete Custom Pattern
1. Find your custom pattern in "התבניות שלי"
2. Click the "מחק" (Delete) button
3. Confirm the deletion

#### Storage
- Custom patterns are stored in browser LocalStorage
- Patterns persist across sessions
- Storage key: `ez_custom_regex_patterns`

### 3. Insert to Field Functionality

The "הכנס לשדה" (Insert to Field) button allows direct insertion of regex patterns into active input fields:

#### How It Works
1. Focus on any input or textarea field in the application
2. Open the Regex Helper Dialog
3. Select a pattern or test your own
4. Click "הכנס לשדה" (Insert to Field)
5. The pattern is inserted at the cursor position

#### Fallback Behavior
- If no field is focused, the pattern is copied to the clipboard
- User receives a notification: "לא נמצא שדה פעיל - התבנית הועתקה ללוח"

### 4. Global Accessibility

The Regex Helper is now globally accessible from anywhere in the application. Users can open it through:

#### Keyboard Shortcut
- **Windows/Linux**: `Ctrl+R`
- **Mac**: `Cmd+R`
- Press the shortcut to open the Regex Helper Dialog instantly from any page
- Press `Escape` to close the dialog
- **Recommended when editing pattern fields in the JSON Schema Editor**

#### Floating Action Button
- A blue floating action button appears on all pages
- Located at the bottom-right of the screen
- Hover tooltip: "עזרת Regex (Ctrl+R)"
- Click to open the Regex Helper Dialog

#### Usage in JSON Schema Editor
When editing a schema with a `pattern` (regex) field:
1. Click on the pattern field to focus it
2. Press `Ctrl+R` (or `Cmd+R`) to open the Regex Helper
3. Select or create a pattern
4. Click "הכנס לשדה" (Insert to Field) to insert directly into the focused field
5. Or click "השתמש בתבנית" (Use Pattern) to copy and close

**Note**: The "עזרת Regex" button has been removed from the Schema Builder toolbar. Use the global shortcuts instead.

#### Integration
- The `RegexHelperProvider` component wraps the entire application
- Provides context for regex helper functionality
- Can be accessed via the `useRegexHelper()` hook (for developers)

## Usage Examples

### Example 1: Using in JSON Schema Editor

```typescript
// User is editing a schema in SchemaBuilderNew
// User adds a "pattern" field to a string property
// User clicks on the pattern input field (it becomes focused)
// User presses Ctrl+R to open Regex Helper
// User navigates to "תבניות ישראליות" category
// User finds "תעודת זהות ישראלית"
// User clicks "בחר" (Select) 
// Pattern is loaded: ^[0-9]{9}$
// User clicks "הכנס לשדה" to insert directly into the focused pattern field
// Pattern is inserted and dialog closes automatically
```

### Example 2: Using Predefined Patterns

```typescript
// User opens Regex Helper (Ctrl+R or floating button) from any page
// Navigates to "תבניות נפוצות" tab
// Finds "טלפון נייד ישראלי" under "תבניות ישראליות"
// Clicks "בחר" (Select)
// Pattern is loaded: ^05[0-9]{8}$
// If a field was focused, clicks "הכנס לשדה" to insert
// Otherwise, clicks "השתמש בתבנית" to copy to clipboard
```

### Example 3: Creating and Saving Custom Pattern

```typescript
// User opens Regex Helper
// Goes to "בודק תבניות" tab
// Enters custom pattern: ^EMP-[0-9]{6}$
// Adds test strings: EMP-123456, EMP-000001
// Verifies pattern matches
// Clicks "שמור כתבנית מותאמת"
// Fills form:
//   - שם: "מספר עובד"
//   - Name: "Employee ID"
//   - תיאור: "מספר עובד בפורמט EMP-XXXXXX"
//   - דוגמאות: "EMP-123456, EMP-000001"
// Clicks "שמור"
// Pattern appears in "התבניות שלי" category
```

### Example 4: Using Global Shortcut Anywhere

```typescript
// User is on any page of the application
// User needs a regex pattern
// Presses Ctrl+R (or Cmd+R on Mac)
// Regex Helper opens instantly
// User selects or creates a pattern
// Pattern is ready to use (copied or inserted)
```

## Technical Implementation

### Components

#### RegexHelperDialog
- **Location**: `src/Frontend/src/components/schema/RegexHelperDialog.tsx`
- **Props**:
  - `visible`: boolean - Controls dialog visibility
  - `onClose`: () => void - Callback when dialog closes
  - `onSelect`: (pattern: string) => void - Callback when pattern is selected

#### RegexHelperProvider
- **Location**: `src/Frontend/src/components/schema/RegexHelperProvider.tsx`
- **Features**:
  - Global keyboard shortcut management
  - Floating action button
  - Context provider for regex helper state
- **Context Methods**:
  - `openRegexHelper()`: Opens the dialog
  - `closeRegexHelper()`: Closes the dialog
  - `selectPattern(pattern)`: Selects a pattern

### LocalStorage Schema

```typescript
interface RegexPattern {
  id: string;                    // Unique ID: custom_${timestamp}
  name: string;                  // English name
  nameHebrew: string;            // Hebrew name
  pattern: string;               // Regex pattern
  description: string;           // Description
  examples: string[];            // Array of example strings
  category: 'custom';            // Always 'custom' for user patterns
  isCustom: true;               // Always true
  createdAt: string;            // ISO timestamp
}

// Storage key: 'ez_custom_regex_patterns'
// Value: JSON array of RegexPattern objects
```

### Integration Points

1. **App.tsx**: Wraps application with `<RegexHelperProvider>`
2. **SchemaBuilderNew.tsx**: No explicit button - use global shortcuts
3. **Any input field**: Works with standard HTML input/textarea elements
4. **JSON Schema Editor Pattern Fields**: Focus the pattern field, then press Ctrl+R

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+R` / `Cmd+R` | Open Regex Helper Dialog |
| `Escape` | Close Regex Helper Dialog |

## User Interface

### Dialog Tabs

1. **תבניות נפוצות** (Pattern Library) - Browse and select predefined patterns
2. **בודק תבניות** (Pattern Tester) - Test regex patterns with sample strings
3. **בונה תבניות** (Pattern Builder) - Visual regex builder with buttons
4. **עזרה** (Help) - Quick reference guide for regex syntax

### Footer Buttons

- **סגור** (Close) - Close the dialog
- **הכנס לשדה** (Insert to Field) - Insert pattern into focused field
- **השתמש בתבנית** (Use Pattern) - Use pattern and close dialog

## Best Practices

### For Users

1. **Use Keyboard Shortcut**: Press `Ctrl+R` when editing pattern fields for quick access
2. **Test Before Saving**: Always test your patterns in the "בודק תבניות" tab before saving
3. **Use Descriptive Names**: Give your custom patterns clear Hebrew names
4. **Add Examples**: Include diverse examples to validate pattern behavior
5. **Organize by Purpose**: Consider your naming convention for custom patterns
6. **Backup Important Patterns**: Custom patterns are stored locally; consider exporting if needed
7. **Focus First**: Click on the input field before opening Regex Helper for direct insertion

### For Developers

1. **Focus Management**: Ensure input fields are properly focused before using "Insert to Field"
2. **Context Usage**: Use `useRegexHelper()` hook to programmatically control the dialog
3. **Pattern Validation**: Always validate regex patterns before applying them
4. **User Feedback**: Provide clear feedback when patterns are inserted or copied

## Troubleshooting

### Pattern Not Inserting
- **Issue**: "הכנס לשדה" copies to clipboard instead of inserting
- **Solution**: Ensure the target input field is focused before clicking the button

### Custom Patterns Lost
- **Issue**: Custom patterns disappear after browser refresh
- **Cause**: LocalStorage might be cleared or disabled
- **Solution**: Check browser settings for LocalStorage permissions

### Keyboard Shortcut Not Working
- **Issue**: Ctrl+R doesn't open the dialog
- **Solution**: 
  - Check if another extension is using the same shortcut
  - Try using the floating action button instead
  - Refresh the page

### Pattern Conflicts
- **Issue**: Browser's native Ctrl+R (refresh) is triggered
- **Solution**: The implementation prevents default browser behavior, but some browsers may override this

## Future Enhancements

Potential future improvements:

1. **Export/Import Patterns**: Allow users to export and share custom patterns
2. **Pattern Categories**: Let users create custom categories for organization
3. **Pattern Search**: Add search functionality to quickly find patterns
4. **Pattern History**: Track recently used patterns
5. **Pattern Favorites**: Mark frequently used patterns as favorites
6. **Regex Cheat Sheet**: Expandable quick reference within the dialog
7. **Visual Pattern Tester**: Highlight matches in real-time as you type

## Support

For issues or questions about the Regex Helper:
- Report bugs using `/reportbug` command in the chat
- Refer to this documentation for usage guidance
- Check the Help tab within the Regex Helper Dialog for syntax reference

## Version History

### Version 2.0 (Current)
- Expanded from 12 to 30 predefined patterns
- Added 7 new pattern categories
- Implemented custom pattern management (save/edit/delete)
- Added "Insert to Field" functionality
- Implemented global keyboard shortcut (Ctrl+R)
- Added floating action button
- Created RegexHelperProvider for global access

### Version 1.0
- Initial implementation with 12 basic patterns
- Pattern library, tester, and builder tabs
- Hebrew UI support
