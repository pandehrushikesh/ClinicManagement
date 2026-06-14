import { useEffect, useState } from 'react';
import { api } from '../api/client';

interface User {
  id: number;
  email: string;
  role: string;
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getUsers()
      .then(r => setUsers(r.items))
      .catch(() => setError('Failed to load users. Admin access required.'));
  }, []);

  return (
    <div>
      <h2>Users</h2>
      {error && <p className="error">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>ID</th>
            <th>Email</th>
            <th>Role</th>
          </tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id}>
              <td>{u.id}</td>
              <td>{u.email}</td>
              <td>
                <span className={`badge badge-${u.role.toLowerCase()}`}>{u.role}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
