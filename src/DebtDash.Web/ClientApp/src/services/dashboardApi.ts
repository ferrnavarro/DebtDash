// ─────────────────────────────────────────────────────────────────────────────
// Comparison Dashboard Types (Feature 001-advanced-loan-dashboards)
// ─────────────────────────────────────────────────────────────────────────────

export type DashboardWindowKey =
  | 'fullHistory'
  | 'trailing6Months'
  | 'trailing12Months'
  | 'yearToDate';

export type ComparisonStatus = 'ahead' | 'onTrack' | 'behind' | 'insufficientData';

export type DashboardState = 'ready' | 'empty' | 'limitedData';

export type MilestoneType =
  | 'divergenceStart'
  | 'highestBalanceGap'
  | 'highestInterestSavings'
  | 'earlyPayoff'
  | 'overlap';

export interface DashboardWindow {
  key: DashboardWindowKey;
  label: string;
  rangeStart: string;
  rangeEnd: string;
}

export interface ComparisonSummary {
  windowKey: DashboardWindowKey;
  currentStatus: ComparisonStatus;
  monthsSaved: number | null;
  projectedPayoffDateDelta: number | null;
  remainingBalanceDelta: number | null;
  cumulativeInterestAvoided: number | null;
  firstMeaningfulDivergenceDate: string | null;
  lastRecalculatedAt: string;
  explanatoryStateMessage: string;
}

export interface ComparisonTimelinePoint {
  date: string;
  actualRemainingBalance: number;
  baselineRemainingBalance: number;
  actualCumulativeInterest: number;
  baselineCumulativeInterest: number;
  actualCumulativePrincipal: number;
  baselineCumulativePrincipal: number;
  balanceDelta: number;
  interestDelta: number;
  payoffProgressDeltaMonths: number;
  containsExtraPrincipalEffect: boolean;
}

export interface ComparisonMilestone {
  type: MilestoneType;
  date: string;
  title: string;
  description: string;
  value: number | null;
}

export interface DashboardComparisonData {
  summary: ComparisonSummary;
  balanceSeries: ComparisonTimelinePoint[];
  costSeries: ComparisonTimelinePoint[];
  milestones: ComparisonMilestone[];
  availableWindows: DashboardWindow[];
  activeWindow: DashboardWindow;
  state: DashboardState;
}

// ─────────────────────────────────────────────────────────────────────────────
// Legacy types (kept for any remaining internal consumers during migration)
// ─────────────────────────────────────────────────────────────────────────────

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

export async function getDashboardComparison(
  window: DashboardWindowKey = 'fullHistory'
): Promise<DashboardComparisonData | null> {
  const windowParam = toApiWindowKey(window);
  const res = await fetch(`${API_BASE}/dashboard${windowParam ? `?window=${windowParam}` : ''}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch dashboard: ${res.statusText}`);
  return res.json();
}

/** Maps camelCase enum values to the kebab-case strings the API expects. */
function toApiWindowKey(key: DashboardWindowKey): string {
  switch (key) {
    case 'trailing6Months': return 'trailing-6-months';
    case 'trailing12Months': return 'trailing-12-months';
    case 'yearToDate': return 'year-to-date';
    default: return 'full-history';
  }
}

/** @deprecated Use getDashboardComparison instead. */
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

