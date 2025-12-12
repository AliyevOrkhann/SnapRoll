using SnapRoll.Application.DTOs;

namespace SnapRoll.Application.Interfaces;

/// <summary>
/// Interface for course management operations.
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Creates a new course.
    /// </summary>
    Task<CourseDto> CreateCourseAsync(string instructorId, CreateCourseRequest request);

    /// <summary>
    /// Gets course by ID.
    /// </summary>
    Task<CourseDto?> GetCourseAsync(Guid courseId);

    /// <summary>
    /// Gets all courses for an instructor.
    /// </summary>
    Task<List<CourseDto>> GetInstructorCoursesAsync(string instructorId);

    /// <summary>
    /// Gets all courses a student is enrolled in.
    /// </summary>
    Task<List<CourseDto>> GetStudentCoursesAsync(string studentId);

    /// <summary>
    /// Gets all courses (admin only).
    /// </summary>
    Task<List<CourseDto>> GetAllCoursesAsync();

    /// <summary>
    /// Enrolls a student in a course.
    /// </summary>
    Task<bool> EnrollStudentAsync(EnrollStudentRequest request);

    /// <summary>
    /// Bulk enrolls students in a course.
    /// </summary>
    Task<int> BulkEnrollStudentsAsync(BulkEnrollRequest request);

    /// <summary>
    /// Removes a student from a course.
    /// </summary>
    Task<bool> UnenrollStudentAsync(Guid courseId, string studentId);

    /// <summary>
    /// Gets enrolled students for a course.
    /// </summary>
    Task<List<UserDto>> GetEnrolledStudentsAsync(Guid courseId);
}
