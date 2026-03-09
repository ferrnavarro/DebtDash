import { test, expect } from '@playwright/test';

// T025 [US2]: E2E chart and window-switching scenarios
// Covers: window selector rendering, window toggling, chart sections visible.

async function seedLoanAndPayments(request: Parameters<Parameters<typeof test>[1]>[0]['request']) {
  await request.put('/api/loan', {
    data: {
      initialPrincipal: 200000,
      annualRate: 5.5,
      termMonths: 360,
      startDate: '2024-01-15',
      fixedMonthlyCosts: 50,
      currencyCode: 'USD',
    },
  });

  const payments = [
    { paymentDate: '2024-02-15', totalPaid: 2000, principalPaid: 1100, interestPaid: 850, feesPaid: 50 },
    { paymentDate: '2024-03-15', totalPaid: 2000, principalPaid: 1150, interestPaid: 800, feesPaid: 50 },
    { paymentDate: '2024-04-15', totalPaid: 1500, principalPaid: 600, interestPaid: 850, feesPaid: 50 },
    { paymentDate: '2024-05-15', totalPaid: 2500, principalPaid: 1650, interestPaid: 800, feesPaid: 50 },
    { paymentDate: '2024-06-15', totalPaid: 2000, principalPaid: 1200, interestPaid: 750, feesPaid: 50 },
    { paymentDate: '2024-07-15', totalPaid: 1500, principalPaid: 700, interestPaid: 750, feesPaid: 50 },
  ];

  for (const p of payments) {
    await request.post('/api/payments', {
      data: { ...p, manualRateOverrideEnabled: false, manualRateOverride: null },
    });
  }
}

test.describe('Dashboard comparison history — window selector and charts', () => {
  test.beforeEach(async ({ request }) => {
    await seedLoanAndPayments(request);
  });

  test('Window selector is rendered on dashboard with payments', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

    // Window selector should appear (DashboardWindowSelector renders buttons)
    const windowGroup = page.getByRole('group', { name: /time window/i });
    await expect(windowGroup).toBeVisible();
  });

  test('All four window buttons are present', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    // Each window should have a button
    await expect(page.getByRole('button', { name: /full history/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /trailing 6 months/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /trailing 12 months/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /year to date/i })).toBeVisible();
  });

  test('Default active window button has aria-pressed=true', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    const fullHistoryBtn = page.getByRole('button', { name: /full history/i });
    await expect(fullHistoryBtn).toHaveAttribute('aria-pressed', 'true');
  });

  test('Clicking trailing 6 months window updates aria-pressed state', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    const trailingBtn = page.getByRole('button', { name: /trailing 6 months/i });
    await trailingBtn.click();
    await page.waitForLoadState('networkidle');

    await expect(trailingBtn).toHaveAttribute('aria-pressed', 'true');
    const fullHistoryBtn = page.getByRole('button', { name: /full history/i });
    await expect(fullHistoryBtn).toHaveAttribute('aria-pressed', 'false');
  });

  test('Balance vs Baseline chart section is visible with payments', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: /balance vs baseline/i })).toBeVisible();
  });

  test('Cumulative interest chart section is visible with payments', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: /cumulative interest/i })).toBeVisible();
  });
});
