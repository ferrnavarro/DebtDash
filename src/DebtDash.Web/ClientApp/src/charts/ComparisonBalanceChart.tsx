import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';
import type { ComparisonTimelinePoint } from '../services/dashboardApi';

interface Props {
  data: ComparisonTimelinePoint[];
}

/** T027: Actual-versus-baseline remaining balance chart. */
export default function ComparisonBalanceChart({ data }: Props) {
  if (data.length === 0) {
    return (
      <p className="chart-empty" role="note">
        Not enough data to display the balance comparison chart.
      </p>
    );
  }

  return (
    <figure role="img" aria-label="Actual vs baseline remaining balance chart">
      <figcaption className="sr-only">
        A line chart comparing your actual remaining loan balance against the no-extra-principal
        baseline schedule over time. When the blue line is below the orange line, you are ahead of
        schedule.
      </figcaption>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={data}>
          <XAxis dataKey="date" />
          <YAxis
            tickFormatter={(v: number) => `$${(v / 1000).toFixed(0)}k`}
            label={{ value: 'Balance ($)', angle: -90, position: 'insideLeft', style: { textAnchor: 'middle' } }}
          />
          <Tooltip
            formatter={(value: number | undefined, name: string | undefined) => [value != null ? `$${value.toFixed(2)}` : '', name ?? '']}
          />
          <Legend />
          <ReferenceLine y={0} stroke="#ccc" />
          <Line
            type="monotone"
            dataKey="actualRemainingBalance"
            name="Actual Balance"
            stroke="#3b82f6"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4 }}
          />
          <Line
            type="monotone"
            dataKey="baselineRemainingBalance"
            name="Baseline Balance"
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
