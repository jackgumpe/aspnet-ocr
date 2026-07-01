import { expect, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { join } from 'node:path';

const pdfFile = {
  name: 'sample-product-sheet.pdf',
  mimeType: 'application/pdf',
  buffer: Buffer.from('%PDF-1.4 asp-ocr-003 mock upload')
};

test.describe('ASP-OCR-003 upload and dashboard', () => {
  test('opens LedgerScan home and emits an intake intent', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'LedgerScan' })).toBeVisible();
    await page.getByRole('button', { name: 'Open intake' }).click();
    await expect(page).toHaveURL(/\/upload$/);
    await expect(page.getByRole('heading', { name: /ocr upload/i })).toBeVisible();
  });

  test('uploads a valid PDF, creates a job, and renders the OCR result', async ({ page }) => {
    await page.goto('/upload');
    await page.locator('input[type="file"]').setInputFiles(pdfFile);

    await expect(page.locator('asp-upload-zone').getByRole('heading', { name: 'sample-product-sheet.pdf' })).toBeVisible();
    await expect(page.getByText('complete')).toBeVisible();
    await page.getByRole('link', { name: /open result/i }).click();

    await expect(page.getByRole('heading', { name: /ocr result viewer/i })).toBeVisible();
    await expect(page.locator('.field-row').filter({ hasText: 'Widget Alpha' })).toBeVisible();
    await expect(page.getByText('CER', { exact: true })).toBeVisible();
    await expect(page.getByText('WER', { exact: true })).toBeVisible();
    await expect(page.getByText('ENGINE CONFIDENCE', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('LIVE_VERIFIED')).toBeVisible();
    await expect(page.locator('[title*="Canonical state code: LIVE_VERIFIED"]')).toBeVisible();
    await expect(page.locator('[title*="NOT_SELECTED does not make LIVE_VERIFIED incomplete"]')).toBeVisible();
    await expect(page.getByText('Verified mechanical latch set')).toBeVisible();
    await page.getByRole('button', { name: /show json/i }).click();
    await expect(page.getByText('mock-ocr')).toBeVisible();
  });

  test('rejects unsupported uploads before API submission', async ({ page }) => {
    await page.goto('/upload');
    await page.locator('input[type="file"]').setInputFiles({
      name: 'notes.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('not an OCR source')
    });

    await expect(page.getByText(/only pdf, png, jpg, and tiff/i)).toBeVisible();
  });

  test('shows API error state and retry flow', async ({ page }) => {
    let failedOnce = false;
    await page.route('**/api/ocr/jobs', async (route) => {
      if (!failedOnce && route.request().method() === 'POST') {
        failedOnce = true;
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'temporary API failure' })
        });
        return;
      }

      await route.fallback();
    });

    await page.goto('/upload');
    await page.locator('input[type="file"]').setInputFiles(pdfFile);
    await expect(page.getByText(/upload failed/i)).toBeVisible();
    await page.getByRole('button', { name: /retry/i }).click();
    await expect(page.getByText('complete')).toBeVisible();
  });

  test('opens dashboard history after processing', async ({ page }) => {
    await page.goto('/upload');
    await page.locator('input[type="file"]').setInputFiles(pdfFile);
    await expect(page.getByText('complete')).toBeVisible();

    await page.getByRole('link', { name: 'Queue' }).click();
    await expect(page.getByRole('heading', { name: /job dashboard/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'sample-product-sheet.pdf' }).first()).toBeVisible();
    await expect(page.getByRole('link', { name: /open result for sample-product-sheet.pdf/i }).first()).toBeVisible();
  });

  test('supports keyboard upload path and visible focus', async ({ page }) => {
    await page.goto('/upload');
    const chooseButton = page.getByRole('button', { name: 'Choose file', exact: true });
    await chooseButton.focus();
    await expect(chooseButton).toBeFocused();
    await expect(chooseButton).toHaveCSS('outline-style', 'solid');

    const chooserPromise = page.waitForEvent('filechooser');
    await page.keyboard.press('Enter');
    const chooser = await chooserPromise;
    await chooser.setFiles(pdfFile);
    await expect(page.getByText('complete')).toBeVisible();
  });

  test('captures ASP-OCR-006 baseline screenshots', async ({ page }) => {
    const screenshotDir = join(process.cwd(), 'asp-ocr-006-screenshots');
    mkdirSync(screenshotDir, { recursive: true });

    await page.goto('/upload');
    await page.screenshot({ path: join(screenshotDir, 'ledger-scan-upload.png'), fullPage: true });
    await page.locator('input[type="file"]').setInputFiles(pdfFile);
    await expect(page.getByText('complete')).toBeVisible();

    await page.getByRole('link', { name: 'Queue' }).click();
    await expect(page.getByRole('heading', { name: /job dashboard/i })).toBeVisible();
    await page.screenshot({ path: join(screenshotDir, 'ledger-scan-dashboard.png'), fullPage: true });

    await page.getByRole('link', { name: /open result for sample-product-sheet.pdf/i }).first().click();
    await expect(page.getByText('LIVE_VERIFIED')).toBeVisible();
    await page.screenshot({ path: join(screenshotDir, 'ledger-scan-result-viewer.png'), fullPage: true });
  });
});

test.describe('responsive shell', () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test('keeps upload and navigation usable on mobile', async ({ page }) => {
    await page.goto('/upload');
    await expect(page.getByRole('navigation', { name: 'Primary' }).getByText('Intake', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Choose file', exact: true })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Primary' }).getByText('Clients', { exact: true })).toBeVisible();
  });

  test('200 percent zoom reflows without horizontal clipping', async ({ page }) => {
    await page.goto('/upload');
    await page.addStyleTag({ content: 'html { font-size: 200%; }' });
    await expect(page.getByRole('button', { name: 'Choose file', exact: true })).toBeVisible();
    await expect.poll(async () => page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true);
  });
});
