import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

function FieldError({ msg }) {
  if (!msg) return null;
  return (
    <span
      style={{
        color: '#dc2626',
        fontSize: '0.78rem',
        marginTop: '0.25rem',
        display: 'block',
      }}
    >
      {msg}
    </span>
  );
}

const SectionLabel = ({ children }) => (
  <p
    style={{
      fontWeight: 600,
      margin: '1.25rem 0 0.6rem',
      color: 'var(--text-secondary, #6b7280)',
      fontSize: '0.75rem',
      textTransform: 'uppercase',
      letterSpacing: '0.06em',
      borderBottom: '1px solid var(--border, #e5e7eb)',
      paddingBottom: '0.35rem',
    }}
  >
    {children}
  </p>
);

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({
    name: '',
    surname: '',
    email: '',
    password: '',
    jmb: '',
    gender: '',
    dateOfBirth: '',
    placeOfBirth: '',
    placeOfResidence: '',
    zipCode: '',
    citizenship: '',
  });

  const [showPassword, setShowPassword] = useState(false);
  const [touched, setTouched] = useState({});
  const [submitError, setSubmitError] = useState('');
  const [loading, setLoading] = useState(false);

  const set = (field) => (e) => {
    const val = e.target ? e.target.value : e;
    setForm((f) => ({ ...f, [field]: val }));
    setTouched((t) => ({ ...t, [field]: true }));
  };

  const blur = (field) => () => setTouched((t) => ({ ...t, [field]: true }));

  const validate = () => {
    const e = {};
    if (!form.name.trim()) e.name = 'First name is required.';
    if (!form.surname.trim()) e.surname = 'Surname is required.';
    if (!form.email.trim()) {
      e.email = 'Email is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(form.email.trim())) {
      e.email = 'Enter a valid email (e.g. name@example.com).';
    }
    if (!form.password) {
      e.password = 'Password is required.';
    } else if (form.password.length < 6) {
      e.password = 'Password must be at least 6 characters.';
    }
    if (!form.jmb.trim()) {
      e.jmb = 'JMB is required.';
    } else if (!/^\d{13}$/.test(form.jmb.trim())) {
      e.jmb = 'JMB must be exactly 13 digits.';
    }
    if (!form.gender) e.gender = 'Please select a gender.';
    if (!form.dateOfBirth) {
      e.dateOfBirth = 'Date of birth is required.';
    } else if (form.dateOfBirth > new Date().toISOString().split('T')[0]) {
      e.dateOfBirth = 'Date of birth cannot be in the future.';
    }
    if (!form.placeOfBirth.trim()) e.placeOfBirth = 'Place of birth is required.';
    if (!form.placeOfResidence.trim()) e.placeOfResidence = 'Place of residence is required.';
    if (!form.zipCode.trim()) {
      e.zipCode = 'ZIP code is required.';
    } else if (!/^\d{5}$/.test(form.zipCode.trim())) {
      e.zipCode = 'ZIP code must be exactly 5 digits.';
    }
    if (!form.citizenship.trim()) e.citizenship = 'Citizenship is required.';
    return e;
  };

  const errors = validate();
  const show = (field) => !!(touched[field] && errors[field]);

  const borderStyle = (field) => ({
    borderColor: show(field) ? '#dc2626' : undefined,
  });

  const handleSubmit = async (e) => {
    e.preventDefault();
    // Touch all fields so every error shows
    setTouched(Object.fromEntries(Object.keys(form).map((k) => [k, true])));
    if (Object.keys(validate()).length > 0) return;

    setSubmitError('');
    setLoading(true);
    try {
      await register({
        fullName: `${form.name.trim()} ${form.surname.trim()}`,
        email: form.email.trim(),
        password: form.password,
        jmb: form.jmb.trim(),
        name: form.name.trim(),
        surname: form.surname.trim(),
        gender: form.gender,
        dateOfBirth: form.dateOfBirth,
        placeOfBirth: form.placeOfBirth.trim(),
        placeOfResidence: form.placeOfResidence.trim(),
        zipCode: form.zipCode.trim(),
        citizenship: form.citizenship.trim(),
      });
      navigate('/citizen');
    } catch (err) {
      const d = err.response?.data;
      setSubmitError(
        typeof d === 'string' ? d : d?.message || d?.title || 'Registration failed. Please try again.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="auth-page"
      style={{ alignItems: 'flex-start', paddingTop: '2rem', paddingBottom: '2rem' }}
    >
      <div className="auth-card" style={{ width: '100%', maxWidth: '580px', margin: '0 auto' }}>
        <div className="auth-header">
          <div className="auth-logo">eGov Portal</div>
          <h1>Create account</h1>
          <p className="auth-subtitle">Register to access digital government services</p>
        </div>

        {submitError && <div className="alert alert-error">{submitError}</div>}

        <form onSubmit={handleSubmit} className="auth-form" noValidate>

          {/* ── Account ── */}
          <SectionLabel>Account</SectionLabel>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 1rem' }}>
            <div className="form-group">
              <label htmlFor="reg-name">First name</label>
              <input
                id="reg-name"
                type="text"
                placeholder="First name"
                value={form.name}
                onChange={set('name')}
                onBlur={blur('name')}
                style={borderStyle('name')}
                autoComplete="given-name"
              />
              <FieldError msg={show('name') ? errors.name : null} />
            </div>

            <div className="form-group">
              <label htmlFor="reg-surname">Surname</label>
              <input
                id="reg-surname"
                type="text"
                placeholder="Surname"
                value={form.surname}
                onChange={set('surname')}
                onBlur={blur('surname')}
                style={borderStyle('surname')}
                autoComplete="family-name"
              />
              <FieldError msg={show('surname') ? errors.surname : null} />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="reg-email">Email address</label>
            <input
              id="reg-email"
              type="email"
              placeholder="example@email.com"
              value={form.email}
              onChange={set('email')}
              onBlur={blur('email')}
              style={borderStyle('email')}
              autoComplete="email"
            />
            <FieldError msg={show('email') ? errors.email : null} />
          </div>

          <div className="form-group">
            <label htmlFor="reg-password">Password</label>
            <div className="password-input-wrapper">
              <input
                id="reg-password"
                type={showPassword ? 'text' : 'password'}
                placeholder="Minimum 6 characters"
                value={form.password}
                onChange={set('password')}
                onBlur={blur('password')}
                style={borderStyle('password')}
                autoComplete="new-password"
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword((c) => !c)}
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            <FieldError msg={show('password') ? errors.password : null} />
          </div>

          {/* ── Personal Information ── */}
          <SectionLabel>Personal Information</SectionLabel>

          <div className="form-group">
            <label htmlFor="reg-jmb">JMB — Unique Master Citizen Number</label>
            <input
              id="reg-jmb"
              type="text"
              placeholder="13-digit number"
              value={form.jmb}
              onChange={(e) => {
                const val = e.target.value.replace(/\D/g, '').slice(0, 13);
                setForm((f) => ({ ...f, jmb: val }));
                setTouched((t) => ({ ...t, jmb: true }));
              }}
              onBlur={blur('jmb')}
              style={borderStyle('jmb')}
              inputMode="numeric"
              maxLength={13}
            />
            <FieldError msg={show('jmb') ? errors.jmb : null} />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 1rem' }}>
            <div className="form-group">
              <label htmlFor="reg-gender">Gender</label>
              <select
                id="reg-gender"
                value={form.gender}
                onChange={set('gender')}
                onBlur={blur('gender')}
                style={borderStyle('gender')}
              >
                <option value="">Select...</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
              <FieldError msg={show('gender') ? errors.gender : null} />
            </div>

            <div className="form-group">
              <label htmlFor="reg-dob">Date of birth</label>
              <input
                id="reg-dob"
                type="date"
                value={form.dateOfBirth}
                onChange={set('dateOfBirth')}
                onBlur={blur('dateOfBirth')}
                style={borderStyle('dateOfBirth')}
                max={new Date().toISOString().split('T')[0]}
              />
              <FieldError msg={show('dateOfBirth') ? errors.dateOfBirth : null} />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="reg-pob">Place of birth</label>
            <input
              id="reg-pob"
              type="text"
              placeholder="City / municipality of birth"
              value={form.placeOfBirth}
              onChange={set('placeOfBirth')}
              onBlur={blur('placeOfBirth')}
              style={borderStyle('placeOfBirth')}
            />
            <FieldError msg={show('placeOfBirth') ? errors.placeOfBirth : null} />
          </div>

          {/* ── Address & Citizenship ── */}
          <SectionLabel>Address &amp; Citizenship</SectionLabel>

          <div className="form-group">
            <label htmlFor="reg-por">Place of residence</label>
            <input
              id="reg-por"
              type="text"
              placeholder="City / municipality of residence"
              value={form.placeOfResidence}
              onChange={set('placeOfResidence')}
              onBlur={blur('placeOfResidence')}
              style={borderStyle('placeOfResidence')}
            />
            <FieldError msg={show('placeOfResidence') ? errors.placeOfResidence : null} />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 1rem' }}>
            <div className="form-group">
              <label htmlFor="reg-zip">ZIP / Postal code</label>
              <input
                id="reg-zip"
                type="text"
                placeholder="5-digit code"
                value={form.zipCode}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 5);
                  setForm((f) => ({ ...f, zipCode: val }));
                  setTouched((t) => ({ ...t, zipCode: true }));
                }}
                onBlur={blur('zipCode')}
                style={borderStyle('zipCode')}
                inputMode="numeric"
                maxLength={5}
              />
              <FieldError msg={show('zipCode') ? errors.zipCode : null} />
            </div>

            <div className="form-group">
              <label htmlFor="reg-citizenship">Citizenship</label>
              <input
                id="reg-citizenship"
                type="text"
                placeholder="e.g. Bosnia and Herzegovina"
                value={form.citizenship}
                onChange={set('citizenship')}
                onBlur={blur('citizenship')}
                style={borderStyle('citizenship')}
              />
              <FieldError msg={show('citizenship') ? errors.citizenship : null} />
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-full"
            disabled={loading}
            style={{ marginTop: '1.5rem' }}
          >
            {loading ? 'Creating account...' : 'Create account'}
          </button>
        </form>

        <p className="auth-footer">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
}