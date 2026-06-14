const BASE = 'http://localhost:7161';

function token() {
  return localStorage.getItem('token');
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (token()) headers['Authorization'] = `Bearer ${token()}`;

  const res = await fetch(`${BASE}${path}`, { ...options, headers });
  const json = await res.json();
  if (!res.ok || !json.success) throw new Error(json.error ?? 'Request failed');
  return json.data as T;
}

export const api = {
  login: (email: string, password: string) =>
    request<{ token: string; email: string; role: string }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  register: (username: string, email: string, password: string) =>
    request<{ token: string; email: string; role: string }>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ username, email, password }),
    }),

  getPatients: (afterId = 0, pageSize = 20) =>
    request<{ items: Patient[] }>(`/api/patients?afterId=${afterId}&pageSize=${pageSize}`),

  createPatient: (body: CreatePatientRequest) =>
    request<Patient>('/api/patients', { method: 'POST', body: JSON.stringify(body) }),

  getAppointments: (afterId = 0, pageSize = 20) =>
    request<{ items: Appointment[] }>(`/api/appointments?afterId=${afterId}&pageSize=${pageSize}`),

  createAppointment: (body: CreateAppointmentRequest) =>
    request<Appointment>('/api/appointments', { method: 'POST', body: JSON.stringify(body) }),

  getDoctors: () =>
    request<{ items: Doctor[] }>('/api/doctors'),

  createDoctor: (body: { firstName: string; lastName: string; specialty: string }) =>
    request<Doctor>('/api/doctors', { method: 'POST', body: JSON.stringify(body) }),
};

export interface Patient {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  phone: string;
}

export interface Appointment {
  id: number;
  patientId: number;
  patientFullName: string;
  doctorId: number;
  doctorFullName: string;
  scheduledAt: string;
  durationMinutes: number;
  status: string;
  notes?: string;
}

export interface CreateAppointmentRequest {
  patientId: number;
  doctorId: number;
  scheduledAt: string;
  durationMinutes: number;
  notes?: string;
}

export interface Doctor {
  id: number;
  fullName: string;
  specialty: string;
  isActive: boolean;
}
