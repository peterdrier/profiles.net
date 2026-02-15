using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Humans.Application.Interfaces;
using Humans.Domain.Constants;
using Humans.Infrastructure.Data;

namespace Humans.Infrastructure.Jobs;

/// <summary>
/// Drains queued Google sync outbox events and executes the underlying sync operations.
/// </summary>
public class ProcessGoogleSyncOutboxJob
{
    private const int BatchSize = 100;
    private const int MaxRetryCount = 10;

    private readonly HumansDbContext _dbContext;
    private readonly IGoogleSyncService _googleSyncService;
    private readonly IClock _clock;
    private readonly ILogger<ProcessGoogleSyncOutboxJob> _logger;

    public ProcessGoogleSyncOutboxJob(
        HumansDbContext dbContext,
        IGoogleSyncService googleSyncService,
        IClock clock,
        ILogger<ProcessGoogleSyncOutboxJob> logger)
    {
        _dbContext = dbContext;
        _googleSyncService = googleSyncService;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var pendingEvents = await _dbContext.GoogleSyncOutboxEvents
            .Where(e => e.ProcessedAt == null && e.RetryCount < MaxRetryCount)
            .OrderBy(e => e.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingEvents.Count == 0)
        {
            return;
        }

        foreach (var outboxEvent in pendingEvents)
        {
            try
            {
                switch (outboxEvent.EventType)
                {
                    case GoogleSyncOutboxEventTypes.AddUserToTeamResources:
                        await _googleSyncService.AddUserToTeamResourcesAsync(
                            outboxEvent.TeamId,
                            outboxEvent.UserId,
                            cancellationToken);
                        break;

                    case GoogleSyncOutboxEventTypes.RemoveUserFromTeamResources:
                        await _googleSyncService.RemoveUserFromTeamResourcesAsync(
                            outboxEvent.TeamId,
                            outboxEvent.UserId,
                            cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown outbox event type '{outboxEvent.EventType}'.");
                }

                outboxEvent.ProcessedAt = _clock.GetCurrentInstant();
                outboxEvent.LastError = null;
            }
            catch (Exception ex)
            {
                outboxEvent.RetryCount += 1;
                outboxEvent.LastError = ex.Message.Length > 4000
                    ? ex.Message[..4000]
                    : ex.Message;

                _logger.LogError(
                    ex,
                    "Failed processing Google sync outbox event {OutboxId} ({EventType}) attempt {Attempt}",
                    outboxEvent.Id,
                    outboxEvent.EventType,
                    outboxEvent.RetryCount);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
