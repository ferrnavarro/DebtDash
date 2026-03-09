# UX Consistency & Accessibility Checklist: Loan Tracker

## Loading States

- [ ] Loan setup page shows loading indicator during data fetch
- [ ] Ledger table shows loading skeleton/spinner during data fetch
- [ ] Dashboard shows loading indicator for KPI cards and charts
- [ ] All loading states have accessible `aria-busy` or `aria-live` attributes

## Empty States

- [ ] Loan setup page shows helpful prompt when no loan configured
- [ ] Ledger table shows "No payments recorded" message with CTA
- [ ] Dashboard shows "Configure loan and add payments" guidance when empty

## Error States

- [ ] API errors display user-friendly messages (not raw HTTP errors)
- [ ] Form validation errors are inline and associated with fields
- [ ] Network failure shows retry option
- [ ] All error messages are accessible via `aria-describedby` or `role="alert"`

## Success States

- [ ] Loan save confirmation is shown and auto-dismisses
- [ ] Payment create/update/delete shows success feedback
- [ ] Dashboard refreshes after data changes

## Keyboard Accessibility

- [ ] All interactive elements are focusable via Tab
- [ ] Forms can be submitted with Enter key
- [ ] Ledger table rows can be navigated with arrow keys
- [ ] Modal dialogs trap focus appropriately
- [ ] Focus returns to trigger element after modal close

## Form Validation

- [ ] Required fields are marked with visual indicator and `aria-required`
- [ ] Validation fires on blur and on submit
- [ ] Error messages are descriptive ("Principal must be greater than 0")
- [ ] Component breakdown mismatch shows clear explanation

## Visual Consistency

- [ ] Consistent spacing and typography across pages
- [ ] Consistent button styles (primary/secondary/destructive)
- [ ] Rate variance badges use consistent color coding
- [ ] Charts use accessible color palette with sufficient contrast

## Screen Reader

- [ ] Page headings follow logical hierarchy (h1 > h2 > h3)
- [ ] Data tables have proper `<th>` and `scope` attributes
- [ ] Chart data is available in text/table form for screen readers
- [ ] Navigation landmarks are properly defined

## Comparison Dashboard States (T005)

- [ ] Comparison status banner shows "ahead", "on-track", "behind", or "insufficient-data" with visible text
- [ ] Summary cards show balance delta, interest avoided, and months saved (or N/A when unavailable)
- [ ] Limited-data state shows `explanatoryStateMessage` explaining why comparison is not yet possible
- [ ] Window selector is keyboard-accessible; active window has `aria-pressed` or equivalent
- [ ] Chart sections have adjacent text summaries for screen-reader users
- [ ] Comparison charts use distinct accessible colors (solid vs dashed) for actual vs baseline series
- [ ] Dashboard refreshes comparison metrics after a payment is added, edited, or deleted
- [ ] All new comparison UI elements respect reduced-motion preferences for chart animations
