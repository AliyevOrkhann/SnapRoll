import React, { useEffect, useState } from 'react';
import { X, Calendar, Clock, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import api from '../../api/axios';

export const AttendanceHistoryModal = ({ isOpen, onClose, course }) => {
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (isOpen && course) {
            fetchHistory();
        }
    }, [isOpen, course]);

    const fetchHistory = async () => {
        try {
            setLoading(true);
            const response = await api.get(`/Attendance/my-history/${course.id}`);
            setHistory(response.data);
        } catch (error) {
            console.error('Failed to fetch attendance history:', error);
        } finally {
            setLoading(false);
        }
    };

    if (!isOpen) return null;

    const getStatusIcon = (status) => {
        switch (status) {
            case 1: // Present
                return <CheckCircle className="h-5 w-5 text-green-500" />;
            case 2: // Late
                return <Clock className="h-5 w-5 text-yellow-500" />;
            case 3: // Absent
                return <XCircle className="h-5 w-5 text-red-500" />;
            default: // Pending or Unknown
                return <AlertCircle className="h-5 w-5 text-gray-400" />;
        }
    };

    const getStatusText = (status) => {
        switch (status) {
            case 1: return 'Present';
            case 2: return 'Late';
            case 3: return 'Absent';
            default: return 'Pending';
        }
    };

    const getStatusColor = (status) => {
        switch (status) {
            case 1: return 'bg-green-100 text-green-800';
            case 2: return 'bg-yellow-100 text-yellow-800';
            case 3: return 'bg-red-100 text-red-800';
            default: return 'bg-gray-100 text-gray-800';
        }
    };

    // Calculate stats
    const total = history.length;
    const present = history.filter(h => h.status === 1 || h.status === 2).length;
    const percentage = total > 0 ? Math.round((present / total) * 100) : 0;

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
            <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
                <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={onClose}></div>

                <div className="relative inline-block w-full max-w-2xl p-6 overflow-hidden text-left align-middle transition-all transform bg-white shadow-xl rounded-2xl">
                    <div className="flex justify-between items-start mb-6">
                        <div>
                            <h3 className="text-lg leading-6 font-medium text-gray-900">
                                Attendance History
                            </h3>
                            <p className="text-sm text-gray-500 mt-1">
                                {course?.courseCode} — {course?.name}
                            </p>
                        </div>
                        <div className="bg-indigo-50 px-3 py-1 rounded-full">
                            <span className="text-sm font-semibold text-indigo-700">
                                {percentage}% Attendance
                            </span>
                        </div>
                    </div>

                    <div className="mt-2 text-sm text-gray-500 mb-4">
                        <div className="flow-root">
                            <ul className="-my-5 divide-y divide-gray-200">
                                {loading ? (
                                    <div className="py-5 text-center">Loading...</div>
                                ) : history.length === 0 ? (
                                    <div className="py-5 text-center">No sessions found.</div>
                                ) : (
                                    history.map((record) => (
                                        <li key={record.sessionId} className="py-4">
                                            <div className="flex items-center space-x-4">
                                                <div className="flex-shrink-0">
                                                    {getStatusIcon(record.status)}
                                                </div>
                                                <div className="flex-1 min-w-0">
                                                    <p className="text-sm font-medium text-gray-900 truncate">
                                                        {new Date(record.startTime).toLocaleDateString(undefined, {
                                                            weekday: 'long',
                                                            year: 'numeric',
                                                            month: 'long',
                                                            day: 'numeric'
                                                        })}
                                                    </p>
                                                    <p className="text-sm text-gray-500">
                                                        {new Date(record.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                                        {record.scannedAt && (
                                                            <span className="ml-2 text-xs">
                                                                (Scanned: {new Date(record.scannedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })})
                                                            </span>
                                                        )}
                                                    </p>
                                                </div>
                                                <div>
                                                    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(record.status)}`}>
                                                        {getStatusText(record.status)}
                                                    </span>
                                                </div>
                                            </div>
                                        </li>
                                    ))
                                )}
                            </ul>
                        </div>
                    </div>

                    <div className="mt-6 flex justify-end">
                        <button
                            type="button"
                            className="inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none sm:text-sm"
                            onClick={onClose}
                        >
                            Close
                        </button>
                    </div>

                    <button
                        className="absolute top-4 right-4 text-gray-400 hover:text-gray-500"
                        onClick={onClose}
                    >
                        <X className="h-6 w-6" />
                    </button>
                </div>
            </div>
        </div>
    );
};