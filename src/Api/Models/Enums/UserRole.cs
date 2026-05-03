namespace LocationManagement.Api.Models.Enums;

/// <summary>
/// Represents the role assigned to a user account.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Standard user with basic permissions (create locations, collections, etc.).
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Administrator with elevated permissions (manage named shapes, audit log, user roles, etc.).
    /// </summary>
    Admin = 1
}
