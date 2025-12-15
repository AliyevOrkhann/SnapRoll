using Microsoft.AspNetCore.Mvc;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;

namespace SnapRoll.API.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user with university email and password.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>JWT token and user information.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Registers a new user and sends verification email.
    /// </summary>
    /// <param name="request">Registration details.</param>
    /// <returns>Success message instructing user to verify email.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Verifies user email address.
    /// </summary>
    /// <param name="request">User ID and verification token.</param>
    /// <returns>Verification result.</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Resends verification email.
    /// </summary>
    /// <param name="request">Email address.</param>
    /// <returns>Result message.</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(VerifyEmailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifyEmailResponse>> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var result = await _authService.ResendVerificationEmailAsync(request.Email);
        return Ok(result);
    }
}
