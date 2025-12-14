import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useSignalR } from '../../context/SignalRContext';
import QRCode from 'react-qr-code';
import api from '../../api/axios';
import { Users, Clock, StopCircle, RefreshCw, Smartphone } from 'lucide-react';

export const LiveSession = () => {
    const { sessionId } = useParams();
    const navigate = useNavigate();
    const { connection, isConnected } = useSignalR();

    const [session, setSession] = useState(null);
    const [stats, setStats] = useState({ presentCount: 0, lateCount: 0, absentCount: 0, pendingCount: 0 });
    const [qrPayload, setQrPayload] = useState(null);
    const [loading, setLoading] = useState(true);
    const [enrolledStudents, setEnrolledStudents] = useState([]);
    const [attendanceRecords, setAttendanceRecords] = useState([]);
    const streamRef = useRef(null);

    // 1. Initial Data Fetch
    useEffect(() => {
        const fetchSession = async () => {
            try {
                const [sessionRes, statsRes] = await Promise.all([
                    api.get(`/Session/${sessionId}`),
                    api.get(`/Dashboard/${sessionId}/stats`)
                ]);
                setSession(sessionRes.data);
                setStats(statsRes.data);
                setLoading(false);
            } catch (err) {
                console.error('Failed to fetch session data', err);
                navigate('/instructor');
            }
        };
        fetchSession();
    }, [sessionId, navigate]);

    // 2. Setup SignalR Listeners & QR Stream
    useEffect(() => {
        if (!isConnected || !connection) return;

        // A. Listen for attendance updates
        const handleAttendanceUpdate = (updatedStats) => {
            setStats(updatedStats);
            // Refresh detailed attendance list when SignalR notifies about updates
            fetchAttendanceRecords();
        };

        // B. Handle session closed event
        const handleSessionClosed = () => {
            alert('Session has been closed.');
            navigate('/instructor');
        };

        connection.on('AttendanceUpdated', handleAttendanceUpdate);
        connection.on('SessionClosed', handleSessionClosed);

        // C. Join the session group
        connection.invoke('JoinSession', sessionId).catch(err => console.error(err));

        // D. Start QR Stream
        const startQrStream = () => {
            streamRef.current = connection.stream("StreamQrToken", sessionId)
                .subscribe({
                    next: (tokenData) => {
                        setQrPayload(JSON.stringify(tokenData));
                    },
                    complete: () => {
                        console.log("Stream completed");
                    },
                    error: (err) => {
                        console.error("Stream error", err);
                    },
                });
        };

        startQrStream();

        return () => {
            connection.off('AttendanceUpdated', handleAttendanceUpdate);
            connection.off('SessionClosed', handleSessionClosed);
            connection.invoke('LeaveSession', sessionId).catch(err => console.error(err));
            if (streamRef.current) {
                streamRef.current.dispose();
            }
        };
    }, [sessionId, connection, isConnected, navigate]);

    // Fetch enrolled students and attendance details for this session
    const fetchEnrollmentAndAttendance = async () => {
        if (!session) return;
        try {
            const [studentsRes, attendanceRes] = await Promise.all([
                api.get(`/Course/${session.courseId}/students`),
                api.get(`/Dashboard/${session.id}/attendance`)
            ]);

            setEnrolledStudents(studentsRes.data || []);
            setAttendanceRecords(attendanceRes.data || []);
        } catch (err) {
            console.error('Failed to fetch enrolled students or attendance', err);
        }
    };

    const fetchAttendanceRecords = async () => {
        if (!session) return;
        try {
            const res = await api.get(`/Dashboard/${session.id}/attendance`);
            setAttendanceRecords(res.data || []);
        } catch (err) {
            console.error('Failed to fetch attendance records', err);
        }
    };

    useEffect(() => {
        if (!session) return;
        fetchEnrollmentAndAttendance();
    }, [session]);

    const presentSet = new Set((attendanceRecords || []).map(a => a.studentId));
    const presentStudents = (enrolledStudents || []).filter(s => presentSet.has(s.id));
    const absentStudents = (enrolledStudents || []).filter(s => !presentSet.has(s.id));

    const closeSession = async () => {
        if (!window.confirm('Are you sure you want to close this session? Students will no longer be able to scan.')) return;

        try {
            await api.post(`/Session/${sessionId}/close`);
            // SignalR will handle the redirect via SessionClosed event, but we can double safe it
            navigate('/instructor');
        } catch (err) {
            alert('Failed to close session');
        }
    };

    if (loading) return <div className="p-8 text-center text-gray-500">Loading Session...</div>;

    return (
        <div className="h-[calc(100vh-4rem)] flex flex-col lg:flex-row bg-gray-100 p-4 gap-4">

            {/* Left Panel: QR Code (Focus) */}
            <div className="flex-1 bg-white rounded-2xl shadow-xl flex flex-col items-center justify-center p-8 relative overflow-hidden">
                <div className="absolute top-0 left-0 w-full h-2 bg-gradient-to-r from-red-500 via-yellow-500 to-green-500 animate-pulse" />

                <h2 className="text-3xl font-bold text-gray-800 mb-8">{session?.courseName} Attendance</h2>

                <div className="relative group">
                    <div className="absolute -inset-1 bg-gradient-to-r from-indigo-500 to-purple-600 rounded-lg blur opacity-25 group-hover:opacity-100 transition duration-1000 group-hover:duration-200"></div>
                    <div className="relative bg-white p-6 rounded-lg border-2 border-gray-100">
                        {qrPayload ? (
                            <div style={{ height: "auto", margin: "0 auto", maxWidth: 300, width: "100%" }}>
                                <QRCode
                                    size={512}
                                    style={{ height: "auto", maxWidth: "100%", width: "100%" }}
                                    value={qrPayload}
                                    viewBox={`0 0 256 256`}
                                />
                            </div>
                        ) : (
                            <div className="h-[300px] w-[300px] flex items-center justify-center bg-gray-50 rounded text-gray-400">
                                <RefreshCw className="animate-spin h-8 w-8 mr-2" /> Connecting Stream...
                            </div>
                        )}
                    </div>
                </div>

                <div className="mt-8 text-center max-w-md">
                    <p className="text-gray-500 flex items-center justify-center mb-2">
                        <Smartphone className="h-5 w-5 mr-2 animate-bounce" />
                        Scan using the SnapRoll App
                    </p>
                    <div className="w-full bg-gray-200 rounded-full h-1.5 mt-4 overflow-hidden">
                        <div className="bg-indigo-600 h-1.5 rounded-full animate-progress-loading w-full" />
                    </div>
                    <p className="text-xs text-gray-400 mt-2">Code rotates every 2 seconds for security</p>
                </div>
            </div>

            {/* Right Panel: Stats & Controls */}
            <div className="lg:w-96 flex flex-col gap-4">

                {/* Stats Card */}
                <div className="bg-white rounded-2xl shadow-sm p-6 flex-1">
                    <h3 className="text-lg font-semibold text-gray-700 mb-6 flex items-center">
                        <Users className="mr-2 text-indigo-600" /> Live Statistics
                    </h3>

                    <div className="grid grid-cols-2 gap-4">
                        <div className="bg-green-50 p-4 rounded-xl text-center">
                            <span className="block text-4xl font-bold text-green-600 mb-1">{stats.presentCount}</span>
                            <span className="text-sm font-medium text-green-800">Present</span>
                        </div>
                        <div className="bg-yellow-50 p-4 rounded-xl text-center">
                            <span className="block text-4xl font-bold text-yellow-600 mb-1">{stats.lateCount}</span>
                            <span className="text-sm font-medium text-yellow-800">Late</span>
                        </div>
                    </div>

                    <div className="mt-8 space-y-4">
                        <div className="flex justify-between items-center p-3 bg-gray-50 rounded-lg">
                            <span className="text-gray-600">Total Enrolled</span>
                            <span className="font-bold text-gray-900">{stats.totalEnrolled}</span>
                        </div>
                        <div className="flex justify-between items-center p-3 bg-gray-50 rounded-lg">
                            <span className="text-gray-600">Pending</span>
                            <span className="font-bold text-gray-900">{stats.pendingCount}</span>
                        </div>
                    </div>
                </div>

                {/* Students List Card */}
                <div className="bg-white rounded-2xl shadow-sm p-6">
                    <h4 className="text-md font-semibold text-gray-700 mb-4">Students</h4>
                    <div className="flex gap-4">
                        <div className="flex-1">
                            <h5 className="text-sm font-medium text-green-600 mb-2">Present ({presentStudents.length})</h5>
                            <ul className="max-h-40 overflow-auto divide-y divide-gray-100">
                                {presentStudents.map(student => (
                                    <li key={student.id} className="py-2 flex justify-between items-center">
                                        <span className="text-sm font-medium">{student.fullName}</span>
                                        <span className="text-xs text-gray-500">{student.universityId}</span>
                                    </li>
                                ))}
                                {presentStudents.length === 0 && <li className="py-2 text-xs text-gray-400">No students present yet.</li>}
                            </ul>
                        </div>
                        <div className="flex-1">
                            <h5 className="text-sm font-medium text-red-600 mb-2">Absent ({absentStudents.length})</h5>
                            <ul className="max-h-40 overflow-auto divide-y divide-gray-100">
                                {absentStudents.map(student => (
                                    <li key={student.id} className="py-2 flex justify-between items-center">
                                        <span className="text-sm">{student.fullName}</span>
                                        <span className="text-xs text-gray-500">{student.universityId}</span>
                                    </li>
                                ))}
                                {absentStudents.length === 0 && <li className="py-2 text-xs text-gray-400">No absent students.</li>}
                            </ul>
                        </div>
                    </div>
                </div>

                {/* Info Card */}
                <div className="bg-white rounded-2xl shadow-sm p-6">
                    <div className="flex items-center text-gray-600 mb-4">
                        <Clock className="w-5 h-5 mr-3 text-gray-400" />
                        <div>
                            <p className="text-xs text-gray-400 uppercase font-bold tracking-wider">Started At</p>
                            <p className="font-medium">{new Date(session?.startTime).toLocaleTimeString()}</p>
                        </div>
                    </div>

                    <button
                        onClick={closeSession}
                        className="w-full mt-2 py-3 px-4 bg-red-50 hover:bg-red-100 text-red-700 border border-red-200 rounded-xl font-medium flex items-center justify-center transition-colors"
                    >
                        <StopCircle className="w-5 h-5 mr-2" />
                        End Session
                    </button>
                </div>
            </div>
        </div>
    );
};
