import { useState } from 'react';
import LoginPage from './pages/LoginPage';
import PatientsPage from './pages/PatientsPage';
import AppointmentsPage from './pages/AppointmentsPage';
import DoctorsPage from './pages/DoctorsPage';
import './App.css';

type Page = 'patients' | 'doctors' | 'appointments';

export default function App() {
  const [loggedIn, setLoggedIn] = useState(!!localStorage.getItem('token'));
  const [page, setPage] = useState<Page>('patients');

  function logout() {
    localStorage.removeItem('token');
    setLoggedIn(false);
  }

  if (!loggedIn) return <LoginPage onLogin={() => setLoggedIn(true)} />;

  return (
    <div className="layout">
      <nav className="sidebar">
        <div className="sidebar-title">🏥 Clinic</div>
        <button className={page === 'patients' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('patients')}>
          Patients
        </button>
        <button className={page === 'doctors' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('doctors')}>
          Doctors
        </button>
        <button className={page === 'appointments' ? 'nav-item active' : 'nav-item'} onClick={() => setPage('appointments')}>
          Appointments
        </button>
        <button className="nav-item logout" onClick={logout}>Logout</button>
      </nav>
      <main className="content">
        {page === 'patients' && <PatientsPage />}
        {page === 'doctors' && <DoctorsPage />}
        {page === 'appointments' && <AppointmentsPage />}
      </main>
    </div>
  );
}
