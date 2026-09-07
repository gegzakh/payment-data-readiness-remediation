export function Metric({ label, value, tone }: { label: string; value: string | number; tone?: 'risk' }) {
  return (
    <div className={tone === 'risk' ? 'metric metric--risk' : 'metric'}>
      <span className={tone === 'risk' ? 'metric__value error' : 'metric__value'}>{value}</span>
      <span className="metric__label">{label}</span>
    </div>
  );
}
