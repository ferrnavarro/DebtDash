export interface PaymentLogEntry {
  id: string;
  paymentDate: string;
  totalPaid: number;
  principalPaid: number;
  interestPaid: number;
  feesPaid: number;
  daysSincePreviousPayment: number;
  remainingBalanceAfterPayment: number;
  calculatedRealRate: number;
  manualRateOverrideEnabled: boolean;
  manualRateOverride: number | null;
  rateVariance: RateVariance | null;
}

export interface RateVariance {
  calculatedRate: number;
  statedOrOverrideRate: number | null;
  varianceAbsolute: number;
  varianceBasisPoints: number;
  isFlagged: boolean;
}

export interface PaymentListResponse {
  items: PaymentLogEntry[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface PaymentUpsertRequest {
  paymentDate: string;
  totalPaid: number;
  principalPaid: number;
  interestPaid: number;
  feesPaid: number;
  manualRateOverrideEnabled?: boolean;
  manualRateOverride?: number | null;
}

const API_BASE = '/api';

export async function getPayments(page = 1, pageSize = 50): Promise<PaymentListResponse> {
  const res = await fetch(`${API_BASE}/payments?page=${page}&pageSize=${pageSize}`);
  if (!res.ok) throw new Error(`Failed to fetch payments: ${res.statusText}`);
  return res.json();
}

export async function createPayment(request: PaymentUpsertRequest): Promise<PaymentLogEntry> {
  const res = await fetch(`${API_BASE}/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `Failed to create payment: ${res.statusText}`);
  }
  return res.json();
}

export async function updatePayment(id: string, request: PaymentUpsertRequest): Promise<PaymentLogEntry> {
  const res = await fetch(`${API_BASE}/payments/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `Failed to update payment: ${res.statusText}`);
  }
  return res.json();
}

export async function deletePayment(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/payments/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error(`Failed to delete payment: ${res.statusText}`);
}
