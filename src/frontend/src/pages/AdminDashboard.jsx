import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { authApi, citizenApi, serviceRequestApi, documentApi } from '../api/services';
import ProgressBar from '../components/ProgressBar';

const REQUEST_STATUSES = ['Submitted', 'OfficerAssigned', 'AwaitingDocuments', 'UnderReview', 'DocumentsRejected', 'Approved', 'Rejected'];
const DOC_STATUSES = ['Submitted', 'UnderReview', 'Approved', 'Rejected'];
const PAGE_SIZE = 8;

export default function AdminDashboard() {
  const { user } = useAuth();
  const [tab, setTab] = useState('requests');

  return (
    <div>
      <h1>Admin Dashboard</h1>
      <div className="tabs">
        <button className={tab === 'requests' ? 'tab active' : 'tab'} onClick={() => setTab('requests')}>
          Service Requests
        </button>
        <button className={tab === 'documents' ? 'tab active' : 'tab'} onClick={() => setTab('documents')}>
          Documents
        </button>
        <button className={tab === 'users' ? 'tab active' : 'tab'} onClick={() => setTab('users')}>
          Users
        </button>
      </div>
      {tab === 'requests' && <RequestsTab />}
      {tab === 'documents' && <DocumentsTab />}
      {tab === 'users' && <UsersTab />}
    </div>
  );
}

