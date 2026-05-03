using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;
using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Implements authentication operations: registration, login, password reset, and password change.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    // Password complexity requirements
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;
    private const int MaxUsernameLength = 50;
    private const int MaxDisplayNameLength = 100;

    // Password reset token expiry: 1 hour
    private static readonly TimeSpan PasswordResetTokenExpiry = TimeSpan.FromHours(1);

    public AuthService(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user with the provided credentials.
    /// </summary>
    public async Task<(Guid UserId, string Jwt)> RegisterAsync(
        string username,
        string displayName,
        string email,
        string password,
        CancellationToken ct)
    {
        // Validate inputs
        ValidateRegistrationInput(username, displayName, email, password);

        // Check uniqueness (case-insensitive)
        var existingUser = await _dbContext.Users
            .Where(u => u.Username.ToLower() == username.ToLower()
                     || u.DisplayName.ToLower() == displayName.ToLower()
                     || u.Email.ToLower() == email.ToLower())
            .FirstOrDefaultAsync(ct);

        if (existingUser != null)
        {
            if (existingUser.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Username already exists.");
            if (existingUser.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Display name already exists.");
            if (existingUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Email already exists.");
        }

        // Hash password with BCrypt (cost 12)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        // Determine role: first user is Admin, all others are Standard
        var userCount = await _dbContext.Users.CountAsync(ct);
        var role = userCount == 0 ? UserRole.Admin : UserRole.Standard;

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            ShowPublicCollections = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {UserId}, role: {Role}", user.Id, role);

        // Issue JWT
        var jwt = _jwtTokenService.IssueToken(user.Id, role.ToString());

        return (user.Id, jwt);
    }

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    public async Task<(Guid UserId, string DisplayName, string Jwt)> LoginAsync(
        string username,
        string password,
        string sourceIp,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await _auditService.RecordAsync(
                "AuthenticationFailed",
                null,
                "User",
                null,
                AuditOutcome.Failure,
                sourceIp,
                ct);

            throw new InvalidOperationException("Invalid username or password.");
        }

        // Find user by username (case-insensitive)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), ct);

        if (user == null)
        {
            await _auditService.RecordAsync(
                "AuthenticationFailed",
                null,
                "User",
                null,
                AuditOutcome.Failure,
                sourceIp,
                ct);

            throw new InvalidOperationException("Invalid username or password.");
        }

        // Verify password
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isPasswordValid)
        {
            await _auditService.RecordAsync(
                "AuthenticationFailed",
                null,
                "User",
                null,
                AuditOutcome.Failure,
                sourceIp,
                ct);

            throw new InvalidOperationException("Invalid username or password.");
        }

        _logger.LogInformation("User logged in: {UserId}", user.Id);

        // Issue JWT
        var jwt = _jwtTokenService.IssueToken(user.Id, user.Role.ToString());

        return (user.Id, user.DisplayName, jwt);
    }

    /// <summary>
    /// Initiates a password reset by generating a single-use token and sending a reset email.
    /// </summary>
    public async Task<string> ForgotPasswordAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "If an account with that email exists, a password reset link has been sent.";

        // Find user by email (case-insensitive)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

        if (user != null)
        {
            // Generate a random token (32 bytes = 256 bits)
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            var token = Convert.ToBase64String(tokenBytes);
            var tokenHash = ComputeSha256Hash(token);

            // Create password reset token record
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.Add(PasswordResetTokenExpiry),
                IsUsed = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.PasswordResetTokens.Add(resetToken);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

            // Send reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.DisplayName, token, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                // Don't throw; the token is already created. Email delivery is best-effort.
            }
        }

        // Always return the same message to prevent user enumeration
        return "If an account with that email exists, a password reset link has been sent.";
    }

    /// <summary>
    /// Completes a password reset using a single-use token.
    /// </summary>
    public async Task<string> ResetPasswordAsync(string token, string newPassword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        ValidatePasswordComplexity(newPassword);

        // Hash the provided token to compare with stored hash
        var tokenHash = ComputeSha256Hash(token);

        // Find the token record
        var resetToken = await _dbContext.PasswordResetTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (resetToken == null)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        if (resetToken.IsUsed)
            throw new InvalidOperationException("Password reset token has already been used.");

        if (DateTimeOffset.UtcNow > resetToken.ExpiresAt)
            throw new InvalidOperationException("Password reset token has expired.");

        // Update user password
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        resetToken.User.PasswordHash = newPasswordHash;
        resetToken.User.UpdatedAt = DateTimeOffset.UtcNow;

        // Mark token as used
        resetToken.IsUsed = true;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset completed for user: {UserId}", resetToken.User.Id);

        return "Password has been reset successfully.";
    }

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    public async Task<string> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new ArgumentException("Current password is required.", nameof(currentPassword));

        ValidatePasswordComplexity(newPassword);

        // Find user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        // Verify current password
        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);

        if (!isCurrentPasswordValid)
            throw new InvalidOperationException("Current password is incorrect.");

        // Update password
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Password changed for user: {UserId}", userId);

        return "Password has been changed successfully.";
    }

    /// <summary>
    /// Validates registration input: username, display name, email, and password.
    /// </summary>
    private static void ValidateRegistrationInput(string username, string displayName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        if (username.Length > MaxUsernameLength)
            throw new ArgumentException($"Username must not exceed {MaxUsernameLength} characters.", nameof(username));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        if (displayName.Length > MaxDisplayNameLength)
            throw new ArgumentException($"Display name must not exceed {MaxDisplayNameLength} characters.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        ValidatePasswordComplexity(password);
    }

    /// <summary>
    /// Validates password complexity: minimum 8 characters, at least one uppercase, one lowercase, and one digit.
    /// </summary>
    private static void ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        if (password.Length < MinPasswordLength)
            throw new ArgumentException($"Password must be at least {MinPasswordLength} characters long.", nameof(password));

        if (password.Length > MaxPasswordLength)
            throw new ArgumentException($"Password must not exceed {MaxPasswordLength} characters.", nameof(password));

        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ArgumentException("Password must contain at least one uppercase letter.", nameof(password));

        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ArgumentException("Password must contain at least one lowercase letter.", nameof(password));

        if (!Regex.IsMatch(password, @"\d"))
            throw new ArgumentException("Password must contain at least one digit.", nameof(password));
    }

    /// <summary>
    /// Validates email format using a simple regex.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Computes SHA-256 hash of a string.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }
}
