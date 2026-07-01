import { expect, test } from '@playwright/test';

const pdfFile = {
  name: 'contrast-fixture.pdf',
  mimeType: 'application/pdf',
  buffer: Buffer.from('%PDF-1.4 asp-ocr-006 contrast lab upload')
};

test.skip(process.env['ASP_OCR_THEME'] !== 'contrast-lab', 'contrast-lab is CI-only and requires ASP_OCR_THEME=contrast-lab');

test('contrast-lab keeps the product shell and proof rail semantics intact', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: /Contrast Laboratory Verification Surface/i })).toBeVisible();

  await page.getByRole('button', { name: /Open intake/i }).click();
  await expect(page).toHaveURL(/\/upload$/);
  await expect(page.getByRole('navigation', { name: 'Primary' }).getByText('Capture', { exact: true })).toBeVisible();
  await page.locator('input[type="file"]').setInputFiles(pdfFile);
  await expect(page.getByText('complete')).toBeVisible();

  await page.getByRole('link', { name: /contrast run ledger/i }).click();
  await expect(page.getByRole('heading', { name: /job dashboard/i })).toBeVisible();
  await page.getByRole('link', { name: /open result for contrast-fixture.pdf/i }).first().click();
  await expect(page.getByText('RESULT_CREATED', { exact: true })).toBeVisible();
  await expect(page.getByText('EVIDENCE_WRITTEN', { exact: true })).toBeVisible();
  await expect(page.getByText('INGESTED', { exact: true })).toBeVisible();
  await expect(page.getByText('QUERYABLE', { exact: true })).toBeVisible();
  await expect(page.getByText('LIVE_VERIFIED', { exact: true })).toBeVisible();
  await expect(page.locator('[title*="Canonical state code: LIVE_VERIFIED"]')).toBeVisible();
  await expect(page.locator('[title*="NOT_SELECTED does not make LIVE_VERIFIED incomplete"]')).toBeVisible();
  await expect(page.getByText('Verified mechanical latch set')).toBeVisible();
  await expect(page.getByText('ENGINE CONFIDENCE', { exact: true }).first()).toBeVisible();

  await page.keyboard.press('Tab');
  await expect(page.locator(':focus')).toBeVisible();
});
