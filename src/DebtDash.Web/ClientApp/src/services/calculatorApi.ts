// ── Types ─────────────────────────────────────────────────────────────────────

export interface PaymentScheduleRequest {
  payoffDate: string; // ISO date string "YYYY-MM-DD"
  feeAmount: number | null;
}

export type RateSource = 'ledger' | 'baseline';

export interface RateQuoteContext {
  annualRate: number;
  source: RateSource;
  resolvedAt: string;
  isFallback: boolean;
  fallbackReason: string | null;
  rateChangedFromBaseline: boolean;
  rateChangeWarning: boolean;
}

export interface SchedulePeriodEntry {
  periodNumber: number;
  dueDate: string; // ISO date string "YYYY-MM-DD"
  principalComponent: number;
  interestComponent: number;
  feeComponent: number;
  totalPayment: number;
  remainingBalance: number;
}

export interface ScheduleSummary {
  totalPrincipal: number;
  totalInterest: number;
  totalFees: number;
  totalAmountPaid: number;
  periodCount: number;
}

export interface PaymentScheduleResponse {
  loanId: string;
  outstandingBalance: number;
  periods: number;
  monthlyPaymentAmount: number;
  feeAmountPerPeriod: number;
  totalMonthlyAmount: number;
  rateQuote: RateQuoteContext;
  entries: SchedulePeriodEntry[];
  summary: ScheduleSummary;
  calculatedAt: string;
}

export interface FeeDefaultResponse {
  defaultFeeAmount: number | null;
  sourcePaymentDate: string | null; // ISO date string "YYYY-MM-DD" or null
}

// ── API client functions ───────────────────────────────────────────────────────

const API_BASE = '/api';

/**
 * GET /api/calculator/default-fee
 * Returns the fee pre-populated from the most recent payment ledger entry.
 * defaultFeeAmount is null when the ledger is empty.
 */
export async function getDefaultFee(): Promise<FeeDefaultResponse> {
  const res = await fetch(`${API_BASE}/calculator/default-fee`);
  if (!res.ok) throw new Error(`Failed to fetch default fee: ${res.statusText}`);
  return res.json();
}

/**
 * POST /api/calculator/schedule
 * Calculates a full monthly amortization schedule for the active loan.
 * Throws with a descriptive message on validation (400) or missing loan (404) errors.
 */
export async function postSchedule(
  request: PaymentScheduleRequest,
): Promise<PaymentScheduleResponse> {
  const res = await fetch(`${API_BASE}/calculator/schedule`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => null);
    // FluentValidation returns { errors: { field: [messages] } }
    if (body?.errors) {
      const messages = Object.values(body.errors as Record<string, string[]>)
        .flat()
        .join(' ');
      throw new Error(messages || `Request failed: ${res.statusText}`);
    }
    const msg = (body as { error?: string } | null)?.error;
    throw new Error(msg || `Request failed: ${res.statusText}`);
  }

  return res.json();
}
