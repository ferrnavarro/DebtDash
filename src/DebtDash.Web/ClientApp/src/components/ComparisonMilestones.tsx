import type { ComparisonMilestone } from '../services/dashboardApi';

interface Props {
  milestones: ComparisonMilestone[];
}

const typeLabels: Record<ComparisonMilestone['type'], string> = {
  divergenceStart: 'Divergence Start',
  highestBalanceGap: 'Largest Balance Advantage',
  highestInterestSavings: 'Greatest Interest Savings',
  earlyPayoff: 'Early Payoff',
  overlap: 'Overlap',
};

/** T037: Comparison milestones list — surface meaningful moments in the actual-vs-baseline story. */
export default function ComparisonMilestones({ milestones }: Props) {
  if (milestones.length === 0) return null;

  return (
    <section className="comparison-milestones" aria-label="Loan milestones">
      <h2>Milestones</h2>
      <ul className="milestones-list" role="list">
        {milestones.map((m, i) => (
          <li key={i} className={`milestone milestone--${m.type}`}>
            <span className="milestone__date" aria-label="Date">{m.date}</span>
            <div className="milestone__body">
              <strong className="milestone__title">{m.title || typeLabels[m.type]}</strong>
              <p className="milestone__description">{m.description}</p>
              {m.value !== null && (
                <span className="milestone__value" aria-label="Associated value">
                  ${Math.abs(m.value).toFixed(2)}
                </span>
              )}
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
