

import React, { useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import TodoApp from './TodoApp'; 
import Login from './pages/Login.jsx';
import Register from './pages/Register.jsx';
import service from './service.js';

// פונקציה לבדיקה האם המשתמש מחובר (משמשת רק לאתחול ה-State)
const checkAuth = () => {
  return localStorage.getItem('jwtToken') !== null;
};

// ************************** רכיב Header מתוקן **************************
// רכיב פשוט להצגת שם המשתמש וכפתור יציאה
// מקבל handleLogout כ-prop
const Header = ({ handleLogout }) => {
    const navigate = useNavigate(); 
    const username = localStorage.getItem('username'); 
    
    const onLogout = () => {
        handleLogout(); // מנקה את ה-State ואת localStorage
        navigate('/login'); // מנווט חזרה לדף הלוגין
    };

    return (
        <div style={{ padding: '10px 20px', backgroundColor: '#f5f5f5', borderBottom: '1px solid #ddd', display: 'flex', justifyContent: 'space-between' }}>
            <span>Welcome, {username}</span>
            <button onClick={onLogout} style={{ background: '#f44336', color: 'white', border: 'none', padding: '5px 10px', cursor: 'pointer' }}>
                Logout
            </button>
        </div>
    );
};

const ProtectedRoute = ({ isAuthenticated, children }) => {
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return children;
};


function App() {
  // ניהול סטטוס האימות באמצעות State
  const [isAuthenticated, setIsAuthenticated] = useState(checkAuth()); 

  // פונקציה שנקראת מה-Login.jsx לאחר לוגין מוצלח
  const handleLoginSuccess = () => {
    setIsAuthenticated(true); // זה גורם ל-Header להופיע מיד!
  };

  // פונקציה לטיפול בלוגאאוט
  const handleLogout = () => {
    service.logout(); // מנקה רק את localStorage (יש לעדכן את service.js בהתאם)
    setIsAuthenticated(false); // זה גורם ל-Header להיעלם מיד!
  };


  return (
    <BrowserRouter>
      {/* הצגת כפתור יציאה רק אם ה-State מעודכן */}
      {isAuthenticated && <Header handleLogout={handleLogout} />}
      
      <Routes>
        
        {/* ניתובים ציבוריים */}
        <Route path="/register" element={<Register />} />
        {/* העברת הפונקציה onLoginSuccess לרכיב Login */}
        <Route 
            path="/login" 
            element={isAuthenticated ? <Navigate to="/" replace /> : <Login onLoginSuccess={handleLoginSuccess} />} />
        
        {/* ניתוב ראשי מוגן - משתמש ב-State */}
        <Route 
          path="/" 
          element={
            <ProtectedRoute isAuthenticated={isAuthenticated}>
              <TodoApp /> 
            </ProtectedRoute>
          } 
        />

        {/* 404 */}
        <Route path="*" element={<h1>404 Not Found</h1>} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;