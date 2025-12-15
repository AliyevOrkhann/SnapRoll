using SnapRoll.Application.DTOs;

namespace SnapRoll.Application.Interfaces;

/// <summary>
/// Interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user and sends verification email.
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Verifies user email with the provided token.
    /// </summary>
    Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request);

    /// <summary>
    /// Resends verification email to the user.
    /// </summary>
    Task<VerifyEmailResponse> ResendVerificationEmailAsync(string email);

    /// <summary>
    /// Gets user information by ID.
    /// </summary>
    Task<UserDto?> GetUserAsync(string userId);

    /// <summary>
    /// Validates a JWT token and returns the user ID.
    /// </summary>
    Task<string?> ValidateTokenAsync(string token);
}
