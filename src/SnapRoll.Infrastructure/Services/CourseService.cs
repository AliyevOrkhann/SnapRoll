using Microsoft.EntityFrameworkCore;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;
using SnapRoll.Domain.Entities;
using SnapRoll.Domain.Enums;
using SnapRoll.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace SnapRoll.Infrastructure.Services;

/// <summary>
/// Service for course and enrollment management.
/// </summary>
public class CourseService : ICourseService
{
    private readonly SnapRollDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public CourseService(SnapRollDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }   

    /// <summary>
    /// Creates a new course.
    /// </summary>
    public async Task<CourseDto> CreateCourseAsync(string instructorId, CreateCourseRequest request)
    {
        // Check if course code already exists
        var existingCourse = await _context.Courses
            .AnyAsync(c => c.CourseCode == request.CourseCode);

        if (existingCourse)
            throw new InvalidOperationException($"A course with code '{request.CourseCode}' already exists");

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CourseCode = request.CourseCode,
            Name = request.Name,
            Description = request.Description,
            InstructorId = instructorId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return await MapToDtoAsync(course);
    }

    /// <summary>
    /// Gets course by ID.
    /// </summary>
    public async Task<CourseDto?> GetCourseAsync(Guid courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        return course != null ? await MapToDtoAsync(course) : null;
    }

    /// <summary>
    /// Gets all courses for an instructor.
    /// </summary>
    public async Task<List<CourseDto>> GetInstructorCoursesAsync(string instructorId)
    {
        var courses = await _context.Courses
            .Include(c => c.Instructor)
            .Where(c => c.InstructorId == instructorId)
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        var dtos = new List<CourseDto>();
        foreach (var course in courses)
        {
            dtos.Add(await MapToDtoAsync(course));
        }
        return dtos;
    }

    /// <summary>
    /// Gets all courses a student is enrolled in.
    /// </summary>
    public async Task<List<CourseDto>> GetStudentCoursesAsync(string studentId)
    {
        var courseIds = await _context.CourseEnrollments
            .Where(e => e.StudentId == studentId && e.IsActive)
            .Select(e => e.CourseId)
            .ToListAsync();

        var courses = await _context.Courses
            .Include(c => c.Instructor)
            .Where(c => courseIds.Contains(c.Id))
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        var dtos = new List<CourseDto>();
        foreach (var course in courses)
        {
            dtos.Add(await MapToDtoAsync(course));
        }
        return dtos;
    }

    /// <summary>
    /// Gets all courses (admin only).
    /// </summary>
    public async Task<List<CourseDto>> GetAllCoursesAsync()
    {
        var courses = await _context.Courses
            .Include(c => c.Instructor)
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        var dtos = new List<CourseDto>();
        foreach (var course in courses)
        {
            dtos.Add(await MapToDtoAsync(course));
        }
        return dtos;
    }

    /// <summary>
    /// Enrolls a student in a course.
    /// </summary>
    public async Task<bool> EnrollStudentAsync(EnrollStudentRequest request)
    {
        // Check if already enrolled
        var existingEnrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.StudentId == request.StudentId);

        if (existingEnrollment != null)
        {
            if (existingEnrollment.IsActive)
                return false; // Already enrolled

            // Reactivate enrollment
            existingEnrollment.IsActive = true;
            existingEnrollment.EnrolledAt = DateTime.UtcNow;
        }
        else
        {
            var enrollment = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                StudentId = request.StudentId,
                EnrolledAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.CourseEnrollments.Add(enrollment);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Bulk enrolls students in a course.
    /// </summary>
    public async Task<int> BulkEnrollStudentsAsync(BulkEnrollRequest request)
    {
        var enrolledCount = 0;

        foreach (var studentId in request.StudentIds)
        {
            var enrolled = await EnrollStudentAsync(new EnrollStudentRequest
            {
                CourseId = request.CourseId,
                StudentId = studentId
            });

            if (enrolled) enrolledCount++;
        }

        return enrolledCount;
    }

    /// <summary>
    /// Removes a student from a course.
    /// </summary>
    public async Task<bool> UnenrollStudentAsync(Guid courseId, string studentId)
    {
        var enrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId && e.IsActive);

        if (enrollment == null)
            return false;

        enrollment.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets enrolled students for a course.
    /// </summary>
    public async Task<List<UserDto>> GetEnrolledStudentsAsync(Guid courseId)
    {
        return await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId && e.IsActive)
            .Include(e => e.Student)
            .Select(e => new UserDto
            {
                Id = e.StudentId,
                Email = e.Student.Email ?? "",
                FullName = e.Student.FullName,
                UniversityId = e.Student.UniversityId,
                UserType = e.Student.UserType.ToString()
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    private async Task<CourseDto> MapToDtoAsync(Course course)
    {
        var enrolledCount = await _context.CourseEnrollments
            .CountAsync(e => e.CourseId == course.Id && e.IsActive);

        return new CourseDto
        {
            Id = course.Id,
            CourseCode = course.CourseCode,
            Name = course.Name,
            Description = course.Description,
            InstructorId = course.InstructorId,
            InstructorName = course.Instructor?.FullName ?? "",
            EnrolledStudentCount = enrolledCount,
            IsActive = course.IsActive,
            CreatedAt = course.CreatedAt
        };
    }
    public async Task AddStudentByEmailAsync(
    Guid courseId,
    string studentEmail,
    string instructorId)
{
    // 1. Verify course exists and belongs to instructor
    var course = await _context.Courses
        .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == instructorId);

    if (course == null)
        throw new InvalidOperationException("Course not found or access denied.");

    // 2. Find student by email
    var student = await _userManager.FindByEmailAsync(studentEmail);

    if (student == null)
        throw new InvalidOperationException("No student found with this email.");

    // 3. Check user type
    if (student.UserType != UserType.Student)
        throw new InvalidOperationException("User is not a student.");

    // 4. Check existing enrollment
    var existingEnrollment = await _context.CourseEnrollments
        .FirstOrDefaultAsync(e =>
            e.CourseId == courseId &&
            e.StudentId == student.Id);

    if (existingEnrollment != null)
    {
        if (existingEnrollment.IsActive)
            throw new InvalidOperationException("Student already enrolled.");

        // Reactivate enrollment
        existingEnrollment.IsActive = true;
        existingEnrollment.EnrolledAt = DateTime.UtcNow;
    }
    else
    {
        // 5. Create enrollment
        var enrollment = new CourseEnrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = student.Id,
            EnrolledAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.CourseEnrollments.Add(enrollment);
    }

    await _context.SaveChangesAsync();
}
}
