import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import type { ComparisonTimelinePoint } from '../services/dashboardApi';

interface Props {
  data: ComparisonTimelinePoint[];
}

/** T028: Actual-versus-baseline cumulative cost (interest) chart. */
export default function ComparisonCostChart({ data }: Props) {
  if (data.length === 0) {
    return (
      <p className="chart-empty" role="note">
        Not enough data to display the cumulative cost comparison chart.
      </p>
    );
  }

  return (
    <figure role="img" aria-label="Actual vs baseline cumulative interest chart">
      <figcaption className="sr-only">
        A line chart comparing cumulative interest paid under your actual payment history against the
        original no-extra-principal baseline. When the blue line is below the orange line, you are
        paying less interest than the baseline schedule.
      </figcaption>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={data}>
          <XAxis dataKey="date" />
          <YAxis
            tickFormatter={(v: number) => `$${(v / 1000).toFixed(0)}k`}
            label={{ value: 'Cumulative Interest', angle: -90, position: 'insideLeft', offset: 10 }}
          />
          <Tooltip
            formatter={(value: number | undefined, name: string | undefined) => [value != null ? `$${value.toFixed(2)}` : '', name ?? '']}
          />
          <Legend />
          <Line
            type="monotone"
            dataKey="actualCumulativeInterest"
            name="Actual Interest"
            stroke="#3b82f6"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4 }}
          />
          <Line
            type="monotone"
            dataKey="baselineCumulativeInterest"
            name="Baseline Interest"
            stroke="#f97316"
            strokeWidth={2}
            strokeDasharray="5 5"
            dot={false}
            activeDot={{ r: 4 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </figure>
  );
}
