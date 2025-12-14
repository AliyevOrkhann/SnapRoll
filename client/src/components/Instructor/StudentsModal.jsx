import React from 'react';
import { X, Users } from 'lucide-react';

export const StudentsModal = ({ isOpen, onClose, students = [], loading = false, course }) => {
    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
            <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
                <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" onClick={onClose}></div>

                <div className="relative inline-block w-full max-w-2xl p-6 overflow-hidden text-left align-middle transition-all transform bg-white shadow-xl rounded-2xl">
                    <div className="sm:flex sm:items-start">
                        <div className="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-indigo-100 sm:mx-0 sm:h-10 sm:w-10">
                            <Users className="h-6 w-6 text-indigo-600" />
                        </div>
                        <div className="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
                            <h3 className="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                                Enrolled Students
                            </h3>
                            <p className="text-sm text-gray-500 mt-1">{course ? `${course.courseCode} — ${course.name}` : ''}</p>

                            <div className="mt-4">
                                {loading ? (
                                    <div className="text-sm text-gray-600">Loading students...</div>
                                ) : students.length === 0 ? (
                                    <div className="text-sm text-gray-600">No students enrolled yet.</div>
                                ) : (
                                    <ul className="divide-y divide-gray-100 max-h-80 overflow-auto">
                                        {students.map(s => (
                                            <li key={s.id} className="py-3 flex items-center justify-between">
                                                <div>
                                                    <div className="text-sm font-medium text-gray-900">{s.fullName}</div>
                                                    <div className="text-xs text-gray-500">University ID: {s.universityId}</div>
                                                </div>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </div>
                        </div>
                    </div>

                    <div className="mt-5 sm:mt-4 sm:flex sm:flex-row-reverse">
                        <button
                            type="button"
                            className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none sm:ml-3 sm:w-auto sm:text-sm"
                            onClick={onClose}
                        >
                            Close
                        </button>
                    </div>
                </div>

                <button
                    className="absolute top-4 right-4 z-50 p-2 text-white bg-black/20 rounded-full hover:bg-black/30 sm:text-gray-400 sm:bg-transparent sm:hover:text-gray-500"
                    onClick={onClose}
                >
                    <X className="h-6 w-6" />
                </button>
            </div>
        </div>
    );
};

export default StudentsModal;
