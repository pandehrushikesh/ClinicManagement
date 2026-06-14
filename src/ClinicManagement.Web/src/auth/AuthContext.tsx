import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';

interface AuthState {
  token: string | null;
  email: string | null;
  role: string | null;
}

interface AuthContextValue extends AuthState {
  setAuth: (token: string, email: string, role: string) => void;
  clearAuth: () => void;
}

function decodeRole(token: string): string | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return (
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      payload['role'] ??
      null
    );
  } catch {
    return null;
  }
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuthState] = useState<AuthState>(() => {
    const token = localStorage.getItem('token');
    const email = localStorage.getItem('email');
    if (token) {
      return { token, email, role: decodeRole(token) };
    }
    return { token: null, email: null, role: null };
  });

  function setAuth(token: string, email: string, role: string) {
    localStorage.setItem('token', token);
    localStorage.setItem('email', email);
    setAuthState({ token, email, role });
  }

  function clearAuth() {
    localStorage.removeItem('token');
    localStorage.removeItem('email');
    setAuthState({ token: null, email: null, role: null });
  }

  return (
    <AuthContext.Provider value={{ ...auth, setAuth, clearAuth }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
