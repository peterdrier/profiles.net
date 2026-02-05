namespace Profiles.Domain.Enums;

/// <summary>
/// Triggers for team join request state machine transitions.
/// </summary>
public enum TeamJoinRequestTrigger
{
    /// <summary>
    /// Approve the join request.
    /// </summary>
    Approve,

    /// <summary>
    /// Reject the join request.
    /// </summary>
    Reject,

    /// <summary>
    /// Withdraw the join request (by the applicant).
    /// </summary>
    Withdraw
}
