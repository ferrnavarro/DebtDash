import type { DashboardWindow, DashboardWindowKey } from '../services/dashboardApi';

interface Props {
  windows: DashboardWindow[];
  activeKey: DashboardWindowKey;
  onSelect: (key: DashboardWindowKey) => void;
}

/** T029: Dashboard window selector — keyboard-accessible preset time-window buttons. */
export default function DashboardWindowSelector({ windows, activeKey, onSelect }: Props) {
  return (
    <nav
      className="window-selector"
      aria-label="Dashboard time window"
      role="group"
    >
      {windows.map((w) => (
        <button
          key={w.key}
          type="button"
          className={`window-selector__btn${w.key === activeKey ? ' window-selector__btn--active' : ''}`}
          aria-pressed={w.key === activeKey}
          onClick={() => onSelect(w.key)}
        >
          {w.label}
        </button>
      ))}
    </nav>
  );
}
