# VetPlatform MVP

## Product Goal

Build a clinical veterinary platform for small-animal clinics that helps staff register owners, patients, appointments, consultations, clinical records, SOAP notes, prescriptions, and follow-up without turning the consultation into a slow administrative workflow.

The first version must be useful even without AI. AI-assisted transcription, question suggestions, and clinical summaries are planned only after the core clinical record is trustworthy, auditable, and usable.

## V1 Scope

V1 focuses on the clinical-administrative foundation:

- Authentication with ASP.NET Core Identity, JWT, refresh tokens, roles, and granular permission policies.
- Clinic administration with platform-level and clinic-level responsibilities separated.
- Owner management.
- Patient management for dogs and cats.
- Basic clinical record timeline.
- Manual consultation entry with SOAP fields.
- Basic dashboard and role-aware navigation.
- Audit metadata, soft delete, optimistic concurrency, and tenant isolation by clinic.

## Deferred Scope

These features are intentionally deferred:

- AI clinical interview assistant.
- Audio capture and transcription.
- AI-generated SOAP drafts or summaries.
- Advanced scheduling, reminders, and notifications.
- Prescription PDF generation and signing workflow.
- Object storage for photos, files, audio, and documents.
- Background jobs with Hangfire.
- Realtime panels with SignalR.
- Docker and production deployment automation.

## Roles

- SuperAdministrador: platform operator. Can create and manage clinics and global configuration.
- Administrador: clinic administrator. Can manage users and clinic configuration inside their clinic.
- Veterinario: clinical user. Can register consultations, records, SOAP notes, prescriptions, and clinical follow-up.
- Recepcion: front-desk user. Can register owners, patients, and appointments, but cannot confirm diagnoses or prescriptions.

Permissions must be enforced through policies, not only role checks.

## Clinical Safety Rules

- AI must never write directly to the final clinical record.
- AI must never diagnose, prescribe, or hide uncertainty.
- Signed or finalized clinical records must not be silently overwritten.
- Corrections to finalized records must be traceable through amendments or version history.
- The veterinarian must review and confirm clinical content before it becomes definitive.
- Tests and seed data must use fictitious clinical information only.

## Privacy And Security Rules

- Each tenant is a clinic. Clinic data must be isolated by `ClinicId`.
- Users should only see records from their clinic unless they are platform administrators.
- Passwords are hashed by Identity.
- JWT signing keys and demo passwords must not be committed.
- Demo data is allowed only for local development or explicit test configuration.
- Clinical data sent to external services must be minimized.
- Audio/transcription processing requires consent and retention rules before implementation.

## Architecture

Backend uses Clean Architecture:

- `VetPlatform.Domain`: entities, constants, shared domain contracts.
- `VetPlatform.Application`: commands, queries, DTOs, validators, application interfaces.
- `VetPlatform.Infrastructure`: EF Core, Identity, JWT, persistence, seed data.
- `VetPlatform.Api`: controllers, middleware, API composition.

Frontend uses Angular standalone components organized by domain:

- `core`: guards, interceptors, services, shared models.
- `layout`: application shell and role-aware navigation.
- `features`: screens grouped by product domain.

## Current Baseline

Implemented:

- Backend solution and Angular frontend.
- Auth endpoints: login, refresh, current user.
- Roles, permissions, and policy-based authorization.
- Clinic list/create endpoints.
- User list/create endpoints scoped by clinic.
- SQL Server EF Core migration.
- Audit timestamps and soft delete foundation.
- Global tenant filter foundation for `ITenantEntity`.
- Integration tests for login, refresh rotation, permissions, and clinic isolation.

## Next Module

The next module is Owners and Patients.

Backend requirements:

- Owner entity scoped by clinic.
- Patient entity scoped by clinic.
- One owner can have many patients.
- Basic patient clinical fields: species, breed, birth date or estimated age, sex, reproductive status, color, current weight, microchip, allergies, chronic diseases, current medications, vaccination status, deworming status, and status.
- Weight history table.
- CRUD endpoints with validation, soft delete, audit metadata, and tenant isolation.
- Integration tests proving users cannot access another clinic's owners or patients.

Frontend requirements:

- Owners list with search.
- Owner detail or compact form.
- Patients list with owner and species filters.
- Patient form with progressive sections, keeping essential fields visible first.
- Navigation entries visible according to permissions.

## UX Direction

The product should feel like a clinical tool, not a billing or administrative system. Prefer:

- Fast search.
- Short primary forms.
- Progressive disclosure for optional clinical details.
- Clear role-based navigation.
- Few clicks for common workflows.
- Draft-friendly screens that do not lose information.

Do not add advanced features only because they are technically possible. Prioritize patient safety, clinical clarity, speed of use, privacy, and traceability.
