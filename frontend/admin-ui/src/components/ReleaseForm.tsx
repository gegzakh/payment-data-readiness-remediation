import { useState } from 'react';
import { releaseEntryTypes, type CreateReleaseInput, type ReleaseEntryType } from '../api/releases';

interface ReleaseFormProps {
  onSubmit: (input: CreateReleaseInput) => void;
  pending: boolean;
}

const today = () => new Date().toISOString().slice(0, 10);

export function ReleaseForm({ onSubmit, pending }: ReleaseFormProps) {
  const [version, setVersion] = useState('');
  const [title, setTitle] = useState('');
  const [releaseDate, setReleaseDate] = useState(today());
  const [summary, setSummary] = useState('');
  const [entryType, setEntryType] = useState<ReleaseEntryType>('Feature');
  const [component, setComponent] = useState('');
  const [entryTitle, setEntryTitle] = useState('');

  return (
    <form
      className="card"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit({
          version,
          title,
          releaseDate,
          summary: summary || null,
          entries: entryTitle ? [{ type: entryType, component, title: entryTitle }] : [],
        });
        setVersion('');
        setTitle('');
        setSummary('');
        setEntryTitle('');
      }}
    >
      <h2>New draft release</h2>
      <div className="toolbar">
        <label>
          Version
          <input onChange={(event) => setVersion(event.target.value)} required value={version} />
        </label>
        <label>
          Title
          <input onChange={(event) => setTitle(event.target.value)} required value={title} />
        </label>
        <label>
          Release date
          <input
            onChange={(event) => setReleaseDate(event.target.value)}
            required
            type="date"
            value={releaseDate}
          />
        </label>
      </div>
      <label>
        Summary
        <textarea onChange={(event) => setSummary(event.target.value)} rows={2} value={summary} />
      </label>
      <fieldset>
        <legend>First entry (optional)</legend>
        <div className="toolbar">
          <label>
            Type
            <select onChange={(event) => setEntryType(event.target.value as ReleaseEntryType)} value={entryType}>
              {releaseEntryTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            Component
            <input onChange={(event) => setComponent(event.target.value)} value={component} />
          </label>
          <label>
            Entry title
            <input onChange={(event) => setEntryTitle(event.target.value)} value={entryTitle} />
          </label>
        </div>
      </fieldset>
      <button disabled={pending} type="submit">
        Create draft
      </button>
    </form>
  );
}
