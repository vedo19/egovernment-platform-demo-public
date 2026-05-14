import { useState, useEffect } from 'react';
import { citizenApi, serviceRequestApi, documentApi } from '../../api/services';
import ProgressBar from '../../components/ProgressBar';
import AppSelect from '../../components/AppSelect';
import AppBadge from '../../components/AppBadge';
import DataTable from '../../components/DataTable';
import {
  canDownloadDocument,
  canUploadRequestDocument,
  isPendingStatus,
} from '../../domain/statuses';
import { formatDate } from '../../utils/date';
import { getStatusType } from '../../utils/status';
import { useSearchParams } from 'react-router-dom';

const REQUEST_TYPES = ['Permit', 'Complaint'];
const DOC_TYPES = [
  'BirthCertificate',
  'NationalId',
  'MarriageCertificate',
  'DeathCertificate',
  'DrivingLicense',
];

const PAGE_SIZE = 5;

export default function CitizenDashboard() {
  const [searchParams] = useSearchParams();
  const tab = searchParams.get('tab') || 'overview';
  const [requestCount, setRequestCount] = useState(0);
  const [documentCount, setDocumentCount] = useState(0);
  const [pendingCount, setPendingCount] = useState(0);
  const [summaryLoading, setSummaryLoading] = useState(true);
  const [profile, setProfile] = useState(null);
  const [profileLoading, setProfileLoading] = useState(true);

  const [search, setSearch] = useState('');
  // We need to know if tabs are in 'new' mode to hide search/filters in hero
  const [tabMode, setTabMode] = useState('list');

  useEffect(() => {
    setSearch('');
    setTabMode('list');
  }, [tab]);

  const loadSummary = async () => {
    setSummaryLoading(true);
    try {
      const [requestsRes, documentsRes, profileRes] = await Promise.all([
        serviceRequestApi.getMyRequests(),
        documentApi.getMyDocuments(),
        citizenApi.getProfile().catch(() => ({ data: null })),
      ]);

      const requests = requestsRes.data || [];
      const documents = documentsRes.data || [];
      setProfile(profileRes.data);
      setProfileLoading(false);

      setRequestCount(requests.length);
      setDocumentCount(documents.length);

      const pendingItems = [...requests, ...documents].filter((item) =>
        isPendingStatus(item?.status)
      );

      setPendingCount(pendingItems.length);
    } catch {
      setRequestCount(0);
      setDocumentCount(0);
      setPendingCount(0);
      setProfileLoading(false);
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
            <h2>Citizen Services Portal</h2>
            <p className="subtitle">
              Securely access government services, manage documents, and track your applications.
            </p>
          </div>
        </div>
      );
    }

    if (tab === 'profile') {
      return (
        <div className="section-header">
          <div>
            <h2>
              {tabMode === 'editing'
                ? profile
                  ? 'Edit Profile'
                  : 'Create Profile'
                : 'Your Profile'}
            </h2>
            <p className="subtitle">Manage your personal information and contact details.</p>
          </div>
          {tabMode !== 'editing' && profile && (
            <div className="header-actions">
              <button className="btn btn-primary" onClick={() => setTabMode('editing')}>
                Edit Profile
              </button>
            </div>
          )}
        </div>
      );
    }

    if (tab === 'requests') {
      return (
        <div className="section-header">
          <div>
            <h2>{tabMode === 'new' ? 'New Service Request' : 'My Service Requests'}</h2>
            <p className="subtitle">
              {tabMode === 'new'
                ? 'Submit a new application for government services.'
                : 'Track request status and review any administrative notes.'}
            </p>
          </div>
          {tabMode === 'list' && (
            <div className="header-actions">
              <input
                className="search-input"
                placeholder="Search requests..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              <button className="btn btn-primary" onClick={() => setTabMode('new')}>
                New Request
              </button>
            </div>
          )}
        </div>
      );
    }

    if (tab === 'documents') {
      return (
        <div className="section-header">
          <div>
            <h2>{tabMode === 'new' ? 'New Document Request' : 'My Documents'}</h2>
            <p className="subtitle">
              {tabMode === 'new'
                ? 'Apply for official documentation from the relevant authorities.'
                : 'Track requested documents and follow their processing status.'}
            </p>
          </div>
          {tabMode === 'list' && (
            <div className="header-actions">
              <input
                className="search-input"
                placeholder="Search documents..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              <button className="btn btn-primary" onClick={() => setTabMode('new')}>
                Request Document
              </button>
            </div>
          )}
        </div>
      );
    }

    return null;
  };

  return (
    <div className="dashboard-page citizen-dashboard">
      <div className="page-hero">{renderHeader()}</div>

      <div className="dashboard-content">
        {tab === 'overview' && (
          <div className="stats-grid">
            <div className="card stat-card">
              <span className="stat-label">Service Requests </span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{requestCount}</strong>
              )}
            </div>
            <div className="card stat-card">
              <span className="stat-label">Documents </span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{documentCount}</strong>
              )}
            </div>
            <div className="card stat-card">
              <span className="stat-label">Pending Items </span>
              {summaryLoading ? (
                <div className="skeleton" style={{ height: '2.25rem' }}></div>
              ) : (
                <strong className="stat-value">{pendingCount}</strong>
              )}
            </div>
          </div>
        )}

        <div className="dashboard-section">
          {tab === 'profile' && (
            <ProfileTab
              profile={profile}
              loading={profileLoading}
              mode={tabMode}
              setMode={setTabMode}
              onRefresh={loadSummary}
            />
          )}
          {tab === 'requests' && (
            <RequestsTab
              onRefreshSummary={loadSummary}
              search={search}
              mode={tabMode}
              setMode={setTabMode}
            />
          )}
          {tab === 'documents' && (
            <DocumentsTab
              onRefreshSummary={loadSummary}
              search={search}
              mode={tabMode}
              setMode={setTabMode}
            />
          )}
        </div>
      </div>
    </div>
  );
}

