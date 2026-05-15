import { useState, useEffect } from 'react';
import { authApi, serviceRequestApi, documentApi, citizenApi } from '../../api/services';
import ProgressBar from '../../components/ProgressBar';
import RoleSelector from '../../components/RoleSelector';
import AppSelect from '../../components/AppSelect';
import AppBadge from '../../components/AppBadge';
import DataTable from '../../components/DataTable';
import Modal from '../../components/Modal';
import { USER_ROLES } from '../../domain/roles';
import {
  DOCUMENT_STATUSES,
  SERVICE_REQUEST_STATUS,
  SERVICE_REQUEST_STATUSES,
} from '../../domain/statuses';
import { formatDate } from '../../utils/date';
import { getStatusType } from '../../utils/status';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

const PAGE_SIZE = 8;

// ── Shared UI Components for Details Dialogs ──

const DetailsSection = ({ title, icon }) => (
  <div className="modal-section-title">
    {icon}
    {title}
  </div>
);

const InfoItem = ({ label, value, icon, span = 1, isId = false, color }) => {
  const isEmpty =
    !value || value === '—' || value === 'None' || value === 'Never' || value === 'Pending';
  return (
    <div className={`info-item ${span === 2 ? 'span-2' : span === 3 ? 'full-width' : ''}`}>
      <span className="info-label">
        {icon}
        {label}
      </span>
      <span
        className={`info-value ${isId ? 'id-font' : ''} ${isEmpty ? 'is-empty' : ''}`}
        style={color ? { color, fontWeight: 700 } : {}}
      >
        {value || '—'}
      </span>
    </div>
  );
};

export default function AdminDashboard() {
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const tab = searchParams.get('tab') || 'overview';
  const [requestCount, setRequestCount] = useState(0);
  const [documentCount, setDocumentCount] = useState(0);
  const [userCount, setUserCount] = useState(0);
  const [summaryLoading, setSummaryLoading] = useState(true);

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  useEffect(() => {
    setSearch('');
    setStatusFilter('');
  }, [tab]);

  const loadSummary = async () => {
    setSummaryLoading(true);
    try {
      const [requestsRes, documentsRes, usersRes] = await Promise.all([
        serviceRequestApi.getAll(),
        documentApi.getAll(),
        authApi.users(),
      ]);

      setRequestCount((requestsRes.data || []).length);
      setDocumentCount((documentsRes.data || []).length);
      setUserCount((usersRes.data || []).length);
    } catch {
      setRequestCount(0);
      setDocumentCount(0);
      setUserCount(0);
    } finally {
      setSummaryLoading(false);
    }
  };

  useEffect(() => {
    loadSummary();
  }, []);

  const renderHeader = () => {
    if (tab === 'overview') {
      return (
        <div className="section-header">
          <div>
            <h2>Platform Administration</h2>
            <p className="subtitle">
              System overview, user management, and service request orchestration.
            </p>
          </div>
        </div>
      );
    }

    if (tab === 'requests') {
      return (
        <div className="section-header">
          <div>
            <h2>All Service Requests</h2>
            <p className="subtitle">
              Review requests, update their status, and assign them to officers.
            </p>
          </div>
          <div className="header-actions">
            <input
              className="search-input"
              placeholder="Search requests..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <AppSelect
              value={statusFilter}
              onChange={setStatusFilter}
              placeholder="All Statuses"
              options={SERVICE_REQUEST_STATUSES}
            />
          </div>
        </div>
      );
    }

    if (tab === 'documents') {
      return (
        <div className="section-header">
          <div>
            <h2>All Documents</h2>
            <p className="subtitle">Manage document status, and officer assignment.</p>
          </div>
          <div className="header-actions">
            <input
              className="search-input"
              placeholder="Search documents..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <AppSelect
              value={statusFilter}
              onChange={setStatusFilter}
              placeholder="All Statuses"
              options={DOCUMENT_STATUSES}
            />
          </div>
        </div>
      );
    }

    if (tab === 'users') {
      return (
        <div className="section-header">
          <div>
            <h2>All Users</h2>
            <p className="subtitle">Search user accounts and manage platform roles.</p>
          </div>
          <div className="header-actions">
            <input
              className="search-input"
              placeholder="Search users..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>
      );
    }

    return null;
  };

  return (
    <div className="dashboard-page admin-dashboard">
      <div className="page-hero">{renderHeader()}</div>

      <div className="dashboard-content">
        {tab === 'overview' && (
          <div className="stats-grid">
            <div className="card stat-card">
              <span className="stat-label">Service Requests</span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{requestCount}</strong>
              )}
            </div>
            <div className="card stat-card">
              <span className="stat-label">Documents</span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{documentCount}</strong>
              )}
            </div>
            <div className="card stat-card">
              <span className="stat-label">Users</span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{userCount}</strong>
              )}
            </div>
          </div>
        )}

        <div className="dashboard-section">
          {tab === 'requests' && (
            <RequestsTab
              onRefreshSummary={loadSummary}
              search={search}
              statusFilter={statusFilter}
            />
          )}
          {tab === 'documents' && (
            <DocumentsTab
              onRefreshSummary={loadSummary}
              search={search}
              statusFilter={statusFilter}
            />
          )}
          {tab === 'users' && (
            <UsersTab onRefreshSummary={loadSummary} search={search} user={user} />
          )}
        </div>
      </div>
    </div>
  );
}

