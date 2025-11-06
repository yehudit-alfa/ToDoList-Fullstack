
import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import service from '../service';

// מקבל את הפונקציה onLoginSuccess כ-prop
export default function Login({ onLoginSuccess }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setMessage('');

    if (!username || !password) {
      setMessage('Username and password are required.');
      return;
    }

    try {
      const response = await service.login(username, password);
      setMessage(`Login successful! Welcome, ${response.username}`);
      
      // 👈 קריאה לפונקציית ה-Callback! זה יעדכן את App.js
      onLoginSuccess(); 
      
      // הניווט מתבצע לאחר עדכון ה-State
      navigate('/'); 

    } catch (error) {
      setMessage('Login failed. Check your credentials.');
      console.error('Login error:', error);
    }
  };

  return (
    <div>
      <h2>Login</h2>
      <form onSubmit={handleLogin}>
        <div>
          <label>Username:</label>
          <input type="text" value={username} onChange={(e) => setUsername(e.target.value)} required />
        </div>
        <div>
          <label>Password:</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>
        <button type="submit">Login</button>
      </form>
      {message && <p>{message}</p>}
      
      {/* קישור לדף ההרשמה */}
      <p>
        Don't have an account? <Link to="/register">Register here</Link>
      </p>
    </div>
  );
}
