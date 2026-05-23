using AwesomeAssertions;
using Humans.Application.Interfaces.Repositories;
using Humans.Application.Interfaces.Users;
using Humans.Application.Services.Users;
using Humans.Application.Tests.Infrastructure;
using Humans.Domain.Entities;
using Humans.Domain.Enums;
using Humans.Infrastructure.Repositories.Profiles;
using Humans.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Humans.Application.Tests.Services.Users;

public sealed class UserServiceProfileOnboardingMutationTests : ServiceTestHarness
{
    private readonly UserService _service;

    public UserServiceProfileOnboardingMutationTests()
    {
        var profileRepository = new ProfileRepository(DbFactory, Clock);
        var userEmailRepository = new UserEmailRepository(DbFactory);
        var contactFieldRepository = new ContactFieldRepository(DbFactory);
        var communicationPreferenceRepository = Substitute.For<ICommunicationPreferenceRepository>();

        _service = new UserService(
            new UserRepository(DbFactory),
            userEmailRepository,
            profileRepository,
            contactFieldRepository,
            communicationPreferenceRepository,
            AdminAuthorization,
            Clock,
            NullLogger<UserService>.Instance);
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RecordConsentCheckCleared_FlipsIsApprovedAndStamps()
    {
        var userId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.RecordConsentCheck,
                ActorUserId: reviewerId,
                ConsentCheckStatus: ConsentCheckStatus.Cleared,
                Notes: "Looks good"));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
        profile.ConsentCheckStatus.Should().Be(ConsentCheckStatus.Cleared);
        profile.IsApproved.Should().BeTrue();
        profile.ConsentCheckAt.Should().Be(Clock.GetCurrentInstant());
        profile.ConsentCheckedByUserId.Should().Be(reviewerId);
        profile.ConsentCheckNotes.Should().Be("Looks good");
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RecordConsentCheckFlagged_KeepsIsApprovedFalse()
    {
        var userId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.RecordConsentCheck,
                ActorUserId: Guid.NewGuid(),
                ConsentCheckStatus: ConsentCheckStatus.Flagged,
                Notes: "Concern X"));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
        profile.ConsentCheckStatus.Should().Be(ConsentCheckStatus.Flagged);
        profile.IsApproved.Should().BeFalse();
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RecordConsentCheckAlreadyRejected_BlocksClear()
    {
        var userId = Guid.NewGuid();
        var profileId = await SeedUserWithProfileAsync(userId);
        var profile = await Db.Profiles.FirstAsync(p => p.Id == profileId);
        profile.RejectedAt = Clock.GetCurrentInstant();
        await Db.SaveChangesAsync();

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.RecordConsentCheck,
                ActorUserId: Guid.NewGuid(),
                ConsentCheckStatus: ConsentCheckStatus.Cleared));

        result.Success.Should().BeFalse();
        result.ErrorKey.Should().Be("AlreadyRejected");
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RecordConsentCheckPending_Throws()
    {
        var userId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ApplyProfileOnboardingMutationAsync(
                userId,
                new UserProfileOnboardingCommand(
                    UserProfileOnboardingMutation.RecordConsentCheck,
                    ActorUserId: Guid.NewGuid(),
                    ConsentCheckStatus: ConsentCheckStatus.Pending)));
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RejectSignup_StampsRejectionFields()
    {
        var userId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.RejectSignup,
                ActorUserId: reviewerId,
                RejectionReason: "Spam account"));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
        profile.RejectedAt.Should().Be(Clock.GetCurrentInstant());
        profile.RejectedByUserId.Should().Be(reviewerId);
        profile.RejectionReason.Should().Be("Spam account");
        profile.IsApproved.Should().BeFalse();
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_RejectSignupAlreadyRejected_ReturnsErrorKey()
    {
        var userId = Guid.NewGuid();
        var profileId = await SeedUserWithProfileAsync(userId);
        var profile = await Db.Profiles.FirstAsync(p => p.Id == profileId);
        profile.RejectedAt = Clock.GetCurrentInstant();
        await Db.SaveChangesAsync();

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.RejectSignup,
                ActorUserId: Guid.NewGuid()));

        result.Success.Should().BeFalse();
        result.ErrorKey.Should().Be("AlreadyRejected");
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_ApproveVolunteer_FlipsIsApprovedTrue()
    {
        var userId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId, isApproved: false);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(UserProfileOnboardingMutation.ApproveVolunteer));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
        profile.IsApproved.Should().BeTrue();
        profile.UpdatedAt.Should().Be(Clock.GetCurrentInstant());
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_SetSuspensionTrue_SetsSuspendedAndState()
    {
        var userId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.SetSuspension,
                Suspended: true,
                Notes: "Disruptive"));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
#pragma warning disable HUM_PROFILE_ISSUSPENDED
        profile.IsSuspended.Should().BeTrue();
#pragma warning restore HUM_PROFILE_ISSUSPENDED
        profile.State.Should().Be(ProfileState.Suspended);
        profile.AdminNotes.Should().Be("Disruptive");
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_SetSuspensionFalse_RestoresActiveWhenIdentityComplete()
    {
        var userId = Guid.NewGuid();
        var profileId = await SeedUserWithProfileAsync(userId);
        var profile = await Db.Profiles.FirstAsync(p => p.Id == profileId);
#pragma warning disable HUM_PROFILE_ISSUSPENDED
        profile.IsSuspended = true;
#pragma warning restore HUM_PROFILE_ISSUSPENDED
        profile.State = ProfileState.Suspended;
        await Db.SaveChangesAsync();

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(
                UserProfileOnboardingMutation.SetSuspension,
                Suspended: false));

        result.Success.Should().BeTrue();
        var fresh = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
#pragma warning disable HUM_PROFILE_ISSUSPENDED
        fresh.IsSuspended.Should().BeFalse();
#pragma warning restore HUM_PROFILE_ISSUSPENDED
        fresh.State.Should().Be(ProfileState.Active);
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_SetConsentCheckPending_FlipsToPending()
    {
        var userId = Guid.NewGuid();
        await SeedUserWithProfileAsync(userId);

        var result = await _service.ApplyProfileOnboardingMutationAsync(
            userId,
            new UserProfileOnboardingCommand(UserProfileOnboardingMutation.SetConsentCheckPending));

        result.Success.Should().BeTrue();
        var profile = await Db.Profiles.AsNoTracking().FirstAsync(p => p.UserId == userId);
        profile.ConsentCheckStatus.Should().Be(ConsentCheckStatus.Pending);
    }

    [HumansFact]
    public async Task ApplyProfileOnboardingMutationAsync_NoProfile_ReturnsNotFound()
    {
        var result = await _service.ApplyProfileOnboardingMutationAsync(
            Guid.NewGuid(),
            new UserProfileOnboardingCommand(UserProfileOnboardingMutation.ApproveVolunteer));

        result.Success.Should().BeFalse();
        result.ErrorKey.Should().Be("NotFound");
    }

    private async Task<Guid> SeedUserWithProfileAsync(
        Guid userId,
        MembershipTier tier = MembershipTier.Volunteer,
        bool isApproved = false)
    {
        SeedUser(userId);
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BurnerName = "Test",
            FirstName = "First",
            LastName = "Last",
            MembershipTier = tier,
            IsApproved = isApproved,
            CreatedAt = Clock.GetCurrentInstant(),
            UpdatedAt = Clock.GetCurrentInstant(),
        };
        Db.Profiles.Add(profile);
        await Db.SaveChangesAsync();
        return profile.Id;
    }
}
