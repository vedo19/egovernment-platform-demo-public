import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { serviceRequestApi, documentApi } from '../api/services';
import ProgressBar from '../components/ProgressBar';
const PAGE_SIZE = 8;

export default function OfficerDashboard() {
  const { user } = useAuth();
  const [tab, setTab] = useState('requests');

  return (
    <div>
      <h1>Officer Dashboard</h1>
      <p className="subtitle">Welcome, {user.fullName}. Below are your assigned tasks.</p>
      <div className="tabs">
        <button className={tab === 'requests' ? 'tab active' : 'tab'} onClick={() => setTab('requests')}>
          My Requests
        </button>
        <button className={tab === 'documents' ? 'tab active' : 'tab'} onClick={() => setTab('documents')}>
          My Documents
        </button>
      </div>
      {tab === 'requests' && <OfficerRequestsTab />}
      {tab === 'documents' && <OfficerDocumentsTab />}
    </div>
  );
}

function OfficerRequestsTab() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => { load(); }, []);

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
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
    }
  };

  const filtered = requests.filter((r) => {
    const q = search.toLowerCase();
    return !q || r.title.toLowerCase().includes(q) || r.type.toLowerCase().includes(q);
  });
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  if (selected) {
    return (
      <div className="card">
        <h2>Request Details</h2>
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
          <strong>Description:</strong>
          <p>{selected.description}</p>
        </div>
        {selected.adminNotes && (
          <div className="detail-description">
            <strong>Previous Notes:</strong>
            <p>{selected.adminNotes}</p>
          </div>
        )}
        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label>Notes / Rejection Reason</label>
          <textarea rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Required when rejecting..." />
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
    <div>
      <div className="section-header">
        <h2>Assigned Service Requests</h2>
        <input className="search-input" placeholder="Search..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>
      {loading ? (
        <p className="loading-text">Loading requests...</p>
      ) : filtered.length === 0 ? (
        <p className="empty">{search ? 'No matching requests.' : 'No pending requests assigned to you.'}</p>
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
                  <td>{new Date(r.createdAt).toLocaleDateString()}</td>
                  <td>
                    <button className="btn btn-sm btn-primary" onClick={() => setSelected(r)}>Review</button>
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

function OfficerDocumentsTab() {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState(null);
  const [reason, setReason] = useState('');
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => { load(); }, []);

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
    } catch (err) {
      const d = err.response?.data;
      setError(typeof d === 'string' ? d : d?.message || d?.title || 'Action failed');
    }
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = d.documentType.replace(/([A-Z])/g, ' $1').trim().toLowerCase();
    return !q || typeName.includes(q) || (d.title || '').toLowerCase().includes(q);
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
            <strong>Description:</strong>
            <p>{selected.description}</p>
          </div>
        )}
        {selected.rejectionReason && (
          <div className="detail-description">
            <strong>Previous Rejection:</strong>
            <p>{selected.rejectionReason}</p>
          </div>
        )}
        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label>Rejection Reason</label>
          <textarea rows={3} value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Required when rejecting..." />
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
    <div>
      <div className="section-header">
        <h2>Assigned Documents</h2>
        <input className="search-input" placeholder="Search..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>
      {loading ? (
        <p className="loading-text">Loading documents...</p>
      ) : filtered.length === 0 ? (
        <p className="empty">{search ? 'No matching documents.' : 'No pending documents assigned to you.'}</p>
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
                  <td>{new Date(d.createdAt).toLocaleDateString()}</td>
                  <td>
                    <button className="btn btn-sm btn-primary" onClick={() => setSelected(d)}>Review</button>
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
