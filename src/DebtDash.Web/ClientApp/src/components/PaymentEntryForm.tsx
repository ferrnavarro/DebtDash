import type { FormEvent } from 'react';
import type { PaymentUpsertRequest } from '../services/paymentApi';

interface PaymentEntryFormProps {
  form: PaymentUpsertRequest;
  onChange: (form: PaymentUpsertRequest) => void;
  onSubmit: (e: FormEvent) => void;
  onCancel: () => void;
  isEditing: boolean;
  isSaving: boolean;
}

export default function PaymentEntryForm({ form, onChange, onSubmit, onCancel, isEditing, isSaving }: PaymentEntryFormProps) {
  const setField = <K extends keyof PaymentUpsertRequest>(key: K, value: PaymentUpsertRequest[K]) =>
    onChange({ ...form, [key]: value });

  return (
    <form className="payment-form" onSubmit={onSubmit} aria-label={isEditing ? 'Edit payment' : 'Add payment'}>
      <h2>{isEditing ? 'Edit Payment' : 'Add Payment'}</h2>
      <div className="form-row">
        <div className="form-field">
          <label htmlFor="paymentDate">Payment Date</label>
          <input id="paymentDate" type="date" value={form.paymentDate} required
            onChange={e => setField('paymentDate', e.target.value)} />
        </div>
        <div className="form-field">
          <label htmlFor="totalPaid">Total Paid</label>
          <input id="totalPaid" type="number" step="0.01" min="0" value={form.totalPaid} required
            onChange={e => setField('totalPaid', parseFloat(e.target.value) || 0)} />
        </div>
      </div>
      <div className="form-row">
        <div className="form-field">
          <label htmlFor="principalPaid">Principal</label>
          <input id="principalPaid" type="number" step="0.01" min="0" value={form.principalPaid} required
            onChange={e => setField('principalPaid', parseFloat(e.target.value) || 0)} />
        </div>
        <div className="form-field">
          <label htmlFor="interestPaid">Interest</label>
          <input id="interestPaid" type="number" step="0.01" min="0" value={form.interestPaid} required
            onChange={e => setField('interestPaid', parseFloat(e.target.value) || 0)} />
        </div>
        <div className="form-field">
          <label htmlFor="feesPaid">Fees</label>
          <input id="feesPaid" type="number" step="0.01" min="0" value={form.feesPaid} required
            onChange={e => setField('feesPaid', parseFloat(e.target.value) || 0)} />
        </div>
      </div>
      <div className="form-field">
        <label>
          <input type="checkbox" checked={form.manualRateOverrideEnabled}
            onChange={e => setField('manualRateOverrideEnabled', e.target.checked)} />
          {' '}Enable manual rate override
        </label>
      </div>
      {form.manualRateOverrideEnabled && (
        <div className="form-field">
          <label htmlFor="manualRateOverride">Override Rate (%)</label>
          <input id="manualRateOverride" type="number" step="0.001" min="0"
            value={form.manualRateOverride ?? ''} required
            onChange={e => setField('manualRateOverride', parseFloat(e.target.value) || null)} />
        </div>
      )}
      <div className="form-actions">
        <button type="submit" className="btn btn-primary" disabled={isSaving}>
          {isSaving ? 'Saving…' : isEditing ? 'Update' : 'Add Payment'}
        </button>
        <button type="button" className="btn" onClick={onCancel}>Cancel</button>
      </div>
    </form>
  );
}
