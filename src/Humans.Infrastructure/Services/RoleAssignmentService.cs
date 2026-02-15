using Microsoft.EntityFrameworkCore;
using NodaTime;
using Humans.Application.Interfaces;
using Humans.Infrastructure.Data;

namespace Humans.Infrastructure.Services;

/// <summary>
/// Role assignment validation/query service.
/// </summary>
public class RoleAssignmentService : IRoleAssignmentService
{
    private readonly HumansDbContext _dbContext;

    public RoleAssignmentService(HumansDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasOverlappingAssignmentAsync(
        Guid userId,
        string roleName,
        Instant validFrom,
        Instant? validTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RoleAssignments
            .AsNoTracking()
            .Where(ra => ra.UserId == userId && ra.RoleName == roleName);

        // Overlap predicate:
        // [A_start, A_end) overlaps [B_start, B_end) iff
        // A_end > B_start AND B_end > A_start.
        // Null end means open-ended.
        if (validTo.HasValue)
        {
            query = query.Where(ra =>
                (ra.ValidTo == null || ra.ValidTo > validFrom) &&
                validTo.Value > ra.ValidFrom);
        }
        else
        {
            query = query.Where(ra => ra.ValidTo == null || ra.ValidTo > validFrom);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
