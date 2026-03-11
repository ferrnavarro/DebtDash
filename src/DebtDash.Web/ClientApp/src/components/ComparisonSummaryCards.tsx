import type { ComparisonSummary, DashboardState } from '../services/dashboardApi';
import KpiCard from './KpiCard';
import { fmtUSD } from '../utils/currency';

interface Props {
  summary: ComparisonSummary;
  state: DashboardState;
}

/** T017: Comparison summary KPI cards — shows balance delta, interest avoided, months saved. */
export default function ComparisonSummaryCards({ summary, state }: Props) {
  if (state === 'empty' || state === 'limitedData') {
    return null;
  }

  const formatDelta = (v: number | null, suffix = '') =>
    v !== null ? `${v > 0 ? '+' : ''}${v.toFixed(1)}${suffix}` : 'N/A';

  const formatCurrency = (v: number | null) =>
    v !== null ? fmtUSD(v) : 'N/A';

  const statusVariant = (s: ComparisonSummary['currentStatus']): 'positive' | 'negative' | 'neutral' => {
    if (s === 'ahead') return 'positive';
    if (s === 'behind') return 'negative';
    return 'neutral';
  };

  return (
    <div
      className="kpi-grid comparison-summary-cards"
      role="region"
      aria-label="Comparison summary metrics"
    >
      <KpiCard
        label="Balance vs Baseline"
        value={formatCurrency(summary.remainingBalanceDelta)}
        variant={
          summary.remainingBalanceDelta !== null && summary.remainingBalanceDelta > 0
            ? 'positive'
            : summary.remainingBalanceDelta !== null && summary.remainingBalanceDelta < 0
              ? 'negative'
              : 'neutral'
        }
      />
      <KpiCard
        label="Interest Avoided"
        value={formatCurrency(summary.cumulativeInterestAvoided)}
        variant={
          summary.cumulativeInterestAvoided !== null && summary.cumulativeInterestAvoided > 0
            ? 'positive'
            : 'neutral'
        }
      />
      <KpiCard
        label="Months Saved"
        value={formatDelta(summary.monthsSaved, ' mo')}
        variant={statusVariant(summary.currentStatus)}
      />
      {summary.projectedPayoffDateDelta !== null && (
        <KpiCard
          label="Payoff Delta"
          value={`${summary.projectedPayoffDateDelta.toFixed(1)} mo`}
          variant={summary.projectedPayoffDateDelta > 0 ? 'positive' : summary.projectedPayoffDateDelta < 0 ? 'negative' : 'neutral'}
        />
      )}
      {summary.firstMeaningfulDivergenceDate && (
        <KpiCard
          label="Divergence Start"
          value={summary.firstMeaningfulDivergenceDate}
          variant="neutral"
        />
      )}
    </div>
  );
}
