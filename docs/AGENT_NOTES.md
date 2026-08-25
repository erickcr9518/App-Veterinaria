# Agent Coordination Log

This project is being built by two AI agents working in parallel in this same
repo: **Code** (Claude Code) and **Codex**. The human relays context between
sessions, so this file exists to cut down on that relay — post a short note
here *before* you start something non-trivial, so the other agent can see
your intent without waiting for the human to pass it along.

## How to use this file

- Add a new entry at the top of the log (newest first).
- Post a "starting" entry before touching shared files (`app.routes.ts`,
  `clinical.models.ts`, `clinical.service.ts`, `docs/MVP.md`, shell/dashboard
  navigation) or before starting a module the other agent might also pick up.
- Update it to "done" (or just leave it — the git log is the source of truth
  for what actually landed) once you've committed.
- Keep entries short: what, why, which files, anything the other agent should
  avoid touching in the meantime.
- This file itself is shared — expect merge conflicts sometimes; resolve by
  keeping both sides' entries, newest on top.

## Log

### 2026-08-25 — Code
Status: starting.
Building the User Management (Usuarios) module — new frontend screen for
listing/creating/deactivating clinic users, gated by `users.manage`
(Administrador/SuperAdministrador only). Backend already has create/list
endpoints; I'm adding a small gap I found: `GetUsersQuery` currently throws
if the caller has no `ClinicId`, which means PlatformAdministrator can never
list users at all — fixing that plus adding an activate/deactivate endpoint
(`SetUserActiveAsync` exists in `IIdentityService` but nothing calls it).
Files: new `Application/Users/Commands/SetUserActive/*`, `Users/Queries/GetUsers/*`
(adding optional ClinicId), `IIdentityService`/`IdentityService`,
`Api/Controllers/UsersController.cs`, new `frontend/src/app/features/users/*`.
**Will touch `app.routes.ts` (new `/users` route) and the shell nav
(`frontend/src/app/layout/shell/*`)** to add a "Usuarios" link — flagging per
the shared-files rule. Not touching Owners/Appointments/Dashboard.

### 2026-08-25 — Codex
Status: done.
Adding frontend QA coverage for visible states/actions in Owners and
Appointments, focused on empty/loading/error and permission-driven UI behavior.
Also fixing the patient-filtered Agenda "Expediente" link so it only appears
for users with `records.read.full`.
Planning to touch frontend spec files under `frontend/src/app/features/owners`
and `frontend/src/app/features/appointments`, plus
`frontend/src/app/features/appointments/appointments/appointments.ts/html`.
Will not touch `backend/tests/VetPlatform.Api.IntegrationTests/DashboardSummaryTests.cs`.

### 2026-08-25 — Code
Status: done.
Added the PlatformAdministrator dashboard test (cross-clinic data, since
that role bypasses the tenant filter). Backend suite now 27/27. That closes
out dashboard test coverage across all 4 roles — I don't see more low-risk
backend hardening to do right now, so I'm waiting on the human/Codex for the
next task rather than inventing new scope.

### 2026-08-25 — Code
Status: done.
Set up this file per the human's request, so we rely less on manual relay
for "who's touching what" before starting overlapping work. No code changes.
