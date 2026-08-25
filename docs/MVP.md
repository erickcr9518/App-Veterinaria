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
- Roles, permissions, and policy-based authorization, including a platform-level `SuperAdministrador` separate from clinic-level `Administrador`.
- Clinic list/create endpoints.
- User list/create/activate-deactivate endpoints scoped by clinic, plus platform-admin provisioning of clinic admins for a clinic they choose, and a frontend "Usuarios" screen (list, create, deactivate) gated by `users.manage`, with a clinic picker for platform administrators.
- Owners and patients, with weight history and progressive-disclosure forms, including a frontend for both.
- Consultations with SOAP notes: draft entry, finalize (sign), and amendments for corrections to finalized records, with a frontend covering the whole lifecycle including the amendment history.
- Appointments backend and frontend with day/week range views, status changes, and role-aware write rules.
- Prescriptions backend and frontend tied to a consultation: draft with one or more items (product, concentration, presentation, quantity, route, frequency, duration, instructions), finalize (locks the record; corrections are issued as a new prescription rather than edited in place), and a per-patient prescription history.
- SQL Server EF Core migrations.
- Audit timestamps, soft delete, and optimistic concurrency.
- Global tenant filter for `ITenantEntity`, reconciled on every startup (stale role permissions are removed, not just added to).
- Integration tests for login, refresh rotation, permissions, clinic isolation, owners/patients isolation, and the consultation lifecycle (draft -> finalize -> amend, blocked direct edits after finalize, tenant isolation, role restrictions).

## Current Clinical Modules

Owners, Patients, Consultations/SOAP, Appointments, Prescriptions, the Patient Record (Expediente del Paciente), and the Dashboard are implemented end to end (backend and frontend).

Delivered backend capabilities:

- Owner entity scoped by clinic.
- Patient entity scoped by clinic.
- One owner can have many patients.
- Basic patient clinical fields: species, breed, birth date or estimated age, sex, reproductive status, color, current weight, microchip, allergies, chronic diseases, current medications, vaccination status, deworming status, and status.
- Weight history table.
- Owner and patient CRUD endpoints with validation, soft delete, audit metadata, and tenant isolation.
- Consultation entity scoped by clinic and tied to a patient and veterinarian.
- SOAP fields for subjective, objective, assessment, and plan.
- Draft and finalized consultation states.
- Amendments for corrections to finalized consultations.
- Appointment entity scoped by clinic: patient, owner, assigned veterinarian, date/time, type of visit, status (scheduled, confirmed, cancelled, completed, no-show), reason, and reminder metadata.
- Appointment range queries for day/week calendar views.
- Appointment status transitions with change history.
- Recepcion can create/edit/cancel any appointment in the clinic (`appointments.write`); veterinarians can only manage their own (`appointments.write.own`).
- Prescription and PrescriptionItem entities tied to a consultation and its patient, with the weight used for dosing captured on the record.
- Draft prescriptions are fully editable (items are replaced wholesale on update); finalize requires at least one item and then locks the record for direct edits (only `prescriptions.write`, held only by Veterinario).
- Prescription history endpoints scoped by patient and by consultation.
- Integration tests proving tenant isolation, role restrictions, blocked direct edits after finalization, and the finalize-requires-an-item rule.
- Dashboard summary endpoint (`GET /api/dashboard/summary`) aggregating today's/upcoming appointments, the current user's own draft consultations and prescriptions pending finalization, and recently added patients, each section gated by the caller's own permissions rather than a dedicated dashboard permission.

Delivered frontend capabilities:

- Owners list with search, edit mode, route guards, and debounced search.
- Patients list with owner and species filters, edit mode, and progressive-disclosure form.
- Consultations: patient timeline, a shared create/edit-draft form (vitals and SOAP visible up front, plan/treatment/follow-up behind a `<details>`), a detail view with a two-step finalize confirmation, and an amend flow whose history renders the stored previous values in readable Spanish labels.
- Appointments day/week view, create/edit form from the calendar, quick appointment start from the patient list, and status changes with a required reason for cancellation/no-show.
- Prescriptions: created from the consultation that generated them (not as a standalone flow), a dynamic add/remove product list, the weight used for dosing pre-filled from the consultation or patient, a two-step finalize confirmation, and a per-patient history. No amend flow by design — correcting a dispensed prescription means creating a new one.
- Patient Record (Expediente del Paciente): a single per-patient screen tying together patient/owner data, consultation history, prescriptions issued, and appointments, filterable by patient from the Agenda; consultations and prescriptions link back into their own detail screens rather than duplicating that logic.
- Dashboard: replaces the static launcher with live clinic data — today's appointment count, the upcoming-appointments list, the signed-in veterinarian's own draft consultations and prescriptions pending finalization, and recently added patients, each card only shown when the user holds the matching permission.
- Navigation entries visible according to permissions throughout.

## Next Module

V1 scope as defined above is now complete end to end. Candidates for what comes next: hardening/QA pass across all modules (edge cases, responsive layout, accessibility), or moving into deferred scope with explicit product sign-off (print/PDF-ready prescriptions, advanced scheduling/reminders, AI-assisted features).

## UX Direction

The product should feel like a clinical tool, not a billing or administrative system. Prefer:

- Fast search.
- Short primary forms.
- Progressive disclosure for optional clinical details.
- Clear role-based navigation.
- Few clicks for common workflows.
- Draft-friendly screens that do not lose information.

Do not add advanced features only because they are technically possible. Prioritize patient safety, clinical clarity, speed of use, privacy, and traceability.
