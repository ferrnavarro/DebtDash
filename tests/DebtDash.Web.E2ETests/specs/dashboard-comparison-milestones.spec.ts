import { test, expect } from '@playwright/test';

// T035 [US3]: E2E milestone and savings scenarios

async function seedLoanWithExtraPrincipal(request: Parameters<Parameters<typeof test>[1]>[0]['request']) {
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

  // Payments with significant extra principal to trigger milestones
  const payments = [
    { paymentDate: '2024-02-15', totalPaid: 3000, principalPaid: 2100, interestPaid: 850, feesPaid: 50 },
    { paymentDate: '2024-03-15', totalPaid: 3000, principalPaid: 2150, interestPaid: 800, feesPaid: 50 },
    { paymentDate: '2024-04-15', totalPaid: 3000, principalPaid: 2150, interestPaid: 800, feesPaid: 50 },
    { paymentDate: '2024-05-15', totalPaid: 3000, principalPaid: 2200, interestPaid: 750, feesPaid: 50 },
    { paymentDate: '2024-06-15', totalPaid: 3000, principalPaid: 2200, interestPaid: 750, feesPaid: 50 },
    { paymentDate: '2024-07-15', totalPaid: 3000, principalPaid: 2200, interestPaid: 750, feesPaid: 50 },
  ];

  for (const p of payments) {
    await request.post('/api/payments', {
      data: { ...p, manualRateOverrideEnabled: false, manualRateOverride: null },
    });
  }
}

test.describe('Dashboard comparison milestones and savings highlights', () => {
  test.beforeEach(async ({ request }) => {
    await seedLoanWithExtraPrincipal(request);
  });

  test('Milestones section is visible when extra principal has been paid', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    // ComparisonMilestones renders a list — look for the section heading
    const milestonesSection = page.getByRole('heading', { name: /milestones/i });
    await expect(milestonesSection).toBeVisible();
  });

  test('Savings highlights section is visible when ahead of baseline', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    // ComparisonSavingsHighlights renders a section — look for savings text
    const savingsSection = page.getByRole('region', { name: /savings/i });
    await expect(savingsSection).toBeVisible();
  });

  test('Status banner shows ahead status when extra principal paid', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    // ComparisonStatusBanner has role="status" and aria-live="polite"
    const banner = page.getByRole('status');
    await expect(banner).toBeVisible();
    // Should contain some status text
    await expect(banner).not.toBeEmpty();
  });

  test('Empty state shows guidance message with no payments', async ({ request, page }) => {
    // Use a fresh loan with no payments for this test
    await request.put('/api/loan', {
      data: {
        initialPrincipal: 150000,
        annualRate: 4.5,
        termMonths: 240,
        startDate: '2024-06-01',
        fixedMonthlyCosts: 0,
        currencyCode: 'USD',
      },
    });

    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await page.waitForLoadState('networkidle');

    // Should show empty state or limited-data guidance
    const main = page.getByRole('main');
    await expect(main).toBeVisible();
    // Guidance text should be present in some form
    const note = page.getByRole('note');
    await expect(note).toBeVisible();
  });
});
