export interface LoanProfile {
  id: string;
  initialPrincipal: number;
  annualRate: number;
  termMonths: number;
  startDate: string;
  fixedMonthlyCosts: number;
  currencyCode: string;
}

export interface LoanProfileUpsertRequest {
  initialPrincipal: number;
  annualRate: number;
  termMonths: number;
  startDate: string;
  fixedMonthlyCosts: number;
  currencyCode: string;
}

const API_BASE = '/api';

export async function getLoan(): Promise<LoanProfile | null> {
  const res = await fetch(`${API_BASE}/loan`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch loan: ${res.statusText}`);
  return res.json();
}

export async function upsertLoan(request: LoanProfileUpsertRequest): Promise<LoanProfile> {
  const res = await fetch(`${API_BASE}/loan`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `Failed to save loan: ${res.statusText}`);
  }
  return res.json();
}
