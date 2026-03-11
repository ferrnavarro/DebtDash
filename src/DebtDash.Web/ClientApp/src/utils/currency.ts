/** ISO 4217 currency formatters for USD. */

const usdFull = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const usdCompact = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  notation: 'compact',
  maximumFractionDigits: 1,
});

/** Format a number as USD — e.g. $1,234.56 */
export const fmtUSD = (n: number): string => usdFull.format(n);

/** Format a number as compact USD for chart axes — e.g. $12.5K */
export const fmtUSDCompact = (n: number): string => usdCompact.format(n);
