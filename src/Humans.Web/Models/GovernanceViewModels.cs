namespace Humans.Web.Models;

public class GovernanceIndexViewModel
{
    /// <summary>
    /// Statutes content by language code (e.g., "es" → markdown, "en" → markdown).
    /// </summary>
    public Dictionary<string, string> StatutesContent { get; set; } = new(StringComparer.Ordinal);

    public bool HasApplication { get; set; }
    public string? ApplicationStatus { get; set; }
    public DateTime? ApplicationSubmittedAt { get; set; }
    public DateTime? ApplicationResolvedAt { get; set; }
    public string? ApplicationStatusBadgeClass { get; set; }
    public bool CanApply { get; set; }

    // Aggregate application statistics (Section 8 transparency)
    public int TotalApplications { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public int ColaboradorApplied { get; set; }
    public int AsociadoApplied { get; set; }
}
