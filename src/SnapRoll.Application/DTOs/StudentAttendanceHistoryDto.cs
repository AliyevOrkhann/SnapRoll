using SnapRoll.Domain.Enums;

namespace SnapRoll.Application.DTOs;

public class StudentAttendanceHistoryDto
{
    public Guid SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime? ScannedAt { get; set; }
    public bool IsActive { get; set; }
}