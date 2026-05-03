namespace LocationManagement.Api.Models.Dtos;

/// <summary>
/// Request DTO for updating user preferences.
/// </summary>
public sealed class UpdatePreferencesRequest
{
    /// <summary>
    /// Whether to show public collections on the user's homepage.
    /// </summary>
    public required bool ShowPublicCollections { get; init; }
}
