import { BarChart, Bar, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { fmtMonthYear } from '../utils/date';

interface Props {
  data: { date: string; principalPaid: number; interestPaid: number }[];
}

export default function PrincipalInterestTrendChart({ data }: Props) {
  return (
    <div role="img" aria-label="Principal vs Interest trend chart">
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={data}>
          <XAxis dataKey="date" tickFormatter={fmtMonthYear} />
          <YAxis />
          <Tooltip />
          <Legend />
          <Bar dataKey="principalPaid" name="Principal" fill="#4f46e5" stackId="a" />
          <Bar dataKey="interestPaid" name="Interest" fill="#f59e0b" stackId="a" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
