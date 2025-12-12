namespace SnapRoll.Domain.Enums;

/// <summary>
/// Defines the type of user in the system.
/// </summary>
public enum UserType
{
    /// <summary>
    /// A student who attends classes and scans QR codes.
    /// </summary>
    Student = 0,

    /// <summary>
    /// An instructor who creates sessions and displays QR codes.
    /// </summary>
    Instructor = 1,

    /// <summary>
    /// An administrator who can manage courses, users, and enrollments.
    /// </summary>
    Admin = 2
}
