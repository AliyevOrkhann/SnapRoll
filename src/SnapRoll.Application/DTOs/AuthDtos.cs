namespace SnapRoll.Application.DTOs;

/// <summary>
/// Login request with university credentials.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// University email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response with JWT token.
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User information DTO.
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UniversityId { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}

/// <summary>
/// Register a new user request.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UniversityId { get; set; } = string.Empty;
    public string UserType { get; set; } = "Student";
}
