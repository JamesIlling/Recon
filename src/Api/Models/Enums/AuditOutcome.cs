namespace LocationManagement.Api.Models.Enums;

/// <summary>
/// Represents the outcome of an audited operation.
/// </summary>
public enum AuditOutcome
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The operation failed.
    /// </summary>
    Failure = 1
}
