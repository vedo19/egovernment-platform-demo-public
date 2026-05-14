/**
 * Maps a status string to a semantic type for AppBadge.
 * @param {string} status
 * @returns {'primary'|'success'|'warning'|'danger'|'info'|'neutral'}
 */
export const getStatusType = (status) => {
  if (!status) return 'neutral';
  const s = status.toLowerCase();

  if (s.includes('approve') || s.includes('success')) return 'success';
  if (s.includes('reject') || s.includes('error') || s.includes('fail')) return 'danger';
  if (s.includes('submit') || s.includes('new')) return 'primary';
  if (s.includes('review') || s.includes('assign') || s.includes('process')) return 'info';
  if (s.includes('wait') || s.includes('pend') || s.includes('document')) return 'warning';

  return 'neutral';
};
