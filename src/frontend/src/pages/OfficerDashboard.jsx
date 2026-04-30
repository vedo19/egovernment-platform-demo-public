import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { serviceRequestApi, documentApi } from '../api/services';

const STATUS_COLORS = {
  Pending: '#f59e0b',
  InProgress: '#3b82f6',
  Processing: '#3b82f6',
  Resolved: '#10b981',
  Ready: '#10b981',
  Collected: '#6b7280',
  Rejected: '#ef4444',
};

const PAGE_SIZE = 8;

export default function OfficerDashboard() {
  const { user } = useAuth();
  const [tab, setTab] = useState('requests');
  const [requestCount, setRequestCount] = useState(0);
  const [documentCount, setDocumentCount] = useState(0);
  const [activeCount, setActiveCount] = useState(0);

  const loadSummary = async () => {
    try {
      const [requestsRes, documentsRes] = await Promise.all([
        serviceRequestApi.getMyAssignments(),
        documentApi.getMyAssignments(),
      ]);

      const requests = (requestsRes.data || []).filter(
        (r) => r.status !== 'Resolved' && r.status !== 'Rejected'
      );
      const documents = (documentsRes.data || []).filter(
        (d) => d.status !== 'Ready' && d.status !== 'Rejected' && d.status !== 'Collected'
      );

      setRequestCount(requests.length);
      setDocumentCount(documents.length);
      setActiveCount(requests.length + documents.length);
    } catch {
      setRequestCount(0);
      setDocumentCount(0);
      setActiveCount(0);
    }
  };

  useEffect(() => {
    loadSummary();
  }, []);

  return (
    <div className="dashboard-page officer-dashboard">
      <div className="page-hero">
        <div>
          <h1>Officer Dashboard</h1>
          <p className="subtitle">
            Welcome, {user?.fullName}. Review assigned requests and process document tasks.
          </p>
        </div>
      </div>

      <div className="stats-grid">
        <div className="card stat-card">
          <span className="stat-label">Assigned Requests </span>
          <strong className="stat-value">{requestCount}</strong>
        </div>
        <div className="card stat-card">
          <span className="stat-label">Assigned Documents </span>
          <strong className="stat-value">{documentCount}</strong>
        </div>
        <div className="card stat-card">
          <span className="stat-label">Active Tasks </span>
          <strong className="stat-value">{activeCount}</strong>
        </div>
      </div>

      <div className="tabs">
        <button
          className={tab === 'requests' ? 'tab active' : 'tab'}
          onClick={() => setTab('requests')}
        >
          My Requests
        </button>
        <button
          className={tab === 'documents' ? 'tab active' : 'tab'}
          onClick={() => setTab('documents')}
        >
          My Documents
        </button>
      </div>

      <div className="dashboard-section">
        {tab === 'requests' && <OfficerRequestsTab onRefreshSummary={loadSummary} />}
        {tab === 'documents' && <OfficerDocumentsTab onRefreshSummary={loadSummary} />}
      </div>
    </div>
  );
}

