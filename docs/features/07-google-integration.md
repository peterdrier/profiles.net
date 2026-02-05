# Google Integration

## Business Context

Nobodies Collective uses Google Workspace for collaboration. The system integrates with Google Drive to automatically provision shared folders for teams and manage access permissions based on team membership.

## User Stories

### US-7.1: Team Folder Provisioning
**As a** team
**I want to** have a shared Google Drive folder automatically created
**So that** team members can collaborate on documents

**Acceptance Criteria:**
- Folder created when team is created
- Named appropriately (e.g., "Team: [Team Name]")
- Located in organization's shared drives
- Tracked in system for permission management

### US-7.2: Automatic Access Grants
**As a** new team member
**I want to** automatically get access to the team's Google resources
**So that** I can immediately participate in team work

**Acceptance Criteria:**
- Access granted on join approval
- Uses member's Google account (OAuth-linked)
- Appropriate permission level (Editor)
- Notification that access was granted

### US-7.3: Automatic Access Revocation
**As a** team
**I want** former members to lose access to resources
**So that** team documents remain protected

**Acceptance Criteria:**
- Access revoked on leave
- Access revoked on removal by admin
- Revocation logged for audit
- Works even if user account is disabled

### US-7.4: Resource Sync Status
**As an** administrator
**I want to** see the status of Google resource sync
**So that** I can troubleshoot access issues

**Acceptance Criteria:**
- Shows last sync timestamp
- Indicates any errors
- Lists provisioned resources
- Manual resync option

## Data Model

### GoogleResource Entity
```
GoogleResource
├── Id: Guid
├── ResourceType: GoogleResourceType [enum]
├── GoogleId: string (256) [unique, Google's ID]
├── Name: string (512)
├── Url: string? (2048) [Google Drive URL]
├── TeamId: Guid? (FK → Team, optional)
├── UserId: Guid? (FK → User, optional)
├── ProvisionedAt: Instant
├── LastSyncedAt: Instant?
├── IsActive: bool
└── ErrorMessage: string? (2000)
```

### GoogleResourceType Enum
```
Folder      // Google Drive folder
Document    // Google Docs document (future)
Spreadsheet // Google Sheets (future)
Group       // Google Groups (future)
```

## Service Interface

### IGoogleSyncService
```csharp
public interface IGoogleSyncService
{
    // Team folder provisioning
    Task<GoogleResource> ProvisionTeamFolderAsync(
        Guid teamId,
        string folderName,
        CancellationToken ct);

    // User folder provisioning (future)
    Task<GoogleResource> ProvisionUserFolderAsync(
        Guid userId,
        string folderName,
        CancellationToken ct);

    // Permission management
    Task SyncResourcePermissionsAsync(Guid resourceId, CancellationToken ct);
    Task SyncAllResourcesAsync(CancellationToken ct);

    // Team membership changes
    Task AddUserToTeamResourcesAsync(Guid teamId, Guid userId, CancellationToken ct);
    Task RemoveUserFromTeamResourcesAsync(Guid teamId, Guid userId, CancellationToken ct);

    // Status
    Task<GoogleResource?> GetResourceStatusAsync(Guid resourceId, CancellationToken ct);
}
```

## Provisioning Flow

### Team Folder Creation
```
┌──────────────────┐
│ Team Created     │
└────────┬─────────┘
         │
         ▼
┌──────────────────────┐
│ ProvisionTeamFolder  │
│ - Name: "Team: X"    │
│ - Parent: Org Drive  │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ Google Drive API     │
│ files.create()       │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────┐
│ Store GoogleResource │
│ - GoogleId           │
│ - URL                │
│ - ProvisionedAt      │
└──────────────────────┘
```

### Permission Sync
```
┌────────────────────┐
│ User Joins Team    │
└────────┬───────────┘
         │
         ▼
┌────────────────────────┐
│ AddUserToTeamResources │
└────────┬───────────────┘
         │
         ▼
┌────────────────────────┐
│ For each team resource:│
│ - Get user's email     │
│ - Google permissions   │
│   API call             │
│ - Grant Editor role    │
└────────┬───────────────┘
         │
         ▼
┌────────────────────────┐
│ Log success/error      │
└────────────────────────┘
```

## Current Implementation

### StubGoogleSyncService

The current implementation is a **stub** that logs all operations without making actual Google API calls. This allows the team workflow to be developed and tested independently.

```csharp
public class StubGoogleSyncService : IGoogleSyncService
{
    public Task AddUserToTeamResourcesAsync(Guid teamId, Guid userId, ...)
    {
        _logger.LogInformation(
            "[STUB] Would add user {UserId} to team {TeamId} resources",
            userId, teamId);
        return Task.CompletedTask;
    }
    // ... similar for other methods
}
```

### Future Implementation

Real implementation will require:
1. Google Workspace Admin SDK credentials
2. Service account with domain-wide delegation
3. OAuth scopes: `drive.file`, `admin.directory.group`
4. Organization's shared drive ID

## Error Handling

### Retry Strategy
```
On Google API error:
  1. Log error with details
  2. Store error in GoogleResource.ErrorMessage
  3. Set IsActive = false if persistent
  4. Background job will retry on next sync
```

### Error Scenarios
| Error | Handling |
|-------|----------|
| Rate limit exceeded | Exponential backoff |
| User not in domain | Skip, log warning |
| Folder not found | Re-provision |
| Permission denied | Alert admin |

## Background Jobs

### GoogleResourceProvisionJob
```
Runs: On demand or scheduled

Tasks:
  1. Find teams without GoogleResource
  2. Provision folders for each
  3. Sync permissions for all resources
  4. Report errors
```

### System Team Sync
When system teams are synced, Google permissions are also updated:
- New member → Add to resources
- Removed member → Remove from resources

## Security Considerations

1. **Minimal Permissions**: Only grant Editor, not Owner
2. **Service Account**: Never expose service account credentials
3. **Audit Trail**: Log all permission changes
4. **Revocation**: Ensure timely removal on leave/suspension
5. **Domain Restriction**: Only add users within the organization domain

## Configuration

```json
{
  "Google": {
    "ServiceAccountKeyPath": "/secrets/google-sa.json",
    "DomainName": "nobodies.es",
    "SharedDriveId": "0ABC123...",
    "TeamsFolderName": "Working Groups"
  }
}
```

## Monitoring

### Metrics
- Resources provisioned (counter)
- Permission grants/revocations (counter)
- API errors by type (counter)
- Sync duration (histogram)

### Alerts
- Repeated API failures
- Permission sync backlog
- Resources in error state

## Related Features

- [Teams](06-teams.md) - Triggers resource provisioning
- [Background Jobs](08-background-jobs.md) - Resource sync job
- [Authentication](01-authentication.md) - User Google identity
