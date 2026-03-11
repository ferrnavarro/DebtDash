import { test, expect, type Page } from '@playwright/test';

/**
 * T007 / T021 / T027: E2E tests for the Payment Calculator page.
 *
 * Prerequisites: the app must be running at http://localhost:5000 with a seeded
 * loan profile and at least one payment entry.  The playwright.config.ts webServer
 * section starts "dotnet run" automatically when reuseExistingServer is false.
 *
 * Seed helpers use the existing REST API endpoints (/api/loan, /api/payments) to
 * put the database into the right state before each test.
 */

const BASE = 'http://localhost:5000';

async function seedLoan(page: Page, opts: { principal?: number; annualRate?: number } = {}) {
  const res = await page.request.put(`${BASE}/api/loan`, {
    data: {
      initialPrincipal: opts.principal ?? 150_000,
      annualRate: opts.annualRate ?? 5.5,
      termMonths: 360,
      startDate: '2024-01-01',
      fixedMonthlyCosts: 0,
      currencyCode: 'USD',
    },
  });
  expect(res.ok()).toBeTruthy();
}

async function seedPayment(page: Page, feesPaid = 75) {
  const res = await page.request.post(`${BASE}/api/payments`, {
    data: {
      paymentDate: '2026-02-15',
      totalPaid: 1_575,
      principalPaid: 1_000,
      interestPaid: 500,
      feesPaid,
      manualRateOverrideEnabled: false,
      manualRateOverride: null,
    },
  });
  expect(res.ok()).toBeTruthy();
}

/** Returns an ISO date string N months from today (first of the target month). */
function payoffDateInMonths(n: number): string {
  const d = new Date();
  d.setMonth(d.getMonth() + n);
  d.setDate(1);
  return d.toISOString().slice(0, 10); // "YYYY-MM-DD"
}

// ── T007: Navigate to /calculator and render a schedule ──────────────────────

test('T007 - calculator page navigates and renders schedule table', async ({ page }) => {
  await seedLoan(page);

  await page.goto(`${BASE}/calculator`);
  await expect(page.locator('h1')).toContainText('Payment Calculator');

  // Enter a payoff date 12 months out
  await page.locator('input[type="date"][name="payoffDate"], input#payoffDate').fill(
    payoffDateInMonths(12),
  );

  // Set a fee amount
  await page.locator('input[type="number"][name="feeAmount"], input#feeAmount').fill('50');

  // Click Calculate
  await page.getByRole('button', { name: /calculate/i }).click();

  // Wait for schedule table to appear
  const table = page.locator('table[aria-label="Payment schedule"]');
  await expect(table).toBeVisible({ timeout: 10_000 });

  // Should have at least one body row
  const rows = table.locator('tbody tr');
  await expect(rows).toHaveCount(12);
});

test('T007 - calculator shows empty state before first calculation', async ({ page }) => {
  await seedLoan(page);

  await page.goto(`${BASE}/calculator`);

  // Schedule table must not be visible until Calculate is clicked
  await expect(page.locator('table[aria-label="Payment schedule"]')).toBeHidden();
});

test('T007 - calculator shows error for past payoff date', async ({ page }) => {
  await seedLoan(page);

  await page.goto(`${BASE}/calculator`);

  // Submit with today's date (same month = 0 periods)
  const today = new Date().toISOString().slice(0, 10);
  await page.locator('input[type="date"][name="payoffDate"], input#payoffDate').fill(today);
  await page.getByRole('button', { name: /calculate/i }).click();

  await expect(page.locator('[role="alert"]')).toBeVisible({ timeout: 5_000 });
});

// ── T021: Fee pre-population and override ────────────────────────────────────

test('T021 - fee field is pre-populated from most recent ledger entry', async ({ page }) => {
  await seedLoan(page);
  await seedPayment(page, 75);

  await page.goto(`${BASE}/calculator`);

  // The fee field should be pre-filled with 75 after the useEffect fires
  const feeInput = page.locator('input[name="feeAmount"], input#feeAmount');
  await expect(feeInput).toHaveValue('75', { timeout: 5_000 });
});

test('T021 - user can override pre-populated fee and schedule uses new value', async ({ page }) => {
  await seedLoan(page);
  await seedPayment(page, 75);

  await page.goto(`${BASE}/calculator`);

  // Override fee to 100
  const feeInput = page.locator('input[name="feeAmount"], input#feeAmount');
  await feeInput.fill('100');

  await page.locator('input[type="date"][name="payoffDate"], input#payoffDate').fill(
    payoffDateInMonths(12),
  );
  await page.getByRole('button', { name: /calculate/i }).click();

  const table = page.locator('table[aria-label="Payment schedule"]');
  await expect(table).toBeVisible({ timeout: 10_000 });

  // All rows in the fee column should show 100
  const feeCells = table.locator('tbody tr td[data-col="fee"]');
  const count = await feeCells.count();
  expect(count).toBeGreaterThan(0);
  for (let i = 0; i < count; i++) {
    await expect(feeCells.nth(i)).toContainText('100');
  }
});

// ── T027: Summary section visibility and math ────────────────────────────────

test('T027 - summary section is visible after schedule renders', async ({ page }) => {
  await seedLoan(page);

  await page.goto(`${BASE}/calculator`);
  await page.locator('input[type="date"][name="payoffDate"], input#payoffDate').fill(
    payoffDateInMonths(12),
  );
  await page.locator('input[name="feeAmount"], input#feeAmount').fill('0');
  await page.getByRole('button', { name: /calculate/i }).click();

  const summary = page.locator('[data-testid="schedule-summary"]');
  await expect(summary).toBeVisible({ timeout: 10_000 });

  // Summary must show total principal, total interest, total fees, total paid
  await expect(summary.getByText(/total principal/i)).toBeVisible();
  await expect(summary.getByText(/total interest/i)).toBeVisible();
  await expect(summary.getByText(/total paid/i)).toBeVisible();
});
