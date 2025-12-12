namespace SnapRoll.Application.DTOs;

/// <summary>
/// Course information DTO.
/// </summary>
public class CourseDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string InstructorId { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public int EnrolledStudentCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create course request.
/// </summary>
public class CreateCourseRequest
{
    public string CourseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Enroll student request.
/// </summary>
public class EnrollStudentRequest
{
    public Guid CourseId { get; set; }
    public string StudentId { get; set; } = string.Empty;
}

/// <summary>
/// Bulk enroll students request.
/// </summary>
public class BulkEnrollRequest
{
    public Guid CourseId { get; set; }
    public List<string> StudentIds { get; set; } = new();
}
