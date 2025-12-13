import axios from 'axios';

const defaultApiBaseUrl = `${window.location.protocol}//${window.location.hostname}:5038/api`;

const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || defaultApiBaseUrl,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Add a request interceptor to attach the JWT token
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

export default api;
