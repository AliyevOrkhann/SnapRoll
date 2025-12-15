import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { CheckCircle, XCircle, Loader2, Mail } from 'lucide-react';

export const VerifyEmail = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const { verifyEmail } = useAuth();
    
    const [status, setStatus] = useState('loading'); // loading, success, error
    const [message, setMessage] = useState('');

    useEffect(() => {
        const userId = searchParams.get('userId');
        const token = searchParams.get('token');

        if (!userId || !token) {
            setStatus('error');
            setMessage('Invalid verification link. Please check your email and try again.');
            return;
        }

        const verify = async () => {
            const result = await verifyEmail(userId, token);
            
            if (result.success) {
                setStatus('success');
                setMessage(result.message || 'Email verified successfully! You can now log in.');
            } else {
                setStatus('error');
                setMessage(result.message || 'Verification failed. The link may have expired.');
            }
        };

        verify();
    }, [searchParams, verifyEmail]);

    return (
        <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
            <div className="sm:mx-auto sm:w-full sm:max-w-md">
                <div className="flex justify-center">
                    <h1 className="text-4xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-indigo-600 to-purple-600">
                        SnapRoll
                    </h1>
                </div>
                <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
                    Email Verification
                </h2>
            </div>

            <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
                <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
                    <div className="text-center">
                        {status === 'loading' && (
                            <div className="flex flex-col items-center">
                                <Loader2 className="h-16 w-16 text-indigo-600 animate-spin mb-4" />
                                <p className="text-gray-600">Verifying your email...</p>
                            </div>
                        )}

                        {status === 'success' && (
                            <div className="flex flex-col items-center">
                                <div className="bg-green-100 rounded-full p-3 mb-4">
                                    <CheckCircle className="h-16 w-16 text-green-500" />
                                </div>
                                <h3 className="text-xl font-semibold text-gray-900 mb-2">
                                    Verification Successful!
                                </h3>
                                <p className="text-gray-600 mb-6">{message}</p>
                                <Link
                                    to="/login"
                                    className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                >
                                    Go to Login
                                </Link>
                            </div>
                        )}

                        {status === 'error' && (
                            <div className="flex flex-col items-center">
                                <div className="bg-red-100 rounded-full p-3 mb-4">
                                    <XCircle className="h-16 w-16 text-red-500" />
                                </div>
                                <h3 className="text-xl font-semibold text-gray-900 mb-2">
                                    Verification Failed
                                </h3>
                                <p className="text-gray-600 mb-6">{message}</p>
                                <div className="space-y-3 w-full">
                                    <Link
                                        to="/login"
                                        className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                                    >
                                        Go to Login
                                    </Link>
                                    <p className="text-sm text-gray-500">
                                        You can request a new verification email from the login page.
                                    </p>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};
