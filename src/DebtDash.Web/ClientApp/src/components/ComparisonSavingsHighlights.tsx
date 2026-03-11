import type { ComparisonSummary } from '../services/dashboardApi';
import { fmtUSD } from '../utils/currency';

interface Props {
  summary: ComparisonSummary;
}

/** T038: Savings highlights component — shows interest avoided and time saved prominently. */
export default function ComparisonSavingsHighlights({ summary }: Props) {
  const hasSavings =
    summary.cumulativeInterestAvoided !== null ||
    summary.monthsSaved !== null ||
    summary.projectedPayoffDateDelta !== null;

  if (!hasSavings) return null;

  return (
    <section className="savings-highlights" aria-label="Savings summary">
      <h2>Your Savings</h2>
      <ul className="savings-list" role="list">
        {summary.cumulativeInterestAvoided !== null && (
          <li className="savings-item savings-item--interest">
            <span className="savings-item__value" aria-label="Interest avoided">
              {fmtUSD(summary.cumulativeInterestAvoided)}
            </span>
            <span className="savings-item__label">in interest avoided so far</span>
          </li>
        )}
        {summary.monthsSaved !== null && (
          <li className="savings-item savings-item--time">
            <span className="savings-item__value" aria-label="Months saved">
              {summary.monthsSaved.toFixed(1)} mo
            </span>
            <span className="savings-item__label">ahead of original schedule</span>
          </li>
        )}
        {summary.projectedPayoffDateDelta !== null && summary.projectedPayoffDateDelta > 0 && (
          <li className="savings-item savings-item--payoff">
            <span className="savings-item__value" aria-label="Payoff acceleration">
              {summary.projectedPayoffDateDelta.toFixed(1)} mo
            </span>
            <span className="savings-item__label">earlier projected payoff</span>
          </li>
        )}
      </ul>
    </section>
  );
}
