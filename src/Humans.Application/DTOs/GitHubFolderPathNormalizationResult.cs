namespace Humans.Application.DTOs;

/// <summary>
/// Result of normalizing and validating a GitHub folder path input.
/// </summary>
public sealed record GitHubFolderPathNormalizationResult(
    bool IsValid,
    string? NormalizedFolderPath,
    string? ErrorMessage);
