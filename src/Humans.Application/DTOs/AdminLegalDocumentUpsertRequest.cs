namespace Humans.Application.DTOs;

/// <summary>
/// Input model for creating or updating an admin-managed legal document.
/// </summary>
public sealed record AdminLegalDocumentUpsertRequest(
    string Name,
    Guid TeamId,
    bool IsRequired,
    bool IsActive,
    int GracePeriodDays,
    string? GitHubFolderPath);
