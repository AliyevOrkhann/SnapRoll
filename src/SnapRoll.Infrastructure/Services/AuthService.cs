using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnapRoll.Application.Configuration;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;
using SnapRoll.Domain.Entities;
using SnapRoll.Domain.Enums;

namespace SnapRoll.Infrastructure.Services;

/// <summary>
/// Service for authentication and JWT token management.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly SmtpSettings _smtpSettings;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IOptions<JwtSettings> jwtSettings,
        IOptions<SmtpSettings> smtpSettings,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _smtpSettings = smtpSettings.Value;
        _emailService = emailService;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        // Check if email is verified
        if (!user.EmailConfirmed)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Please verify your email before logging in. Check your inbox for the verification link.",
                RequiresEmailVerification = true
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToUserDto(user)
        };
    }

    /// <summary>
    /// Registers a new user and sends verification email.
    /// </summary>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new RegisterResponse
            {
                Success = false,
                ErrorMessage = "A user with this email already exists"
            };
        }

        // Parse user type
        if (!Enum.TryParse<UserType>(request.UserType, ignoreCase: true, out var userType))
        {
            userType = UserType.Student;
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UniversityId = request.UniversityId,
            UserType = userType,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new RegisterResponse
            {
                Success = false,
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Add role claim
        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, userType.ToString()));

        // Generate email confirmation token and send email
        await SendVerificationEmailAsync(user);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful! Please check your email to verify your account.",
            UserId = user.Id
        };
    }

    /// <summary>
    /// Verifies user email with the provided token.
    /// </summary>
    public async Task<VerifyEmailResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                ErrorMessage = "Invalid verification link"
            };
        }

        if (user.EmailConfirmed)
        {
            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email is already verified. You can log in."
            };
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return new VerifyEmailResponse
            {
                Success = false,
                ErrorMessage = "Invalid or expired verification token. Please request a new verification email."
            };
        }

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Email verified successfully! You can now log in."
        };
    }

    /// <summary>
    /// Resends verification email to the user.
    /// </summary>
    public async Task<VerifyEmailResponse> ResendVerificationEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal if user exists
            return new VerifyEmailResponse
            {
                Success = true,
                Message = "If an account with this email exists, a verification email has been sent."
            };
        }

        if (user.EmailConfirmed)
        {
            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email is already verified. You can log in."
            };
        }

        await SendVerificationEmailAsync(user);

        return new VerifyEmailResponse
        {
            Success = true,
            Message = "Verification email sent. Please check your inbox."
        };
    }

    private async Task SendVerificationEmailAsync(AppUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = HttpUtility.UrlEncode(token);
        var verificationLink = $"{_smtpSettings.FrontendBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={encodedToken}";

        var subject = "Verify your SnapRoll account";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4F46E5;'>Welcome to SnapRoll!</h1>
                    <p>Hi {user.FullName},</p>
                    <p>Thank you for registering with SnapRoll. Please verify your email address by clicking the button below:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}' 
                           style='background-color: #4F46E5; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Verify Email
                        </a>
                    </div>
                    <p>Or copy and paste this link into your browser:</p>
                    <p style='word-break: break-all; color: #6B7280;'>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 30px 0;'>
                    <p style='color: #6B7280; font-size: 12px;'>If you didn't create an account with SnapRoll, please ignore this email.</p>
                </div>
            </body>
            </html>";

        await _emailService.SendEmailAsync(user.Email!, subject, body);
    }

    /// <summary>
    /// Gets user information by ID.
    /// </summary>
    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }

    /// <summary>
    /// Validates a JWT token and returns the user ID.
    /// </summary>
    public Task<string?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return Task.FromResult(userId);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private string GenerateJwtToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.FullName),
            new("UniversityId", user.UniversityId),
            new(ClaimTypes.Role, user.UserType.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToUserDto(AppUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UniversityId = user.UniversityId,
            UserType = user.UserType.ToString()
        };
    }
}
