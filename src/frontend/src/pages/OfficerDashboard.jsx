import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { serviceRequestApi, documentApi } from '../api/services';
import ProgressBar from '../components/ProgressBar';
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
        (r) => r.status !== 'Approved' && r.status !== 'Rejected'
      );
      const documents = (documentsRes.data || []).filter(
        (d) => d.status !== 'Approved' && d.status !== 'Rejected'
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
  const [fileBusy, setFileBusy] = useState(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await serviceRequestApi.getAssignedToMe();
      // Only show actionable items — hide already resolved/rejected
      setRequests(data.filter((r) => r.status !== 'Approved' && r.status !== 'Rejected'));
    } catch { /* empty */ } finally {
      setLoading(false);
    }
  };

  const handleAction = async (request, action) => {
    if ((action === 'reject' || action === 'reject-documents' || action === 'request-documents') && !notes.trim()) {
      setError('You must provide a note for this action.');
      return;
    }

    setError('');

    try {
      if (action === 'approve') {
        await serviceRequestApi.approve(request.id);
      } else if (action === 'reject-documents') {
        await serviceRequestApi.rejectDocuments(request.id, notes);
      } else if (action === 'reject') {
        await serviceRequestApi.reject(request.id, notes);
      } else if (action === 'request-documents') {
        await serviceRequestApi.requestDocuments(request.id, notes);
      } else if (action === 'start-review') {
        await serviceRequestApi.updateStatus(request.id, { status: 'UnderReview' });
      }
      setSelected(null);
      setNotes('');
      await load();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
    }
  };

  const handleOpenDocument = async (documentId) => {
    setError('');
    setFileBusy(true);
    try {
      await documentApi.openSupportingFile(documentId);
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.error || d?.message || 'Failed to open document');
    } finally {
      setFileBusy(false);
    }
  };

  const handleDownloadDocument = async (documentId, request) => {
    setError('');
    setFileBusy(true);
    try {
      const suggestedName = `${request.type}-${request.id}.pdf`;
      await documentApi.downloadSupportingFile(documentId, suggestedName);
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.error || d?.message || 'Failed to download document');
    } finally {
      setFileBusy(false);
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
        <div className="detail-grid">
          <div><strong>Type:</strong> {selected.type}</div>
          <div><strong>Title:</strong> {selected.title}</div>
          <div><strong>Status:</strong> {selected.status}</div>
          <div style={{ gridColumn: '1 / span 2' }}><strong>Progress:</strong> <ProgressBar percentage={selected.progressPercentage} color={selected.progressColor} /></div>
          <div><strong>Created:</strong> {new Date(selected.createdAt).toLocaleDateString()}</div>
          <div><strong>Citizen ID:</strong> <span className="id-cell-inline">{selected.citizenUserId}</span></div>
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
        {selected.linkedDocumentId && (
          <div className="btn-group">
            <button className="btn btn-primary" disabled={fileBusy} onClick={() => handleOpenDocument(selected.linkedDocumentId)}>
              {fileBusy ? 'Opening...' : 'View Submitted PDF'}
            </button>
            <button className="btn btn-outline" disabled={fileBusy} onClick={() => handleDownloadDocument(selected.linkedDocumentId, selected)}>
              Download PDF
            </button>
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
          {(selected.status === 'UnderReview') && (
            <button className="btn btn-success" onClick={() => handleAction(selected, 'approve')}>Approve</button>
          )}
          {(selected.type === 'Permit' && selected.status === 'UnderReview' && selected.linkedDocumentId) && (
            <button className="btn btn-primary" onClick={() => handleAction(selected, 'reject-documents')}>Reject Documents</button>
          )}
          {(selected.status === 'UnderReview') && (
            <button className="btn btn-danger" onClick={() => handleAction(selected, 'reject')}>Reject</button>
          )}
          {(selected.type === 'Permit' && selected.status === 'OfficerAssigned') && (
            <button className="btn btn-primary" onClick={() => handleAction(selected, 'request-documents')}>Request Documents</button>
          )}
          {(selected.status === 'OfficerAssigned') && (
            <button className="btn btn-outline" onClick={() => handleAction(selected, 'start-review')}>Start Review</button>
          )}
          <button className="btn btn-outline" onClick={() => { setSelected(null); setNotes(''); setError(''); }}>Back</button>
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
                <th>Progress</th>
                <th>Created</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((r) => (
                <tr key={r.id}>
                  <td>{r.type}</td>
                  <td className="desc-cell">{r.title}</td>
                  <td>{r.status}</td>
                  <td style={{ minWidth: '180px' }}>
                    <ProgressBar percentage={r.progressPercentage} color={r.progressColor} />
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

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await documentApi.getAssignedToMe();
      // Only show actionable items — hide already completed/rejected
      setDocuments(data.filter((d) => d.status !== 'Approved' && d.status !== 'Rejected'));
    } catch { /* empty */ } finally {
      setLoading(false);
    }
  };

  const handleAction = async (document, action) => {
    if (action === 'reject' && !reason.trim()) {
      setError('You must provide a reason when rejecting a document.');
      return;
    }

    setError('');

    try {
      if (action === 'start-review') {
        await documentApi.startReview(document.id);
      } else if (action === 'approve') {
        await documentApi.approve(document.id);
      } else if (action === 'reject') {
        await documentApi.reject(document.id, reason);
      }
      setSelected(null);
      setReason('');
      await load();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
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
        <h2>Document Details</h2>
        {error && <div className="alert alert-error">{error}</div>}
        <div className="detail-grid">
          <div><strong>Type:</strong> {selected.documentType.replace(/([A-Z])/g, ' $1').trim()}</div>
          <div><strong>Title:</strong> {selected.title}</div>
          <div><strong>Status:</strong> {selected.status}</div>
          <div style={{ gridColumn: '1 / span 2' }}><strong>Progress:</strong> <ProgressBar percentage={selected.progressPercentage} color={selected.progressColor} /></div>
          <div><strong>Created:</strong> {new Date(selected.createdAt).toLocaleDateString()}</div>
          <div><strong>Citizen ID:</strong> <span className="id-cell-inline">{selected.citizenUserId}</span></div>
          <div><strong>Reference #:</strong> {selected.referenceNumber || '—'}</div>
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
          {(selected.status === 'Submitted') && (
            <button className="btn btn-primary" onClick={() => handleAction(selected, 'start-review')}>Start Review</button>
          )}
          {(selected.status === 'UnderReview') && (
            <button className="btn btn-success" onClick={() => handleAction(selected, 'approve')}>Approve</button>
          )}
          {(selected.status === 'UnderReview') && (
            <button className="btn btn-danger" onClick={() => handleAction(selected, 'reject')}>Reject</button>
          )}
          <button className="btn btn-outline" onClick={() => { setSelected(null); setReason(''); setError(''); }}>Back</button>
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
                <th>Progress</th>
                <th>Created</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((d) => (
                <tr key={d.id}>
                  <td>{d.documentType.replace(/([A-Z])/g, ' $1').trim()}</td>
                  <td className="desc-cell">{d.title}</td>
                  <td>{d.status}</td>
                  <td style={{ minWidth: '180px' }}>
                    <ProgressBar percentage={d.progressPercentage} color={d.progressColor} />
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
