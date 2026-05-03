using LocationManagement.Api.Models.Enums;

namespace LocationManagement.Api.Models.Entities;

/// <summary>
/// Represents a registered user account in the Location Management system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the username (login identifier).
    /// Maximum length: 50 characters. Must be unique (case-insensitive).
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the display name shown to other users.
    /// Maximum length: 100 characters. Must be unique (case-insensitive).
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the email address associated with the user account.
    /// Must be unique (case-insensitive).
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the bcrypt password hash.
    /// The plaintext password is never persisted or logged.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the user's role (Standard or Admin).
    /// Defaults to Standard for newly registered users.
    /// </summary>
    public required UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user's avatar image.
    /// Nullable; users may not have an avatar.
    /// Foreign key to Image.Id.
    /// </summary>
    public Guid? AvatarImageId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether public collections from other users
    /// should be displayed on this user's homepage.
    /// Defaults to true.
    /// </summary>
    public required bool ShowPublicCollections { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the user's avatar image.
    /// </summary>
    public virtual Image? AvatarImage { get; set; }

    /// <summary>
    /// Gets or sets the collection of locations created by this user.
    /// </summary>
    public virtual ICollection<Location> Locations { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of location collections owned by this user.
    /// </summary>
    public virtual ICollection<LocationCollection> LocationCollections { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of pending edits submitted by this user.
    /// </summary>
    public virtual ICollection<PendingEdit> PendingEdits { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of notifications for this user.
    /// </summary>
    public virtual ICollection<Notification> Notifications { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of audit events performed by this user.
    /// </summary>
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of password reset tokens issued to this user.
    /// </summary>
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of named shapes created by this user.
    /// </summary>
    public virtual ICollection<NamedShape> NamedShapes { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of images uploaded by this user.
    /// </summary>
    public virtual ICollection<Image> UploadedImages { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of pending membership requests submitted by this user.
    /// </summary>
    public virtual ICollection<PendingMembershipRequest> PendingMembershipRequests { get; set; } = [];
}
