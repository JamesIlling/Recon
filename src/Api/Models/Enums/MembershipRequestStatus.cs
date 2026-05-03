namespace LocationManagement.Api.Models.Enums;

/// <summary>
/// Represents the status of a pending membership request for a location collection.
/// </summary>
public enum MembershipRequestStatus
{
    /// <summary>
    /// The membership request is awaiting approval or rejection by the collection owner.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The membership request has been approved by the collection owner.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// The membership request has been rejected by the collection owner.
    /// </summary>
    Rejected = 2
}
