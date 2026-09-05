# Pilot Release Checklist

This is a working checklist for taking VetPlatform from "runs on our machines"
to "a real clinic uses it with real patient data." It is based on an audit of
the current code (2026-08-25), not a generic template — every item below
points at something concrete in the repo. Check items off as they're done;
update this file in the same PR/commit that closes an item.

Scope: this covers what's needed for a **single-clinic pilot with a small,
trusted staff** (a handful of accounts, one location). It does not cover
scaling to many clinics at once, which would need more of the "operational
readiness" section below.

See `docs/DEPLOYMENT.md` for the actual how-to-stand-this-up steps (Docker
Compose). This file is the "is it safe/ready" audit; that one is the runbook.

## Must do before any real patient data touches this

These are gaps that matter the moment a real clinic's data — not test
fixtures — goes into the system.

- [ ] **Provision a real database.** Nothing here should point at the shared
      dev SQL instance. `appsettings.json`'s `ConnectionStrings:DefaultConnection`
      is a local SQLEXPRESS default — the pilot needs its own connection
      string supplied via `appsettings.Production.json` (not committed) or
      environment variables.
- [ ] **Generate a real JWT signing key.** `Jwt:SigningKey` is empty in the
      committed `appsettings.json` by design (`ValidateJwtSettings` in
      `backend/src/VetPlatform.Infrastructure/DependencyInjection.cs` throws
      on startup if it's missing or under 32 bytes) — good, but it means a
      real secret has to be set for the pilot environment before the API will
      even start. Use a secrets manager or environment variable, not a
      committed file.
- [ ] **Confirm demo seeding is off.** `Program.cs` seeds demo data when
      `Seed:DemoData` is `true` *or* the environment is Development. The
      committed `appsettings.json` has no `Seed` section, so it defaults to
      off outside Development — verify the pilot's actual deployed config
      doesn't set `Seed:DemoData: true` and doesn't reuse the dev
      `DemoAdminPassword`. Provision the pilot's first Administrador account
      deliberately instead.
- [ ] **Set the real CORS origin.** `Cors:AllowedOrigins` in `appsettings.json`
      only has `http://localhost:4200`. It needs the pilot's actual deployed
      frontend origin, or login/every API call will fail with a CORS error.
- [ ] **Set the real frontend API URL.** `frontend/src/environments/environment.ts`
      (the production build) points at `/api` (a relative path — assumes the
      frontend and API are served from the same origin behind a reverse
      proxy). Decide the actual deploy topology and update this if the API
      lives on a different host/port.
- [x] **Add login lockout.** Identity now records failed login attempts,
      locks accounts for 15 minutes after 5 failed attempts, resets the
      failure counter on successful login, and enables lockout for both new
      and existing users during seeding. Covered by
      `Login_Locks_User_After_Repeated_Failed_Attempts`.
- [x] **Add logout revocation.** `POST /api/auth/logout` now revokes the
      submitted refresh token, and the frontend's `logout()` calls it before
      leaving the user on `/login`. Covered by `Logout_Revokes_Refresh_Token`
      in `AuthAndAuthorizationTests` plus an `AuthService` unit test.
- [x] **Authenticated password change.** The `/account` screen lets signed-in
      users change their own password after entering the current password.
      Successful changes revoke active refresh tokens and log the browser out,
      so old remembered sessions must authenticate again. Covered by
      `ChangePassword_Requires_Current_Password_And_Revokes_Existing_Refresh_Token`
      and the Account component spec.
- [x] **Access-token invalidation on password change/reset.** Access tokens
      now embed the Identity security stamp and it's checked on every
      authenticated request, so a stolen/cached JWT stops working immediately
      after a password change or reset — not just the refresh token, which
      was already covered above. Found and fixed a real bug while finishing
      this: `CurrentUserService` cached `HttpContext.User` in its
      constructor, and resolving `UserManager` inside the new token-validation
      check could construct it (transitively, via `ApplicationDbContext`'s
      tenant filter) *before* authentication finished — permanently freezing
      an unauthenticated snapshot and breaking every authenticated endpoint,
      not just this one. Fixed by reading `HttpContext.User` lazily instead.
      Full story in `docs/AGENT_NOTES.md`. Backend 45/45, frontend 46/46,
      E2E 6/6, verified live (login → page reload → session restore).

## Should do soon, not necessarily before day one

- [x] **Rate limiting.** Auth-sensitive endpoints (`login`, `refresh`,
      `logout`, `forgot-password`, `reset-password`, `change-password`) now
      use ASP.NET Core rate limiting by client IP, defaulting to 10 requests per minute and
      returning `429` when exceeded. Configurable through
      `RateLimiting:Auth:*` and covered by integration tests.
- [x] **Refresh-token cleanup.** Login and refresh-token rotation now remove
      stale refresh-token rows for that same user (`RevokedAtUtc` set or
      `ExpiresAtUtc` in the past), so normal use does not grow the
      `RefreshTokens` table forever. Refresh keeps the current token long
      enough to revoke and link it to the replacement token. Covered by
      `Login_Removes_Inactive_Refresh_Tokens_For_User` and
      `Refresh_Removes_Inactive_Refresh_Tokens_For_User`.
- [x] **Structured logging.** Serilog now writes structured request/response
      logs (method, path, status, duration) plus everything the app already
      logged (including `ExceptionHandlingMiddleware`'s unhandled-exception
      logging, unchanged) to both the console and a rolling daily file under
      `logs/` (14-day retention, gitignored). `builder.Host.UseSerilog(...)`
      in `Program.cs` also calls `ReadFrom.Configuration`, so a `Serilog`
      section can be added later to point at a real aggregator (Seq,
      Application Insights, Sentry, etc.) without touching code — that
      vendor choice is still a real pilot decision, just no longer blocked
      on infrastructure. In Docker, the file sink writes inside the
      container; mount `logs/` as a volume if you want it to survive
      `docker compose down` or be readable from the host.
- [x] **Database backups.** `scripts/backup-db.sh` / `scripts/restore-db.sh`
      wrap `BACKUP DATABASE`/`RESTORE DATABASE` against the `sqlserver`
      container, writing to a bind-mounted `./backups/` (gitignored — real
      patient data). Restore asks for confirmation first (destructive).
      **Not automated yet** — nothing schedules `backup-db.sh` or copies its
      output off the machine; see `docs/DEPLOYMENT.md`'s Backups section
      for the cron + offsite-copy step still needed before day one.
- [x] **Health check endpoint.** `GET /health` (anonymous) checks real
      database connectivity via `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>()`
      — returns `200 Healthy` or a non-200 with the DB unreachable. Wired
      into `docker-compose.yml`'s `api` service so `frontend` won't start
      routing to it until it's actually ready. Covered by
      `HealthCheckTests`.
- [x] **A deploy story.** ~~There is no Dockerfile...~~ Done: `backend/Dockerfile`,
      `frontend/Dockerfile` + `frontend/nginx.conf`, root `docker-compose.yml`,
      and `docs/DEPLOYMENT.md` walk through standing this up via Docker Compose
      on any host. Still manual (`git pull` + `docker compose up`), no CD (CI
      exists, see below). Password reset SMTP/reset URL env vars are wired
      into Compose. **Docker is now actually verified**: `.github/workflows/ci.yml`'s
      `docker` job runs `docker compose up --build` on a real GitHub-hosted
      Linux runner and checks `/health`, the frontend, and the nginx `/api/*`
      proxy — confirmed green on commit `373908b`. No longer "unverified."
- [x] **CI.** `.github/workflows/ci.yml` runs backend build+test, frontend
      build+test, the full Playwright E2E suite against a real SQL Server
      container, and the Docker Compose stack, on every push/PR to `main`.
      All 4 jobs green as of commit `373908b` (fixed a Node 20→24 mismatch —
      Angular CLI 22 requires Node ≥22.22.3/24.15.0 and was failing silently
      in CI while working locally on a newer Node).
- [x] **A way to manage platform-administrator accounts.** The Usuarios
      screen now lets a SuperAdministrador switch between platform accounts
      and clinic-scoped staff. `GET /api/users` without `clinicId` returns
      only platform administrators for platform users; `GET /api/users?clinicId=...`
      still returns only that clinic's staff. Platform users can activate or
      deactivate other platform administrators, while self-lockout prevention
      remains in place and clinic admins still cannot see or touch platform
      accounts. Covered by `UsersTests` plus the Users component spec.

## Data readiness

- [ ] Decide who provisions the pilot clinic's first Administrador account,
      and how the password reaches them securely (not over plain chat/email).
- [ ] Confirm the pilot's real owners/patients/staff data is entered fresh —
      none of the QA/demo data in the current shared dev database (test
      accounts like `vet.test@vetplatform.dev`, `Clinica Intrusa`, etc.) is
      meant for a real clinic; it exists purely from this project's own
      testing.

## Known functional limitations to tell the pilot clinic about

Worth setting expectations rather than surprising them:

- [x] Printable prescriptions: an "Imprimir receta" button on the
      prescription detail screen prints a clean, letterhead layout (clinic
      name, patient/owner/veterinarian, products) with the app's own
      navigation hidden — the browser's own print dialog handles physical
      copies or saving to PDF, no server-side PDF generation needed.
- [x] Audit log: `/audit` aggregates owners/patients created, consultations
      (created, finalized, amended), prescriptions (created, finalized), and
      appointment status changes into one chronological feed, respecting
      the existing `audit.read.all` (clinic-wide) vs. `audit.read.own`
      (just the caller's own actions) permissions. Covered by
      `AuditLogTests`; verified live against this project's real
      accumulated history.
- [x] Self-service password reset: `/forgot-password` and `/reset-password`
      use ASP.NET Identity reset tokens, return a generic response so unknown
      emails are not disclosed, and support SMTP configuration through
      `PasswordReset:Smtp:*`. Successful resets revoke the user's active
      refresh tokens, forcing old remembered sessions to log in again. In
      Development/Testing the reset URL can be exposed directly to keep local
      QA practical; production relies on email.
- Draft consultations/prescriptions on the Dashboard are scoped to *your
  own* records — a vet won't see a colleague's unfinished draft there (this
  is intentional, but worth explaining so it doesn't read as a bug).

## Automated test coverage snapshot

As of this checklist update: backend 52/52 (2 unit + 50 integration),
frontend 46/46, E2E 6/6 —
re-run all before relying on these numbers, since they move as both agents
add coverage.
These cover role/permission access
control, tenant isolation, and the core clinical-record lifecycle
(draft → finalize → amend) fairly thoroughly.

- [x] **Minimal browser-driven E2E.** Added with Playwright (`frontend/e2e/`,
      `npm run e2e`) — login (valid/invalid/logout), role-based access
      (Administrador vs. Recepcion), and one full clinical-workflow smoke
      test (consultation draft → finalize → prescription draft → finalize),
      run against the real backend + a real database, not mocks. 6/6 passing
      as of this commit. Deliberately minimal — not a substitute for the
      unit/integration suites, just a "did we break the whole app" tripwire.

## Sign-off

- [ ] Erick (product owner) has reviewed the "known limitations" section and
      is comfortable piloting with those gaps open.
- [ ] Someone has actually run through the pilot clinic's real workflows
      end-to-end against the pilot environment (not localhost) before
      onboarding real patients.
