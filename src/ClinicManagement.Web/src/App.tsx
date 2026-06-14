import { useState } from 'react';
import { AuthProvider, useAuth } from './auth/AuthContext';
import LoginPage from './pages/LoginPage';
import PatientsPage from './pages/PatientsPage';
import AppointmentsPage from './pages/AppointmentsPage';
import DoctorsPage from './pages/DoctorsPage';
import UsersPage from './pages/UsersPage';
import './App.css';

type Page = 'patients' | 'doctors' | 'appointments' | 'users';

function Shell() {
  const { token, email, role, clearAuth, setAuth } = useAuth();
  const [page, setPage] = useState<Page>('patients');

  if (!token) return <LoginPage onLogin={(t, e, r) => { setAuth(t, e, r); setPage('patients'); }} />;

  return (
    <div className="layout">
      <nav className="sidebar">
        <div className="sidebar-title">Clinic</div>
        <div className="sidebar-user">{email}<br /><span className={`badge badge-${role?.toLowerCase()}`}>{role}</span></div>
        <button className={page === 'patients' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('patients')}>
          Patients
        </button>
        <button className={page === 'doctors' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('doctors')}>
          Doctors
        </button>
        <button className={page === 'appointments' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('appointments')}>
          Appointments
        </button>
        {role === 'Admin' && (
          <button className={page === 'users' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('users')}>
            Users
          </button>
        )}
        <button className="nav-item logout" onClick={clearAuth}>Logout</button>
      </nav>
      <main className="content">
        {page === 'patients' && <PatientsPage />}
        {page === 'doctors' && <DoctorsPage />}
        {page === 'appointments' && <AppointmentsPage />}
        {page === 'users' && role === 'Admin' && <UsersPage />}
      </main>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <Shell />
    </AuthProvider>
  );
}
