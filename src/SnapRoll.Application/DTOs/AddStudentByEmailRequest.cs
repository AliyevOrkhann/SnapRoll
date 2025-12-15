namespace SnapRoll.Application.DTOs;

public class AddStudentByEmailRequest
{
    public Guid CourseId { get; set; }
    public string StudentEmail { get; set; } = string.Empty;
}