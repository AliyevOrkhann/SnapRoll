using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SnapRoll.Application.Configuration;
using SnapRoll.Application.DTOs;
using SnapRoll.Application.Interfaces;
using SnapRoll.Domain.Enums;

namespace SnapRoll.Application.Services;

/// <summary>
/// Cryptographically secure QR token engine.
/// Generates and validates time-based tokens with HMACSHA256 signatures.
/// </summary>
public class QrEngine : IQrEngine
{
    private readonly QrSettings _settings;
    private readonly ConcurrentDictionary<Guid, QrTokenPayload> _currentTokens = new();
    private readonly ConcurrentDictionary<Guid, long> _sequenceNumbers = new();

    public QrEngine(IOptions<QrSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Generates a new cryptographically secure QR token.
    /// Token format: Base64(sessionId|timestamp|nonce|signature)
    /// </summary>
    public QrTokenPayload GenerateToken(Guid sessionId)
    {
        var timestamp = DateTime.UtcNow;
        var nonce = GenerateNonce();
        var sequence = _sequenceNumbers.AddOrUpdate(sessionId, 1, (_, s) => s + 1);

        // Create the payload
        var payloadData = $"{sessionId}|{timestamp:O}|{nonce}";
        var signature = ComputeSignature(payloadData);
        var fullToken = $"{payloadData}|{signature}";
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullToken));

        var payload = new QrTokenPayload
        {
            Token = encodedToken,
            SessionId = sessionId,
            GeneratedAt = timestamp,
            ExpiresAt = timestamp.AddSeconds(_settings.TotalValidWindowSeconds),
            Sequence = sequence
        };

        // Cache the current token
        _currentTokens.AddOrUpdate(sessionId, payload, (_, _) => payload);

        return payload;
    }

    /// <summary>
    /// Validates a scanned QR token.
    /// Checks: Signature, TTL (2-4 second window), Session match.
    /// </summary>
    public TokenValidationResult ValidateToken(string token, Guid expectedSessionId)
    {
        try
        {
            // Decode the token
            var decodedBytes = Convert.FromBase64String(token);
            var decodedToken = Encoding.UTF8.GetString(decodedBytes);
            var parts = decodedToken.Split('|');

            if (parts.Length != 4)
            {
                return TokenValidationResult.Failure(ScanResult.Invalid, "Malformed token");
            }

            var sessionIdPart = parts[0];
            var timestampPart = parts[1];
            var noncePart = parts[2];
            var signaturePart = parts[3];

            // Validate session ID format
            if (!Guid.TryParse(sessionIdPart, out var tokenSessionId))
            {
                return TokenValidationResult.Failure(ScanResult.Invalid, "Invalid session ID in token");
            }

            // Verify session matches
            if (tokenSessionId != expectedSessionId)
            {
                return TokenValidationResult.Failure(ScanResult.SessionInvalid, "Token is for a different session");
            }

            // Validate timestamp format
            if (!DateTime.TryParse(timestampPart, out var tokenTimestamp))
            {
                return TokenValidationResult.Failure(ScanResult.Invalid, "Invalid timestamp in token");
            }

            // Verify signature
            var payloadData = $"{sessionIdPart}|{timestampPart}|{noncePart}";
            var expectedSignature = ComputeSignature(payloadData);
            
            if (!ConstantTimeEquals(signaturePart, expectedSignature))
            {
                return TokenValidationResult.Failure(ScanResult.Invalid, "Invalid token signature");
            }

            // Check TTL - token must be within the valid window
            var now = DateTime.UtcNow;
            var tokenAge = now - tokenTimestamp;
            
            if (tokenAge.TotalSeconds > _settings.TotalValidWindowSeconds)
            {
                return TokenValidationResult.Failure(ScanResult.Expired, 
                    $"Token expired. Age: {tokenAge.TotalSeconds:F1}s, Max: {_settings.TotalValidWindowSeconds}s");
            }

            // Check if token is from the future (clock skew protection)
            if (tokenAge.TotalSeconds < -1) // Allow 1 second of clock skew
            {
                return TokenValidationResult.Failure(ScanResult.Invalid, "Token timestamp is in the future");
            }

            return TokenValidationResult.Success(tokenSessionId, tokenTimestamp);
        }
        catch (FormatException)
        {
            return TokenValidationResult.Failure(ScanResult.Invalid, "Token is not valid Base64");
        }
        catch (Exception ex)
        {
            return TokenValidationResult.Failure(ScanResult.Invalid, $"Token validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current cached token for a session.
    /// </summary>
    public QrTokenPayload? GetCurrentToken(Guid sessionId)
    {
        if (_currentTokens.TryGetValue(sessionId, out var token))
        {
            // Check if token is still valid
            if (token.ExpiresAt > DateTime.UtcNow)
            {
                return token;
            }
        }
        return null;
    }

    /// <summary>
    /// Generates a cryptographically secure nonce.
    /// </summary>
    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Computes HMACSHA256 signature for the payload.
    /// </summary>
    private string ComputeSignature(string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }

    /// <summary>
    /// Clears the token cache for a session (called when session closes).
    /// </summary>
    public void ClearSessionTokens(Guid sessionId)
    {
        _currentTokens.TryRemove(sessionId, out _);
        _sequenceNumbers.TryRemove(sessionId, out _);
    }
}
