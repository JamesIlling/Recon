namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for changing a user's display name.
/// </summary>
public sealed class ChangeDisplayNameRequest
{
    /// <summary>
    /// The new display name (required, non-empty, max 100 characters).
    /// </summary>
    public required string NewDisplayName { get; init; }
}
