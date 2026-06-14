import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Doctor } from '../api/client';

interface CreateDoctorRequest {
  firstName: string;
  lastName: string;
  specialty: string;
}

export default function DoctorsPage() {
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateDoctorRequest>({ firstName: '', lastName: '', specialty: '' });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  async function load() {
    setLoading(true);
    try {
      const data = await api.getDoctors();
      setDoctors(data.items);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load doctors');
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
      await api.createDoctor(form);
      setShowForm(false);
      setForm({ firstName: '', lastName: '', specialty: '' });
      load();
    } catch (err: unknown) {
      setFormError(err instanceof Error ? err.message : 'Failed to create doctor');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h2>Doctors</h2>
        <button className="btn-primary" style={{ width: 'auto' }} onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ New Doctor'}
        </button>
      </div>

      {showForm && (
        <form className="card form-card" onSubmit={handleCreate}>
          <h3>New Doctor</h3>
          <div className="form-grid">
            <div className="field">
              <label>First Name</label>
              <input value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} required />
            </div>
            <div className="field">
              <label>Last Name</label>
              <input value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} required />
            </div>
            <div className="field full-width">
              <label>Specialty</label>
              <input value={form.specialty} onChange={e => setForm({ ...form, specialty: e.target.value })} required placeholder="e.g. Cardiology" />
            </div>
          </div>
          {formError && <div className="error">{formError}</div>}
          <button className="btn-primary" type="submit" disabled={saving}>
            {saving ? 'Saving...' : 'Create Doctor'}
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
                <th>Specialty</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {doctors.length === 0 && (
                <tr><td colSpan={3} className="empty">No doctors found</td></tr>
              )}
              {doctors.map(d => (
                <tr key={d.id}>
                  <td>{d.fullName}</td>
                  <td>{d.specialty}</td>
                  <td><span className={d.isActive ? 'badge active' : 'badge inactive'}>{d.isActive ? 'Active' : 'Inactive'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
