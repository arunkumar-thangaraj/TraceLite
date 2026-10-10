import Icon, { type IconName } from "./Icon";

interface MetricCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: IconName;
  accent?: "blue" | "green" | "amber" | "red" | "violet";
  tone?: "default" | "success" | "danger";
}

export default function MetricCard({
  title,
  value,
  subtitle,
  icon,
  accent = "blue",
  tone = "default",
}: MetricCardProps) {
  return (
    <div className={`panel metric-card metric-card-${tone}`}>
      <div className="metric-card-top">
        <p className="metric-title">{title}</p>

        <span className={`metric-icon accent-${accent}`}>
          <Icon name={icon} size={18} />
        </span>
      </div>

      <p className="metric-value">{value}</p>

      {subtitle && (
        <p className="metric-subtitle">{subtitle}</p>
      )}
    </div>
  );
}
