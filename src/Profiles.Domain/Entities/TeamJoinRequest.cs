using NodaTime;
using Profiles.Domain.Enums;
using Stateless;

namespace Profiles.Domain.Entities;

/// <summary>
/// Represents a request to join a team.
/// Uses Stateless state machine for workflow management.
/// </summary>
public class TeamJoinRequest
{
    private StateMachine<TeamJoinRequestStatus, TeamJoinRequestTrigger>? _stateMachine;

    /// <summary>
    /// Unique identifier for the join request.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Foreign key to the team.
    /// </summary>
    public Guid TeamId { get; init; }

    /// <summary>
    /// Navigation property to the team.
    /// </summary>
    public Team Team { get; set; } = null!;

    /// <summary>
    /// Foreign key to the requesting user.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Navigation property to the requesting user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Current status of the request.
    /// </summary>
    public TeamJoinRequestStatus Status { get; private set; } = TeamJoinRequestStatus.Pending;

    /// <summary>
    /// Optional message from the requester.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When the request was submitted.
    /// </summary>
    public Instant RequestedAt { get; init; }

    /// <summary>
    /// When the request was resolved (approved/rejected/withdrawn).
    /// </summary>
    public Instant? ResolvedAt { get; private set; }

    /// <summary>
    /// ID of the user who reviewed the request.
    /// </summary>
    public Guid? ReviewedByUserId { get; private set; }

    /// <summary>
    /// Navigation property to the reviewer.
    /// </summary>
    public User? ReviewedByUser { get; set; }

    /// <summary>
    /// Notes from the reviewer.
    /// </summary>
    public string? ReviewNotes { get; private set; }

    /// <summary>
    /// Navigation property to state history.
    /// </summary>
    public ICollection<TeamJoinRequestStateHistory> StateHistory { get; } = new List<TeamJoinRequestStateHistory>();

    /// <summary>
    /// Gets the state machine for this request.
    /// </summary>
    public StateMachine<TeamJoinRequestStatus, TeamJoinRequestTrigger> StateMachine =>
        _stateMachine ??= CreateStateMachine();

    private StateMachine<TeamJoinRequestStatus, TeamJoinRequestTrigger> CreateStateMachine()
    {
        var machine = new StateMachine<TeamJoinRequestStatus, TeamJoinRequestTrigger>(
            () => Status,
            s => Status = s);

        machine.Configure(TeamJoinRequestStatus.Pending)
            .Permit(TeamJoinRequestTrigger.Approve, TeamJoinRequestStatus.Approved)
            .Permit(TeamJoinRequestTrigger.Reject, TeamJoinRequestStatus.Rejected)
            .Permit(TeamJoinRequestTrigger.Withdraw, TeamJoinRequestStatus.Withdrawn);

        machine.Configure(TeamJoinRequestStatus.Approved)
            .OnEntry(() => ResolvedAt = SystemClock.Instance.GetCurrentInstant());

        machine.Configure(TeamJoinRequestStatus.Rejected)
            .OnEntry(() => ResolvedAt = SystemClock.Instance.GetCurrentInstant());

        machine.Configure(TeamJoinRequestStatus.Withdrawn)
            .OnEntry(() => ResolvedAt = SystemClock.Instance.GetCurrentInstant());

        return machine;
    }

    /// <summary>
    /// Approves this join request.
    /// </summary>
    /// <param name="reviewerUserId">The ID of the reviewer.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="clock">The clock to use for timestamps.</param>
    public void Approve(Guid reviewerUserId, string? notes, IClock clock)
    {
        StateMachine.Fire(TeamJoinRequestTrigger.Approve);
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = notes;
        AddStateHistory(TeamJoinRequestStatus.Approved, reviewerUserId, clock, notes);
    }

    /// <summary>
    /// Rejects this join request.
    /// </summary>
    /// <param name="reviewerUserId">The ID of the reviewer.</param>
    /// <param name="reason">The reason for rejection.</param>
    /// <param name="clock">The clock to use for timestamps.</param>
    public void Reject(Guid reviewerUserId, string reason, IClock clock)
    {
        StateMachine.Fire(TeamJoinRequestTrigger.Reject);
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = reason;
        AddStateHistory(TeamJoinRequestStatus.Rejected, reviewerUserId, clock, reason);
    }

    /// <summary>
    /// Withdraws this join request.
    /// </summary>
    /// <param name="clock">The clock to use for timestamps.</param>
    public void Withdraw(IClock clock)
    {
        StateMachine.Fire(TeamJoinRequestTrigger.Withdraw);
        AddStateHistory(TeamJoinRequestStatus.Withdrawn, UserId, clock);
    }

    private void AddStateHistory(TeamJoinRequestStatus newStatus, Guid actorUserId, IClock clock, string? notes = null)
    {
        StateHistory.Add(new TeamJoinRequestStateHistory
        {
            Id = Guid.NewGuid(),
            TeamJoinRequestId = Id,
            Status = newStatus,
            ChangedByUserId = actorUserId,
            ChangedAt = clock.GetCurrentInstant(),
            Notes = notes
        });
    }
}
