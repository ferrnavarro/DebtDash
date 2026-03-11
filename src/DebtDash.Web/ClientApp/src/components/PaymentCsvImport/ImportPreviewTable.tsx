import { useState } from 'react';
import { confirmCsvImport } from '../../services/paymentApi';
import type { ImportPreviewResponse, ImportConfirmResponse } from '../../services/paymentApi';

interface ImportPreviewTableProps {
  preview: ImportPreviewResponse;
  onImported: (result: ImportConfirmResponse) => void;
  onCancel: () => void;
}

export default function ImportPreviewTable({ preview, onImported, onCancel }: ImportPreviewTableProps) {
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleConfirm() {
    setConfirming(true);
    setError(null);
    try {
      const result = await confirmCsvImport(preview.validRows);
      onImported(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed.');
      setConfirming(false);
    }
  }

  return (
    <div className="import-preview">
      <p aria-live="polite">
        {preview.totalRows} row{preview.totalRows !== 1 ? 's' : ''} found —{' '}
        <strong>{preview.validCount} valid</strong>
        {preview.invalidCount > 0 && (
          <>{', '}<span className="text-warning">{preview.invalidCount} invalid (will be skipped)</span></>
        )}
      </p>

      {preview.invalidCount > 0 && (
        <section aria-label="Invalid rows details">
          <h4>Invalid rows (not imported):</h4>
          <ul role="list" className="import-error-list">
            {preview.invalidRows.map((row) => (
              <li key={row.rowIndex}>
                <strong>Row {row.rowIndex}:</strong> {row.errors.join('; ')}
              </li>
            ))}
          </ul>
        </section>
      )}

      {error && (
        <div role="alert" className="alert alert-error">{error}</div>
      )}

      <div className="form-actions">
        {preview.validCount > 0 ? (
          <button
            type="button"
            className="btn btn-primary"
            disabled={confirming}
            onClick={handleConfirm}
          >
            {confirming
              ? 'Importing…'
              : `Import ${preview.validCount} payment${preview.validCount !== 1 ? 's' : ''}`}
          </button>
        ) : (
          <p className="text-muted">No valid rows to import. Fix errors and try again.</p>
        )}
        <button type="button" className="btn" onClick={onCancel} disabled={confirming}>
          Cancel
        </button>
      </div>
    </div>
  );
}
