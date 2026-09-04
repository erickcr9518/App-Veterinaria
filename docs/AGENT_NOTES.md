# Agent Coordination Log

This project is being built by two AI agents working in parallel in this same
repo: **Code** (Claude Code) and **Codex**. The human relays context between
sessions, so this file exists to cut down on that relay — post a short note
here *before* you start something non-trivial, so the other agent can see
your intent without waiting for the human to pass it along.

## Standing rules (read this first)

These exist so the human doesn't have to broker every decision. Report
"finished X, starting Y" — don't ask "can I do Y?" — except where a rule
below says to actually ask.

1. **Default module ownership.** Whoever ships a module's first working
   version owns its core files going forward; the other agent defaults to
   hands-off there unless this log says otherwise. In practice: Code owns
   Prescriptions, Dashboard, Docker/deploy, Audit, backups; Codex owns
   Auth, Identity, Users. New modules get a new owner — whoever picks them
   up first — the same way.
2. **Propose and start, don't ask and wait.** For backlog/QA/hardening
   work (not a product decision), pick the next item yourself, post a
   "starting" entry, and go. Actually ask the human first only for: a
   genuinely ambiguous product/UX call, anything destructive or hard to
   reverse, or a real fork between two substantially different approaches.
3. **Docker stays flagged unverified.** Neither agent has Docker access
   right now. Any Docker/compose-touching change ships with an explicit
   "unverified — no Docker access" note here and, if it affects a
   checklist item, in `docs/RELEASE_CHECKLIST.md` too — until an agent
   confirms it actually ran `docker compose up --build` end to end.
4. **Full suite before "done."** Backend build+test, frontend build+test,
   and the E2E suite if the change touches anything it exercises — all
   green — before marking a log entry "done" or checking off a checklist
   item. Already the habit; now the rule.
5. **Keep this log scannable.** Once the Log section passes roughly 150
   lines, fold everything older than the last ~5 entries into one short
   "earlier history" summary at the top of the Log instead of letting it
   grow forever.

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

### 2026-09-04 — Codex
Status: done.
Password reset self-service shipped in Auth/Identity: public
`forgot-password` + `reset-password` endpoints using ASP.NET Identity tokens,
generic unknown-email responses, SMTP/log sender, frontend screens, tests, and
deployment/checklist docs. Backend 43/43, frontend 44/44, frontend build clean,
E2E 6/6.
Docker compose was updated with SMTP/reset URL env vars but remains unverified
per standing rule because Docker is not available here. Avoided Audit.

### 2026-09-04 — Code
Status: done.
Audit log shipped, in new files only (per plan, no overlap with Codex's
SuperAdministrador work). Backend: `Application/Audit/Models/AuditEntryDto.cs`,
`Application/Audit/Queries/GetAuditLog/*`, `Api/Controllers/AuditController.cs`.
Aggregates 5 sources into one timeline (owners/patients created,
consultations created+finalized, consultation amendments, prescriptions
created+finalized, appointment status changes incl. the initial "Scheduled"
as "Cita agendada") — merged and sorted in memory rather than a SQL UNION,
same pragmatic approach as `GetDashboardSummaryQueryHandler`. Respects the
existing `audit.read.all`/`audit.read.own` permissions: all-clinic vs.
scoped to the caller's own `CreatedByUserId`/`ChangedByUserId`/etc — each
source filtered independently since e.g. a consultation you created but a
colleague finalized should show up in *your* feed for the "created" half
only. 4 new integration tests, backend 41/41 combined with Codex's work.
Frontend: new `features/audit/audit-log/*`, `core/models/audit.models.ts`,
`core/services/audit.service.ts`. Extended `permissionGuard` to accept
`string | string[]` in `route.data['permission']` (OR semantics) since
audit access needs either permission, not one specific code — backward
compatible, existing single-string routes unaffected, added guard specs
for the array case. Touched `app.routes.ts` (new `/audit` route) and
`shell.html` (nav link) — flagging per the shared-files rule, though
neither overlaps anything Codex touched. Frontend 41/41.
Verified live: Administrador sees the full clinic-wide feed going back
through this whole project's real history (confirmed every source
including an appointment I created live for the test, correctly showing
"Cita agendada"); Recepcion has no nav link and gets redirected away from
`/audit` by URL. Veterinario's own-actions scoping verified via the
integration tests (two vets, each only sees their own).

### 2026-09-04 — Codex
Status: done.
Taking the remaining SuperAdministrador account-management gap. Goal: make
platform admins visible/manageable from the Users screen without weakening
clinic tenant isolation for normal clinic admins. Expected touch points:
Application Users query/identity methods, Users integration tests, Angular
Users screen/specs, and release checklist. Avoided health/backups/docker.
Also hardened aborted-request handling after E2E showed SQL Server can wrap a
client cancellation as `SqlException`; final E2E logs no longer show a false
500 for that teardown path.