function RequestsTab() {
  const [requests, setRequests] = useState([]);
  const [officers, setOfficers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  // Track pending (unsaved) changes per row: { [id]: { status?, officerId? } }
  const [pending, setPending] = useState({});
  const [saving, setSaving] = useState({});
  const [saved, setSaved] = useState({});

  useEffect(() => {
    load();
    loadOfficers();
  }, [statusFilter]);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await serviceRequestApi.getAllRequests(statusFilter || undefined);
      setRequests(data);
      setPending({});
    } catch { /* empty */ } finally {
      setLoading(false);
    }
  };

  const loadOfficers = async () => {
    try {
      const { data } = await authApi.users();
      setOfficers(data.filter((u) => u.role === 'Officer'));
    } catch { /* empty */ }
  };

  const setPendingField = (id, field, value) => {
    setPending((p) => ({ ...p, [id]: { ...p[id], [field]: value } }));
    setSaved((s) => { const n = { ...s }; delete n[id]; return n; });
  };

  const handleSave = async (r) => {
    const changes = pending[r.id];
    if (!changes) return;
    setSaving((s) => ({ ...s, [r.id]: true }));
    try {
      if (changes.status && changes.status !== r.status) {
        await serviceRequestApi.updateStatus(r.id, { status: changes.status });
      }
      if (changes.officerId && r.status === 'Submitted') {
        await serviceRequestApi.assignOfficerV2(r.id, changes.officerId);
      }
      setSaved((s) => ({ ...s, [r.id]: true }));
      setPending((p) => { const n = { ...p }; delete n[r.id]; return n; });
      await load();
    } catch { /* empty */ } finally {
      setSaving((s) => ({ ...s, [r.id]: false }));
    }
  };

  const handleChange = (id) => {
    setSaved((s) => { const n = { ...s }; delete n[id]; return n; });
  };

  const filtered = requests.filter((r) => {
    const q = search.toLowerCase();
    return !q || r.title.toLowerCase().includes(q) || r.type.toLowerCase().includes(q) || r.citizenUserId.toLowerCase().includes(q);
  });
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  return (
    <div>
      <div className="section-header">
        <h2>All Service Requests</h2>
        <div className="header-actions">
          <input className="search-input" placeholder="Search..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">All Statuses</option>
            {REQUEST_STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>
      {loading ? (
        <p className="loading-text">Loading requests...</p>
      ) : filtered.length === 0 ? (
        <p className="empty">{search ? 'No matching requests.' : 'No requests found.'}</p>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Title</th>
                <th>Citizen ID</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((r) => {
                const hasPending = !!pending[r.id];
                const isSaved = !!saved[r.id];
                const isSaving = !!saving[r.id];
                return (
                  <tr key={r.id}>
                    <td>{r.type}</td>
                    <td className="desc-cell">{r.title}</td>
                    <td className="id-cell" title={r.citizenUserId}>{r.citizenUserId}</td>
                    <td>{r.status}</td>
                    <td style={{ minWidth: '180px' }}>
                      <ProgressBar percentage={r.progressPercentage} color={r.progressColor} />
                    </td>
                    <td>{new Date(r.createdAt).toLocaleDateString()}</td>
                    <td className="actions-cell">
                      {isSaved ? (
                        <button className="btn btn-sm btn-outline" onClick={() => handleChange(r.id)}>Change</button>
                      ) : (
                        <>
                          <select
                            value={pending[r.id]?.status || r.status}
                            onChange={(e) => setPendingField(r.id, 'status', e.target.value)}
                          >
                            {REQUEST_STATUSES.map((s) => (
                              <option key={s} value={s}>{s}</option>
                            ))}
                          </select>
                          <select
                            value={pending[r.id]?.officerId || r.assignedOfficerId || ''}
                            onChange={(e) => setPendingField(r.id, 'officerId', e.target.value)}
                            disabled={r.status !== 'Submitted'}
                          >
                            <option value="" disabled>Assign Officer</option>
                            {officers.map((o) => (
                              <option key={o.id} value={o.id}>{o.fullName}</option>
                            ))}
                          </select>
                          {hasPending && (
                            <button className="btn btn-sm btn-success" onClick={() => handleSave(r)} disabled={isSaving}>
                              {isSaving ? 'Saving...' : 'Save'}
                            </button>
                          )}
                        </>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {totalPages > 1 && (
            <div className="pagination">
              <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
              <span>Page {safePage} of {totalPages}</span>
              <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function DocumentsTab() {
  const [documents, setDocuments] = useState([]);
  const [officers, setOfficers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pending, setPending] = useState({});
  const [saving, setSaving] = useState({});
  const [saved, setSaved] = useState({});

  useEffect(() => {
    load();
    loadOfficers();
  }, [statusFilter]);

  const load = async () => {
    setLoading(true);
    try {
      const params = statusFilter ? { status: statusFilter } : {};
      const { data } = await documentApi.getAll(params);
      setDocuments(data);
      setPending({});
    } catch { /* empty */ } finally {
      setLoading(false);
    }
  };

  const loadOfficers = async () => {
    try {
      const { data } = await authApi.users();
      setOfficers(data.filter((u) => u.role === 'Officer'));
    } catch { /* empty */ }
  };

  const setPendingField = (id, field, value) => {
    setPending((p) => ({ ...p, [id]: { ...p[id], [field]: value } }));
    setSaved((s) => { const n = { ...s }; delete n[id]; return n; });
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
      setPending((p) => { const n = { ...p }; delete n[d.id]; return n; });
      await load();
    } catch { /* empty */ } finally {
      setSaving((s) => ({ ...s, [d.id]: false }));
    }
  };

  const handleChange = (id) => {
    setSaved((s) => { const n = { ...s }; delete n[id]; return n; });
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = d.documentType.replace(/([A-Z])/g, ' $1').trim().toLowerCase();
    return !q || typeName.includes(q) || d.citizenUserId.toLowerCase().includes(q) || (d.referenceNumber || '').toLowerCase().includes(q);
  });
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  return (
    <div>
      <div className="section-header">
        <h2>All Documents</h2>
        <div className="header-actions">
          <input className="search-input" placeholder="Search..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">All Statuses</option>
            {DOC_STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>
      {loading ? (
        <p className="loading-text">Loading documents...</p>
      ) : filtered.length === 0 ? (
        <p className="empty">{search ? 'No matching documents.' : 'No documents found.'}</p>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Reference #</th>
                <th>Citizen ID</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((d) => {
                const hasPending = !!pending[d.id];
                const isSaved = !!saved[d.id];
                const isSaving = !!saving[d.id];
                return (
                  <tr key={d.id}>
                    <td>{d.documentType.replace(/([A-Z])/g, ' $1').trim()}</td>
                    <td>{d.status}</td>
                    <td style={{ minWidth: '180px' }}>
                      <ProgressBar percentage={d.progressPercentage} color={d.progressColor} />
                    </td>
                    <td>{d.referenceNumber || '—'}</td>
                    <td className="id-cell" title={d.citizenUserId}>{d.citizenUserId}</td>
                    <td>{new Date(d.createdAt).toLocaleDateString()}</td>
                    <td className="actions-cell">
                      {isSaved ? (
                        <button className="btn btn-sm btn-outline" onClick={() => handleChange(d.id)}>Change</button>
                      ) : (
                        <>
                          <select
                            value={pending[d.id]?.status || d.status}
                            onChange={(e) => setPendingField(d.id, 'status', e.target.value)}
                          >
                            {DOC_STATUSES.map((s) => (
                              <option key={s} value={s}>{s}</option>
                            ))}
                          </select>
                          <select
                            value={pending[d.id]?.officerId || d.processedByOfficerId || ''}
                            onChange={(e) => setPendingField(d.id, 'officerId', e.target.value)}
                          >
                            <option value="" disabled>Assign Officer</option>
                            {officers.map((o) => (
                              <option key={o.id} value={o.id}>{o.fullName}</option>
                            ))}
                          </select>
                          {hasPending && (
                            <button className="btn btn-sm btn-success" onClick={() => handleSave(d)} disabled={isSaving}>
                              {isSaving ? 'Saving...' : 'Save'}
                            </button>
                          )}
                        </>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {totalPages > 1 && (
            <div className="pagination">
              <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
              <span>Page {safePage} of {totalPages}</span>
              <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function UsersTab() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const { data } = await authApi.users();
      setUsers(data);
    } catch { /* empty */ } finally {
      setLoading(false);
    }
  };

  const changeRole = async (userId, newRole) => {
    try {
      await authApi.updateRole(userId, newRole);
      await loadUsers();
    } catch { /* empty */ }
  };

  const filtered = users.filter((u) => {
    const q = search.toLowerCase();
    return !q || u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q) || u.role.toLowerCase().includes(q);
  });
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  return (
    <div>
      <div className="section-header">
        <h2>All Users</h2>
        <input className="search-input" placeholder="Search users..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>
      {loading ? (
        <p className="loading-text">Loading users...</p>
      ) : filtered.length === 0 ? (
        <p className="empty">{search ? 'No matching users.' : 'No users found.'}</p>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Change Role</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((u) => (
                <tr key={u.id}>
                  <td>{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>
                    <span className="badge" style={{ backgroundColor: u.role === 'Admin' ? '#8b5cf6' : u.role === 'Officer' ? '#3b82f6' : '#10b981' }}>
                      {u.role}
                    </span>
                  </td>
                  <td>
                    <select
                      value={u.role}
                      onChange={(e) => changeRole(u.id, e.target.value)}
                    >
                      <option value="Citizen">Citizen</option>
                      <option value="Officer">Officer</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {totalPages > 1 && (
            <div className="pagination">
              <button disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
              <span>Page {safePage} of {totalPages}</span>
              <button disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
