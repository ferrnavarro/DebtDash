export interface DashboardData {
  totalInterestPaid: number;
  totalCapitalPaid: number;
  averageRealRateWeighted: number;
  timeRemainingMonths: number;
  originalTermMonths: number;
  principalInterestTrendSeries: { date: string; principalPaid: number; interestPaid: number }[];
  debtCountdownSeries: { date: string; remainingBalance: number }[];
}

export interface ProjectionData {
  predictedEndDate: string;
  remainingMonthsEstimate: number;
  principalVelocity: number;
  baselineRemainingMonths: number;
  deltaMonthsVsBaseline: number;
}

const API_BASE = '/api';

export async function getDashboard(): Promise<DashboardData | null> {
  const res = await fetch(`${API_BASE}/dashboard`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch dashboard: ${res.statusText}`);
  return res.json();
}

export async function getProjection(): Promise<ProjectionData | null> {
  const res = await fetch(`${API_BASE}/projections/true-end-date`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch projection: ${res.statusText}`);
  return res.json();
}
