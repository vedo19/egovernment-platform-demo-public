export default function ProgressBar({ percentage = 0, color = 'blue' }) {
  const safePercentage = Math.max(0, Math.min(100, Number(percentage) || 0));

  return (
    <div className="progressbar-track" role="progressbar" aria-valuenow={safePercentage} aria-valuemin={0} aria-valuemax={100}>
      <div
        className={`progressbar-fill progressbar-${color}`}
        style={{ width: `${safePercentage}%` }}
      />
      <span className="progressbar-label">{safePercentage}%</span>
    </div>
  );
}
