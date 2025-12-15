import { createContext, useContext, useState, useEffect } from 'react';
import api from '../api/axios';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const storedUser = localStorage.getItem('user');
        const token = localStorage.getItem('token');

        if (storedUser && token) {
            setUser(JSON.parse(storedUser));
        }
        setLoading(false);
    }, []);

    const login = async (email, password) => {
        try {
            const response = await api.post('/Auth/login', { email, password });
            const { token, user, requiresEmailVerification } = response.data;

            if (requiresEmailVerification) {
                return {
                    success: false,
                    requiresEmailVerification: true,
                    message: response.data.errorMessage || 'Please verify your email before logging in.'
                };
            }

            localStorage.setItem('token', token);
            localStorage.setItem('user', JSON.stringify(user));
            setUser(user);
            return { success: true };
        } catch (error) {
            const data = error.response?.data;
            return {
                success: false,
                requiresEmailVerification: data?.requiresEmailVerification || false,
                message: data?.errorMessage || 'Login failed'
            };
        }
    };

    const register = async (userData) => {
        try {
            const response = await api.post('/Auth/register', userData);
            // Registration no longer returns token - user must verify email first
            return { 
                success: true,
                message: response.data.message || 'Registration successful! Please check your email to verify your account.'
            };
        } catch (error) {
            return {
                success: false,
                message: error.response?.data?.errorMessage || 'Registration failed'
            };
        }
    };

    const verifyEmail = async (userId, token) => {
        try {
            const response = await api.post('/Auth/verify-email', { userId, token });
            return {
                success: response.data.success,
                message: response.data.message || response.data.errorMessage
            };
        } catch (error) {
            return {
                success: false,
                message: error.response?.data?.errorMessage || 'Email verification failed'
            };
        }
    };

    const resendVerification = async (email) => {
        try {
            const response = await api.post('/Auth/resend-verification', { email });
            return {
                success: true,
                message: response.data.message
            };
        } catch (error) {
            return {
                success: false,
                message: error.response?.data?.errorMessage || 'Failed to resend verification email'
            };
        }
    };

    const logout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        setUser(null);
    };

    return (
        <AuthContext.Provider value={{ user, login, register, logout, loading, verifyEmail, resendVerification }}>
            {!loading && children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);
