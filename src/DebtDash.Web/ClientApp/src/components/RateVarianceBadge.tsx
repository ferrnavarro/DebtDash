interface RateVarianceBadgeProps {
  isFlagged: boolean;
  varianceBasisPoints: number;
}

export default function RateVarianceBadge({ isFlagged, varianceBasisPoints }: RateVarianceBadgeProps) {
  return (
    <span
      className={`variance-badge ${isFlagged ? 'flagged' : 'normal'}`}
      role="status"
      aria-label={isFlagged ? `Rate variance flagged: ${varianceBasisPoints.toFixed(1)} basis points` : 'Rate variance normal'}
    >
      {isFlagged ? `⚠ ${varianceBasisPoints.toFixed(1)}bp` : '✓ Normal'}
    </span>
  );
}
