import { test, expect } from '@playwright/test';

/**
 * Metrics Configuration E2E Tests
 * Tests for defining and managing business metrics
 */

test.describe('Metrics Configuration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/metrics');
  });

  test('should display metrics list', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /metrics/i })).toBeVisible();
    await expect(page.locator('.ant-table, .ant-list')).toBeVisible();
  });

  test('should create new metric definition', async ({ page }) => {
    // Click create
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Fill metric name
    await page.getByLabel(/name/i).fill('total_sales');

    // Fill description
    await page.getByLabel(/description/i).fill('Total sales amount per transaction');

    // Select metric type
    await page.locator('[data-testid="metric-type-select"]').click();
    await page.getByRole('option', { name: /counter|sum/i }).click();

    // Configure source field
    await page.getByLabel(/field|source/i).fill('amount');

    // Save metric
    await page.getByRole('button', { name: /save|create/i }).click();

    // Verify creation
    await expect(page.getByText('total_sales')).toBeVisible();
  });

  test('should use formula builder for calculated metrics', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    await page.getByLabel(/name/i).fill('profit_margin');

    // Select calculated type
    await page.locator('[data-testid="metric-type-select"]').click();
    await page.getByRole('option', { name: /calculated|formula/i }).click();

    // Open formula builder
    await page.getByRole('button', { name: /formula builder|build/i }).click();

    // Formula builder dialog should appear
    await expect(page.getByRole('dialog', { name: /formula/i })).toBeVisible();

    // Add fields to formula
    await page.getByText('revenue').click();
    await page.getByRole('button', { name: /-/ }).click();
    await page.getByText('cost').click();

    // Confirm formula
    await page.getByRole('button', { name: /apply|confirm/i }).click();

    // Formula should appear in input
    await expect(page.getByLabel(/formula/i)).toHaveValue(/revenue.*cost/);
  });

  test('should configure filter conditions', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    await page.getByLabel(/name/i).fill('filtered_metric');

    // Navigate to filters section
    await page.getByRole('tab', { name: /filter|conditions/i }).click();

    // Add filter condition
    await page.getByRole('button', { name: /add condition|filter/i }).click();

    // Configure filter
    await page.locator('[data-testid="filter-field-select"]').click();
    await page.getByRole('option', { name: /region/i }).click();

    await page.locator('[data-testid="filter-operator-select"]').click();
    await page.getByRole('option', { name: /equals/i }).click();

    await page.getByLabel(/value/i).fill('EMEA');

    // Verify filter added
    await expect(page.getByText(/region.*equals.*EMEA/i)).toBeVisible();
  });

  test('should configure aggregation settings', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    await page.getByLabel(/name/i).fill('avg_transaction_value');

    // Open aggregation helper
    await page.getByRole('button', { name: /aggregation|helper/i }).click();

    // Select aggregation type
    await page.getByRole('option', { name: /average/i }).click();

    // Configure time window
    await page.locator('[data-testid="time-window-select"]').click();
    await page.getByRole('option', { name: /1 hour|hourly/i }).click();

    // Set group by
    await page.locator('[data-testid="group-by-select"]').click();
    await page.getByRole('option', { name: /customer_type/i }).click();

    // Apply aggregation
    await page.getByRole('button', { name: /apply/i }).click();
  });

  test('should configure alert rules for metric', async ({ page }) => {
    // Navigate to existing metric
    await page.waitForSelector('.ant-table-row, .ant-list-item');
    await page.locator('.ant-table-row, .ant-list-item').first().click();

    // Click alerts tab
    await page.getByRole('tab', { name: /alert|warning/i }).click();

    // Add alert rule
    await page.getByRole('button', { name: /add alert|create rule/i }).click();

    // Configure threshold
    await page.getByLabel(/threshold/i).fill('1000');

    // Select condition
    await page.locator('[data-testid="alert-condition-select"]').click();
    await page.getByRole('option', { name: /greater than/i }).click();

    // Set severity
    await page.locator('[data-testid="alert-severity-select"]').click();
    await page.getByRole('option', { name: /warning/i }).click();

    // Save alert
    await page.getByRole('button', { name: /save|create/i }).click();

    // Verify alert created
    await expect(page.getByText(/alert.*created|rule.*saved/i)).toBeVisible();
  });

  test('should preview metric query', async ({ page }) => {
    await page.waitForSelector('.ant-table-row, .ant-list-item');
    await page.locator('.ant-table-row, .ant-list-item').first().click();

    // Click preview/test button
    await page.getByRole('button', { name: /preview|test|query/i }).click();

    // Results panel should appear
    await expect(page.getByTestId('query-results')).toBeVisible({ timeout: 30000 });

    // Should show data or chart
    await expect(page.locator('.recharts-wrapper, .ant-statistic')).toBeVisible();
  });

  test('should use PromQL expression helper', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Select PromQL type
    await page.locator('[data-testid="metric-type-select"]').click();
    await page.getByRole('option', { name: /promql|custom/i }).click();

    // Open PromQL helper
    await page.getByRole('button', { name: /promql helper|expression/i }).click();

    // Helper dialog should appear
    await expect(page.getByRole('dialog', { name: /promql/i })).toBeVisible();

    // Use template
    await page.getByRole('button', { name: /template|example/i }).click();
    await page.getByRole('option', { name: /rate|sum/i }).click();

    // Expression should be populated
    await expect(page.locator('[data-testid="promql-editor"]')).toContainText(/rate|sum/);
  });
});

test.describe('Metrics Dashboard', () => {
  test('should display metrics overview dashboard', async ({ page }) => {
    await page.goto('/metrics/dashboard');

    // Dashboard should load
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Charts should render
    await expect(page.locator('.recharts-wrapper')).toHaveCount.greaterThan(0);
  });

  test('should filter dashboard by time range', async ({ page }) => {
    await page.goto('/metrics/dashboard');

    // Open time range picker
    await page.getByRole('button', { name: /time range|period/i }).click();

    // Select last 24 hours
    await page.getByRole('option', { name: /24 hours|1 day/i }).click();

    // Dashboard should refresh
    await expect(page.locator('.recharts-wrapper')).toBeVisible();
  });

  test('should drill down into metric details', async ({ page }) => {
    await page.goto('/metrics/dashboard');

    // Wait for charts
    await page.waitForSelector('.recharts-wrapper');

    // Click on a chart
    await page.locator('.recharts-wrapper').first().click();

    // Should navigate to detailed view
    await expect(page).toHaveURL(/metrics\/\w+/);
  });
});