function RequestsTab({ onRefreshSummary, search, statusFilter }) {
  const [requests, setRequests] = useState([]);
  const [officers, setOfficers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pending, setPending] = useState({});
  const [saving, setSaving] = useState({});
  const [saved, setSaved] = useState({});
  const [selectedRequest, setSelectedRequest] = useState(null);

  useEffect(() => {
    load();
    loadOfficers();
    setPage(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter]);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await serviceRequestApi.getAllRequests(statusFilter || undefined);
      setRequests(data);
      setPending({});
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
  }, [search]);

  const loadOfficers = async () => {
    try {
      const { data } = await authApi.users();
      setOfficers((data || []).filter((u) => u.role === USER_ROLES.OFFICER));
    } catch {
      /* empty */
    }
  };

  const setPendingField = (id, field, value) => {
    setPending((p) => ({ ...p, [id]: { ...p[id], [field]: value } }));
    setSaved((s) => {
      const n = { ...s };
      delete n[id];
      return n;
    });
  };

  const handleSave = async (r) => {
    const changes = pending[r.id];
    if (!changes) return;

    setSaving((s) => ({ ...s, [r.id]: true }));

    try {
      if (changes.status && changes.status !== r.status) {
        await serviceRequestApi.updateStatus(r.id, { status: changes.status });
      }
      if (changes.officerId && r.status === SERVICE_REQUEST_STATUS.SUBMITTED) {
        await serviceRequestApi.assignOfficerV2(r.id, changes.officerId);
      }

      setSaved((s) => ({ ...s, [r.id]: true }));
      setPending((p) => {
        const n = { ...p };
        delete n[r.id];
        return n;
      });

      await load();
      onRefreshSummary?.();
    } catch {
      /* empty */
    } finally {
      setSaving((s) => ({ ...s, [r.id]: false }));
    }
  };

  const handleChange = (id) => {
    setSaved((s) => {
      const n = { ...s };
      delete n[id];
      return n;
    });
  };

  const filtered = requests.filter((r) => {
    const q = search.toLowerCase();
    const title = (r.title || '').toLowerCase();
    const type = (r.type || '').toLowerCase();
    const citizenId = (r.citizenUserId || '').toLowerCase();

    return !q || title.includes(q) || type.includes(q) || citizenId.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  const columns = [
    { header: 'Type', key: 'type', className: 'col-type' },
    { header: 'Title', key: 'title', className: 'col-expand desc-cell' },
    {
      header: 'Citizen ID',
      key: 'citizenUserId',
      className: 'col-id id-cell',
      render: (r) => <span title={r.citizenUserId}>{r.citizenUserId}</span>,
    },
    {
      header: 'Submitted',
      key: 'createdAt',
      className: 'col-date',
      render: (r) => formatDate(r.createdAt),
    },
    {
      header: 'Status',
      key: 'status',
      className: 'col-status',
      render: (r) => {
        if (saved[r.id]) {
          return <AppBadge type={getStatusType(r.status)}>{r.status}</AppBadge>;
        }
        return (
          <AppSelect
            value={pending[r.id]?.status || r.status}
            onChange={(val) => setPendingField(r.id, 'status', val)}
            options={SERVICE_REQUEST_STATUSES}
          />
        );
      },
    },
    {
      header: 'Assign Officer',
      key: 'assignment',
      className: 'col-expand',
      render: (r) => {
        if (saved[r.id]) {
          const officer = officers.find((o) => o.id === r.assignedOfficerId);
          return <span className="officer-text">{officer?.fullName || 'Unassigned'}</span>;
        }
        return (
          <AppSelect
            value={pending[r.id]?.officerId || r.assignedOfficerId || ''}
            onChange={(val) => setPendingField(r.id, 'officerId', val)}
            disabled={r.status !== SERVICE_REQUEST_STATUS.SUBMITTED}
            placeholder="Assign Officer"
            options={officers.map((o) => ({ label: o.fullName, value: o.id }))}
          />
        );
      },
    },
    {
      header: 'Action',
      key: 'save_action',
      className: 'col-action',
      render: (r) => {
        const hasPending = !!pending[r.id];
        const isSaved = !!saved[r.id];
        const isSaving = !!saving[r.id];

        if (isSaved) {
          return (
            <button className="btn btn-sm btn-outline" onClick={() => handleChange(r.id)}>
              Edit Again
            </button>
          );
        }

        return hasPending ? (
          <button
            className="btn btn-sm btn-success"
            onClick={() => handleSave(r)}
            disabled={isSaving}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        ) : (
          '—'
        );
      },
    },
  ];

  return (
    <div className="card">
      <DataTable
        columns={columns}
        data={paged}
        loading={loading}
        emptyMessage={search ? 'No matching requests.' : 'No requests found.'}
        onRowClick={(r) => setSelectedRequest(r)}
      />

      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
            Previous
          </button>
          <span>
            Page {safePage} of {totalPages}
          </span>
          <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>
            Next
          </button>
        </div>
      )}

      <Modal
        isOpen={!!selectedRequest}
        onClose={() => setSelectedRequest(null)}
        title="Service Request Details"
        className="details-modal"
        maxWidth="900px"
        footer={
          <button className="btn btn-outline" onClick={() => setSelectedRequest(null)}>
            Close
          </button>
        }
      >
        {selectedRequest && (
          <div className="modern-user-details">
            <DetailsSection
              title="Request Information"
              icon={
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                </svg>
              }
            />
            <div className="info-display-grid">
              <InfoItem
                label="Title"
                value={selectedRequest.title}
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                    <polyline points="14 2 14 8 20 8" />
                  </svg>
                }
              />
              <InfoItem
                label="Type"
                value={selectedRequest.type}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="16" x2="12" y2="12" />
                    <line x1="12" y1="8" x2="12.01" y2="8" />
                  </svg>
                }
              />
              <InfoItem
                label="Description"
                value={selectedRequest.description}
                span={3}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
                  </svg>
                }
              />
              <InfoItem
                label="Current Status"
                value={
                  <AppBadge type={getStatusType(selectedRequest.status)}>
                    {selectedRequest.status}
                  </AppBadge>
                }
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                  </svg>
                }
              />
              <InfoItem
                label="Citizen ID"
                value={selectedRequest.citizenUserId}
                span={2}
                isId
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                }
              />
            </div>

            <DetailsSection
              title="Administrative & Timeline"
              icon={
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                  <line x1="16" y1="2" x2="16" y2="6" />
                  <line x1="8" y1="2" x2="8" y2="6" />
                  <line x1="3" y1="10" x2="21" y2="10" />
                </svg>
              }
            />
            <div className="info-display-grid">
              <InfoItem
                label="Submitted On"
                value={formatDate(selectedRequest.createdAt)}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                }
              />
              <InfoItem
                label="Last Updated"
                value={formatDate(selectedRequest.updatedAt, 'Never')}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 20v-6M9 20v-10M15 20v-2M18 20v-16M21 20H3" />
                  </svg>
                }
              />
              <InfoItem
                label="Resolved On"
                value={formatDate(selectedRequest.resolvedAt, 'Pending')}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                }
              />

              <InfoItem
                label="Assigned Officer"
                value={
                  <>
                    {officers.find((o) => o.id === selectedRequest.assignedOfficerId)?.fullName ||
                      'Unassigned'}
                    {selectedRequest.assignedOfficerId && (
                      <span className="id-cell-inline" style={{ marginLeft: '0.5rem' }}>
                        {selectedRequest.assignedOfficerId}
                      </span>
                    )}
                  </>
                }
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                    <circle cx="8.5" cy="7" r="4" />
                    <polyline points="17 11 19 13 23 9" />
                  </svg>
                }
              />
              <InfoItem
                label="Linked Document"
                value={selectedRequest.linkedDocumentId}
                isId
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
                    <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
                  </svg>
                }
              />

              <InfoItem
                label="Officer Notes"
                value={selectedRequest.officerNote}
                span={3}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                  </svg>
                }
              />
              <InfoItem
                label="System Admin Notes"
                value={selectedRequest.adminNotes}
                span={3}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                  </svg>
                }
              />
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

