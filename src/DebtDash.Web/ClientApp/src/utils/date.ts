const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
                    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/**
 * Format an ISO date string (YYYY-MM-DD) as a compact month label for chart axes.
 * e.g. "2024-03-01" → "Mar '24"
 */
export function fmtMonthYear(iso: string): string {
  const [year, month] = iso.split('-');
  return `${monthNames[parseInt(month, 10) - 1]} '${year.slice(2)}`;
}

/**
 * Format an ISO date string (YYYY-MM-DD) as a 4-digit year for chart axes.
 * e.g. "2024-03-01" → "2024"
 */
export function fmtYear(iso: string): string {
  return iso.slice(0, 4);
}
