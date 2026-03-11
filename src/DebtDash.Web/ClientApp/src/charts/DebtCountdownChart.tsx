import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import { fmtMonthYear } from '../utils/date';

interface Props {
  data: { date: string; remainingBalance: number }[];
}

export default function DebtCountdownChart({ data }: Props) {
  return (
    <div role="img" aria-label="Debt countdown chart">
      <ResponsiveContainer width="100%" height={300}>
        <AreaChart data={data}>
          <XAxis dataKey="date" tickFormatter={fmtMonthYear} />
          <YAxis />
          <Tooltip />
          <Area
            type="monotone"
            dataKey="remainingBalance"
            name="Remaining Balance"
            stroke="#ef4444"
            fill="#fecaca"
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