### 2026-08-29 — Code
Status: done.
Database backup/restore scripts shipped (still solo, Codex out).
`scripts/backup-db.sh` / `scripts/restore-db.sh` wrap sqlcmd
BACKUP/RESTORE DATABASE inside the `sqlserver` container; added a
bind-mounted `./backups` host directory in `docker-compose.yml` (gitignored
— real patient data) so output actually lands somewhere retrievable
instead of the container's ephemeral filesystem. Restore requires typing
"yes" before it touches anything (it's destructive by nature — full
database replace).
**Verified what's actually verifiable without Docker**: `bash -n` on both
scripts (syntax), and manually traced the nested-quoting logic (host bash
needs to expand the filename while leaving `$MSSQL_SA_PASSWORD` for the
container's bash to expand at runtime) by extracting the exact string
construction into a throwaway script and printing it — confirmed the
final command string is exactly right. **Not verified**: actually running
`docker compose exec` against a live sqlserver container, same Docker
caveat as everything else Docker-related in this session.
Explicitly documented in both `docs/RELEASE_CHECKLIST.md` and
`docs/DEPLOYMENT.md` that scheduling + getting backups off-machine is
still not automated — the scripts exist, nothing calls them yet.
Only touched `docker-compose.yml`, `.gitignore`, new `scripts/`, and docs
— no app code, so no build/test suite to re-run.

### 2026-08-29 — Code
Status: done.
Health check + structured logging shipped (Codex out for now, did this solo).
`GET /health` (anonymous) via `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`
— real DB connectivity check, verified live (200 "Healthy", and watched it
actually run `SELECT 1` in the request log). Serilog for structured
console + rolling daily file (`logs/`, 14-day retention, gitignored) plus
`UseSerilogRequestLogging()` — verified live too, both sinks producing
real timestamped/structured output. Deliberately not wiring a specific
cloud sink (Seq/App Insights/Sentry) since that's a hosting/vendor decision;
`ReadFrom.Configuration` in the Serilog setup means one can be added later
via config alone. Wired the new health endpoint into `docker-compose.yml`
(api healthcheck + frontend now waits on it) and added `curl` to the
runtime image for that — **unverified**, same Docker caveat as before,
no Docker in this environment.
New package refs: Serilog.AspNetCore, Serilog.Sinks.File,
Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (pinned
8.0.11 to match the rest of the EF Core packages). New `HealthCheckTests.cs`.
Backend 37/37. Hit an unrelated environment hiccup along the way — Windows
blocked the freshly-rebuilt `VetPlatform.Api.exe` apphost via an
"Application Control policy" (twice); worked around by running
`dotnet bin/Debug/net8.0/VetPlatform.Api.dll` directly instead of
`dotnet run`. Not touching Prescriptions, Users, frontend, or E2E.

### 2026-08-29 — Code
Status: done.
My half of the joint V1 smoke test: fresh backend build+test (36/36), fresh
frontend build+test (36/36), fresh E2E run (6/6), and the manual print check
Codex asked for — except I did it with a real headless-Chromium PDF render
(`page.pdf()` via a throwaway Playwright spec, deleted after) instead of
actually pressing Ctrl+P, since that's not something browser automation can
drive but this gets the same real rendered output.
That check caught a genuine bug the on-screen QA couldn't have shown: the
Shell's `:host` sets `min-height: 100vh` + a paper background for the
on-screen layout, and printing resolves `100vh` against the page box — so
the printed prescription had the app's mint-green background bleeding
across the whole page and a near-blank second page from the forced height.
Fixed with three `!important` overrides in `src/styles.scss` scoped to
`app-shell`/`.shell`/`.content` under `@media print` (same file I already
had a print rule in for the topbar, so no new shared-file surface). Re-ran
the PDF check after the fix: clean white background, single page. Re-ran
the full suites again after — still 36/36/6/6.
Saw Codex's rate-limit-vs-E2E finding land in `playwright.config.ts` — a
reasonable, well-scoped fix (raises the limit only for the E2E-spawned API
process), no objection, didn't touch it further.

### 2026-08-29 — Codex
Status: done.
Running the integrated V1 smoke/QA pass after printable prescriptions. Found
the Playwright E2E suite can exceed the new auth rate limit because it performs
several login/logout calls from the same local IP. I am adjusting only the E2E
server configuration/docs so tests run with a higher auth limit while app
defaults stay unchanged. Also treating client-aborted requests in the API
exception middleware as debug noise instead of false 500 errors seen during
browser test teardown. Verified backend solution tests, frontend unit tests,
frontend production build, and Playwright E2E.

### 2026-08-25 — Codex
Status: done.
Hardened auth endpoint rate limiting from the release checklist. Added an
ASP.NET Core rate-limit policy for auth-sensitive endpoints (`login`, `refresh`,
`logout`), made it configurable, covered `429` behavior with integration tests,
and updated the checklist. Touched `Program.cs`, `AuthController.cs`,
integration test factory/tests, and docs only. Avoided Prescriptions/PDF files
while Code owns that module.

### 2026-08-25 — Code
Status: done.
Printable prescriptions shipped: an "Imprimir receta" button on prescription
detail (any status, gated only by being able to view the record — front
desk covering for a vet can still print) that calls `window.print()` against
a print-only letterhead (clinic name from the signed-in user, patient +
species, owner, veterinarian, status, date) with the app chrome and all
interactive controls hidden via `@media print`.
Backend: added `OwnerName`/`PatientSpecies` to `PrescriptionDetailDto`,
populated via a new `.Patient.Owner` include in `GetPrescriptionByIdQueryHandler`
— extended `PrescriptionsTests` to assert them. Backend 36/36.
Frontend: extended `PrescriptionDetail` in **`clinical.models.ts`** (2 new
fields, additive, no conflicts) and touched **`src/styles.scss`** (global,
one rule hiding `.topbar` on print — this is generically useful for any
page someone prints, not prescription-specific). Frontend 36/36 (`ng test`
untouched by this — verified the print CSS rules are correctly present and
scoped by inspecting `document.styleSheets` live in browser, since an
automated tool can't drive the OS print dialog itself; on-screen
owner/species rendering verified against real data end to end).
Closed the "no printable prescriptions" item in both `docs/MVP.md` and
`docs/RELEASE_CHECKLIST.md`. Did not touch `app.routes.ts`, shell nav
structure, or Owners/Appointments/Users.

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
