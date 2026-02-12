---
name: nav-audit
description: "Audit site navigation to ensure all functionality is reachable through obvious UI paths. Finds dead-end features, missing backlinks, and poor discoverability."
argument-hint: "all | teams | admin | profile | consent | governance"
---

# Navigation & Discoverability Audit

Audit every feature in the system to verify it's reachable through obvious, intuitive navigation paths. Produces an improvement list for discussion — does NOT make changes.

## Severity Levels

| Severity | Meaning |
|----------|---------|
| **SHOWSTOPPER** | Feature exists but is only reachable by manually typing a URL. No link anywhere in the UI leads to it. (Exception: health/metrics endpoints whose consumers are machines, not users.) |
| **POOR** | Feature is reachable but the path is non-obvious — buried in a page the user wouldn't think to look, requires too many clicks, or the link text doesn't communicate what it does. |
| **MISSING BACKLINK** | A page exists for managing X, but the listing/parent page for X doesn't link to it (e.g., can view a team but can't get to pending join requests from the team list). |
| **SUGGESTION** | Navigation works but could be improved (e.g., shortcut from dashboard, breadcrumb, contextual action button). |

## Authorization Awareness

Different roles see different navigation. Audit each route in context of who can access it:

| Role | Sees |
|------|------|
| **Unauthenticated** | Public pages only (home, login) |
| **Authenticated (no approval)** | Membership gate pages (profile setup, consent) |
| **Active member** | Full member experience (profile, teams, consent, privacy) |
| **Lead** | Team admin for their teams |
| **Board** | Admin panel (subset), member approval |
| **Admin** | Full admin panel, configuration |

A feature being "unreachable" means unreachable for the role that should be able to use it. Admin-only features hidden from regular members is correct, not a bug.

## Process

### Step 1: Build the route inventory

Scan the codebase for all controller actions and Razor pages. For each route, record:
- URL pattern
- HTTP method
- Required authorization (role/policy)
- What it does (from action name, comments, or view content)

Sources to scan:
- `src/Humans.Web/Controllers/*.cs` — look for `[Route]`, `[HttpGet]`, `[HttpPost]`, `[Authorize]` attributes
- `src/Humans.Web/Pages/**/*.cshtml` — Razor pages
- `src/Humans.Web/Views/**/*.cshtml` — MVC views

### Step 2: Build the navigation map

For each route found in Step 1, search the views/layouts for links TO that route. Check:
- **Main navigation** (layout/navbar): `_Layout.cshtml`, `_AdminLayout.cshtml`, sidebar partials
- **Page-level links**: buttons, `<a>` tags, `asp-action`, `asp-controller`, `asp-page` tag helpers
- **Contextual actions**: action buttons in list items (edit, delete, view details)
- **Breadcrumbs**: if the site uses breadcrumbs, check they provide upward navigation
- **Dashboard widgets**: admin dashboard cards/links that lead to features

Record for each route: how many inbound links exist and from where.

### Step 3: Analyze gaps

For each route, determine:

1. **Is it linked from its "natural parent"?**
   - Detail pages should be linked from their list page
   - Edit pages should be linked from their view page
   - Admin sub-pages should be linked from the admin nav/dashboard
   - User features should be linked from the main nav or profile

2. **Is the link text/placement discoverable?**
   - Would a user looking for this feature find it within 1-2 clicks?
   - Is the link labeled clearly (not just an icon with no tooltip)?
   - Is it in the section a user would expect?

3. **Are there orphan routes?**
   - Routes with zero inbound links from any view = SHOWSTOPPER
   - Routes only linked from non-obvious places = POOR

4. **Cross-role consistency:**
   - If a Board member can approve applications, is there a link to pending applications from somewhere they'd naturally look?
   - If a Lead can manage their team, can they get to team admin from the team detail page?

### Step 4: Check action flows

Beyond page-level links, check that multi-step workflows have clear forward/backward navigation:
- After submitting a form, where does the user land? Can they get back?
- List pages with items that have actions (approve, reject, edit) — are the actions visible?
- Are confirmation/success pages dead ends, or do they link back to the relevant list?

### Step 5: Produce report

Output a structured report:

```
## Navigation Audit Report

### Route Inventory
Total routes found: N (N public, N member, N lead, N board, N admin)

### Issues Found

#### SHOWSTOPPERS (must fix)
| Route | Feature | Problem | Suggested Fix |
|-------|---------|---------|---------------|
| /path | Description | No inbound links | Add link from X page |

#### POOR Discoverability
| Route | Feature | Current Path | Suggested Improvement |
|-------|---------|-------------|----------------------|
| /path | Description | Only linked from X | Also link from Y |

#### MISSING BACKLINKS
| From Page | Missing Link To | Why Expected |
|-----------|----------------|--------------|
| /teams | /teams/{slug}/requests | Team list should show pending request count/link |

#### SUGGESTIONS
| Area | Suggestion | Rationale |
|------|-----------|-----------|
| Admin dashboard | Add link to X | Common admin task, currently 3 clicks away |

### Summary
- Showstoppers: N
- Poor discoverability: N
- Missing backlinks: N
- Suggestions: N
```

### Step 6: Wait for discussion

Present the report. Do NOT make any code changes until the user reviews and approves specific items. The user may disagree with suggestions or want to prioritize differently.

## Scope Control

If `$ARGUMENTS` specifies a section, only audit that area:
- `all` (default) — entire site
- `teams` — team browsing, joining, team admin
- `admin` — admin panel navigation
- `profile` — profile, email, privacy, contact fields
- `consent` — legal documents and consent flow
- `governance` — asociado application flow
