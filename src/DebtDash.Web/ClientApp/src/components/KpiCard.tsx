interface KpiCardProps {
  label: string;
  value: string;
  variant?: 'positive' | 'negative' | 'neutral';
}

export default function KpiCard({ label, value, variant = 'neutral' }: KpiCardProps) {
  return (
    <div className={`kpi-card kpi-${variant}`} role="group" aria-label={label}>
      <div className="kpi-value">{value}</div>
      <div className="kpi-label">{label}</div>
    </div>
  );
}
