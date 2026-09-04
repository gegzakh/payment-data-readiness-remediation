import type { ReleaseDto } from '../api/releases';

export function ReleaseCard({ release }: { release: ReleaseDto }) {
  return (
    <article className="release">
      <header>
        <h2>
          {release.version} — {release.title}
        </h2>
        <time dateTime={release.releaseDate}>{release.releaseDate}</time>
      </header>
      {release.summary && <p>{release.summary}</p>}
      {release.groups.map((group) => (
        <div className="release__group" key={group.component}>
          <h3>{group.component}</h3>
          <ul>
            {group.entries.map((entry) => (
              <li key={entry.id}>
                <span className={`tag tag--${entry.type.toLowerCase()}`}>{entry.type}</span>
                <span className="release__entry-title">{entry.title}</span>
                {entry.body && <p className="muted">{entry.body}</p>}
                {entry.references.length > 0 && <p className="muted">Refs: {entry.references.join(', ')}</p>}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </article>
  );
}
