import React, { useState } from 'react';
import { X, Users, Plus, Loader2 } from 'lucide-react';
import api from '../../api/axios';

export const StudentsModal = ({ isOpen, onClose, students = [], loading = false, course, onStudentAdded }) => {
    const [email, setEmail] = useState('');
    const [adding, setAdding] = useState(false);
    const [message, setMessage] = useState(null); // { type: 'success' | 'error', text: string }

    if (!isOpen) return null;

    const handleAddStudent = async (e) => {
        e.preventDefault();
        if (!email || !course) return;

        setAdding(true);
        setMessage(null);

        try {
            await api.post('/Course/add-student-by-email', {
                courseId: course.id,
                studentEmail: email
            });

            setMessage({ type: 'success', text: 'Student added successfully!' });
            setEmail('');
            if (onStudentAdded) onStudentAdded();
        } catch (error) {
            setMessage({
                type: 'error',
                text: error.response?.data?.message || 'Failed to add student. Ensure email is correct and user is registered.'
            });
        } finally {
            setAdding(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
            <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
                <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" onClick={onClose}></div>

                <div className="relative inline-block w-full max-w-2xl p-6 overflow-hidden text-left align-middle transition-all transform bg-white shadow-xl rounded-2xl">
                    <div className="sm:flex sm:items-start mb-6">
                        <div className="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-indigo-100 sm:mx-0 sm:h-10 sm:w-10">
                            <Users className="h-6 w-6 text-indigo-600" />
                        </div>
                        <div className="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
                            <h3 className="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                                Enrolled Students
                            </h3>
                            <p className="text-sm text-gray-500 mt-1">{course ? `${course.courseCode} — ${course.name}` : ''}</p>
                        </div>
                    </div>

                    {/* Add Student Form */}
                    <div className="mb-6 bg-gray-50 p-4 rounded-lg">
                        <h4 className="text-sm font-medium text-gray-900 mb-2">Add Student by Email</h4>
                        <form onSubmit={handleAddStudent} className="flex gap-2">
                            <input
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                placeholder="Enter student email"
                                className="flex-1 shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 rounded-md p-2 border"
                                required
                            />
                            <button
                                type="submit"
                                disabled={adding || !email}
                                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                            >
                                {adding ? <Loader2 className="animate-spin h-4 w-4" /> : <Plus className="h-4 w-4 mr-1" />}
                                Add
                            </button>
                        </form>
                        {message && (
                            <p className={`mt-2 text-sm ${message.type === 'success' ? 'text-green-600' : 'text-red-600'}`}>
                                {message.text}
                            </p>
                        )}
                    </div>

                    {/* Students List */}
                    <div className="mt-4">
                        <h4 className="text-sm font-medium text-gray-900 mb-3">Class List ({students.length})</h4>
                        {loading ? (
                            <div className="text-sm text-gray-600 flex items-center justify-center py-4">
                                <Loader2 className="animate-spin h-5 w-5 mr-2 text-indigo-600" /> Loading students...
                            </div>
                        ) : students.length === 0 ? (
                            <div className="text-sm text-gray-500 italic text-center py-4 border border-dashed rounded-lg">
                                No students enrolled yet.
                            </div>
                        ) : (
                            <ul className="divide-y divide-gray-100 max-h-60 overflow-auto border rounded-md">
                                {students.map(s => (
                                    <li key={s.id} className="py-3 px-4 flex items-center justify-between hover:bg-gray-50">
                                        <div>
                                            <div className="text-sm font-medium text-gray-900">{s.fullName}</div>
                                            <div className="text-xs text-gray-500">{s.email}</div>
                                        </div>
                                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                            Student
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>

                    <div className="mt-6 sm:mt-6 sm:flex sm:flex-row-reverse">
                        <button
                            type="button"
                            className="w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none sm:ml-3 sm:w-auto sm:text-sm"
                            onClick={onClose}
                        >
                            Close
                        </button>
                    </div>

                    <button
                        className="absolute top-4 right-4 z-50 p-2 text-gray-400 hover:text-gray-500"
                        onClick={onClose}
                    >
                        <X className="h-6 w-6" />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default StudentsModal;
