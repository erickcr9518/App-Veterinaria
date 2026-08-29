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

### 2026-08-25 — Codex
Status: done.
Hardened auth endpoint rate limiting from the release checklist. Added an
ASP.NET Core rate-limit policy for auth-sensitive endpoints (`login`, `refresh`,
`logout`), made it configurable, covered `429` behavior with integration tests,
and updated the checklist. Touched `Program.cs`, `AuthController.cs`,
integration test factory/tests, and docs only. Avoided Prescriptions/PDF files
while Code owns that module.

### 2026-08-25 — Code
Status: starting.
Printable/PDF prescriptions (backlog item, my call to take since I own
Prescriptions). Going with a print-optimized browser view + "Imprimir
receta" button (window.print()) rather than server-side PDF generation —
matches the original MVP.md wording ("print/PDF-ready layout") without a
new backend PDF library dependency; the owner/pharmacy gets a physical or
browser-saved-PDF copy either way.
Files, in order:
1. Backend: `Prescriptions/Models/PrescriptionDetailDto.cs` (add OwnerName,
   PatientSpecies), `Prescriptions/Queries/GetPrescriptionById/GetPrescriptionByIdQueryHandler.cs`
   (populate them via `.Patient.Owner`), plus its integration test coverage.
2. Frontend model: **`core/models/clinical.models.ts`** — adding 2 fields to
   `PrescriptionDetail`. This is a listed shared-file hotspot; the change is
   additive only (new optional-ish fields), shouldn't conflict with anything
   in flight, but flagging per the rule.
3. Frontend: `features/prescriptions/prescription-detail/{ts,html,scss}` —
   print button, print-only layout, show owner/species on screen too.
4. **`src/styles.scss`** (global) — one small `@media print` rule to hide
   the shell's topbar/nav when printing. Also a shared file, also additive.
5. `docs/MVP.md` and `docs/RELEASE_CHECKLIST.md` — close out the "no
   printable prescriptions" known limitation.
Not touching `app.routes.ts`, shell nav structure, Owners/Appointments/Users,
or anything else in flight.

### 2026-08-25 — Codex
Status: done.
Hardening login lockout from the release checklist: configure Identity lockout,
record failed password attempts in `IdentityService.ValidateCredentialsAsync`,
enable lockout for newly seeded/created users, and add integration coverage.
Touched backend auth/identity seed + tests and release checklist only. Avoided
frontend routes, shell navigation, permissions, Docker, and E2E files while Code
owned Playwright E2E setup.

### 2026-08-25 — Code
Status: done.
Set up minimal browser-driven E2E tests with Playwright — actually run and
passing 6/6 against the real backend + a real SQL Server, not just written
on paper (unlike the Docker setup, Playwright's Chromium installed fine
here). New `frontend/e2e/{helpers,login,role-access,clinical-workflow}.spec.ts`
+ `frontend/playwright.config.ts` (auto-starts both dev servers if not
already running), `npm run e2e`, README instructions, and marked the
"minimal E2E" checklist item done in RELEASE_CHECKLIST.md. Coverage: login
(valid/invalid/logout), Administrador-vs-Recepcion route access, and one
full consultation-draft→finalize→prescription-draft→finalize smoke test.
Tests create their own fresh owners/patients/staff via API for isolation —
no shared-state dependency on other QA data sitting in the dev DB.
Confirmed `ng test` doesn't pick up the e2e/ specs (still 36/36). Only
touched `frontend/package.json`, `package-lock.json`, `.gitignore`, `README.md`
— nothing in `src/`.

### 2026-08-25 — Codex
Status: done.
Hardening auth logout from the release checklist: add an API logout endpoint that
revokes refresh tokens, wire the frontend "Cerrar sesión" action to call it, and
cover the refresh-token revocation behavior with integration tests. Touched Auth
command/controller/service files, auth tests, and the release checklist only.
Avoided Docker, deployment docs, `app.routes.ts`, shell navigation, and
permission defaults while Code owned deploy support.

### 2026-08-25 — Code
Status: done.
Added Docker deploy support: `backend/Dockerfile`, `frontend/Dockerfile` +
`frontend/nginx.conf` (serves the built Angular app, reverse-proxies
`/api/*` to the API container so the browser only sees one origin), root
`docker-compose.yml` (api + sqlserver + frontend), `.env.example`, and
`docs/DEPLOYMENT.md`. Found a real gap while writing the bootstrap steps:
there's no self-registration, so a fresh deploy with `Seed:DemoData=false`
would have zero users and no way to ever log in — compose intentionally
seeds the demo accounts as the bootstrap path, documented in DEPLOYMENT.md
("log in, create the real clinic, retire the demo accounts"). Also found
platform-administrator accounts can't be deactivated from any screen
(not clinic-scoped) — logged in RELEASE_CHECKLIST.md, not fixed.
Could not actually run `docker compose build` — Docker isn't installed in
this environment — so verified by hand: every path/filename the Dockerfiles
reference against the real repo structure, and the Angular build output dir
(`dist/frontend/browser`) against a real `ng build` run. Recommend an actual
`docker compose up --build` smoke test with Docker available before relying
on this for the real pilot deploy. No app code touched.

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
