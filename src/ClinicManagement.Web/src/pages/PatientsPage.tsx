import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Patient, CreatePatientRequest } from '../api/client';

export default function PatientsPage() {
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreatePatientRequest>({
    firstName: '', lastName: '', dateOfBirth: '', email: '', phone: '',
  });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  async function load() {
    setLoading(true);
    try {
      const data = await api.getPatients();
      setPatients(data.items);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load patients');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setFormError('');
    setSaving(true);
    try {
      await api.createPatient(form);
      setShowForm(false);
      setForm({ firstName: '', lastName: '', dateOfBirth: '', email: '', phone: '' });
      load();
    } catch (err: unknown) {
      setFormError(err instanceof Error ? err.message : 'Failed to create patient');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Patients</h2>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ New Patient'}
        </button>
      </div>

      {showForm && (
        <form className="card form-card" onSubmit={handleCreate}>
          <h3>New Patient</h3>
          <div className="form-grid">
            <div className="field">
              <label>First Name</label>
              <input value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} required />
            </div>
            <div className="field">
              <label>Last Name</label>
              <input value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} required />
            </div>
            <div className="field">
              <label>Date of Birth</label>
              <input type="date" value={form.dateOfBirth} onChange={e => setForm({ ...form, dateOfBirth: e.target.value })} required />
            </div>
            <div className="field">
              <label>Email</label>
              <input type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} required />
            </div>
            <div className="field">
              <label>Phone</label>
              <input value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} required />
            </div>
          </div>
          {formError && <div className="error">{formError}</div>}
          <button className="btn-primary" type="submit" disabled={saving}>
            {saving ? 'Saving...' : 'Create Patient'}
          </button>
        </form>
      )}

      {loading && <div className="status">Loading...</div>}
      {error && <div className="error">{error}</div>}

      {!loading && !error && (
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Date of Birth</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {patients.length === 0 && (
                <tr><td colSpan={5} className="empty">No patients found</td></tr>
              )}
              {patients.map(p => (
                <tr key={p.id}>
                  <td>{p.fullName}</td>
                  <td>{new Date(p.dateOfBirth).toLocaleDateString()}</td>
                  <td>{p.email}</td>
                  <td>{p.phone}</td>
                  <td><span className={p.isActive ? 'badge active' : 'badge inactive'}>{p.isActive ? 'Active' : 'Inactive'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
