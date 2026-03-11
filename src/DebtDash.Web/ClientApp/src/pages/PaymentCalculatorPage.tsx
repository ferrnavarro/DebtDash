import { useState, useEffect } from 'react';
import type { FormEvent } from 'react';
import {
  getDefaultFee,
  postSchedule,
} from '../services/calculatorApi';
import type { PaymentScheduleResponse } from '../services/calculatorApi';

type PageStatus = 'idle' | 'loading' | 'error';

const fmt = (n: number) =>
  n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const fmtDate = (iso: string) => {
  const [y, m, d] = iso.split('-');
  return `${m}/${d}/${y}`;
};

/** Returns a date string "YYYY-MM-DD" for the first day of N months from today. */
function minPayoffDate(): string {
  const d = new Date();
  d.setDate(1);
  d.setMonth(d.getMonth() + 1);
  return d.toISOString().slice(0, 10);
}

export default function PaymentCalculatorPage() {
  const [payoffDate, setPayoffDate] = useState('');
  const [feeAmount, setFeeAmount] = useState('');
  const [feeHint, setFeeHint] = useState<string | null>(null);
  const [status, setStatus] = useState<PageStatus>('idle');
  const [error, setError] = useState<string | null>(null);
  const [schedule, setSchedule] = useState<PaymentScheduleResponse | null>(null);

  // T025 / T003: Pre-populate fee from most recent ledger entry on mount
  useEffect(() => {
    getDefaultFee()
      .then((resp) => {
        if (resp.defaultFeeAmount !== null) {
          setFeeAmount(String(resp.defaultFeeAmount));
          setFeeHint(
            resp.sourcePaymentDate
              ? `From payment on ${fmtDate(resp.sourcePaymentDate)}`
              : null,
          );
        }
      })
      .catch(() => {
        // Fee defaulting is best-effort; a missing loan / network error is not fatal here.
      });
  }, []);

  async function handleCalculate(e: FormEvent) {
    e.preventDefault();
    setStatus('loading');
    setError(null);
    setSchedule(null);

    try {
      const result = await postSchedule({
        payoffDate,
        feeAmount: feeAmount.trim() === '' ? null : Number(feeAmount),
      });
      setSchedule(result);
      setStatus('idle');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to calculate schedule.');
      setStatus('error');
    }
  }

  return (
    <main className="page">
      <h1>Payment Calculator</h1>
      <p className="page-description">
        Enter a target payoff date to generate your month-by-month payment schedule.
      </p>

      {/* ── Calculator form ── */}
      <form className="form" onSubmit={handleCalculate} aria-label="Payment calculator form">
        <div className="form-row">
          <div className="form-field">
            <label htmlFor="payoffDate">Payoff Date</label>
            <input
              id="payoffDate"
              name="payoffDate"
              type="date"
              min={minPayoffDate()}
              value={payoffDate}
              onChange={(e) => setPayoffDate(e.target.value)}
              required
              disabled={status === 'loading'}
              aria-required="true"
              aria-label="Target payoff date"
            />
          </div>

          <div className="form-field">
            <label htmlFor="feeAmount">Monthly Fee ($)</label>
            <input
              id="feeAmount"
              name="feeAmount"
              type="number"
              min="0"
              step="0.01"
              value={feeAmount}
              onChange={(e) => setFeeAmount(e.target.value)}
              disabled={status === 'loading'}
              placeholder="0.00"
              aria-label="Monthly fee amount"
            />
            {feeHint && (
              <span className="text-muted" aria-live="polite">
                {feeHint}
              </span>
            )}
          </div>
        </div>

        <div className="form-actions">
          <button
            type="submit"
            className="btn btn-primary"
            disabled={status === 'loading' || !payoffDate}
            aria-busy={status === 'loading'}
          >
            {status === 'loading' ? 'Calculating…' : 'Calculate'}
          </button>
        </div>
      </form>

      {/* ── T016-T018 / US2: Error state ── */}
      {status === 'error' && error && (
        <div role="alert" className="alert alert-error" aria-live="assertive">
          {error}
        </div>
      )}

      {/* ── Results ── */}
      {schedule && (
        <>
          {/* T017 / US2: Fallback warning banner */}
          {schedule.rateQuote.isFallback && (
            <div
              role="alert"
              className="alert alert-warning"
              aria-live="polite"
            >
              No payments recorded; using configured loan rate of{' '}
              <strong>{schedule.rateQuote.annualRate}%</strong> (baseline).
            </div>
          )}

          {/* T018 / US2: Rate-change notice */}
          {!schedule.rateQuote.isFallback && schedule.rateQuote.rateChangeWarning && (
            <div
              role="alert"
              className="alert alert-rate-change"
              aria-live="polite"
            >
              The rate from your payment ledger (<strong>{schedule.rateQuote.annualRate}%</strong>)
              differs from your configured loan rate by more than 0.5 percentage points.
              Please verify the rate before proceeding.
            </div>
          )}

          {/* T016 / US2: Rate info header */}
          <div className="rate-info text-muted" aria-label="Rate information">
            Rate used: <strong>{schedule.rateQuote.annualRate}%</strong>{' '}
            (source:{' '}
            <span>
              {schedule.rateQuote.source === 'ledger' ? 'Ledger' : 'Baseline (configured rate)'}
            </span>
            )
          </div>

          {/* T012 / US1: Schedule table */}
          <div className="table-container" role="region" aria-label="Schedule table">
            <table aria-label="Payment schedule">
              <thead>
                <tr>
                  <th scope="col">#</th>
                  <th scope="col">Due Date</th>
                  <th scope="col">Payment</th>
                  <th scope="col">Principal</th>
                  <th scope="col">Interest</th>
                  <th scope="col">Fee</th>
                  <th scope="col">Remaining</th>
                </tr>
              </thead>
              <tbody>
                {schedule.entries.map((entry) => (
                  <tr key={entry.periodNumber}>
                    <td>{entry.periodNumber}</td>
                    <td>{fmtDate(entry.dueDate)}</td>
                    <td>${fmt(entry.totalPayment)}</td>
                    <td>${fmt(entry.principalComponent)}</td>
                    <td>${fmt(entry.interestComponent)}</td>
                    <td data-col="fee">${fmt(entry.feeComponent)}</td>
                    <td>${fmt(entry.remainingBalance)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* T029 / US4: Cost summary */}
          <section
            className="kpi-grid"
            data-testid="schedule-summary"
            aria-label="Schedule summary"
          >
            <div className="kpi-card">
              <div className="kpi-value">${fmt(schedule.summary.totalPrincipal)}</div>
              <div className="kpi-label">Total Principal</div>
            </div>
            <div className="kpi-card">
              <div className="kpi-value">${fmt(schedule.summary.totalInterest)}</div>
              <div className="kpi-label">Total Interest</div>
            </div>
            <div className="kpi-card">
              <div className="kpi-value">${fmt(schedule.summary.totalFees)}</div>
              <div className="kpi-label">Total Fees</div>
            </div>
            <div className="kpi-card kpi-negative">
              <div className="kpi-value">${fmt(schedule.summary.totalAmountPaid)}</div>
              <div className="kpi-label">Total Paid</div>
            </div>
            <div className="kpi-card">
              <div className="kpi-value">{schedule.summary.periodCount}</div>
              <div className="kpi-label">Months</div>
            </div>
          </section>
        </>
      )}
    </main>
  );
}
