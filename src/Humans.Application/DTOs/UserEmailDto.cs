using Humans.Domain.Enums;

namespace Humans.Application.DTOs;

/// <summary>
/// User email data for display purposes.
/// </summary>
public record UserEmailDto(
    Guid Id,
    string Email,
    bool IsVerified,
    bool IsOAuth,
    bool IsNotificationTarget,
    ContactFieldVisibility? Visibility,
    int DisplayOrder);

/// <summary>
/// User email data for the Manage Emails page.
/// </summary>
public record UserEmailEditDto(
    Guid Id,
    string Email,
    bool IsVerified,
    bool IsOAuth,
    bool IsNotificationTarget,
    ContactFieldVisibility? Visibility,
    bool IsPendingVerification);
