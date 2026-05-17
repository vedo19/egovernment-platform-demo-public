import React from 'react';

const TYPE_MAP = {
  primary: 'app-badge-primary',
  success: 'app-badge-success',
  warning: 'app-badge-warning',
  danger: 'app-badge-danger',
  info: 'app-badge-info',
  neutral: 'app-badge-neutral',
};

/**
 * A reusable Badge component with semantic styling.
 * @param {Object} props
 * @param {React.ReactNode} props.children - The content of the badge
 * @param {'primary'|'success'|'warning'|'danger'|'info'|'neutral'} [props.type] - Semantic type for coloring
 * @param {string} [props.className] - Additional CSS classes
 */
export default function AppBadge({ children, type = 'neutral', className = '' }) {
  const typeClass = TYPE_MAP[type] || TYPE_MAP.neutral;

  return <span className={`app-badge ${typeClass} ${className}`}>{children}</span>;
}
