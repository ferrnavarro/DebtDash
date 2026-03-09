import { test, expect } from '@playwright/test';

// T001: Dashboard comparison summary E2E spec scaffold
// These tests will be fleshed out in T015 (US1), T025 (US2), T035 (US3) as
// the backend comparison payload and frontend components are implemented.

test.describe('Dashboard comparison summary', () => {
  test.beforeEach(async ({ request }) => {
    // Seed a loan profile via the API before each test
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
  });

  test('Dashboard loads without errors when no payments exist', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
    await expect(page.getByRole('alert')).not.toBeVisible();
  });

  test('Dashboard shows empty state guidance with no payments', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /dashboard/i }).click();
    // Should show some guidance text when no payment data exists
    const main = page.getByRole('main');
    await expect(main).toBeVisible();
  });
});
