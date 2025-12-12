import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import api from '../../api/axios';
import { Check, AlertCircle, ArrowLeft, BookOpen } from 'lucide-react';

// New pattern: dedicated join-by-link page for students, wired to the existing Course join endpoint.
export const JoinCourse = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState({ loading: true, success: null, message: '' });

  useEffect(() => {
    const courseId = searchParams.get('courseId');

    if (!courseId) {
      setStatus({
        loading: false,
        success: false,
        message: 'Invalid or missing course link.',
      });
      return;
    }

    const join = async () => {
      try {
        const response = await api.post(`/Course/${courseId}/join`);
        setStatus({
          loading: false,
          success: true,
          message: response.data?.message || 'Enrolled in course successfully!',
        });

        // Redirect back to student home after a short delay
        setTimeout(() => navigate('/student', { replace: true }), 2500);
      } catch (err) {
        const msg = err.response?.data?.message || 'Failed to join the course.';
        setStatus({
          loading: false,
          success: false,
          message: msg,
        });
      }
    };

    join();
  }, [searchParams, navigate]);

  if (status.loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="bg-white rounded-2xl shadow-lg p-8 flex flex-col items-center text-center">
          <div className="h-16 w-16 rounded-full bg-indigo-50 flex items-center justify-center mb-4">
            <BookOpen className="h-8 w-8 text-indigo-600 animate-pulse" />
          </div>
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Joining course…</h2>
          <p className="text-gray-500 text-sm">Please wait while we confirm your enrollment.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-lg p-8 text-center">
        {status.success ? (
          <>
            <div className="h-20 w-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <Check className="h-10 w-10 text-green-600" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">You joined the course!</h2>
            <p className="text-gray-600">{status.message}</p>
            <p className="text-xs text-gray-400 mt-4">Redirecting to your courses…</p>
          </>
        ) : (
          <>
            <div className="h-20 w-20 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <AlertCircle className="h-10 w-10 text-red-600" />
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Unable to join</h2>
            <p className="text-red-500 font-medium">{status.message}</p>
            <button
              onClick={() => navigate('/student')}
              className="mt-6 inline-flex items-center justify-center w-full py-3 px-4 bg-indigo-600 text-white rounded-xl font-medium hover:bg-indigo-700"
            >
              <ArrowLeft className="h-5 w-5 mr-2" />
              Back to My Courses
            </button>
          </>
        )}
      </div>
    </div>
  );
};


