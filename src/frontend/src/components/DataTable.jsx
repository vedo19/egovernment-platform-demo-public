import React from 'react';

/**
 * A highly reusable and styled Table component.
 * @param {Object} props
 * @param {Array} props.columns - Array of column definitions { header: string, key: string, render?: (item) => ReactNode, className?: string, style?: object }
 * @param {Array} props.data - Array of data objects to display
 * @param {boolean} [props.loading] - Loading state
 * @param {string} [props.emptyMessage] - Message to show when data is empty
 * @param {string} [props.className] - Extra class for the table
 */
export default function DataTable({
  columns = [],
  data = [],
  loading = false,
  emptyMessage = 'No data available.',
  className = '',
  onRowClick = null,
}) {
  if (loading) {
    return (
      <div className="data-table-container">
        <table className={`data-table is-loading ${className}`}>
          <thead>
            <tr>
              {columns.map((col, idx) => (
                <th key={col.key || idx} className={col.className} style={col.style}>
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {[...Array(8)].map((_, rowIdx) => (
              <tr key={`skeleton-row-${rowIdx}`}>
                {columns.map((col, colIdx) => (
                  <td key={`skeleton-col-${colIdx}`} className={col.className} style={col.style}>
                    <div className="skeleton skeleton-text"></div>
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="empty-state-card">
        <p className="empty">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="data-table-container">
      <table className={`data-table ${className}`}>
        <thead>
          <tr>
            {columns.map((col, idx) => (
              <th key={col.key || idx} className={col.className} style={col.style}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item, rowIdx) => (
            <tr
              key={item.id || rowIdx}
              onClick={() => onRowClick && onRowClick(item)}
              className={onRowClick ? 'is-clickable' : ''}
            >
              {columns.map((col, colIdx) => (
                <td
                  key={`${rowIdx}-${col.key || colIdx}`}
                  className={col.className}
                  style={col.style}
                  data-label={col.header}
                >
                  {col.render ? col.render(item) : item[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
