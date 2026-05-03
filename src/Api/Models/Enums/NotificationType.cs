namespace LocationManagement.Api.Models.Enums;

/// <summary>
/// Represents the type of in-app notification event.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// A pending edit has been submitted for a location by a non-creator user.
    /// </summary>
    PendingEditSubmitted = 0,

    /// <summary>
    /// A pending edit has been approved by the location creator.
    /// </summary>
    PendingEditApproved = 1,

    /// <summary>
    /// A pending edit has been rejected by the location creator.
    /// </summary>
    PendingEditRejected = 2,

    /// <summary>
    /// A membership request for a collection has been approved by the collection owner.
    /// </summary>
    MembershipApproved = 3,

    /// <summary>
    /// A membership request for a collection has been rejected by the collection owner.
    /// </summary>
    MembershipRejected = 4
}
