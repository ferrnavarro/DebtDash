import { useState, useEffect } from 'react';
import { getDashboard, getProjection } from '../services/dashboardApi';
import type { DashboardData, ProjectionData } from '../services/dashboardApi';
import KpiCard from '../components/KpiCard';
import PrincipalInterestTrendChart from '../charts/PrincipalInterestTrendChart';
import DebtCountdownChart from '../charts/DebtCountdownChart';

type Status = 'loading' | 'idle' | 'error';

export default function DashboardPage() {
  const [status, setStatus] = useState<Status>('loading');
  const [error, setError] = useState<string | null>(null);
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [projection, setProjection] = useState<ProjectionData | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    setStatus('loading');
    setError(null);
    try {
      const [dash, proj] = await Promise.all([getDashboard(), getProjection()]);
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
          <button onClick={loadData} type="button">Retry</button>
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

      <div className="kpi-grid" role="region" aria-label="Key metrics">
        <KpiCard label="Total Interest Paid" value={`$${dashboard.totalInterestPaid.toFixed(2)}`} />
        <KpiCard label="Total Capital Paid" value={`$${dashboard.totalCapitalPaid.toFixed(2)}`} />
        <KpiCard label="Avg Real Rate" value={`${dashboard.averageRealRateWeighted.toFixed(4)}%`} />
        <KpiCard label="Time Remaining" value={`${dashboard.timeRemainingMonths.toFixed(1)} mo`} />
        <KpiCard label="Original Term" value={`${dashboard.originalTermMonths} mo`} />
        {projection && (
          <>
            <KpiCard label="Predicted End Date" value={projection.predictedEndDate} />
            <KpiCard
              label="vs Baseline"
              value={`${projection.deltaMonthsVsBaseline > 0 ? '+' : ''}${projection.deltaMonthsVsBaseline.toFixed(1)} mo`}
              variant={projection.deltaMonthsVsBaseline < 0 ? 'positive' : projection.deltaMonthsVsBaseline > 0 ? 'negative' : 'neutral'}
            />
          </>
        )}
      </div>

      {dashboard.principalInterestTrendSeries.length > 0 && (
        <section className="chart-section">
          <h2>Principal vs Interest Trend</h2>
          <PrincipalInterestTrendChart data={dashboard.principalInterestTrendSeries} />
        </section>
      )}

      {dashboard.debtCountdownSeries.length > 0 && (
        <section className="chart-section">
          <h2>Debt Countdown</h2>
          <DebtCountdownChart data={dashboard.debtCountdownSeries} />
        </section>
      )}
    </main>
  );
}
