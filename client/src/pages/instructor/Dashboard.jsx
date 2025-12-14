import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../../api/axios';
import { Plus, Users, ArrowRight, Play, LinkIcon } from 'lucide-react';
import { CreateCourseModal } from '../../components/Instructor/CreateCourseModal';
import StudentsModal from '../../components/Instructor/StudentsModal';

export const InstructorDashboard = () => {
    const [courses, setCourses] = useState([]);
    const [sessions, setSessions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [isStudentsOpen, setIsStudentsOpen] = useState(false);
    const [students, setStudents] = useState([]);
    const [studentsLoading, setStudentsLoading] = useState(false);
    const [selectedCourse, setSelectedCourse] = useState(null);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [coursesRes, sessionsRes] = await Promise.all([
                    api.get('/Course/my-courses'),
                    api.get('/Session/active')
                ]);
                setCourses(coursesRes.data);
                setSessions(sessionsRes.data);
            } catch (error) {
                console.error('Error fetching dashboard data:', error);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, []);

    const startSession = async (courseId) => {
        try {
            const response = await api.post('/Session/create', { courseId });
            window.location.href = `/instructor/session/${response.data.id}`;
        } catch (err) {
            alert(err.response?.data?.message || 'Failed to start session');
        }
    };

    const copyEnrollLink = (courseId) => {
        const origin = window.location.origin;
        const link = `${origin}/student/join?courseId=${courseId}`;

        if (navigator.clipboard?.writeText) {
            navigator.clipboard.writeText(link)
                .then(() => {
                    alert('Enrollment link copied to clipboard!');
                })
                .catch(() => {
                    alert('Failed to copy link. You can share this manually: ' + link);
                });
        } else {
            alert('Copy is not supported in this browser. Share this link: ' + link);
        }
    };

    const handleCourseCreated = (newCourse) => {
        setCourses([...courses, newCourse]);
    };

    const openStudents = async (course) => {
        setSelectedCourse(course);
        setIsStudentsOpen(true);
        setStudentsLoading(true);
        try {
            const res = await api.get(`/Course/${course.id}/students`);
            setStudents(res.data || []);
        } catch (err) {
            console.error('Failed to fetch students:', err);
            setStudents([]);
        } finally {
            setStudentsLoading(false);
        }
    };

    if (loading) return <div>Loading...</div>;

    return (
        <div className="space-y-6">
            <CreateCourseModal
                isOpen={isCreateModalOpen}
                onClose={() => setIsCreateModalOpen(false)}
                onCourseCreated={handleCourseCreated}
            />
            <StudentsModal
                isOpen={isStudentsOpen}
                onClose={() => setIsStudentsOpen(false)}
                students={students}
                loading={studentsLoading}
                course={selectedCourse}
            />

            {/* Active Sessions Section */}
            <section>
                <h2 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
                    <Play className="bg-green-100 text-green-600 rounded-full p-1 mr-2 h-7 w-7" />
                    Active Sessions
                </h2>

                {sessions.length === 0 ? (
                    <div className="bg-white rounded-lg shadow-sm p-6 text-center text-gray-500 border border-dashed border-gray-300">
                        No active sessions right now.
                    </div>
                ) : (
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                        {sessions.map(session => (
                            <Link
                                key={session.id}
                                to={`/instructor/session/${session.id}`}
                                className="block bg-white border border-green-200 rounded-lg shadow-sm hover:shadow-md transition-shadow p-6"
                            >
                                <div className="flex justify-between items-start">
                                    <div>
                                        <h3 className="text-lg font-medium text-gray-900">{session.courseName}</h3>
                                        <p className="text-sm text-gray-500">{session.courseCode}</p>
                                    </div>
                                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 animate-pulse">
                                        Live
                                    </span>
                                </div>
                                <div className="mt-4 flex items-center justify-between text-sm text-gray-500">
                                    <span>Started {new Date(session.startTime).toLocaleTimeString()}</span>
                                    <div className="flex items-center">
                                        <span className="font-bold text-gray-900 mr-1">{session.presentCount}</span> Present
                                    </div>
                                </div>
                            </Link>
                        ))}
                    </div>
                )}
            </section>

            {/* Courses Section */}
            <section>
                <div className="flex items-center justify-between mb-4">
                    <h2 className="text-lg font-medium text-gray-900 flex items-center">
                        <Users className="bg-indigo-100 text-indigo-600 rounded-full p-1 mr-2 h-7 w-7" />
                        Your Courses
                    </h2>
                    <button
                        onClick={() => setIsCreateModalOpen(true)}
                        className="inline-flex items-center px-3 py-2 border border-transparent shadow-sm text-sm leading-4 font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                    >
                        <Plus className="h-4 w-4 mr-1" /> New Course
                    </button>
                </div>

                <div className="bg-white shadow overflow-hidden sm:rounded-md">
                    <ul className="divide-y divide-gray-200">
                        {courses.map(course => (
                            <li key={course.id}>
                                <div className="px-4 py-4 sm:px-6 hover:bg-gray-50 flex items-center justify-between">
                                    <div className="flex items-center">
                                        <div className="flex-shrink-0 h-10 w-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600 font-bold">
                                            {course.courseCode.substring(0, 2)}
                                        </div>
                                        <div className="ml-4">
                                            <h3 className="text-lg font-medium text-indigo-600 truncate">{course.courseCode}</h3>
                                            <p className="text-sm text-gray-500">{course.name}</p>
                                        </div>
                                    </div>
                                    <div className="flex items-center space-x-4">
                                        <div className="text-sm text-gray-500 hidden sm:block">
                                            {course.enrolledStudentCount} Students
                                        </div>
                                        <button
                                            type="button"
                                            onClick={() => openStudents(course)}
                                            className="inline-flex items-center px-3 py-1.5 border text-xs font-medium rounded-full text-indigo-700 bg-white hover:bg-gray-50 focus:outline-none mr-2"
                                        >
                                            <Users className="h-3 w-3 mr-1 text-indigo-600" />
                                            Students
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => copyEnrollLink(course.id)}
                                            className="inline-flex items-center px-3 py-1.5 border text-xs font-medium rounded-full text-indigo-700 bg-indigo-50 hover:bg-indigo-100 focus:outline-none"
                                        >
                                            <LinkIcon className="h-3 w-3 mr-1" />
                                            Share Link
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => startSession(course.id)}
                                            className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-full shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none"
                                        >
                                            Start Session <ArrowRight className="ml-1 h-3 w-3" />
                                        </button>
                                    </div>
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            </section>
        </div>
    );
};