function DocumentsTab({ onRefreshSummary, search, statusFilter }) {
  const [documents, setDocuments] = useState([]);
  const [officers, setOfficers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pending, setPending] = useState({});
  const [saving, setSaving] = useState({});
  const [saved, setSaved] = useState({});
  const [selectedDocument, setSelectedDocument] = useState(null);

  useEffect(() => {
    load();
    loadOfficers();
    setPage(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter]);

  const load = async () => {
    setLoading(true);
    try {
      const params = statusFilter ? { status: statusFilter } : {};
      const { data } = await documentApi.getAll(params);
      setDocuments(data || []);
      setPending({});
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
  }, [search]);

  const loadOfficers = async () => {
    try {
      const { data } = await authApi.users();
      setOfficers((data || []).filter((u) => u.role === USER_ROLES.OFFICER));
    } catch {
      /* empty */
    }
  };

  const setPendingField = (id, field, value) => {
    setPending((p) => ({ ...p, [id]: { ...p[id], [field]: value } }));
    setSaved((s) => {
      const n = { ...s };
      delete n[id];
      return n;
    });
  };

  const handleSave = async (d) => {
    const changes = pending[d.id];
    if (!changes) return;

    setSaving((s) => ({ ...s, [d.id]: true }));

    try {
      if (changes.status && changes.status !== d.status) {
        await documentApi.updateStatus(d.id, { status: changes.status });
      }
      if (changes.officerId) {
        await documentApi.assignOfficer(d.id, changes.officerId);
      }

      setSaved((s) => ({ ...s, [d.id]: true }));
      setPending((p) => {
        const n = { ...p };
        delete n[d.id];
        return n;
      });

      await load();
      onRefreshSummary?.();
    } catch {
      /* empty */
    } finally {
      setSaving((s) => ({ ...s, [d.id]: false }));
    }
  };

  const handleChange = (id) => {
    setSaved((s) => {
      const n = { ...s };
      delete n[id];
      return n;
    });
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = (d.documentType || '')
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .toLowerCase();
    const citizenId = (d.citizenUserId || '').toLowerCase();

    return !q || typeName.includes(q) || citizenId.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  const columns = [
    {
      header: 'Type',
      key: 'documentType',
      className: 'col-type',
      render: (d) => d.documentType.replace(/([A-Z])/g, ' $1').trim(),
    },
    {
      header: 'Citizen ID',
      key: 'citizenUserId',
      className: 'col-id id-cell',
      render: (d) => <span title={d.citizenUserId}>{d.citizenUserId || '—'}</span>,
    },
    {
      header: 'Submitted',
      key: 'createdAt',
      className: 'col-date',
      render: (d) => formatDate(d.createdAt),
    },
    {
      header: 'Status',
      key: 'status',
      className: 'col-status',
      render: (d) => {
        if (saved[d.id]) {
          return <AppBadge type={getStatusType(d.status)}>{d.status}</AppBadge>;
        }
        return (
          <AppSelect
            value={pending[d.id]?.status || d.status}
            onChange={(val) => setPendingField(d.id, 'status', val)}
            options={DOCUMENT_STATUSES}
          />
        );
      },
    },
    {
      header: 'Assign Officer',
      key: 'assignment',
      className: 'col-expand',
      render: (d) => {
        if (saved[d.id]) {
          const officer = officers.find((o) => o.id === d.processedByOfficerId);
          return <span className="officer-text">{officer?.fullName || 'Unassigned'}</span>;
        }
        return (
          <AppSelect
            value={pending[d.id]?.officerId || d.processedByOfficerId || ''}
            onChange={(val) => setPendingField(d.id, 'officerId', val)}
            placeholder="Assign Officer"
            options={officers.map((o) => ({ label: o.fullName, value: o.id }))}
          />
        );
      },
    },
    {
      header: 'Action',
      key: 'save_action',
      className: 'col-action',
      render: (d) => {
        const hasPending = !!pending[d.id];
        const isSaved = !!saved[d.id];
        const isSaving = !!saving[d.id];

        if (isSaved) {
          return (
            <button className="btn btn-sm btn-outline" onClick={() => handleChange(d.id)}>
              Edit Again
            </button>
          );
        }

        return hasPending ? (
          <button
            className="btn btn-sm btn-success"
            onClick={() => handleSave(d)}
            disabled={isSaving}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        ) : (
          '—'
        );
      },
    },
  ];

  return (
    <div className="card">
      <DataTable
        columns={columns}
        data={paged}
        loading={loading}
        emptyMessage={search ? 'No matching documents.' : 'No documents found.'}
        onRowClick={(d) => setSelectedDocument(d)}
      />

      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
            Previous
          </button>
          <span>
            Page {safePage} of {totalPages}
          </span>
          <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>
            Next
          </button>
        </div>
      )}

      <Modal
        isOpen={!!selectedDocument}
        onClose={() => setSelectedDocument(null)}
        title="Document Request Details"
        className="details-modal"
        maxWidth="900px"
        footer={
          <button className="btn btn-outline" onClick={() => setSelectedDocument(null)}>
            Close
          </button>
        }
      >
        {selectedDocument && (
          <div className="modern-user-details">
            <DetailsSection
              title="Document Information"
              icon={
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                </svg>
              }
            />
            <div className="info-display-grid">
              <InfoItem
                label="Document Type"
                value={selectedDocument.documentType.replace(/([A-Z])/g, ' $1').trim()}
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                    <polyline points="14 2 14 8 20 8" />
                  </svg>
                }
              />
              <InfoItem
                label="Current Status"
                value={
                  <AppBadge type={getStatusType(selectedDocument.status)}>
                    {selectedDocument.status}
                  </AppBadge>
                }
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                  </svg>
                }
              />
              <InfoItem
                label="Citizen ID"
                value={selectedDocument.citizenUserId}
                span={2}
                isId
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                }
              />
              <InfoItem
                label="Reference Number"
                value={selectedDocument.referenceNumber}
                isId
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                }
              />
            </div>

            <DetailsSection
              title="Administrative & Timeline"
              icon={
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                  <line x1="16" y1="2" x2="16" y2="6" />
                  <line x1="8" y1="2" x2="8" y2="6" />
                  <line x1="3" y1="10" x2="21" y2="10" />
                </svg>
              }
            />
            <div className="info-display-grid">
              <InfoItem
                label="Submitted On"
                value={formatDate(selectedDocument.createdAt)}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                }
              />
              <InfoItem
                label="Completed On"
                value={formatDate(selectedDocument.completedAt, 'Pending')}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <polyline points="22 11.08 11.11 22 2 12.91 5.33 9.58 11.11 15.33 18.67 7.75 22 11.08" />
                  </svg>
                }
              />
              <InfoItem
                label="Expires On"
                value={formatDate(selectedDocument.expiresAt, 'No expiry')}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                    <line x1="12" y1="9" x2="12" y2="13" />
                    <line x1="12" y1="17" x2="12.01" y2="17" />
                  </svg>
                }
              />

              <InfoItem
                label="Processing Officer"
                value={
                  <>
                    {officers.find((o) => o.id === selectedDocument.processedByOfficerId)
                      ?.fullName || 'Unassigned'}
                    {selectedDocument.processedByOfficerId && (
                      <span className="id-cell-inline" style={{ marginLeft: '0.5rem' }}>
                        {selectedDocument.processedByOfficerId}
                      </span>
                    )}
                  </>
                }
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                    <circle cx="8.5" cy="7" r="4" />
                    <polyline points="17 11 19 13 23 9" />
                  </svg>
                }
              />

              <InfoItem
                label="Rejection Reason"
                value={selectedDocument.rejectionReason}
                span={3}
                color={selectedDocument.rejectionReason ? '#dc2626' : undefined}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                  </svg>
                }
              />
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

