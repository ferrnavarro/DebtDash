import { useState, useEffect } from 'react';
import type { FormEvent } from 'react';
import {
  getPayments,
  createPayment,
  updatePayment,
  deletePayment,
} from '../services/paymentApi';
import type { PaymentLogEntry, PaymentUpsertRequest } from '../services/paymentApi';

type Status = 'idle' | 'loading' | 'saving' | 'error';

export default function LedgerPage() {
  const [status, setStatus] = useState<Status>('loading');
  const [error, setError] = useState<string | null>(null);
  const [payments, setPayments] = useState<PaymentLogEntry[]>([]);
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<PaymentUpsertRequest>({
    paymentDate: '',
    totalPaid: 0,
    principalPaid: 0,
    interestPaid: 0,
    feesPaid: 0,
    manualRateOverrideEnabled: false,
    manualRateOverride: null,
  });

  const pageSize = 50;

  useEffect(() => {
    loadPayments();
  }, [page]);

  async function loadPayments() {
    setStatus('loading');
    setError(null);
    try {
      const data = await getPayments(page, pageSize);
      setPayments(data.items);
      setTotalItems(data.totalItems);
      setStatus('idle');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load payments');
      setStatus('error');
    }
  }

  function resetForm() {
    setForm({
      paymentDate: '',
      totalPaid: 0,
      principalPaid: 0,
      interestPaid: 0,
      feesPaid: 0,
      manualRateOverrideEnabled: false,
      manualRateOverride: null,
    });
    setEditingId(null);
    setShowForm(false);
  }

  function startEdit(entry: PaymentLogEntry) {
    setForm({
      paymentDate: entry.paymentDate,
      totalPaid: entry.totalPaid,
      principalPaid: entry.principalPaid,
      interestPaid: entry.interestPaid,
      feesPaid: entry.feesPaid,
      manualRateOverrideEnabled: entry.manualRateOverrideEnabled,
      manualRateOverride: entry.manualRateOverride,
    });
    setEditingId(entry.id);
    setShowForm(true);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setStatus('saving');
    setError(null);
    try {
      if (editingId) {
        await updatePayment(editingId, form);
      } else {
        await createPayment(form);
      }
      resetForm();
      await loadPayments();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save payment');
      setStatus('error');
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this payment entry?')) return;
    setStatus('saving');
    try {
      await deletePayment(id);
      await loadPayments();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete payment');
      setStatus('error');
    }
  }

  if (status === 'loading' && payments.length === 0) {
    return (
      <main className="page">
        <h1>Payment Ledger</h1>
        <div aria-busy="true" aria-live="polite"><p>Loading payments...</p></div>
      </main>
    );
  }

  return (
    <main className="page">
      <h1>Payment Ledger</h1>

      {error && (
        <div role="alert" className="alert alert-error">
          {error}
          <button onClick={loadPayments} type="button">Retry</button>
        </div>
      )}

      <button
        type="button"
        className="btn btn-primary"
        onClick={() => { resetForm(); setShowForm(true); }}
      >
        + Add Payment
      </button>

      {showForm && (
        <form onSubmit={handleSubmit} className="form payment-form">
          <h2>{editingId ? 'Edit Payment' : 'New Payment'}</h2>
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="paymentDate">Payment Date</label>
              <input id="paymentDate" type="date" required aria-required="true"
                value={form.paymentDate}
                onChange={(e) => setForm({ ...form, paymentDate: e.target.value })} />
            </div>
            <div className="form-field">
              <label htmlFor="totalPaid">Total Paid</label>
              <input id="totalPaid" type="number" step="0.01" min="0.01" required aria-required="true"
                value={form.totalPaid || ''}
                onChange={(e) => setForm({ ...form, totalPaid: parseFloat(e.target.value) || 0 })} />
            </div>
          </div>
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="principalPaid">Principal</label>
              <input id="principalPaid" type="number" step="0.01" min="0" required
                value={form.principalPaid || ''}
                onChange={(e) => setForm({ ...form, principalPaid: parseFloat(e.target.value) || 0 })} />
            </div>
            <div className="form-field">
              <label htmlFor="interestPaid">Interest</label>
              <input id="interestPaid" type="number" step="0.01" min="0" required
                value={form.interestPaid || ''}
                onChange={(e) => setForm({ ...form, interestPaid: parseFloat(e.target.value) || 0 })} />
            </div>
            <div className="form-field">
              <label htmlFor="feesPaid">Fees/Insurance</label>
              <input id="feesPaid" type="number" step="0.01" min="0" required
                value={form.feesPaid || ''}
                onChange={(e) => setForm({ ...form, feesPaid: parseFloat(e.target.value) || 0 })} />
            </div>
          </div>
          <div className="form-field">
            <label>
              <input type="checkbox" checked={form.manualRateOverrideEnabled || false}
                onChange={(e) => setForm({ ...form, manualRateOverrideEnabled: e.target.checked })} />
              Enable Manual Rate Override
            </label>
          </div>
          {form.manualRateOverrideEnabled && (
            <div className="form-field">
              <label htmlFor="manualRateOverride">Override Rate (%)</label>
              <input id="manualRateOverride" type="number" step="0.0001" required
                value={form.manualRateOverride ?? ''}
                onChange={(e) => setForm({ ...form, manualRateOverride: parseFloat(e.target.value) || null })} />
            </div>
          )}
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={status === 'saving'}>
              {status === 'saving' ? 'Saving...' : editingId ? 'Update' : 'Create'}
            </button>
            <button type="button" className="btn" onClick={resetForm}>Cancel</button>
          </div>
        </form>
      )}

      {payments.length === 0 ? (
        <div className="empty-state" aria-live="polite">
          <p>No payments recorded yet. Add your first payment to start tracking.</p>
        </div>
      ) : (
        <>
          <div className="table-container">
            <table aria-label="Payment ledger">
              <thead>
                <tr>
                  <th scope="col">Date</th>
                  <th scope="col">Days</th>
                  <th scope="col">Total</th>
                  <th scope="col">Principal</th>
                  <th scope="col">Interest</th>
                  <th scope="col">Fees</th>
                  <th scope="col">Balance</th>
                  <th scope="col">Real Rate</th>
                  <th scope="col">Variance</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {payments.map((p) => (
                  <tr key={p.id}>
                    <td>{p.paymentDate}</td>
                    <td>{p.daysSincePreviousPayment}</td>
                    <td>{p.totalPaid.toFixed(2)}</td>
                    <td>{p.principalPaid.toFixed(2)}</td>
                    <td>{p.interestPaid.toFixed(2)}</td>
                    <td>{p.feesPaid.toFixed(2)}</td>
                    <td>{p.remainingBalanceAfterPayment.toFixed(2)}</td>
                    <td>{p.calculatedRealRate.toFixed(4)}%</td>
                    <td>
                      {p.rateVariance ? (
                        <span className={`variance-badge ${p.rateVariance.isFlagged ? 'flagged' : 'normal'}`}
                          title={`Variance: ${p.rateVariance.varianceBasisPoints.toFixed(1)} bps`}>
                          {p.rateVariance.isFlagged ? '⚠️' : '✓'} {p.rateVariance.varianceBasisPoints.toFixed(1)}bp
                        </span>
                      ) : '—'}
                    </td>
                    <td>
                      <button className="btn btn-sm" onClick={() => startEdit(p)}>Edit</button>
                      <button className="btn btn-sm btn-danger" onClick={() => handleDelete(p.id)}>Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="pagination">
            <button disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span>Page {page} of {Math.ceil(totalItems / pageSize)}</span>
            <button disabled={page * pageSize >= totalItems} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        </>
      )}
    </main>
  );
}
