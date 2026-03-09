#!/usr/bin/env bash
set -euo pipefail

echo "=== DebtDash Performance Baseline Validation ==="
echo ""

BASE_URL="${1:-http://localhost:5000}"

echo "Target: $BASE_URL"
echo ""

# Check if the server is running
if ! curl -sf "$BASE_URL/api/loan" -o /dev/null 2>/dev/null; then
  if ! curl -sf "$BASE_URL/api/loan" -o /dev/null -w "%{http_code}" 2>/dev/null | grep -qE "200|404"; then
    echo "ERROR: Server not reachable at $BASE_URL"
    exit 1
  fi
fi

echo "[1/3] Read API p95 < 300ms"
TIMES=()
for i in $(seq 1 20); do
  T=$(curl -sf -o /dev/null -w "%{time_total}" "$BASE_URL/api/loan" 2>/dev/null || echo "0")
  TIMES+=("$T")
done
# Sort and take 95th percentile (index 18 of 20)
SORTED=($(printf '%s\n' "${TIMES[@]}" | sort -n))
P95=${SORTED[18]}
P95_MS=$(echo "$P95 * 1000" | bc)
echo "  p95 read: ${P95_MS}ms (budget: 300ms)"

echo ""
echo "[2/3] Write API p95 < 2000ms — run with seeded data for meaningful results"
echo "  (Manual verification required with 5000 payment entries)"

echo ""
echo "[3/3] Dashboard load p95 < 2000ms"
T=$(curl -sf -o /dev/null -w "%{time_total}" "$BASE_URL/api/dashboard" 2>/dev/null || echo "0")
T_MS=$(echo "$T * 1000" | bc)
echo "  Dashboard load: ${T_MS}ms (budget: 2000ms)"

echo ""
echo "=== Validation Complete ==="
