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
    /// Registers a new user.
    /// </summary>
    Task<LoginResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Gets user information by ID.
    /// </summary>
    Task<UserDto?> GetUserAsync(string userId);

    /// <summary>
    /// Validates a JWT token and returns the user ID.
    /// </summary>
    Task<string?> ValidateTokenAsync(string token);
}