function ProfileTab({ profile, loading, mode, setMode, onRefresh }) {
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

  const editing = mode === 'editing';

  useEffect(() => {
    if (profile) {
      setForm({
        phoneNumber: profile.phoneNumber || '',
        address: profile.address || '',
        dateOfBirth: profile.dateOfBirth || '',
        nationalId: profile.nationalId || '',
        city: profile.city || '',
        gender: profile.gender || '',
      });
    }
  }, [profile]);

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

      setMode('list');
      await onRefresh();
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
        <div className="section-header">
          <div>
            <div
              className="skeleton"
              style={{ width: '120px', height: '1.2rem', marginBottom: '0.5rem' }}
            ></div>
            <div className="skeleton" style={{ width: '250px', height: '0.8rem' }}></div>
          </div>
        </div>
        <div className="detail-grid">
          {[...Array(6)].map((_, i) => (
            <div key={i} style={{ height: '3.5rem', display: 'flex', alignItems: 'center' }}>
              <div
                className="skeleton"
                style={{ width: '30%', height: '0.8rem', marginRight: '1rem' }}
              ></div>
              <div className="skeleton" style={{ width: '50%', height: '0.8rem' }}></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!profile && !editing) {
    return (
      <div className="card empty-state-card">
        <h2>Profile Information</h2>
        <p className="subtitle">Complete your personal information to use services more easily.</p>
        <button className="btn btn-primary" onClick={() => setMode('editing')}>
          Create Profile
        </button>
      </div>
    );
  }

  if (editing) {
    return (
      <div className="card">
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
              <AppSelect
                value={form.gender}
                onChange={(val) => setForm({ ...form, gender: val })}
                required
                placeholder="Select..."
                options={['Male', 'Female', 'Other']}
              />
            </div>
          </div>

          <div className="btn-group">
            <button type="submit" className="btn btn-primary">
              Save Profile
            </button>
            <button type="button" className="btn btn-outline" onClick={() => setMode('list')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
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

function RequestsTab({ onRefreshSummary, search, mode, setMode }) {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState({
    type: 'Permit',
    title: '',
    description: '',
  });
  const [error, setError] = useState('');
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

  useEffect(() => {
    setPage(1);
  }, [search]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      await serviceRequestApi.create(form);
      setForm({ type: 'Permit', title: '', description: '' });
      setMode('list');
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
      setUploadError(
        typeof d === 'string' ? d : d?.error || d?.message || 'Failed to upload document'
      );
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
      setUploadError(
        typeof d === 'string' ? d : d?.error || d?.message || 'Failed to open uploaded document'
      );
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

  const columns = [
    { header: 'Type', key: 'type', className: 'col-type' },
    { header: 'Title', key: 'title', className: 'col-expand' },
    {
      header: 'Status',
      key: 'status',
      className: 'col-status',
      render: (r) => <AppBadge type={getStatusType(r.status)}>{r.status}</AppBadge>,
    },
    {
      header: 'Progress',
      key: 'progress',
      className: 'col-progress',
      render: (r) => <ProgressBar percentage={r.progressPercentage} color={r.progressColor} />,
    },
    {
      header: 'Officer Note',
      key: 'officerNote',
      className: 'col-expand desc-cell',
      render: (r) =>
        canUploadRequestDocument(r.status) ? r.officerNote || 'No note provided' : '—',
    },
    {
      header: 'Upload PDF',
      key: 'upload',
      className: 'col-expand',
      render: (r) =>
        canUploadRequestDocument(r.status) ? (
          <input
            type="file"
            accept="application/pdf,.pdf"
            disabled={uploadingId === r.id}
            onChange={(e) => handleDocumentUpload(r.id, e.target.files?.[0])}
          />
        ) : (
          '—'
        ),
    },
    {
      header: 'Uploaded PDF',
      key: 'linkedDocumentId',
      render: (r) =>
        r.linkedDocumentId ? (
          <button
            className="btn btn-sm btn-outline"
            disabled={documentBusyId === r.id}
            onClick={() => handleOpenDocument(r)}
          >
            {documentBusyId === r.id ? 'Opening...' : 'View'}
          </button>
        ) : (
          '—'
        ),
    },
    { header: 'Created', key: 'createdAt', render: (r) => formatDate(r.createdAt) },
  ];

  if (mode === 'new') {
    return (
      <div className="card">
        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Type</label>
            <AppSelect
              value={form.type}
              onChange={(val) => setForm({ ...form, type: val })}
              options={REQUEST_TYPES}
            />
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
            <button type="button" className="btn btn-outline" onClick={() => setMode('list')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
      {uploadError && <div className="alert alert-error">{uploadError}</div>}

      <DataTable
        columns={columns}
        data={paged}
        loading={loading}
        emptyMessage={search ? 'No matching requests.' : 'No service requests yet.'}
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
    </div>
  );
}

function DocumentsTab({ onRefreshSummary, search, mode, setMode }) {
  const [documents, setDocuments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState({
    documentType: 'BirthCertificate',
    title: '',
    description: '',
  });
  const [error, setError] = useState('');
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

  useEffect(() => {
    setPage(1);
  }, [search]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      await documentApi.create(form);
      setForm({ documentType: 'BirthCertificate', title: '', description: '' });
      setMode('list');
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

  const handleDownload = async (doc) => {
    try {
      const { data } = await documentApi.download(doc.id);
      const url = window.URL.createObjectURL(data);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${doc.documentType}_${doc.referenceNumber || doc.id}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Download failed', err);
    }
  };

  const filtered = documents.filter((d) => {
    const q = search.toLowerCase();
    const typeName = (d.documentType || '')
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .toLowerCase();
    const status = (d.status || '').toLowerCase();

    return !q || typeName.includes(q) || status.includes(q);
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
      header: 'Status',
      key: 'status',
      className: 'col-status',
      render: (d) => <AppBadge type={getStatusType(d.status)}>{d.status}</AppBadge>,
    },
    {
      header: 'Progress',
      key: 'progress',
      className: 'col-progress',
      render: (d) => <ProgressBar percentage={d.progressPercentage} color={d.progressColor} />,
    },
    {
      header: 'Reason',
      key: 'rejectionReason',
      className: 'col-expand desc-cell',
      render: (d) => d.rejectionReason || '—',
    },
    {
      header: 'Expires',
      key: 'expiresAt',
      className: 'col-date',
      render: (d) => (canDownloadDocument(d.status) ? formatDate(d.expiresAt, 'No expiry') : '—'),
    },
    {
      header: 'Created',
      key: 'createdAt',
      className: 'col-date',
      render: (d) => formatDate(d.createdAt),
    },
    {
      header: 'Actions',
      key: 'actions',
      className: 'col-action',
      render: (d) =>
        canDownloadDocument(d.status) ? (
          <button className="btn btn-sm btn-primary" onClick={() => handleDownload(d)}>
            Download
          </button>
        ) : (
          '—'
        ),
    },
  ];

  if (mode === 'new') {
    return (
      <div className="card">
        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Document Type</label>
            <AppSelect
              value={form.documentType}
              onChange={(val) => setForm({ ...form, documentType: val })}
              options={DOC_TYPES.map((t) => ({
                label: t.replace(/([A-Z])/g, ' $1').trim(),
                value: t,
              }))}
            />
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
            <button type="button" className="btn btn-outline" onClick={() => setMode('list')}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    );
  }

  return (
    <div className="card">
      <DataTable
        columns={columns}
        data={paged}
        loading={loading}
        emptyMessage={search ? 'No matching documents.' : 'No documents requested yet.'}
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
    </div>
  );
}
