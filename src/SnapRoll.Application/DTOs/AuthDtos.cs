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
    public bool RequiresEmailVerification { get; set; }
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

/// <summary>
/// Registration response (doesn't include token until email is verified).
/// </summary>
public class RegisterResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Email verification request.
/// </summary>
public class VerifyEmailRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Email verification response.
/// </summary>
public class VerifyEmailResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Resend verification email request.
/// </summary>
public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

