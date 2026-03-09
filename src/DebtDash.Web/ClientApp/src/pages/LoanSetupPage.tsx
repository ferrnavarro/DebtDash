import { useState, useEffect } from 'react';
import type { FormEvent } from 'react';
import { getLoan, upsertLoan } from '../services/loanApi';
import type { LoanProfileUpsertRequest } from '../services/loanApi';

type Status = 'idle' | 'loading' | 'saving' | 'success' | 'error';

export default function LoanSetupPage() {
  const [status, setStatus] = useState<Status>('loading');
  const [error, setError] = useState<string | null>(null);
  const [loanId, setLoanId] = useState<string | null>(null);
  const [form, setForm] = useState<LoanProfileUpsertRequest>({
    initialPrincipal: 0,
    annualRate: 0,
    termMonths: 0,
    startDate: '',
    fixedMonthlyCosts: 0,
    currencyCode: 'USD',
  });

  useEffect(() => {
    loadLoan();
  }, []);

  async function loadLoan() {
    setStatus('loading');
    setError(null);
    try {
      const loan = await getLoan();
      if (loan) {
        setLoanId(loan.id);
        setForm({
          initialPrincipal: loan.initialPrincipal,
          annualRate: loan.annualRate,
          termMonths: loan.termMonths,
          startDate: loan.startDate,
          fixedMonthlyCosts: loan.fixedMonthlyCosts,
          currencyCode: loan.currencyCode,
        });
      }
      setStatus('idle');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load loan');
      setStatus('error');
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus('saving');
    setError(null);
    try {
      const saved = await upsertLoan(form);
      setLoanId(saved.id);
      setStatus('success');
      setTimeout(() => setStatus('idle'), 2000);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save loan');
      setStatus('error');
    }
  }

  function updateField<K extends keyof LoanProfileUpsertRequest>(
    key: K,
    value: LoanProfileUpsertRequest[K]
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  if (status === 'loading') {
    return (
      <div aria-busy="true" aria-live="polite" className="page-loading">
        <p>Loading loan configuration...</p>
      </div>
    );
  }

  return (
    <main className="page">
      <h1>Loan Setup</h1>
      <p className="page-description">
        {loanId ? 'Edit your loan baseline configuration.' : 'Configure your loan to start tracking payments.'}
      </p>

      {status === 'error' && error && (
        <div role="alert" className="alert alert-error">
          {error}
          <button onClick={loadLoan} type="button">Retry</button>
        </div>
      )}

      {status === 'success' && (
        <div role="status" className="alert alert-success">
          Loan saved successfully!
        </div>
      )}

      <form onSubmit={handleSubmit} className="form">
        <div className="form-field">
          <label htmlFor="initialPrincipal">Initial Principal</label>
          <input
            id="initialPrincipal"
            type="number"
            step="0.01"
            min="0.01"
            required
            aria-required="true"
            value={form.initialPrincipal || ''}
            onChange={(e) => updateField('initialPrincipal', parseFloat(e.target.value) || 0)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="annualRate">Annual Interest Rate (%)</label>
          <input
            id="annualRate"
            type="number"
            step="0.01"
            min="0"
            required
            aria-required="true"
            value={form.annualRate || ''}
            onChange={(e) => updateField('annualRate', parseFloat(e.target.value) || 0)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="termMonths">Term (months)</label>
          <input
            id="termMonths"
            type="number"
            min="1"
            required
            aria-required="true"
            value={form.termMonths || ''}
            onChange={(e) => updateField('termMonths', parseInt(e.target.value) || 0)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="startDate">Start Date</label>
          <input
            id="startDate"
            type="date"
            required
            aria-required="true"
            value={form.startDate}
            onChange={(e) => updateField('startDate', e.target.value)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="fixedMonthlyCosts">Fixed Monthly Costs</label>
          <input
            id="fixedMonthlyCosts"
            type="number"
            step="0.01"
            min="0"
            value={form.fixedMonthlyCosts || ''}
            onChange={(e) => updateField('fixedMonthlyCosts', parseFloat(e.target.value) || 0)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="currencyCode">Currency Code</label>
          <input
            id="currencyCode"
            type="text"
            maxLength={3}
            minLength={3}
            required
            aria-required="true"
            value={form.currencyCode}
            onChange={(e) => updateField('currencyCode', e.target.value.toUpperCase())}
          />
        </div>

        <button type="submit" disabled={status === 'saving'} className="btn btn-primary">
          {status === 'saving' ? 'Saving...' : loanId ? 'Update Loan' : 'Create Loan'}
        </button>
      </form>
    </main>
  );
}
