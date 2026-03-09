import { useState, useEffect } from 'react';
import { getDashboardComparison, getProjection } from '../services/dashboardApi';
import type { DashboardComparisonData, DashboardWindowKey, ProjectionData } from '../services/dashboardApi';
import KpiCard from '../components/KpiCard';
import ComparisonStatusBanner from '../components/ComparisonStatusBanner';
import ComparisonSummaryCards from '../components/ComparisonSummaryCards';
import DashboardWindowSelector from '../components/DashboardWindowSelector';
import ComparisonBalanceChart from '../charts/ComparisonBalanceChart';
import ComparisonCostChart from '../charts/ComparisonCostChart';
import ComparisonMilestones from '../components/ComparisonMilestones';
import ComparisonSavingsHighlights from '../components/ComparisonSavingsHighlights';

type Status = 'loading' | 'idle' | 'error';

export default function DashboardPage() {
  const [status, setStatus] = useState<Status>('loading');
  const [error, setError] = useState<string | null>(null);
  const [dashboard, setDashboard] = useState<DashboardComparisonData | null>(null);
  const [projection, setProjection] = useState<ProjectionData | null>(null);
  const [activeWindow, setActiveWindow] = useState<DashboardWindowKey>('fullHistory');

  useEffect(() => {
    loadData(activeWindow);
  }, [activeWindow]);

  async function loadData(window: DashboardWindowKey) {
    setStatus('loading');
    setError(null);
    try {
      const [dash, proj] = await Promise.all([
        getDashboardComparison(window),
        getProjection(),
      ]);
      setDashboard(dash);
      setProjection(proj);
      setStatus('idle');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load dashboard');
      setStatus('error');
    }
  }

  if (status === 'loading') {
    return (
      <main className="page">
        <h1>Dashboard</h1>
        <div aria-busy="true" aria-live="polite"><p>Loading dashboard...</p></div>
      </main>
    );
  }

  if (status === 'error') {
    return (
      <main className="page">
        <h1>Dashboard</h1>
        <div role="alert" className="alert alert-error">
          {error}
          <button onClick={() => loadData(activeWindow)} type="button">Retry</button>
        </div>
      </main>
    );
  }

  if (!dashboard) {
    return (
      <main className="page">
        <h1>Dashboard</h1>
        <div className="empty-state" aria-live="polite">
          <p>Configure a loan and add payments to see your dashboard metrics.</p>
        </div>
      </main>
    );
  }

  return (
    <main className="page">
      <h1>Dashboard</h1>

      {/* Time window selector — T029/T030 */}
      {dashboard.availableWindows.length > 0 && (
        <DashboardWindowSelector
          windows={dashboard.availableWindows}
          activeKey={dashboard.activeWindow.key}
          onSelect={(key) => setActiveWindow(key)}
        />
      )}

      {/* Comparison status banner — T018/T020 */}
      <ComparisonStatusBanner summary={dashboard.summary} state={dashboard.state} />

      {/* Comparison summary KPI cards — T017 */}
      <ComparisonSummaryCards summary={dashboard.summary} state={dashboard.state} />

      {/* Legacy KPI metrics region */}
      {projection && (
        <section aria-label="Projection metrics">
          <div className="kpi-grid" role="region" aria-label="Projection key metrics">
            <KpiCard label="Predicted End Date" value={projection.predictedEndDate} />
            <KpiCard
              label="vs Baseline"
              value={`${projection.deltaMonthsVsBaseline > 0 ? '+' : ''}${projection.deltaMonthsVsBaseline.toFixed(1)} mo`}
              variant={projection.deltaMonthsVsBaseline < 0 ? 'positive' : projection.deltaMonthsVsBaseline > 0 ? 'negative' : 'neutral'}
            />
          </div>
        </section>
      )}

      {/* Savings highlights — T038/T039 */}
      <ComparisonSavingsHighlights summary={dashboard.summary} />

      {/* Comparison balance chart — T027/T030 */}
      {dashboard.balanceSeries.length > 0 && (
        <section className="chart-section">
          <h2>Balance vs Baseline</h2>
          <ComparisonBalanceChart data={dashboard.balanceSeries} />
        </section>
      )}

      {/* Comparison cost chart — T028/T030 */}
      {dashboard.costSeries.length > 0 && (
        <section className="chart-section">
          <h2>Cumulative Interest vs Baseline</h2>
          <ComparisonCostChart data={dashboard.costSeries} />
        </section>
      )}

      {/* Milestones — T037/T039 */}
      {dashboard.milestones.length > 0 && (
        <ComparisonMilestones milestones={dashboard.milestones} />
      )}

      {/* Limited-data / empty detail message — T020 */}
      {(dashboard.state === 'empty' || dashboard.state === 'limitedData') && (
        <div className="empty-state" aria-live="polite" role="note">
          <p>{dashboard.summary.explanatoryStateMessage}</p>
        </div>
      )}
    </main>
  );
}
