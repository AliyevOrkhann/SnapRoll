import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { LogOut, User } from 'lucide-react';

const ProtectedRoute = ({ allowedRoles }) => {
    const { user, loading } = useAuth();
    const location = useLocation();

    if (loading) return <div className="min-h-screen flex items-center justify-center">Loading...</div>;

    if (!user) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    if (allowedRoles && !allowedRoles.includes(user.userType)) {
        return <Navigate to="/unauthorized" replace />;
    }

    return <Outlet />;
};

export const Layout = () => {
    const { user, logout } = useAuth();

    return (
        <div className="min-h-screen bg-gray-50 font-sans text-gray-900">
            <nav className="bg-white shadow-sm border-b border-gray-200">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between h-16">
                        <div className="flex items-center">
                            <span className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-indigo-600 to-purple-600">
                                SnapRoll
                            </span>
                        </div>

                        {user && (
                            <div className="flex items-center space-x-4">
                                <div className="flex items-center text-sm text-gray-500">
                                    <User className="h-4 w-4 mr-2" />
                                    <span className="hidden sm:inline">{user.fullName}</span>
                                    <span className="ml-2 px-2 py-0.5 rounded text-xs font-medium bg-gray-100 uppercase tracking-wide">
                                        {user.userType}
                                    </span>
                                </div>
                                <button
                                    onClick={logout}
                                    className="p-2 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-100"
                                    title="Logout"
                                >
                                    <LogOut className="h-5 w-5" />
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            </nav>

            <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
                <Outlet />
            </main>
        </div>
    );
};

export default ProtectedRoute;
