import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Appointment, CreateAppointmentRequest, Patient, Doctor } from '../api/client';

export default function AppointmentsPage() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [doctors, setDoctors] = useState<Doctor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateAppointmentRequest>({
    patientId: 0, doctorId: 0, scheduledAt: '', durationMinutes: 30, notes: '',
  });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  async function load() {
    setLoading(true);
    try {
      const [apptData, patientData] = await Promise.all([
        api.getAppointments(),
        api.getPatients(),
      ]);
      setAppointments(apptData.items);
      setPatients(patientData.items);

      try {
        const doctorData = await api.getDoctors();
        setDoctors(doctorData.items);
      } catch {
        setDoctors([]);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load appointments');
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
      await api.createAppointment(form);
      setShowForm(false);
      setForm({ patientId: 0, doctorId: 0, scheduledAt: '', durationMinutes: 30, notes: '' });
      load();
    } catch (err: unknown) {
      setFormError(err instanceof Error ? err.message : 'Failed to create appointment');
    } finally {
      setSaving(false);
    }
  }

  const statusColor: Record<string, string> = {
    Scheduled: 'badge scheduled',
    Completed: 'badge active',
    Cancelled: 'badge inactive',
  };

  return (
    <div className="page">
      <div className="page-header">
        <h2>Appointments</h2>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ New Appointment'}
        </button>
      </div>

      {showForm && (
        <form className="card form-card" onSubmit={handleCreate}>
          <h3>New Appointment</h3>
          <div className="form-grid">
            <div className="field">
              <label>Patient</label>
              <select value={form.patientId} onChange={e => setForm({ ...form, patientId: +e.target.value })} required>
                <option value={0}>-- Select Patient --</option>
                {patients.map(p => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="field">
              <label>Doctor ID</label>
              {doctors.length > 0 ? (
                <select value={form.doctorId} onChange={e => setForm({ ...form, doctorId: +e.target.value })} required>
                  <option value={0}>-- Select Doctor --</option>
                  {doctors.map(d => <option key={d.id} value={d.id}>{d.fullName}</option>)}
                </select>
              ) : (
                <input type="number" placeholder="Doctor ID" value={form.doctorId || ''} onChange={e => setForm({ ...form, doctorId: +e.target.value })} required min={1} />
              )}
            </div>
            <div className="field">
              <label>Date & Time</label>
              <input type="datetime-local" value={form.scheduledAt} onChange={e => setForm({ ...form, scheduledAt: e.target.value })} required />
            </div>
            <div className="field">
              <label>Duration (minutes)</label>
              <input type="number" value={form.durationMinutes} onChange={e => setForm({ ...form, durationMinutes: +e.target.value })} min={15} step={15} required />
            </div>
            <div className="field full-width">
              <label>Notes</label>
              <textarea value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} rows={2} />
            </div>
          </div>
          {formError && <div className="error">{formError}</div>}
          <button className="btn-primary" type="submit" disabled={saving}>
            {saving ? 'Saving...' : 'Create Appointment'}
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
                <th>Patient</th>
                <th>Doctor</th>
                <th>Scheduled At</th>
                <th>Duration</th>
                <th>Status</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {appointments.length === 0 && (
                <tr><td colSpan={6} className="empty">No appointments found</td></tr>
              )}
              {appointments.map(a => (
                <tr key={a.id}>
                  <td>{a.patientFullName}</td>
                  <td>{a.doctorFullName}</td>
                  <td>{new Date(a.scheduledAt).toLocaleString()}</td>
                  <td>{a.durationMinutes} min</td>
                  <td><span className={statusColor[a.status] ?? 'badge'}>{a.status}</span></td>
                  <td>{a.notes ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
