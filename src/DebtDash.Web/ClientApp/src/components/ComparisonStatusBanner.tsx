import type { ComparisonSummary, DashboardState } from '../services/dashboardApi';

interface Props {
  summary: ComparisonSummary;
  state: DashboardState;
}

/** T018: Comparison status banner — communicates current vs-baseline status in plain language. */
export default function ComparisonStatusBanner({ summary, state }: Props) {
  const bannerClass = () => {
    if (state === 'empty' || state === 'limitedData') return 'status-banner status-banner--info';
    switch (summary.currentStatus) {
      case 'ahead': return 'status-banner status-banner--positive';
      case 'behind': return 'status-banner status-banner--negative';
      case 'onTrack': return 'status-banner status-banner--neutral';
      default: return 'status-banner status-banner--info';
    }
  };

  const statusLabel = () => {
    if (state === 'empty') return 'No Data';
    if (state === 'limitedData') return 'Limited Data';
    switch (summary.currentStatus) {
      case 'ahead': return 'Ahead of Schedule';
      case 'behind': return 'Behind Schedule';
      case 'onTrack': return 'On Track';
      default: return 'Insufficient Data';
    }
  };

  return (
    <div
      className={bannerClass()}
      role="status"
      aria-live="polite"
      aria-label={`Comparison status: ${statusLabel()}`}
    >
      <strong className="status-banner__label">{statusLabel()}</strong>
      <p className="status-banner__message">{summary.explanatoryStateMessage}</p>
      <span className="status-banner__recalculated" aria-label="Last updated">
        Updated {new Date(summary.lastRecalculatedAt).toLocaleString()}
      </span>
    </div>
  );
}
