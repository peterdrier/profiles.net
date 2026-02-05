# Nobodies Profiles - Documentation

## Overview

Nobodies Profiles is a membership management system for Nobodies Collective, a Spanish nonprofit organization. The system manages the complete membership lifecycle including authentication, profile management, membership applications, legal compliance (GDPR consent), team collaboration, and administrative governance.

## Feature Documentation

| Document | Description |
|----------|-------------|
| [User Authentication & Accounts](features/01-authentication.md) | Google OAuth, user accounts, role assignments |
| [Member Profiles](features/02-member-profiles.md) | Personal information, burner names, location |
| [Membership Applications](features/03-membership-applications.md) | Application workflow and approval process |
| [Legal Documents & Consent](features/04-legal-documents-consent.md) | GDPR compliance, document versioning, consent tracking |
| [Membership Status](features/05-membership-status.md) | Status computation, compliance tracking |
| [Teams & Working Groups](features/06-teams.md) | Self-organizing teams, system teams, join requests |
| [Google Integration](features/07-google-integration.md) | Drive provisioning, resource management |
| [Background Jobs](features/08-background-jobs.md) | Automated sync, reminders, compliance enforcement |
| [Administration](features/09-administration.md) | Admin dashboard, member management |

## Architecture

The system follows Clean Architecture with four layers:

```
+------------------+
|      Web         |  Controllers, Views, ViewModels
+------------------+
         |
+------------------+
|   Application    |  Interfaces, DTOs, Use Cases
+------------------+
         |
+------------------+
|  Infrastructure  |  EF Core, Services, Jobs
+------------------+
         |
+------------------+
|     Domain       |  Entities, Enums, Value Objects
+------------------+
```

## Key Technologies

- **ASP.NET Core 10** - Web framework
- **Entity Framework Core** - ORM with PostgreSQL
- **Hangfire** - Background job processing
- **Stateless** - State machine library
- **NodaTime** - Temporal operations
- **Google OAuth 2.0** - Authentication
- **Google Places API** - Location autocomplete

## Quick Links

- [Technical Documentation](.claude/README.md) - Coding rules, data model, analyzers
- [Build Commands](../CLAUDE.md#build-commands) - How to build and run
