# TODO

## Pending

### Add admin UI for managing Google resources per team

Create an admin interface to manage Google Workspace resources (Drive folders, Groups) associated with teams.

**Features needed:**
- [ ] View existing Google resources linked to a team (on team admin page)
- [ ] Provision new Drive folder for a team
- [ ] Provision new Google Group for a team
- [ ] View resource status (last synced, errors)
- [ ] Manual sync trigger for team resources
- [ ] Remove/deactivate resources

**Related files:**
- `IGoogleSyncService` interface with provisioning methods
- `GoogleResource` entity linked to Team
- `GoogleWorkspaceSyncService` for actual API calls
- Admin team management pages to extend

---

## Completed

*None yet*
