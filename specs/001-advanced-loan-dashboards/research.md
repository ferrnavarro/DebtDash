# Research: Advanced Loan Comparison Dashboards

## Comparison Baseline Strategy

- Decision: Derive the no-extra-principal baseline from the original loan terms and
  scheduled payment behavior, then compare every actual point against the
  corresponding baseline point on a shared time axis.
- Rationale: This directly answers the user need to understand how the loan would have
  evolved without extra capital payments and keeps the comparison explainable.
- Alternatives considered: Comparing only current totals was rejected because it hides
  when divergence began; storing a second persisted amortization ledger was rejected
  because it duplicates derivable data and adds consistency risk.

## Comparison Read Model Strategy

- Decision: Keep comparison outputs as a derived dashboard read model returned by the
  dashboard API rather than persisting a separate comparison entity as the source of
  truth.
- Rationale: The repo already recalculates dashboard and projection outputs from loan
  and payment history. Extending that pattern minimizes schema risk and keeps the
  implementation simple.
- Alternatives considered: Persisting comparison timeline rows was rejected because
  recalculation after payment edits would create avoidable synchronization work.

## Dashboard API Shape

- Decision: Extend the existing dashboard response with explicit comparison summary,
  comparison series, milestone points, and a requested time-window selector.
- Rationale: The product already exposes a dashboard endpoint. Enriching one payload
  keeps the frontend data flow simple and avoids multiple requests for the same page.
- Alternatives considered: Creating a second comparison-only endpoint was rejected
  because it would split tightly related dashboard data and increase client
  orchestration complexity.

## Time Windowing Approach

- Decision: Support a bounded set of user-meaningful windows such as full history,
  last 6 months, last 12 months, and year to date.
- Rationale: The spec requires full-history and shorter recent-period views while
  avoiding arbitrary analytical configuration. Preset windows keep both API and UI
  predictable.
- Alternatives considered: Fully custom date-range analytics was rejected because it
  adds scope, extra validation, and more complex chart-state handling than this
  feature requires.

## Visualization Strategy

- Decision: Use synchronized time-series charts for remaining balance and cumulative
  cost, plus comparison KPI cards and milestone callouts.
- Rationale: The existing frontend already uses Recharts and dashboard cards. Shared
  visual patterns reduce implementation risk while making the actual-versus-baseline
  story readable.
- Alternatives considered: Dense multi-axis or highly customized financial charting
  was rejected because it would increase cognitive load and likely violate current UX
  consistency expectations.

## Comparison Milestone Definition

- Decision: Define milestone outputs around current acceleration status, first
  divergence period, current months saved, projected payoff delta, and cumulative
  interest avoided.
- Rationale: These directly support the requested understanding of how the loan is
  behaving over time and what extra capital payments are achieving.
- Alternatives considered: Reporting only a single payoff-date delta was rejected
  because it under-explains the progression and savings story.

## Limited-Data Handling

- Decision: Return explicit dashboard state metadata when history is too short or when
  actual and baseline paths do not yet meaningfully diverge.
- Rationale: The constitution requires explicit UX states, and the spec requires the
  system to explain limited comparison outputs rather than imply false precision.
- Alternatives considered: Hiding comparison graphs until more data exists was
  rejected because users still benefit from seeing baseline context and overlap.

## Performance Validation Strategy

- Decision: Validate enriched dashboard response time and render responsiveness with
  seeded histories up to 5,000 payments, using integration tests for payload timing
  and existing regression or performance test lanes for heavier scenarios.
- Rationale: The feature adds derived chart series and summaries, so performance must
  be measured on realistic histories before merge.
- Alternatives considered: Relying only on ad hoc local testing was rejected because
  the constitution requires measurable budgets and repeatable validation.