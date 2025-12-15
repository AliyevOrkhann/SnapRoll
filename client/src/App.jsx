import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import { SignalRProvider } from './context/SignalRContext';
import { Layout } from './components/Layout/Layout';
import ProtectedRoute from './components/Layout/Layout';

// Pages
import { Login } from './pages/auth/Login';
import { VerifyEmail } from './pages/auth/VerifyEmail';
import { InstructorDashboard } from './pages/instructor/Dashboard';
import { LiveSession } from './pages/instructor/LiveSession';
import { SessionHistory } from './pages/instructor/SessionHistory';
import { StudentHome } from './pages/student/StudentHome';
import { ScanPage } from './pages/student/ScanPage';
import { JoinCourse } from './pages/student/JoinCourse';

const Unauthorized = () => (
  <div className="h-screen flex items-center justify-center bg-gray-50">
    <div className="text-center">
      <h1 className="text-4xl font-bold text-red-600 mb-4">403</h1>
      <p className="text-xl text-gray-700 mb-8">Unauthorized Access</p>
      <a href="/login" className="text-indigo-600 hover:text-indigo-800 font-medium">Return to Login</a>
    </div>
  </div>
);

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <SignalRProvider>
          <Routes>
            {/* Public Routes */}
            <Route path="/login" element={<Login />} />
            <Route path="/verify-email" element={<VerifyEmail />} />
            <Route path="/unauthorized" element={<Unauthorized />} />

            {/* Default Redirect */}
            <Route path="/" element={<Navigate to="/login" replace />} />

            {/* Protected Routes Wrapper */}
            <Route element={<Layout />}>

              {/* Instructor Routes */}
              <Route element={<ProtectedRoute allowedRoles={['Instructor', 'Admin']} />}>
                <Route path="/instructor" element={<InstructorDashboard />} />
                <Route path="/instructor/session/:sessionId" element={<LiveSession />} />
                <Route path="/instructor/course/:courseId/history" element={<SessionHistory />} />
              </Route>

              {/* Student Routes */}
              <Route element={<ProtectedRoute allowedRoles={['Student']} />}>
                <Route path="/student" element={<StudentHome />} />
                <Route path="/student/join" element={<JoinCourse />} />
              </Route>

            </Route>

            {/* Student Scan Route - No Layout (Fullscreen) */}
            <Route element={<ProtectedRoute allowedRoles={['Student']} />}>
              <Route path="/student/scan" element={<ScanPage />} />
            </Route>

          </Routes>
        </SignalRProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