function UsersTab({ onRefreshSummary, search, user }) {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState(null);
  const [profile, setProfile] = useState(null);
  const [profileLoading, setProfileLoading] = useState(false);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const { data } = await authApi.users();
      setUsers(data || []);
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (selectedUser) {
      fetchProfile(selectedUser.id);
    } else {
      setProfile(null);
    }
  }, [selectedUser]);

  const fetchProfile = async (userId) => {
    setProfileLoading(true);
    setProfile(null);
    try {
      const { data } = await citizenApi.getByUserId(userId);
      setProfile(data);
    } catch {
      setProfile(null);
    } finally {
      setProfileLoading(false);
    }
  };

  useEffect(() => {
    setPage(1);
  }, [search]);

  const changeRole = async (userId, newRole) => {
    try {
      await authApi.updateRole(userId, newRole);
      await loadUsers();
      onRefreshSummary?.();
    } catch {
      /* empty */
    }
  };

  const filtered = users.filter((u) => {
    const q = search.toLowerCase();
    const fullName = (u.fullName || '').toLowerCase();
    const email = (u.email || '').toLowerCase();
    const role = (u.role || '').toLowerCase();

    return !q || fullName.includes(q) || email.includes(q) || role.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  const columns = [
    { header: 'Name', key: 'fullName', className: 'col-expand', render: (u) => u.fullName || '—' },
    { header: 'Email', key: 'email', className: 'col-email', render: (u) => u.email || '—' },
    {
      header: 'Role',
      key: 'role',
      className: 'col-role',
      render: (u) => {
        let type = 'neutral';
        if (u.role === USER_ROLES.ADMIN) type = 'primary';
        if (u.role === USER_ROLES.OFFICER) type = 'info';
        if (u.role === USER_ROLES.CITIZEN) type = 'success';

        return <AppBadge type={type}>{u.role || 'Unknown'}</AppBadge>;
      },
    },
    {
      header: 'Change Role',
      key: 'changeRole',
      className: 'col-status',
      render: (u) => (
        <RoleSelector
          value={u.role}
          onChange={(newRole) => changeRole(u.id, newRole)}
          disabled={u.id === user?.id || u.email === 'admin@government.gov'}
        />
      ),
    },
  ];

  return (
    <div className="card">
      <DataTable
        columns={columns}
        data={paged}
        loading={loading}
        emptyMessage={search ? 'No matching users.' : 'No users found.'}
        onRowClick={(u) => setSelectedUser(u)}
      />

      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>
            Previous
          </button>
          <span>
            Page {safePage} of {totalPages}
          </span>
          <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>
            Next
          </button>
        </div>
      )}

      <Modal
        isOpen={!!selectedUser}
        onClose={() => setSelectedUser(null)}
        title="User Account Details"
        className="details-modal"
        maxWidth="900px"
        footer={
          <button className="btn btn-outline" onClick={() => setSelectedUser(null)}>
            Close
          </button>
        }
      >
        {selectedUser && (
          <div className="modern-user-details">
            <DetailsSection
              title="Account Information"
              icon={
                <svg
                  width="16"
                  height="16"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2.5"
                >
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              }
            />
            <div className="info-display-grid">
              <InfoItem
                label="Full Name"
                value={selectedUser.fullName}
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                }
              />
              <InfoItem
                label="System Role"
                value={
                  <AppBadge
                    type={
                      selectedUser.role === USER_ROLES.ADMIN
                        ? 'primary'
                        : selectedUser.role === USER_ROLES.OFFICER
                          ? 'info'
                          : 'success'
                    }
                  >
                    {selectedUser.role}
                  </AppBadge>
                }
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                  </svg>
                }
              />
              <InfoItem
                label="Email Address"
                value={selectedUser.email}
                span={2}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                    <polyline points="22,6 12,13 2,6" />
                  </svg>
                }
              />
              <InfoItem
                label="Account Status"
                value="Active"
                color="#16a34a"
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                    <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                  </svg>
                }
              />
              <InfoItem
                label="Unique User ID"
                value={selectedUser.id}
                span={2}
                isId
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                    <circle cx="8.5" cy="7" r="4" />
                    <line x1="20" y1="8" x2="20" y2="14" />
                    <line x1="17" y1="11" x2="23" y2="11" />
                  </svg>
                }
              />
              <InfoItem
                label="Member Since"
                value={formatDate(selectedUser.createdAt)}
                icon={
                  <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.5"
                  >
                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                    <line x1="16" y1="2" x2="16" y2="6" />
                    <line x1="8" y1="2" x2="8" y2="6" />
                    <line x1="3" y1="10" x2="21" y2="10" />
                  </svg>
                }
              />
            </div>

            {(selectedUser.role === USER_ROLES.CITIZEN || profile) && (
              <>
                <DetailsSection
                  title="Citizen Profile Details"
                  icon={
                    <svg
                      width="16"
                      height="16"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2.5"
                    >
                      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                      <circle cx="9" cy="7" r="4" />
                      <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                    </svg>
                  }
                />
                {profileLoading ? (
                  <div className="info-display-grid">
                    {[...Array(5)].map((_, i) => (
                      <div key={i} className={`info-item ${i === 4 ? 'span-2' : ''}`}>
                        <div className="skeleton skeleton-label"></div>
                        <div className="skeleton skeleton-value"></div>
                      </div>
                    ))}
                  </div>
                ) : profile ? (
                  <div className="info-display-grid">
                    <InfoItem
                      label="National ID"
                      value={profile.nationalId}
                      icon={
                        <svg
                          width="14"
                          height="14"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2.5"
                        >
                          <rect x="3" y="4" width="18" height="16" rx="2" />
                          <line x1="7" y1="8" x2="17" y2="8" />
                          <line x1="7" y1="12" x2="17" y2="12" />
                          <line x1="7" y1="16" x2="12" y2="16" />
                        </svg>
                      }
                    />
                    <InfoItem
                      label="Phone Number"
                      value={profile.phoneNumber}
                      icon={
                        <svg
                          width="14"
                          height="14"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2.5"
                        >
                          <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z" />
                        </svg>
                      }
                    />
                    <InfoItem
                      label="Gender"
                      value={profile.gender}
                      icon={
                        <svg
                          width="14"
                          height="14"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2.5"
                        >
                          <circle cx="12" cy="12" r="10" />
                          <line x1="12" y1="8" x2="12" y2="12" />
                          <line x1="12" y1="16" x2="12.01" y2="16" />
                        </svg>
                      }
                    />
                    <InfoItem
                      label="Date of Birth"
                      value={profile.dateOfBirth}
                      icon={
                        <svg
                          width="14"
                          height="14"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2.5"
                        >
                          <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
                        </svg>
                      }
                    />
                    <InfoItem
                      label="City & Address"
                      value={`${profile.city}${profile.address ? `, ${profile.address}` : ''}`}
                      span={2}
                      icon={
                        <svg
                          width="14"
                          height="14"
                          viewBox="0 0 24 24"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2.5"
                        >
                          <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z" />
                          <circle cx="12" cy="10" r="3" />
                        </svg>
                      }
                    />
                  </div>
                ) : (
                  <div
                    className="empty-state-card"
                    style={{ padding: '1.5rem', borderStyle: 'dashed', background: '#f8fafc' }}
                  >
                    <p className="empty" style={{ fontSize: '0.85rem', color: '#64748b' }}>
                      No citizen profile has been linked to this account yet.
                    </p>
                  </div>
                )}
              </>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}
