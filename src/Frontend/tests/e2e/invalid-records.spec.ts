import { test, expect } from '@playwright/test';

/**
 * Invalid Records Management E2E Tests
 * Tests for reviewing and correcting validation failures
 */

test.describe('Invalid Records Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/invalid-records');
  });

  test('should display invalid records list', async ({ page }) => {
    // Verify page loaded
    await expect(page.getByRole('heading', { name: /invalid records/i })).toBeVisible();

    // Table should be visible
    await expect(page.locator('.ant-table')).toBeVisible();
  });

  test('should filter records by data source', async ({ page }) => {
    // Open filter dropdown
    await page.locator('[data-testid="datasource-filter"]').click();

    // Select a data source
    await page.getByRole('option').first().click();

    // Apply filter
    await page.getByRole('button', { name: /apply|filter/i }).click();

    // Wait for filtered results
    await page.waitForResponse(response =>
      response.url().includes('/api/invalid-records') && response.status() === 200
    );
  });

  test('should filter records by validation error type', async ({ page }) => {
    // Open error type filter
    await page.locator('[data-testid="error-type-filter"]').click();

    // Select error type
    await page.getByRole('option', { name: /required field|type mismatch|format/i }).click();

    // Records should update
    await expect(page.locator('.ant-table-row')).toHaveCount.greaterThan(0);
  });

  test('should view record details', async ({ page }) => {
    // Wait for records to load
    await page.waitForSelector('.ant-table-row');

    // Click on first record
    await page.locator('.ant-table-row').first().click();

    // Details panel should appear
    await expect(page.getByTestId('record-details-panel')).toBeVisible();

    // Should show field values and errors
    await expect(page.getByText(/field|error|validation/i)).toBeVisible();
  });

  test('should edit invalid record', async ({ page }) => {
    // Wait for records
    await page.waitForSelector('.ant-table-row');

    // Click edit on first record
    await page.locator('.ant-table-row').first().getByRole('button', { name: /edit/i }).click();

    // Edit modal should appear
    await expect(page.getByRole('dialog')).toBeVisible();

    // Modify a field value
    await page.getByLabel(/customer_id|field/i).first().fill('CORRECTED_VALUE');

    // Save changes
    await page.getByRole('button', { name: /save|update/i }).click();

    // Success message
    await expect(page.getByText(/saved|updated/i)).toBeVisible();
  });

  test('should revalidate corrected record', async ({ page }) => {
    await page.waitForSelector('.ant-table-row');

    // Select a record
    await page.locator('.ant-table-row').first().getByRole('checkbox').check();

    // Click revalidate
    await page.getByRole('button', { name: /revalidate/i }).click();

    // Wait for validation to complete
    await expect(page.getByText(/revalidating|processing/i)).toBeVisible();
    await expect(page.getByText(/completed|validated/i)).toBeVisible({ timeout: 30000 });
  });

  test('should bulk delete invalid records', async ({ page }) => {
    await page.waitForSelector('.ant-table-row');

    // Select multiple records
    await page.locator('.ant-table-row').first().getByRole('checkbox').check();
    await page.locator('.ant-table-row').nth(1).getByRole('checkbox').check();

    // Click bulk delete
    await page.getByRole('button', { name: /delete selected|bulk delete/i }).click();

    // Confirm deletion
    await page.getByRole('button', { name: /confirm|yes/i }).click();

    // Verify deletion
    await expect(page.getByText(/deleted/i)).toBeVisible();
  });

  test('should export invalid records', async ({ page }) => {
    await page.waitForSelector('.ant-table-row');

    // Click export button
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /export/i }).click();

    // Select format
    await page.getByRole('option', { name: /csv/i }).click();

    // Wait for download
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toContain('invalid-records');
  });

  test('should paginate through records', async ({ page }) => {
    // Wait for table
    await page.waitForSelector('.ant-table');

    // Click next page
    await page.getByRole('button', { name: /next|>/i }).click();

    // URL should update with page number
    await expect(page).toHaveURL(/page=2/);

    // Navigate back
    await page.getByRole('button', { name: /previous|</i }).click();
    await expect(page).toHaveURL(/page=1/);
  });
});

test.describe('Invalid Records Correction Workflow', () => {
  test('should complete full correction workflow', async ({ page }) => {
    await page.goto('/invalid-records');

    // Wait for data
    await page.waitForSelector('.ant-table-row');

    // Select record with type mismatch error
    await page.locator('.ant-table-row').filter({ hasText: /type mismatch/i }).first().click();

    // Edit the record
    await page.getByRole('button', { name: /edit/i }).click();

    // Fix the value
    const errorField = page.locator('[data-error="true"]').first();
    await errorField.fill('123');

    // Save
    await page.getByRole('button', { name: /save/i }).click();

    // Revalidate
    await page.getByRole('button', { name: /revalidate/i }).click();

    // Should move to valid or show success
    await expect(page.getByText(/validation passed|now valid/i)).toBeVisible({ timeout: 30000 });
  });
});
