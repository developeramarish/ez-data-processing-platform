# EZ Platform Frontend Testing Guide

## Overview

This guide covers E2E testing setup using Playwright for the EZ Data Processing Platform frontend.

## Quick Start

### 1. Install Playwright

```bash
# Install Playwright and browsers
npm install -D @playwright/test
npx playwright install
```

### 2. Run Tests

```bash
# Run all tests
npx playwright test

# Run specific test file
npx playwright test datasource.spec.ts

# Run tests in UI mode (interactive)
npx playwright test --ui

# Run tests with browser visible
npx playwright test --headed

# Run specific browser only
npx playwright test --project=chromium
```

### 3. View Test Reports

```bash
# Generate and open HTML report
npx playwright show-report
```

## Test Structure

```
tests/
├── e2e/
│   ├── datasource.spec.ts    # DataSource CRUD and configuration
│   ├── invalid-records.spec.ts # Invalid records management
│   └── metrics.spec.ts        # Metrics configuration and alerts
├── fixtures/                  # Test data fixtures
└── README.md                  # This file
```

## Test Categories

### 1. DataSource Management (`datasource.spec.ts`)
- **CRUD Operations**: Create, Read, Update, Delete data sources
- **Connection Testing**: Test various connection types (Local, FTP, SFTP)
- **Configuration Tabs**: Basic info, Connection, Schedule, Schema, Output
- **Validation**: Form validation and error handling

### 2. Invalid Records (`invalid-records.spec.ts`)
- **Record Viewing**: Display and navigate invalid records
- **Filtering**: Filter by data source, error type, date
- **Correction Workflow**: Edit, revalidate, delete records
- **Bulk Operations**: Multi-select delete, export

### 3. Metrics Configuration (`metrics.spec.ts`)
- **Metric Definition**: Create and edit metric definitions
- **Formula Builder**: Build calculated metrics with formulas
- **Aggregation**: Configure time windows and grouping
- **Alert Rules**: Set up threshold-based alerts
- **PromQL Helper**: Assistance for custom PromQL queries

## Running Against Different Environments

### Local Development
```bash
# Default: uses http://localhost:3000
npx playwright test
```

### Staging Environment
```bash
BASE_URL=https://staging.ez-platform.com npx playwright test
```

### Production (Read-only tests)
```bash
BASE_URL=https://ez-platform.com npx playwright test --grep @smoke
```

## Writing New Tests

### Test Structure
```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/feature-path');
  });

  test('should do something', async ({ page }) => {
    // Arrange
    await page.getByRole('button', { name: /action/i }).click();

    // Act
    await page.getByLabel(/field/i).fill('value');

    // Assert
    await expect(page.getByText(/success/i)).toBeVisible();
  });
});
```

### Locator Best Practices

1. **Prefer role-based locators** (most accessible):
```typescript
await page.getByRole('button', { name: /submit/i })
await page.getByRole('heading', { name: /title/i })
await page.getByRole('tab', { name: /settings/i })
```

2. **Use test IDs for complex components**:
```typescript
await page.getByTestId('datasource-filter')
await page.locator('[data-testid="metric-type-select"]')
```

3. **Text-based locators with regex** (flexible):
```typescript
await page.getByText(/create|add|new/i)
await page.getByLabel(/name/i)
```

### Waiting Strategies

```typescript
// Wait for element
await expect(page.getByText(/loaded/i)).toBeVisible({ timeout: 30000 });

// Wait for navigation
await page.waitForURL(/\/details/);

// Wait for API response
await page.waitForResponse(resp => resp.url().includes('/api/data'));

// Wait for network idle
await page.waitForLoadState('networkidle');
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: 18

      - name: Install dependencies
        run: npm ci
        working-directory: src/frontend

      - name: Install Playwright Browsers
        run: npx playwright install --with-deps
        working-directory: src/frontend

      - name: Run Playwright tests
        run: npx playwright test
        working-directory: src/frontend
        env:
          CI: true
          BASE_URL: ${{ vars.STAGING_URL }}

      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: src/frontend/playwright-report/
```

## Debugging Failed Tests

### 1. Use Trace Viewer
```bash
# Run with trace
npx playwright test --trace on

# View trace for failed tests
npx playwright show-trace test-results/*/trace.zip
```

### 2. Debug Mode
```bash
# Run with debugger
npx playwright test --debug

# Pause execution on failure
npx playwright test --debug-on-fail
```

### 3. Screenshots and Videos
Configured automatically in `playwright.config.ts`:
- Screenshots: On failure
- Video: On first retry
- Trace: On first retry

## Performance Testing Tips

### Measure Page Load
```typescript
test('should load dashboard quickly', async ({ page }) => {
  const startTime = Date.now();
  await page.goto('/metrics/dashboard');
  await page.waitForSelector('.recharts-wrapper');
  const loadTime = Date.now() - startTime;

  expect(loadTime).toBeLessThan(5000); // 5 seconds max
});
```

### Check Network Requests
```typescript
test('should not make excessive API calls', async ({ page }) => {
  const requests: string[] = [];
  page.on('request', req => requests.push(req.url()));

  await page.goto('/');
  await page.waitForLoadState('networkidle');

  const apiCalls = requests.filter(r => r.includes('/api/'));
  expect(apiCalls.length).toBeLessThan(20);
});
```

## Test Data Management

### Use Fixtures for Test Data
```typescript
// fixtures/datasources.ts
export const testDataSource = {
  name: 'E2E Test Source',
  description: 'Created by automated tests',
  type: 'local-file',
  path: '/data/test',
  format: 'csv'
};

// In test file
import { testDataSource } from '../fixtures/datasources';

test('should create datasource', async ({ page }) => {
  await page.getByLabel(/name/i).fill(testDataSource.name);
  // ...
});
```

### Clean Up After Tests
```typescript
test.afterEach(async ({ page }) => {
  // Clean up test data via API
  await page.request.delete('/api/datasources?name=E2E*');
});
```

## Common Issues and Solutions

### 1. Flaky Tests
- Add explicit waits for dynamic content
- Use `waitForLoadState('networkidle')` after navigation
- Increase timeouts for slow operations

### 2. Selector Not Found
- Use more flexible regex patterns
- Add data-testid attributes for complex components
- Check element visibility before interaction

### 3. Authentication
```typescript
// Global setup for authenticated tests
import { chromium } from '@playwright/test';

async function globalSetup() {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('/login');
  await page.fill('[name="email"]', process.env.TEST_USER!);
  await page.fill('[name="password"]', process.env.TEST_PASS!);
  await page.click('button[type="submit"]');
  await page.context().storageState({ path: 'auth.json' });
  await browser.close();
}

export default globalSetup;
```

## Resources

- [Playwright Documentation](https://playwright.dev/docs/intro)
- [Locator Guide](https://playwright.dev/docs/locators)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [API Testing](https://playwright.dev/docs/api-testing)
