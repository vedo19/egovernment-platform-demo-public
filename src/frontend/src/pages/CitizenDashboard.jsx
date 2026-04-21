import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { citizenApi, serviceRequestApi, documentApi } from '../api/services';
import ProgressBar from '../components/ProgressBar';

const REQUEST_TYPES = ['Permit', 'Complaint'];
const DOC_TYPES = [
  'BirthCertificate',
  'NationalId',
  'MarriageCertificate',
  'DeathCertificate',
  'DrivingLicense',
];
const UPLOAD_ALLOWED_STATUSES = new Set(['AwaitingDocuments', 'DocumentsRejected']);
const PAGE_SIZE = 5;

export default function CitizenDashboard() {
  const { user } = useAuth();
  const [tab, setTab] = useState('profile');
  const [requestCount, setRequestCount] = useState(0);
  const [documentCount, setDocumentCount] = useState(0);
  const [pendingCount, setPendingCount] = useState(0);

  const loadSummary = async () => {
    try {
      const [requestsRes, documentsRes] = await Promise.all([
        serviceRequestApi.getMyRequests(),
        documentApi.getMyDocuments(),
      ]);

      const requests = requestsRes.data || [];
      const documents = documentsRes.data || [];

      setRequestCount(requests.length);
      setDocumentCount(documents.length);

      const pendingItems = [...requests, ...documents].filter(
        (item) =>
          item?.status === 'Pending' ||
          item?.status === 'InProgress' ||
          item?.status === 'Processing'
      );

      setPendingCount(pendingItems.length);
    } catch {
      setRequestCount(0);
      setDocumentCount(0);
      setPendingCount(0);
    }
  };

  useEffect(() => {
    loadSummary();
  }, []);

  return (
    <div className="dashboard-page citizen-dashboard">
      <div className="page-hero">
        <div>
          <h1>Welcome, {user?.fullName}</h1>
          <p className="subtitle">
            Manage your profile, submit service requests, and track official documents in one place.
          </p>
        </div>
      </div>

      <div className="stats-grid">
        <div className="card stat-card">
          <span className="stat-label">Service Requests </span>
          <strong className="stat-value">{requestCount}</strong>
        </div>
        <div className="card stat-card">
          <span className="stat-label">Documents </span>
          <strong className="stat-value">{documentCount}</strong>
        </div>
        <div className="card stat-card">
          <span className="stat-label">Pending Items </span>
          <strong className="stat-value">{pendingCount}</strong>
        </div>
      </div>

      <div className="tabs">
        <button
          className={tab === 'profile' ? 'tab active' : 'tab'}
          onClick={() => setTab('profile')}
        >
          Profile
        </button>
        <button
          className={tab === 'requests' ? 'tab active' : 'tab'}
          onClick={() => setTab('requests')}
        >
          Service Requests
        </button>
        <button
          className={tab === 'documents' ? 'tab active' : 'tab'}
          onClick={() => setTab('documents')}
        >
          Documents
        </button>
      </div>

      <div className="dashboard-section">
        {tab === 'profile' && <ProfileTab />}
        {tab === 'requests' && <RequestsTab onRefreshSummary={loadSummary} />}
        {tab === 'documents' && <DocumentsTab onRefreshSummary={loadSummary} />}
      </div>
    </div>
  );
}