function OfficerRequestsTab({ onRefreshSummary }) {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await serviceRequestApi.getMyAssignments();
      setRequests((data || []).filter((r) => r.status !== 'Resolved' && r.status !== 'Rejected'));
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (id, status) => {
    if (status === 'Rejected' && !notes.trim()) {
      setError('You must provide a reason when rejecting a request.');
      return;
    }

    setError('');

    try {
      await serviceRequestApi.updateStatus(id, {
        status,
        adminNotes: notes || undefined,
      });
      setSelected(null);
      setNotes('');
      await load();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
    }
  };

  const filtered = requests.filter((r) => {
    const q = search.toLowerCase();
    const title = (r.title || '').toLowerCase();
    const type = (r.type || '').toLowerCase();
    return !q || title.includes(q) || type.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  if (selected) {
    return (
      <div className="card">
        <div className="section-header">
          <h2>Request Details</h2>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <div className="profile-info-grid">
          <div className="info-item">
            <span className="info-label">Type</span>
            <span className="info-value">{selected.type || '—'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Title</span>
            <span className="info-value">{selected.title || '—'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Status</span>
            <span className="info-value">
              <span
                className="badge"
                style={{
                  backgroundColor: STATUS_COLORS[selected.status] || '#6b7280',
                }}
              >
                {selected.status || 'Unknown'}
              </span>
            </span>
          </div>
          <div className="info-item">
            <span className="info-label">Created</span>
            <span className="info-value">
              {selected.createdAt ? new Date(selected.createdAt).toLocaleDateString() : '—'}
            </span>
          </div>
          <div className="info-item">
            <span className="info-label">Citizen ID</span>
            <span className="info-value id-cell-inline">{selected.citizenUserId || '—'}</span>
          </div>
        </div>

        <div className="detail-description">
          <strong>Description</strong>
          <p>{selected.description || 'No description provided.'}</p>
        </div>

        {selected.adminNotes && (
          <div className="detail-description">
            <strong>Previous Notes</strong>
            <p>{selected.adminNotes}</p>
          </div>
        )}

        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label>Notes / Rejection Reason</label>
          <textarea
            rows={3}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Required when rejecting..."
          />
        </div>

        <div className="btn-group">
          <button className="btn btn-success" onClick={() => handleAction(selected.id, 'Resolved')}>
            Approve
          </button>
          <button className="btn btn-danger" onClick={() => handleAction(selected.id, 'Rejected')}>
            Reject
          </button>
          <button
            className="btn btn-outline"
            onClick={() => {
              setSelected(null);
              setNotes('');
              setError('');
            }}
          >
            Back
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="section-header">
        <div>
          <h2>Assigned Service Requests</h2>
          <p className="subtitle">
            Review active requests assigned to you and take action when needed.
          </p>
        </div>
        <input
          className="search-input"
          placeholder="Search requests..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      {loading ? (
        <p className="loading-text">Loading requests...</p>
      ) : filtered.length === 0 ? (
        <div className="empty-state-card">
          <p className="empty">
            {search ? 'No matching requests.' : 'No pending requests assigned to you.'}
          </p>
        </div>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Title</th>
                <th>Status</th>
                <th>Created</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((r) => (
                <tr key={r.id}>
                  <td>{r.type || '—'}</td>
                  <td className="desc-cell">{r.title || '—'}</td>
                  <td>
                    <span
                      className="badge"
                      style={{
                        backgroundColor: STATUS_COLORS[r.status] || '#6b7280',
                      }}
                    >
                      {r.status || 'Unknown'}
                    </span>
                  </td>
                  <td>{r.createdAt ? new Date(r.createdAt).toLocaleDateString() : '—'}</td>
                  <td>
                    <button className="btn btn-sm btn-primary" onClick={() => setSelected(r)}>
                      Review
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

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
        </>
      )}
    </div>
  );
}

function OfficerDocumentsTab({ onRefreshSummary }) {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await documentApi.getMyAssignments();
      setDocuments(
        (data || []).filter(
          (d) => d.status !== 'Ready' && d.status !== 'Rejected' && d.status !== 'Collected'
        )
      );
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (id, status) => {
    if (status === 'Rejected' && !reason.trim()) {
      setError('You must provide a reason when rejecting a document.');
      return;
    }

    setError('');

    try {
      await documentApi.updateStatus(id, {
        status,
        rejectionReason: reason || undefined,
      });
      setSelected(null);
      setReason('');
      setPreviewUrl(null);
      await load();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
    }
  };

  const handlePreview = async (id) => {
    setPreviewLoading(true);
    setError('');
    try {
      const { data } = await documentApi.preview(id);
      const url = window.URL.createObjectURL(data);
      setPreviewUrl(url);
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Failed to load preview');
    } finally {
      setPreviewLoading(false);
    }
  };

  const closePreview = () => {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = (d.documentType || '')
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .toLowerCase();
    const title = (d.title || '').toLowerCase();
    return !q || typeName.includes(q) || title.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  if (selected) {
    return (
      <div className="card">
        <div className="section-header">
          <h2>Document Details</h2>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <div className="profile-info-grid">
          <div className="info-item">
            <span className="info-label">Type</span>
            <span className="info-value">
              {(selected.documentType || '').replace(/([A-Z])/g, ' $1').trim() || '—'}
            </span>
          </div>
          <div className="info-item">
            <span className="info-label">Title</span>
            <span className="info-value">{selected.title || '—'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Status</span>
            <span className="info-value">
              <span
                className="badge"
                style={{
                  backgroundColor: STATUS_COLORS[selected.status] || '#6b7280',
                }}
              >
                {selected.status || 'Unknown'}
              </span>
            </span>
          </div>
          <div className="info-item">
            <span className="info-label">Created</span>
            <span className="info-value">
              {selected.createdAt ? new Date(selected.createdAt).toLocaleDateString() : '—'}
            </span>
          </div>
          <div className="info-item">
            <span className="info-label">Citizen ID</span>
            <span className="info-value id-cell-inline">{selected.citizenUserId || '—'}</span>
          </div>
          <div className="info-item">
            <span className="info-label">Reference #</span>
            <span className="info-value">{selected.referenceNumber || '—'}</span>
          </div>
        </div>

        {selected.description && (
          <div className="detail-description">
            <strong>Description</strong>
            <p>{selected.description}</p>
          </div>
        )}

        {selected.rejectionReason && (
          <div className="detail-description">
            <strong>Previous Rejection</strong>
            <p>{selected.rejectionReason}</p>
          </div>
        )}

        {previewUrl && (
          <div style={{ margin: '1rem 0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
              <strong>Document Preview (Draft)</strong>
              <button className="btn btn-sm btn-outline" onClick={closePreview}>Close Preview</button>
            </div>
            <iframe
              src={previewUrl}
              title="Document Preview"
              style={{ width: '100%', height: '500px', border: '1px solid #d1d5db', borderRadius: '8px' }}
            />
          </div>
        )}

        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label>Rejection Reason</label>
          <textarea
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Required when rejecting..."
          />
        </div>

        <div className="btn-group">
          <button
            className="btn btn-outline"
            onClick={() => handlePreview(selected.id)}
            disabled={previewLoading}
          >
            {previewLoading ? 'Loading...' : 'Preview Document'}
          </button>
          <button className="btn btn-success" onClick={() => handleAction(selected.id, 'Ready')}>Approve</button>
          <button className="btn btn-danger" onClick={() => handleAction(selected.id, 'Rejected')}>Reject</button>
          <button className="btn btn-outline" onClick={() => { setSelected(null); setReason(''); setError(''); closePreview(); }}>Back</button>
        </div>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="section-header">
        <div>
          <h2>Assigned Documents</h2>
          <p className="subtitle">
            Process active document requests and update their final outcome.
          </p>
        </div>
        <input
          className="search-input"
          placeholder="Search documents..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
      </div>

      {loading ? (
        <p className="loading-text">Loading documents...</p>
      ) : filtered.length === 0 ? (
        <div className="empty-state-card">
          <p className="empty">
            {search ? 'No matching documents.' : 'No pending documents assigned to you.'}
          </p>
        </div>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Title</th>
                <th>Status</th>
                <th>Created</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((d) => (
                <tr key={d.id}>
                  <td>{(d.documentType || '').replace(/([A-Z])/g, ' $1').trim() || '—'}</td>
                  <td className="desc-cell">{d.title || '—'}</td>
                  <td>
                    <span
                      className="badge"
                      style={{
                        backgroundColor: STATUS_COLORS[d.status] || '#6b7280',
                      }}
                    >
                      {d.status || 'Unknown'}
                    </span>
                  </td>
                  <td>{d.createdAt ? new Date(d.createdAt).toLocaleDateString() : '—'}</td>
                  <td>
                    <button className="btn btn-sm btn-primary" onClick={() => setSelected(d)}>
                      Review
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

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
        </>
      )}
    </div>
  );
}
