import { getStatusTone } from "../utils/format";

interface MethodBadgeProps {
  method: string;
}

export function MethodBadge({ method }: MethodBadgeProps) {
  return (
    <span className="badge method-badge">
      {method.toUpperCase()}
    </span>
  );
}

interface StatusBadgeProps {
  statusCode: number;
  label?: string;
}

export function StatusBadge({
  statusCode,
  label,
}: StatusBadgeProps) {
  return (
    <span
      className={`badge badge-${getStatusTone(statusCode)}`}
    >
      <span className="badge-dot" aria-hidden="true" />
      {statusCode}
      {label && ` ${label}`}
    </span>
  );
}
