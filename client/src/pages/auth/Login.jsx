import { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { useNavigate, useLocation } from 'react-router-dom';
import { LogIn, UserPlus, GraduationCap, School } from 'lucide-react';

export const Login = () => {
    const [isLogin, setIsLogin] = useState(true);
    const [userType, setUserType] = useState('Student');
    const [formData, setFormData] = useState({
        email: '',
        password: '',
        fullName: '',
        universityId: ''
    });
    const [error, setError] = useState('');

    const { login, register } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const from = location.state?.from?.pathname || (userType === 'Instructor' ? '/instructor' : '/student');

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');

        let result;
        if (isLogin) {
            result = await login(formData.email, formData.password);
        } else {
            result = await register({ ...formData, userType });
        }

        if (result.success) {
            if (isLogin) {
                // Redirect based on role if just logging in without specific destination
                const user = JSON.parse(localStorage.getItem('user'));
                const dest = user.userType === 'Instructor' ? '/instructor' : '/student';
                navigate(dest, { replace: true });
            } else {
                navigate(userType === 'Instructor' ? '/instructor' : '/student', { replace: true });
            }
        } else {
            setError(result.message);
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
            <div className="sm:mx-auto sm:w-full sm:max-w-md">
                <div className="flex justify-center">
                    <h1 className="text-4xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-indigo-600 to-purple-600">
                        SnapRoll
                    </h1>
                </div>
                <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
                    {isLogin ? 'Sign in to your account' : 'Create your account'}
                </h2>
            </div>

            <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
                <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
                    <form className="space-y-6" onSubmit={handleSubmit}>
                        {!isLogin && (
                            <>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700">Full Name</label>
                                    <div className="mt-1">
                                        <input
                                            type="text"
                                            required
                                            className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                                            value={formData.fullName}
                                            onChange={e => setFormData({ ...formData, fullName: e.target.value })}
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700">University ID</label>
                                    <div className="mt-1">
                                        <input
                                            type="text"
                                            required
                                            className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                                            value={formData.universityId}
                                            onChange={e => setFormData({ ...formData, universityId: e.target.value })}
                                        />
                                    </div>
                                </div>

                                {/* Role Switcher */}
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">I am a...</label>
                                    <div className="flex space-x-4">
                                        <button
                                            type="button"
                                            onClick={() => setUserType('Student')}
                                            className={`flex-1 flex items-center justify-center px-4 py-2 border rounded-md text-sm font-medium ${userType === 'Student'
                                                    ? 'border-indigo-500 text-indigo-600 bg-indigo-50'
                                                    : 'border-gray-300 text-gray-700 bg-white hover:bg-gray-50'
                                                }`}
                                        >
                                            <GraduationCap className="mr-2 h-5 w-5" />
                                            Student
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => setUserType('Instructor')}
                                            className={`flex-1 flex items-center justify-center px-4 py-2 border rounded-md text-sm font-medium ${userType === 'Instructor'
                                                    ? 'border-indigo-500 text-indigo-600 bg-indigo-50'
                                                    : 'border-gray-300 text-gray-700 bg-white hover:bg-gray-50'
                                                }`}
                                        >
                                            <School className="mr-2 h-5 w-5" />
                                            Instructor
                                        </button>
                                    </div>
                                </div>
                            </>
                        )}

                        <div>
                            <label className="block text-sm font-medium text-gray-700">Email address</label>
                            <div className="mt-1">
                                <input
                                    type="email"
                                    required
                                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                                    value={formData.email}
                                    onChange={e => setFormData({ ...formData, email: e.target.value })}
                                />
                            </div>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700">Password</label>
                            <div className="mt-1">
                                <input
                                    type="password"
                                    required
                                    className="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                                    value={formData.password}
                                    onChange={e => setFormData({ ...formData, password: e.target.value })}
                                />
                            </div>
                        </div>

                        {error && (
                            <div className="text-red-500 text-sm">{error}</div>
                        )}

                        <div>
                            <button
                                type="submit"
                                className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                            >
                                {isLogin ? (
                                    <><LogIn className="mr-2 h-5 w-5" /> Sign In</>
                                ) : (
                                    <><UserPlus className="mr-2 h-5 w-5" /> Register</>
                                )}
                            </button>
                        </div>
                    </form>

                    <div className="mt-6">
                        <div className="relative">
                            <div className="absolute inset-0 flex items-center">
                                <div className="w-full border-t border-gray-300" />
                            </div>
                            <div className="relative flex justify-center text-sm">
                                <span className="px-2 bg-white text-gray-500">
                                    Or
                                </span>
                            </div>
                        </div>

                        <div className="mt-6 grid grid-cols-1 gap-3">
                            <button
                                onClick={() => setIsLogin(!isLogin)}
                                className="w-full flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                            >
                                {isLogin ? 'Create new account' : 'Sign in back'}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
