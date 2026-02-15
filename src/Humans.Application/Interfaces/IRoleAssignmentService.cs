using NodaTime;

namespace Humans.Application.Interfaces;

/// <summary>
/// Service for validating role assignment windows.
/// </summary>
public interface IRoleAssignmentService
{
    /// <summary>
    /// Checks whether the proposed role window overlaps any existing window
    /// for the same user and role.
    /// </summary>
    Task<bool> HasOverlappingAssignmentAsync(
        Guid userId,
        string roleName,
        Instant validFrom,
        Instant? validTo = null,
        CancellationToken cancellationToken = default);
}
