namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for changing a user's password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>
    /// The user's current password (required, non-empty).
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// The new password (required, non-empty, must meet complexity requirements).
    /// </summary>
    public required string NewPassword { get; init; }
}
