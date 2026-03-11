import { useState, useRef } from 'react';
import type { ChangeEvent } from 'react';
import { validateCsvImport, getImportTemplateUrl } from '../../services/paymentApi';
import type { ImportPreviewResponse } from '../../services/paymentApi';

interface CsvImportDropzoneProps {
  onPreviewReady: (preview: ImportPreviewResponse) => void;
}

type DropzoneState = 'idle' | 'validating' | 'error';

export default function CsvImportDropzone({ onPreviewReady }: CsvImportDropzoneProps) {
  const [state, setState] = useState<DropzoneState>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  async function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setState('validating');
    setErrorMessage(null);

    try {
      const preview = await validateCsvImport(file);
      setState('idle');
      onPreviewReady(preview);
    } catch (err) {
      setState('error');
      setErrorMessage(err instanceof Error ? err.message : 'File validation failed.');
    } finally {
      if (inputRef.current) inputRef.current.value = '';
    }
  }

  return (
    <div className="csv-import-dropzone" aria-live="polite">
      <p>
        <a
          href={getImportTemplateUrl()}
          download="payment-import-template.csv"
          aria-label="Download CSV template for payment import"
        >
          Download CSV template
        </a>{' '}
        to see the required format.
      </p>

      <div className="csv-import-input-row">
        <label htmlFor="csv-file-input" className="btn">
          {state === 'validating' ? 'Validating…' : 'Choose CSV file'}
        </label>
        <input
          ref={inputRef}
          id="csv-file-input"
          type="file"
          accept=".csv"
          aria-label="Choose a CSV file to import payments"
          disabled={state === 'validating'}
          onChange={handleFileChange}
          style={{ display: 'none' }}
        />
      </div>

      {state === 'error' && errorMessage && (
        <div role="alert" className="alert alert-error">
          {errorMessage}
        </div>
      )}
    </div>
  );
}
