using Humans.Application.DTOs;
using Humans.Application.Interfaces.Users;
using Humans.Domain.Entities;
using Humans.Domain.Enums;
using NodaTime;

namespace Humans.Application.Interfaces.Profiles;

/// <summary>
/// Service for managing profile data and profile-side merge fan-out.
/// </summary>
/// <remarks>
/// SurfaceBudget intentionally removed for the duration of the Users+Profile
/// section merge — the interface is shrinking as its surface migrates onto
/// IUserService over the next several PRs and is slated for deletion when the
/// merge completes. Per-PR budget churn is not useful while that is in flight.
/// </remarks>
public interface IProfileService : IApplicationService, IUserMerge
{
    /// <summary>
    /// Issue #635 (§15i): idempotently materialize a <see cref="ProfileState.Stub"/>
    /// Profile row for the given user. Called by User-creation paths
    /// (<c>AccountController.ExternalLoginCallback</c>, <c>AccountController.CompleteSignup</c>,
    /// <c>AccountProvisioningService.FindOrCreateUserByEmailAsync</c>) so cross-
    /// section reads can rely on a non-null Profile pointer. No-op if a profile
    /// already exists. The caching decorator refreshes the UserInfo entry
    /// after the write so downstream reads see the new Stub immediately.
    /// </summary>
    Task EnsureStubProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the profile's <see cref="Profile.MembershipTier"/> and
    /// <see cref="Profile.UpdatedAt"/>, persists, and invalidates the
    /// profile's cache entry. No-op with a warning log if the user has no
    /// profile. Used by governance services that previously mutated the
    /// profile directly through a cross-domain navigation property.
    /// </summary>
    Task SetMembershipTierAsync(
        Guid userId,
        MembershipTier tier,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the profile picture for the given profile, read from the
    /// filesystem store. The DB <c>ProfilePictureContentType</c> column is
    /// consulted first as a GDPR gate — if null (no picture, or anonymized)
    /// the call returns <c>null</c> without touching disk so a stale on-disk
    /// file left behind by a failed anonymization cleanup is not served.
    /// Returns <c>null</c> when the file is missing on disk. Centralizing
    /// the read path here keeps controllers free of <see cref="IFileStorage"/>.
    /// </summary>
    Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Issue nobodies-collective/Humans#702: snapshot for the profile-picture
    /// DB→FS migration verification page. Returns the total count of profiles
    /// with a custom picture (<c>ProfilePictureContentType IS NOT NULL</c>),
    /// how many of those have the expected file on disk, and the DB-only rows
    /// (the at-risk laggards that need to be migrated before Phase 2 drops
    /// <c>Profile.ProfilePictureData</c>). FS existence is checked with
    /// <see cref="IFileStorage.TryReadAsync"/> against the same key the read
    /// path uses; the FS-key helper stays section-internal.
    /// </summary>
    Task<ProfilePictureMigrationSnapshot> GetProfilePictureMigrationSnapshotAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Persists a new custom profile picture for the user's profile. No-op (logs a
    /// warning) if the user has no profile yet. Invalidates the UserInfo cache
    /// entry via the caching decorator. Callers are responsible for validating and
    /// resizing the image before calling.
    /// </summary>
    Task SetProfilePictureAsync(Guid userId, byte[] pictureData, string contentType, CancellationToken ct = default);
    Task<Guid> SaveProfileAsync(Guid userId, string displayName, ProfileSaveRequest request, string language, CancellationToken ct = default);
    Task<(bool CanAdd, int MinutesUntilResend, Guid? PendingEmailId)>
        GetEmailCooldownInfoAsync(Guid pendingEmailId, CancellationToken ct = default);

    /// <summary>
    /// Reconciles the user's CV entries (volunteer history) with the provided set.
    /// No-op if the user has no profile.
    /// </summary>
    Task SaveCVEntriesAsync(Guid userId, IReadOnlyList<CVEntry> entries, CancellationToken ct = default);

    /// <summary>
    /// Gets the languages associated with a profile, ordered by proficiency (descending) then language code.
    /// Returns an empty list if the profile does not exist.
    /// </summary>
    Task<IReadOnlyList<ProfileLanguageSnapshot>> GetProfileLanguagesAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Replaces all languages for the given profile with the new set.
    /// </summary>
    Task SaveProfileLanguagesAsync(Guid profileId, IReadOnlyList<ProfileLanguage> languages, CancellationToken ct = default);


    /// <summary>
    /// Anonymizes the personal fields of the user's profile for GDPR
    /// expiry-based deletion: clears first/last name → "Deleted"/"User",
    /// burner name → empty, and blanks every optional demographic /
    /// emergency-contact / admin-note / contribution-interest field. Also
    /// removes every <c>ContactField</c> and <c>VolunteerHistoryEntry</c> row
    /// owned by the profile in the same save. No-op if the user has no
    /// profile. Returns <c>true</c> if a profile was anonymized. Used by the
    /// account deletion job via
    /// <see cref="Users.IAccountDeletionService.AnonymizeExpiredAccountAsync"/>.
    /// </summary>
    Task<bool> AnonymizeExpiredProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Sets <see cref="Profile.IsSuspended"/> to true and stamps
    /// <see cref="Profile.UpdatedAt"/> for users whose consent grace period has
    /// expired. Unlike the per-human lifecycle mutation on
    /// <see cref="IUserService.ApplyProfileOnboardingMutationAsync"/>, this variant does not
    /// require an admin actor, skip-list already-suspended profiles (so the
    /// caller can pre-filter with the returned set), and does not write an
    /// audit log entry — the caller is expected to emit the
    /// <see cref="Humans.Domain.Enums.AuditAction.MemberSuspended"/> entry
    /// itself so it can include job-specific context.
    /// Returns the set of user ids whose profile was actually mutated (i.e.
    /// those who had a profile and were not already suspended).
    /// No-op (absent from the returned set) for users without a profile or
    /// already suspended. Used by the SuspendNonCompliantMembersJob so the
    /// Profile section owns the write (design-rules §2c).
    /// </summary>
    Task<IReadOnlySet<Guid>> SuspendForMissingConsentAsync(
        IReadOnlyCollection<Guid> userIds,
        Instant now,
        CancellationToken ct = default);

    /// <summary>
    /// For every profile whose <see cref="Profile.MembershipTier"/> equals
    /// <paramref name="currentTier"/> and whose <c>UserId</c> is NOT in
    /// <paramref name="userIdsToKeep"/>, downgrade the tier to the value
    /// supplied by <paramref name="fallbackTierByUser"/> (falling back to
    /// <see cref="MembershipTier.Volunteer"/> when the user is absent from
    /// the map). Stamps <see cref="Profile.UpdatedAt"/> to
    /// <paramref name="now"/> and persists in a single save. Returns a list
    /// of (UserId, NewTier) tuples so the caller can emit audit entries
    /// without a second round-trip. Used by
    /// <c>SystemTeamSyncJob.SyncTierTeamAsync</c> so the job does not write
    /// to <c>profiles</c> directly (design-rules §2c).
    /// </summary>
    Task<IReadOnlyList<(Guid UserId, MembershipTier NewTier)>>
        DowngradeTierForExpiredAsync(
            MembershipTier currentTier,
            IReadOnlyCollection<Guid> userIdsToKeep,
            IReadOnlyDictionary<Guid, MembershipTier> fallbackTierByUser,
            Instant now,
            CancellationToken ct = default);

    /// <summary>
    /// Sets, updates, or clears the IBAN on the user's profile.
    /// Pass <c>null</c> or an empty string to clear. The caller is
    /// responsible for validating the IBAN before calling (use
    /// <c>IbanValidator.IsValid</c>). Writes <see cref="Domain.Enums.AuditAction.IbanSet"/>
    /// or <see cref="Domain.Enums.AuditAction.IbanRemove"/> via
    /// <see cref="IAuditLogService"/>. No-op (returns false) if the user has no
    /// profile. Used exclusively by the expense-report IBAN modal route.
    /// </summary>
    Task<bool> SetIbanAsync(Guid userId, string? iban, CancellationToken ct = default);
}

public sealed record ProfileLanguageSnapshot(
    Guid Id,
    Guid ProfileId,
    string LanguageCode,
    LanguageProficiency Proficiency);
