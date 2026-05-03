using LocationManagement.Api.Data;
using LocationManagement.Api.Models.Entities;
using LocationManagement.Api.Models.Enums;
using LocationManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Unit tests for AuthService covering registration, login, password reset, and password change.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _authService = new AuthService(
            _dbContext,
            _mockJwtTokenService.Object,
            _mockEmailService.Object,
            _mockAuditService.Object,
            _mockLogger.Object);

        // Setup default JWT token mock
        _mockJwtTokenService
            .Setup(x => x.IssueToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("test-jwt-token");
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_WithValidInput_CreatesUserAndReturnsJwt()
    {
        // Arrange
        var username = "testuser";
        var displayName = "Test User";
        var email = "test@example.com";
        var password = "SecurePass123";

        // Act
        var (userId, jwt) = await _authService.RegisterAsync(username, displayName, email, password, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, userId);
        Assert.Equal("test-jwt-token", jwt);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
        Assert.Equal(displayName, user.DisplayName);
        Assert.Equal(email, user.Email);
        // First user is assigned Admin role
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task RegisterAsync_FirstUser_AssignedAdminRole()
    {
        // Arrange
        var username = "firstuser";
        var displayName = "First User";
        var email = "first@example.com";
        var password = "SecurePass123";

        // Act
        var (userId, _) = await _authService.RegisterAsync(username, displayName, email, password, CancellationToken.None);

        // Assert
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task RegisterAsync_SecondUser_AssignedStandardRole()
    {
        // Arrange - Create first user
        await _authService.RegisterAsync("user1", "User One", "user1@example.com", "SecurePass123", CancellationToken.None);

        // Act - Create second user
        var (userId, _) = await _authService.RegisterAsync("user2", "User Two", "user2@example.com", "SecurePass123", CancellationToken.None);

        // Assert
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Standard, user.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        await _authService.RegisterAsync("testuser", "Test User", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync("testuser", "Another User", "another@example.com", "SecurePass123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateDisplayName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _authService.RegisterAsync("user1", "Test User", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync("user2", "Test User", "another@example.com", "SecurePass123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        await _authService.RegisterAsync("user1", "User One", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync("user2", "User Two", "test@example.com", "SecurePass123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_PasswordTooShort_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "Test User", "test@example.com", "Short1", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_PasswordNoUppercase_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "Test User", "test@example.com", "lowercase123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_PasswordNoLowercase_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "Test User", "test@example.com", "UPPERCASE123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_PasswordNoDigit_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "Test User", "test@example.com", "NoDigitsHere", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_EmptyUsername_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("", "Test User", "test@example.com", "SecurePass123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_EmptyDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "", "test@example.com", "SecurePass123", CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_InvalidEmail_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterAsync("testuser", "Test User", "invalid-email", "SecurePass123", CancellationToken.None));
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUserIdDisplayNameAndJwt()
    {
        // Arrange
        var username = "testuser";
        var displayName = "Test User";
        var email = "test@example.com";
        var password = "SecurePass123";

        var (userId, _) = await _authService.RegisterAsync(username, displayName, email, password, CancellationToken.None);

        // Act
        var (returnedUserId, returnedDisplayName, jwt) = await _authService.LoginAsync(username, password, "127.0.0.1", CancellationToken.None);

        // Assert
        Assert.Equal(userId, returnedUserId);
        Assert.Equal(displayName, returnedDisplayName);
        Assert.Equal("test-jwt-token", jwt);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        await _authService.RegisterAsync("testuser", "Test User", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync("testuser", "WrongPassword123", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync("nonexistent", "SecurePass123", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyUsername_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync("", "SecurePass123", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.LoginAsync("testuser", "", "127.0.0.1", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_RecordsAuditEventOnFailure()
    {
        // Act
        try
        {
            await _authService.LoginAsync("nonexistent", "password", "127.0.0.1", CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        _mockAuditService.Verify(
            x => x.RecordAsync(
                "AuthenticationFailed",
                null,
                "User",
                null,
                AuditOutcome.Failure,
                "127.0.0.1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_SendsResetEmail()
    {
        // Arrange
        var email = "test@example.com";
        await _authService.RegisterAsync("testuser", "Test User", email, "SecurePass123", CancellationToken.None);

        // Act
        var message = await _authService.ForgotPasswordAsync(email, CancellationToken.None);

        // Assert
        Assert.Contains("password reset link has been sent", message);
        _mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnknownEmail_ReturnsSameMessage()
    {
        // Act
        var message = await _authService.ForgotPasswordAsync("unknown@example.com", CancellationToken.None);

        // Assert
        Assert.Contains("password reset link has been sent", message);
        _mockEmailService.Verify(
            x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPassword()
    {
        // Arrange
        var email = "test@example.com";
        var oldPassword = "SecurePass123";
        var newPassword = "NewSecurePass456";

        await _authService.RegisterAsync("testuser", "Test User", email, oldPassword, CancellationToken.None);

        // Generate a reset token
        var resetTokenTask = _authService.ForgotPasswordAsync(email, CancellationToken.None);
        await resetTokenTask;

        // Extract the token from the email service call
        var emailCall = _mockEmailService.Invocations.FirstOrDefault();
        var resetToken = emailCall?.Arguments[2] as string;

        // Act
        var message = await _authService.ResetPasswordAsync(resetToken!, newPassword, CancellationToken.None);

        // Assert
        Assert.Contains("reset successfully", message);

        // Verify new password works
        var (_, _, _) = await _authService.LoginAsync("testuser", newPassword, "127.0.0.1", CancellationToken.None);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.ResetPasswordAsync("invalid-token", "NewSecurePass456", CancellationToken.None));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithWeakPassword_ThrowsArgumentException()
    {
        // Arrange
        var email = "test@example.com";
        await _authService.RegisterAsync("testuser", "Test User", email, "SecurePass123", CancellationToken.None);

        await _authService.ForgotPasswordAsync(email, CancellationToken.None);

        var emailCall = _mockEmailService.Invocations.FirstOrDefault();
        var resetToken = emailCall?.Arguments[2] as string;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.ResetPasswordAsync(resetToken!, "weak", CancellationToken.None));
    }

    #endregion

    #region Change Password Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_UpdatesPassword()
    {
        // Arrange
        var email = "test@example.com";
        var oldPassword = "SecurePass123";
        var newPassword = "NewSecurePass456";

        var (userId, _) = await _authService.RegisterAsync("testuser", "Test User", email, oldPassword, CancellationToken.None);

        // Act
        var message = await _authService.ChangePasswordAsync(userId, oldPassword, newPassword, CancellationToken.None);

        // Assert
        Assert.Contains("changed successfully", message);

        // Verify new password works
        var (_, _, _) = await _authService.LoginAsync("testuser", newPassword, "127.0.0.1", CancellationToken.None);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var (userId, _) = await _authService.RegisterAsync("testuser", "Test User", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.ChangePasswordAsync(userId, "WrongPassword123", "NewSecurePass456", CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWeakNewPassword_ThrowsArgumentException()
    {
        // Arrange
        var (userId, _) = await _authService.RegisterAsync("testuser", "Test User", "test@example.com", "SecurePass123", CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.ChangePasswordAsync(userId, "SecurePass123", "weak", CancellationToken.None));
    }

    #endregion
}
