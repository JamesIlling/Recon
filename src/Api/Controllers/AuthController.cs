using LocationManagement.Api.Models.Dtos;
using LocationManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocationManagement.Api.Controllers;

/// <summary>
/// Provides authentication endpoints: registration, login, password reset, and password change.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request containing username, display name, email, and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created user ID and JWT token.</returns>
    /// <response code="201">User successfully registered.</response>
    /// <response code="400">Validation error (invalid input, password too weak, etc.).</response>
    /// <response code="409">Username, display name, or email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthRegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] AuthRegisterRequest request,
        CancellationToken ct)
    {
        try
        {
            var (userId, jwt) = await _authService.RegisterAsync(
                request.Username,
                request.DisplayName,
                request.Email,
                request.Password,
                ct);

            var response = new AuthRegisterResponse
            {
                UserId = userId,
                Jwt = jwt
            };

            return CreatedAtAction(nameof(Register), response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration conflict: {Message}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="request">The login request containing username and password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user ID, display name, and JWT token on success.</returns>
    /// <response code="200">User successfully authenticated.</response>
    /// <response code="400">Validation error (missing username or password).</response>
    /// <response code="401">Invalid username or password.</response>
    /// <response code="429">Too many login attempts. Please try again later.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] AuthLoginRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required." });
        }

        try
        {
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var (userId, displayName, jwt) = await _authService.LoginAsync(
                request.Username,
                request.Password,
                sourceIp,
                ct);

            var response = new AuthLoginResponse
            {
                UserId = userId,
                DisplayName = displayName,
                Jwt = jwt
            };

            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new { error = "Invalid username or password." });
        }
    }

    /// <summary>
    /// Initiates a password reset by sending a reset email.
    /// Returns the same response for known and unknown emails to prevent user enumeration.
    /// </summary>
    /// <param name="request">The forgot password request containing the email address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A success message (same for known and unknown emails).</returns>
    /// <response code="200">Password reset email sent (or would have been sent if email exists).</response>
    /// <response code="400">Validation error (missing email).</response>
    /// <response code="429">Too many password reset requests. Please try again later.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] AuthForgotPasswordRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required." });
        }

        var message = await _authService.ForgotPasswordAsync(request.Email, ct);
        return Ok(new { message });
    }

    /// <summary>
    /// Completes a password reset using a single-use token.
    /// </summary>
    /// <param name="request">The reset password request containing the token and new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Password successfully reset.</response>
    /// <response code="400">Validation error (invalid token, weak password, etc.).</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] AuthResetPasswordRequest request,
        CancellationToken ct)
    {
        try
        {
            var message = await _authService.ResetPasswordAsync(request.Token, request.NewPassword, ct);
            return Ok(new { message });
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Password reset validation error");
            return BadRequest(new { error = "Invalid token or password." });
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Password reset failed");
            return BadRequest(new { error = "Invalid token or password." });
        }
    }
}
