import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../../api/axios';
import { Calendar, CheckCircle, XCircle, Clock, ArrowRight, QrCode, ClipboardList } from 'lucide-react';
import { AttendanceHistoryModal } from '../../components/Student/AttendanceHistoryModal';

export const StudentHome = () => {
    const [courses, setCourses] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedCourse, setSelectedCourse] = useState(null);
    const [isHistoryOpen, setIsHistoryOpen] = useState(false);

    useEffect(() => {
        const fetchCourses = async () => {
            try {
                const response = await api.get('/Course/my-courses');
                setCourses(response.data);
            } catch (err) {
                console.error(err);
            } finally {
                setLoading(false);
            }
        };
        fetchCourses();
    }, []);

    const openHistory = (course) => {
        setSelectedCourse(course);
        setIsHistoryOpen(true);
    };

    if (loading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <AttendanceHistoryModal
                isOpen={isHistoryOpen}
                onClose={() => setIsHistoryOpen(false)}
                course={selectedCourse}
            />

            {/* Hero Section / Scan Action */}
            <div className="bg-gradient-to-r from-indigo-600 to-purple-600 rounded-2xl shadow-lg p-6 text-white text-center sm:text-left sm:flex sm:items-center sm:justify-between">
                <div>
                    <h2 className="text-2xl font-bold mb-2">Ready to mark attendance?</h2>
                    <p className="text-indigo-100">Scan the QR code displayed on the classroom screen.</p>
                </div>
                <Link
                    to="/student/scan"
                    className="mt-4 sm:mt-0 inline-flex items-center px-6 py-3 border border-transparent shadow-sm text-base font-medium rounded-full text-indigo-600 bg-white hover:bg-indigo-50"
                >
                    <QrCode className="mr-2 h-5 w-5" />
                    Scan Now
                </Link>
            </div>

            {/* Course List */}
            <div>
                <h3 className="text-lg font-medium text-gray-900 mb-4 px-2">My Courses</h3>
                <div className="bg-white shadow overflow-hidden sm:rounded-lg">
                    <ul className="divide-y divide-gray-200">
                        {courses.map(course => (
                            <li key={course.id}>
                                <div className="px-4 py-4 sm:px-6 hover:bg-gray-50 transition">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center">
                                            <div className="flex-shrink-0 h-10 w-10 rounded-lg bg-gray-100 flex items-center justify-center text-gray-600 font-bold">
                                                {course.courseCode.substring(0, 3)}
                                            </div>
                                            <div className="ml-4">
                                                <p className="text-sm font-medium text-indigo-600 truncate">{course.courseCode}</p>
                                                <p className="text-base text-gray-900 font-semibold">{course.name}</p>
                                            </div>
                                        </div>
                                    </div>
                                    <div className="mt-4 sm:flex sm:justify-between sm:items-center">
                                        <div className="sm:flex">
                                            <p className="flex items-center text-sm text-gray-500">
                                                Instructor: {course.instructorName}
                                            </p>
                                        </div>
                                        <button
                                            onClick={() => openHistory(course)}
                                            className="mt-2 sm:mt-0 inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-full text-indigo-700 bg-indigo-100 hover:bg-indigo-200 focus:outline-none"
                                        >
                                            <ClipboardList className="mr-1.5 h-4 w-4" />
                                            View Attendance
                                        </button>
                                    </div>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
};