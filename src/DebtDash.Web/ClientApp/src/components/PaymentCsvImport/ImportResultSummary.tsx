import type { ImportConfirmResponse } from '../../services/paymentApi';

interface ImportResultSummaryProps {
  result: ImportConfirmResponse;
  onReset: () => void;
}

export default function ImportResultSummary({ result, onReset }: ImportResultSummaryProps) {
  return (
    <div className="import-result" aria-live="polite">
      <p>
        <strong>{result.importedCount}</strong> payment{result.importedCount !== 1 ? 's' : ''} imported
        {result.skippedCount > 0 && (
          <>{', '}<span className="text-warning">{result.skippedCount} skipped</span></>
        )}
        .
      </p>

      {result.skippedCount > 0 && (
        <section aria-label="Skipped rows">
          <h4>Skipped rows:</h4>
          <ul role="list" className="import-error-list">
            {result.skippedRows.map((row) => (
              <li key={row.rowIndex}>
                <strong>Row {row.rowIndex}:</strong> {row.reason}
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="form-actions">
        <button type="button" className="btn btn-primary" onClick={onReset}>
          Import another file
        </button>
      </div>
    </div>
  );
}
