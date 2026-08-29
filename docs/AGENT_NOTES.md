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
Status: done.
Wrote `docs/RELEASE_CHECKLIST.md`. Concrete findings from auditing the actual
code (not a generic template): no login lockout (`IdentityService.ValidateCredentialsAsync`
calls `CheckPasswordAsync` directly, no `options.Lockout` configured), no
`/logout` endpoint so refresh tokens outlive a client-side logout, no rate
limiting anywhere, no deploy story (no Dockerfile/CI at all), plus the usual
secrets/CORS/connection-string items that need real values before a pilot
env. Also listed known functional gaps to tell the pilot clinic about
(no PDF prescriptions, no audit-log UI, no password reset). Doc-only,
no app code touched.

### 2026-08-25 — Codex
Status: done.
Adding frontend auth-flow QA coverage for the JWT interceptor and auth guard:
API bearer headers, refresh retry on 401, refresh failure logout, and guard
redirect behavior. Planning to touch only new spec files under
`frontend/src/app/core/interceptors` and `frontend/src/app/core/guards`.
Avoiding Users, `app.routes.ts`, and shell nav while Code owns the Usuarios module.

### 2026-08-25 — Code
Status: done.
Built the User Management (Usuarios) module end to end, gated by
`users.manage` (Administrador/SuperAdministrador only).
Backend: fixed `GetUsersQuery` so PlatformAdministrator can list a clinic's
users (previously threw — that role has no `ClinicId`), added
`POST /api/users/{id}/status` (activate/deactivate) with tenant checks and
self-lockout prevention. 4 new integration tests, backend suite 31/31.
Frontend: new `/users` route + "Usuarios" shell nav link (touched
`app.routes.ts` and `layout/shell/shell.html` per the shared-files rule —
just the one nav `<a>`, no other shell changes). New `features/users/*`,
`core/models/user.models.ts` + `clinic.models.ts`, `core/services/users.service.ts`
+ `clinics.service.ts`. 3 new specs, frontend suite 35/35.
Verified live in browser: Administrador create/list/deactivate own clinic
(self-row has no deactivate button), PlatformAdministrator clinic-picker
across clinics, Recepcion/Veterinario correctly redirected away from `/users`.
Committing and pushing now.

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
