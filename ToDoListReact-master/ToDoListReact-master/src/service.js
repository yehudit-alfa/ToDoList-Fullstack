
import axios from 'axios';

// כתובת הבסיס היא השרת בלבד
const apiBaseUrl = 'http://localhost:5233'; 

// 1. יצירת מופע axios מותאם אישית
const api = axios.create({
    baseURL: apiBaseUrl 
});

// 2. Interceptor לבקשות (Request) - מוסיף את הטוקן
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('jwtToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 3. Interceptor לתגובות (Response) - טיפול בשגיאת 401
api.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response) {
      console.error("API Response Error:", error.response.data);
      
      // אם השרת מחזיר שגיאת 401 (Unauthorized)
      if (error.response.status === 401) {
          localStorage.removeItem('jwtToken'); 
          localStorage.removeItem('username'); 
          
          // נווט ללוגין רק אם אנחנו לא כבר שם או בדף ההרשמה
          if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
              window.location.href = '/login'; 
          }
      }
    }
    return Promise.reject(error);
  }
);


export default {
  // ******************************
  // פונקציות אימות (Auth) - קוראות לנתיבים הישירים (/login, /register)
  // ******************************
  login: async (username, password) => {
    const result = await api.post('/auth/login', { username, password });
    
    // שומר את ה-JWT ואת שם המשתמש
    localStorage.setItem('jwtToken', result.data.token); 
    localStorage.setItem('username', result.data.username); 
    
    return result.data; 
  },

  register: async (username, password) => {
    const result = await api.post('/auth/register', { username, password });
    return result.data;
  },

  logout: () => {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('username');
  },

  // ******************************
  // פונקציות המשימות המוגנות - קוראות לנתיבים הישירים (/items)
  // ******************************
  getTasks: async () => {
    const result = await api.get('/items'); 
    return result.data;
  },

  addTask: async (name) => {
    const task = { name: name, isComplete: false };
    const result = await api.post('/items', task);
    return result.data;
  },

  setCompleted: async (id, isComplete) => {
    const updatedTask = { id: id, isComplete: isComplete };
    // שולח PUT לכתובת הנכונה, לדוגמה: http://localhost:5233/items/1
    await api.put(`/items/${id}`, updatedTask); 
    return {};
  },

  deleteTask: async (id) => {
    // שולח DELETE לכתובת הנכונה, לדוגמה: http://localhost:5233/items/1
    await api.delete(`/items/${id}`);
    return {};
  }
};