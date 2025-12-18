import { test, expect } from '@playwright/test';

/**
 * DataSource Management E2E Tests
 * Tests CRUD operations for data sources in the EZ Platform
 */

test.describe('DataSource Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the data sources page
    await page.goto('/');
  });

  test('should display the data sources list', async ({ page }) => {
    // Check page title/heading
    await expect(page.getByRole('heading', { name: /data source/i })).toBeVisible();

    // Verify table or list component exists
    await expect(page.locator('.ant-table')).toBeVisible();
  });

  test('should navigate to create new data source', async ({ page }) => {
    // Click create button
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Should navigate to create form
    await expect(page).toHaveURL(/create|new/);
  });

  test('should fill out basic info tab', async ({ page }) => {
    // Navigate to create page
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Fill basic information
    await page.getByLabel(/name/i).fill('Test DataSource');
    await page.getByLabel(/description/i).fill('Test data source for E2E testing');

    // Select file format
    await page.locator('[data-testid="file-format-select"]').click();
    await page.getByRole('option', { name: /csv/i }).click();

    // Verify form values are set
    await expect(page.getByLabel(/name/i)).toHaveValue('Test DataSource');
  });

  test('should configure connection settings', async ({ page }) => {
    // Navigate to existing data source or create form
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Navigate to connection tab
    await page.getByRole('tab', { name: /connection/i }).click();

    // Select connection type (Local File)
    await page.locator('[data-testid="connection-type-select"]').click();
    await page.getByRole('option', { name: /local file/i }).click();

    // Fill path
    await page.getByLabel(/path/i).fill('/data/input');
    await page.getByLabel(/pattern/i).fill('*.csv');

    // Test connection button should be visible
    await expect(page.getByRole('button', { name: /test connection/i })).toBeVisible();
  });

  test('should configure schedule with cron expression', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Navigate to schedule tab
    await page.getByRole('tab', { name: /schedule/i }).click();

    // Enable scheduling
    await page.getByRole('switch', { name: /enable/i }).click();

    // Enter cron expression
    await page.getByLabel(/cron/i).fill('0 */15 * * * *');

    // Cron helper dialog should be available
    await expect(page.getByRole('button', { name: /helper|explain/i })).toBeVisible();
  });

  test('should define validation schema', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Navigate to schema tab
    await page.getByRole('tab', { name: /schema/i }).click();

    // Add a field
    await page.getByRole('button', { name: /add field/i }).click();

    // Fill field details
    await page.getByLabel(/field name/i).fill('customer_id');
    await page.locator('[data-testid="field-type-select"]').click();
    await page.getByRole('option', { name: /string/i }).click();

    // Mark as required
    await page.getByRole('checkbox', { name: /required/i }).check();

    // Field should appear in the list
    await expect(page.getByText('customer_id')).toBeVisible();
  });

  test('should configure output destinations', async ({ page }) => {
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Navigate to output tab
    await page.getByRole('tab', { name: /output/i }).click();

    // Add Kafka destination
    await page.getByRole('button', { name: /add destination/i }).click();

    // Select Kafka type
    await page.locator('[data-testid="destination-type-select"]').click();
    await page.getByRole('option', { name: /kafka/i }).click();

    // Configure topic
    await page.getByLabel(/topic/i).fill('processed-data');

    // Save destination
    await page.getByRole('button', { name: /save|add/i }).click();

    // Destination should appear in list
    await expect(page.getByText('processed-data')).toBeVisible();
  });

  test('should save and view data source', async ({ page }) => {
    // Navigate to create page
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Fill minimum required fields
    await page.getByLabel(/name/i).fill('E2E Test Source');

    // Save the data source
    await page.getByRole('button', { name: /save|submit|create/i }).click();

    // Should redirect to list or details page
    await expect(page.getByText('E2E Test Source')).toBeVisible({ timeout: 15000 });
  });

  test('should edit existing data source', async ({ page }) => {
    // Wait for data to load
    await page.waitForSelector('.ant-table-row');

    // Click on first data source
    await page.locator('.ant-table-row').first().click();

    // Click edit button
    await page.getByRole('button', { name: /edit/i }).click();

    // Modify description
    await page.getByLabel(/description/i).fill('Updated via E2E test');

    // Save changes
    await page.getByRole('button', { name: /save|update/i }).click();

    // Verify changes saved
    await expect(page.getByText(/updated|saved/i)).toBeVisible();
  });

  test('should delete data source with confirmation', async ({ page }) => {
    // Wait for data to load
    await page.waitForSelector('.ant-table-row');

    // Click delete on first row
    await page.locator('.ant-table-row').first().getByRole('button', { name: /delete/i }).click();

    // Confirm deletion
    await page.getByRole('button', { name: /confirm|yes|ok/i }).click();

    // Verify deletion message
    await expect(page.getByText(/deleted|removed/i)).toBeVisible();
  });
});

test.describe('DataSource Connection Testing', () => {
  test('should test local file connection successfully', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: /create|add|new/i }).click();

    // Setup connection
    await page.getByRole('tab', { name: /connection/i }).click();
    await page.locator('[data-testid="connection-type-select"]').click();
    await page.getByRole('option', { name: /local file/i }).click();
    await page.getByLabel(/path/i).fill('/data/input');

    // Test connection
    await page.getByRole('button', { name: /test connection/i }).click();

    // Wait for test result
    await expect(page.getByText(/connection successful|connected/i)).toBeVisible({ timeout: 30000 });
  });

  test('should handle connection test failure gracefully', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button', { name: /create|add|new/i }).click();

    await page.getByRole('tab', { name: /connection/i }).click();
    await page.locator('[data-testid="connection-type-select"]').click();
    await page.getByRole('option', { name: /local file/i }).click();
    await page.getByLabel(/path/i).fill('/nonexistent/path');

    await page.getByRole('button', { name: /test connection/i }).click();

    // Should show error message
    await expect(page.getByText(/error|failed|not found/i)).toBeVisible({ timeout: 30000 });
  });
});
