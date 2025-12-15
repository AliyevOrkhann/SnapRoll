using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Controllers;

/// <summary>
/// Controller for course management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ISessionService _sessionService;

    public CourseController(ICourseService courseService, ISessionService sessionService)
    {
        _courseService = courseService;
        _sessionService = sessionService;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    /// <summary>
    /// Creates a new course (Instructor/Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        try
        {
            var course = await _courseService.CreateCourseAsync(UserId, request);
            return Ok(course);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a course by ID.
    /// </summary>
    [HttpGet("{courseId:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid courseId)
    {
        var course = await _courseService.GetCourseAsync(courseId);
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }

        return Ok(course);
    }

    /// <summary>
    /// Gets all courses for the current user.
    /// Instructors see courses they teach, students see enrolled courses.
    /// </summary>
    [HttpGet("my-courses")]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseDto>>> GetMyCourses()
    {
        List<CourseDto> courses;

        if (UserRole == "Instructor" || UserRole == "Admin")
        {
            courses = await _courseService.GetInstructorCoursesAsync(UserId);
        }
        else
        {
            courses = await _courseService.GetStudentCoursesAsync(UserId);
        }

        return Ok(courses);
    }

    /// <summary>
    /// Gets all courses (Admin only).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseDto>>> GetAllCourses()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return Ok(courses);
    }

    /// <summary>
    /// Enrolls a student in a course (Admin/Instructor only).
    /// </summary>
    [HttpPost("enroll")]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnrollStudent([FromBody] EnrollStudentRequest request)
    {
        var result = await _courseService.EnrollStudentAsync(request);
        if (!result)
        {
            return BadRequest(new { message = "Student is already enrolled" });
        }

        return Ok(new { message = "Student enrolled successfully" });
    }

    /// <summary>
    /// Bulk enrolls students in a course (Admin/Instructor only).
    /// </summary>
    [HttpPost("enroll/bulk")]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkEnrollStudents([FromBody] BulkEnrollRequest request)
    {
        var count = await _courseService.BulkEnrollStudentsAsync(request);
        return Ok(new { message = $"{count} students enrolled successfully" });
    }

    /// <summary>
    /// Removes a student from a course (Admin/Instructor only).
    /// </summary>
    [HttpDelete("{courseId:guid}/students/{studentId}")]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnenrollStudent(Guid courseId, string studentId)
    {
        var result = await _courseService.UnenrollStudentAsync(courseId, studentId);
        if (!result)
        {
            return NotFound(new { message = "Enrollment not found" });
        }

        return Ok(new { message = "Student unenrolled successfully" });
    }

    /// <summary>
    /// Gets enrolled students for a course (Instructor/Admin only).
    /// </summary>
    [HttpGet("{courseId:guid}/students")]
    [Authorize(Roles = "Instructor,Admin")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetEnrolledStudents(Guid courseId)
    {
        var students = await _courseService.GetEnrolledStudentsAsync(courseId);
        return Ok(students);
    }

    /// <summary>
    /// Allows a student to enroll themselves into a course using a shared link.
    /// </summary>
    /// <param name="courseId">The course identifier from the invite link.</param>
    [HttpPost("{courseId:guid}/join")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinCourse(Guid courseId)
    {
        var result = await _courseService.EnrollStudentAsync(new EnrollStudentRequest
        {
            CourseId = courseId,
            StudentId = UserId
        });

        if (!result)
        {
            return BadRequest(new { message = "You are already enrolled in this course" });
        }

        return Ok(new { message = "Enrolled in course successfully" });
    }

    /// <summary>
    /// Gets sessions for a course.
    /// </summary>
    [HttpGet("{courseId:guid}/sessions")]
    [ProducesResponseType(typeof(List<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SessionResponse>>> GetCourseSessions(Guid courseId)
    {
        var sessions = await _sessionService.GetCourseSessionsAsync(courseId);
        return Ok(sessions);
    }

    [Authorize(Roles = "Instructor")]
[HttpPost("add-student-by-email")]
public async Task<IActionResult> AddStudentByEmail(
    [FromBody] AddStudentByEmailRequest request)
{
    var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(instructorId))
        return Unauthorized();

    await _courseService.AddStudentByEmailAsync(
        request.CourseId,
        request.StudentEmail,
        instructorId);

    return Ok(new { message = "Student added successfully." });
}
}