function ProfileTab() {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    phoneNumber: '',
    address: '',
    dateOfBirth: '',
    nationalId: '',
    city: '',
    gender: '',
  });
  const [error, setError] = useState('');
  const [msg, setMsg] = useState('');

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    setLoading(true);
    try {
      const { data } = await citizenApi.getProfile();
      setProfile(data);
      setForm({
        phoneNumber: data.phoneNumber || '',
        address: data.address || '',
        dateOfBirth: data.dateOfBirth || '',
        nationalId: data.nationalId || '',
        city: data.city || '',
        gender: data.gender || '',
      });
    } catch (err) {
      if (err.response?.status === 404) setProfile(null);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setMsg('');

    try {
      if (profile) {
        await citizenApi.updateProfile(form);
        setMsg('Profile updated successfully');
      } else {
        await citizenApi.createProfile(form);
        setMsg('Profile created successfully');
      }

      setEditing(false);
      await loadProfile();
    } catch (err) {
      const d = err.response?.data;
      setError(
        typeof d === 'string'
          ? d
          : d?.message || d?.title || JSON.stringify(d?.errors || d) || 'Failed to save profile'
      );
    }
  };

  if (loading) {
    return (
      <div className="card">
        <p className="loading-text">Loading profile...</p>
      </div>
    );
  }

  if (!profile && !editing) {
    return (
      <div className="card empty-state-card">
        <h2>Profile Information</h2>
        <p className="subtitle">Complete your personal information to use services more easily.</p>
        <button className="btn btn-primary" onClick={() => setEditing(true)}>
          Create Profile
        </button>
      </div>
    );
  }

  if (editing) {
    return (
      <div className="card">
        <div className="section-header">
          <h2>{profile ? 'Edit Profile' : 'Create Profile'}</h2>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {msg && <div className="alert alert-success">{msg}</div>}

        <form onSubmit={handleSubmit}>
          <div className="detail-grid">
            <div className="form-group">
              <label>Phone Number</label>
              <input
                value={form.phoneNumber}
                onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label>City</label>
              <input
                value={form.city}
                onChange={(e) => setForm({ ...form, city: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label>Address</label>
              <input
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label>Date of Birth</label>
              <input
                type="date"
                value={form.dateOfBirth}
                onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label>National ID</label>
              <input
                value={form.nationalId}
                onChange={(e) => setForm({ ...form, nationalId: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label>Gender</label>
              <select
                value={form.gender}
                onChange={(e) => setForm({ ...form, gender: e.target.value })}
                required
              >
                <option value="">Select...</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
            </div>
          </div>

          <div className="btn-group">
            <button type="submit" className="btn btn-primary">
              Save Profile
            </button>
            <button type="button" className="btn btn-outline" onClick={() => setEditing(false)}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="section-header">
        <h2>Your Profile</h2>
        <button className="btn btn-primary" onClick={() => setEditing(true)}>
          Edit Profile
        </button>
      </div>

      {msg && <div className="alert alert-success">{msg}</div>}

      <div className="profile-info-grid">
        <div className="info-item">
          <span className="info-label">Phone Number</span>
          <span className="info-value">{profile.phoneNumber || '—'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">City</span>
          <span className="info-value">{profile.city || '—'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">Address</span>
          <span className="info-value">{profile.address || '—'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">Date of Birth</span>
          <span className="info-value">{profile.dateOfBirth || '—'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">National ID</span>
          <span className="info-value">{profile.nationalId || '—'}</span>
        </div>
        <div className="info-item">
          <span className="info-label">Gender</span>
          <span className="info-value">{profile.gender || '—'}</span>
        </div>
      </div>
    </div>
  );
}

function RequestsTab({ onRefreshSummary }) {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showing, setShowing] = useState('list');
  const [form, setForm] = useState({
    type: 'Permit',
    title: '',
    description: '',
  });
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [uploadingId, setUploadingId] = useState(null);
  const [uploadError, setUploadError] = useState('');
  const [documentBusyId, setDocumentBusyId] = useState(null);

  useEffect(() => {
    loadRequests();
  }, []);

  const loadRequests = async () => {
    setLoading(true);
    try {
      const { data } = await serviceRequestApi.getMyRequests();
      setRequests(data);
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      await serviceRequestApi.create(form);
      setForm({ type: 'Permit', title: '', description: '' });
      setShowing('list');
      await loadRequests();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(
        typeof d === 'string'
          ? d
          : d?.message || d?.title || JSON.stringify(d?.errors || d) || 'Failed to create request'
      );
    }
  };

  const handleDocumentUpload = async (requestId, file) => {
    if (!file) return;
    setUploadError('');
    setUploadingId(requestId);
    try {
      await serviceRequestApi.uploadDocument(requestId, file);
      await loadRequests();
    } catch (err) {
      const d = err.response?.data;
      setUploadError(typeof d === 'string' ? d : d?.error || d?.message || 'Failed to upload document');
    } finally {
      setUploadingId(null);
    }
  };

  const handleOpenDocument = async (request) => {
    if (!request.linkedDocumentId) return;
    setUploadError('');
    setDocumentBusyId(request.id);
    try {
      await documentApi.openSupportingFile(request.linkedDocumentId);
    } catch (err) {
      const d = err.response?.data;
      setUploadError(typeof d === 'string' ? d : d?.error || d?.message || 'Failed to open uploaded document');
    } finally {
      setDocumentBusyId(null);
    }
  };

  const filtered = requests.filter((r) => {
    const q = search.toLowerCase();
    const title = (r.title || '').toLowerCase();
    const type = (r.type || '').toLowerCase();
    const status = (r.status || '').toLowerCase();

    return !q || title.includes(q) || type.includes(q) || status.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  if (showing === 'new') {
    return (
      <div className="card">
        <div className="section-header">
          <h2>New Service Request</h2>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Type</label>
            <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
              {REQUEST_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Title</label>
            <input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
            />
          </div>

          <div className="form-group">
            <label>Description</label>
            <textarea
              rows={4}
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              required
            />
          </div>

          <div className="btn-group">
            <button type="submit" className="btn btn-primary">
              Submit Request
            </button>
            <button type="button" className="btn btn-outline" onClick={() => setShowing('list')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="section-header">
        <div>
          <h2>My Service Requests</h2>
          <p className="subtitle">Track request status and review any administrative notes.</p>
        </div>

        <div className="header-actions">
          <input
            className="search-input"
            placeholder="Search requests..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <button className="btn btn-primary" onClick={() => setShowing('new')}>
            New Request
          </button>
        </div>
      </div>

      {loading ? (
        <p className="loading-text">Loading requests...</p>
      ) : filtered.length === 0 ? (
        <div className="empty-state-card">
          <p className="empty">{search ? 'No matching requests.' : 'No service requests yet.'}</p>
        </div>
      ) : (
        <>
          {uploadError && <div className="alert alert-error">{uploadError}</div>}
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Title</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Officer Note</th>
                <th>Upload PDF</th>
                <th>Uploaded PDF</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((r) => (
                <tr key={r.id}>
                  <td>{r.type}</td>
                  <td>{r.title}</td>
                  <td>{r.status}</td>
                  <td style={{ minWidth: '180px' }}>
                    <ProgressBar percentage={r.progressPercentage} color={r.progressColor} />
                  </td>
                  <td className="desc-cell">
                    {UPLOAD_ALLOWED_STATUSES.has(r.status) ? (r.officerNote || 'No note provided') : '—'}
                  </td>
                  <td>
                    {UPLOAD_ALLOWED_STATUSES.has(r.status) ? (
                      <input
                        type="file"
                        accept="application/pdf,.pdf"
                        disabled={uploadingId === r.id}
                        onChange={(e) => handleDocumentUpload(r.id, e.target.files?.[0])}
                      />
                    ) : (
                      '—'
                    )}
                  </td>
                  <td>
                    {r.linkedDocumentId ? (
                      <button className="btn btn-sm btn-outline" disabled={documentBusyId === r.id} onClick={() => handleOpenDocument(r)}>
                        {documentBusyId === r.id ? 'Opening...' : 'View'}
                      </button>
                    ) : '—'}
                  </td>
                  <td>{new Date(r.createdAt).toLocaleDateString()}</td>
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

function DocumentsTab({ onRefreshSummary }) {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showing, setShowing] = useState('list');
  const [form, setForm] = useState({
    documentType: 'BirthCertificate',
    title: '',
    description: '',
  });
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    setLoading(true);
    try {
      const { data } = await documentApi.getMyDocuments();
      setDocuments(data);
    } catch {
      /* empty */
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      await documentApi.create(form);
      setForm({ documentType: 'BirthCertificate', title: '', description: '' });
      setShowing('list');
      await loadDocuments();
      onRefreshSummary?.();
    } catch (err) {
      const d = err.response?.data;
      setError(
        typeof d === 'string'
          ? d
          : d?.message ||
              d?.title ||
              JSON.stringify(d?.errors || d) ||
              'Failed to create document request'
      );
    }
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = (d.documentType || '')
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .toLowerCase();
    const status = (d.status || '').toLowerCase();
    const referenceNumber = (d.referenceNumber || '').toLowerCase();

    return !q || typeName.includes(q) || status.includes(q) || referenceNumber.includes(q);
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const paged = filtered.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  if (showing === 'new') {
    return (
      <div className="card">
        <div className="section-header">
          <h2>Request New Document</h2>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Document Type</label>
            <select
              value={form.documentType}
              onChange={(e) => setForm({ ...form, documentType: e.target.value })}
            >
              {DOC_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t.replace(/([A-Z])/g, ' $1').trim()}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Title</label>
            <input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
            />
          </div>

          <div className="form-group">
            <label>Description (optional)</label>
            <textarea
              rows={3}
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>

          <div className="btn-group">
            <button type="submit" className="btn btn-primary">
              Submit Request
            </button>
            <button type="button" className="btn btn-outline" onClick={() => setShowing('list')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
      <div className="section-header">
        <div>
          <h2>My Documents</h2>
          <p className="subtitle">Track requested documents and follow their processing status.</p>
        </div>

        <div className="header-actions">
          <input
            className="search-input"
            placeholder="Search documents..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <button className="btn btn-primary" onClick={() => setShowing('new')}>
            Request Document
          </button>
        </div>
      </div>

      {loading ? (
        <p className="loading-text">Loading documents...</p>
      ) : filtered.length === 0 ? (
        <div className="empty-state-card">
          <p className="empty">
            {search ? 'No matching documents.' : 'No documents requested yet.'}
          </p>
        </div>
      ) : (
        <>
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Status</th>
                <th>Progress</th>
                <th>Reason</th>
                <th>Reference #</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((d) => (
                <tr key={d.id}>
                  <td>{d.documentType.replace(/([A-Z])/g, ' $1').trim()}</td>
                  <td>{d.status}</td>
                  <td style={{ minWidth: '180px' }}>
                    <ProgressBar percentage={d.progressPercentage} color={d.progressColor} />
                  </td>
                  <td className="desc-cell">{d.rejectionReason || '—'}</td>
                  <td>{d.referenceNumber || '—'}</td>
                  <td>{d.createdAt ? new Date(d.createdAt).toLocaleDateString() : '—'}</td>
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
