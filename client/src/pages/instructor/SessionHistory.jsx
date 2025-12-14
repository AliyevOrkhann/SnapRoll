import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../../api/axios';
import { Download, Clock, Activity } from 'lucide-react';

export const SessionHistory = () => {
    const { courseId } = useParams();
    const [sessions, setSessions] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchHistory = async () => {
            try {
                const res = await api.get(`/Session/course/${courseId}/history`);
                setSessions(res.data || []);
            } catch (err) {
                console.error('Failed to fetch session history', err);
                setSessions([]);
            } finally {
                setLoading(false);
            }
        };
        fetchHistory();
    }, [courseId]);

    const downloadReport = async (session) => {
        try {
            const res = await api.get(`/Session/${session.id}/report`, { responseType: 'blob' });
            const url = window.URL.createObjectURL(new Blob([res.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', `attendance_${session.sessionCode || session.id}.csv`);
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(url);
        } catch (err) {
            alert(err.response?.data?.message || 'Failed to download report');
        }
    };

    if (loading) return <div className="p-6">Loading session history...</div>;

    return (
        <div className="space-y-6 p-6">
            <div className="flex items-center justify-between">
                <h2 className="text-xl font-semibold">Session History</h2>
                <Link to="/instructor" className="text-sm text-indigo-600 hover:underline">Back to Dashboard</Link>
            </div>

            {sessions.length === 0 ? (
                <div className="bg-white rounded-lg shadow-sm p-6 text-gray-500">No sessions found.</div>
            ) : (
                <div className="bg-white rounded-lg shadow-sm overflow-hidden">
                    <ul className="divide-y divide-gray-100">
                        {sessions.map(s => (
                            <li key={s.id} className="px-6 py-4 flex items-center justify-between">
                                <div>
                                    <div className="text-md font-medium text-gray-800">{s.courseName}</div>
                                    <div className="text-sm text-gray-500">{s.courseCode} • {s.isActive ? 'Active' : 'Closed'}</div>
                                    <div className="text-xs text-gray-400 mt-1">Started: {new Date(s.startTime).toLocaleString()} {s.endTime ? `· Ended: ${new Date(s.endTime).toLocaleString()}` : ''}</div>
                                </div>
                                <div className="flex items-center space-x-3">
                                    <div className="text-right text-sm text-gray-600">
                                        <div><strong className="text-gray-900">{s.presentCount}</strong> Present</div>
                                        <div><strong className="text-gray-900">{s.lateCount}</strong> Late</div>
                                        <div><strong className="text-gray-900">{s.absentCount}</strong> Absent</div>
                                    </div>
                                    <button
                                        onClick={() => downloadReport(s)}
                                        className="inline-flex items-center px-3 py-2 bg-indigo-50 text-indigo-700 rounded-md text-sm hover:bg-indigo-100"
                                    >
                                        <Download className="w-4 h-4 mr-2" /> Export CSV
                                    </button>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            )}
        </div>
    );
};
