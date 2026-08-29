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

## Should do soon, not necessarily before day one

- [x] **Rate limiting.** Auth-sensitive endpoints (`login`, `refresh`,
      `logout`) now use ASP.NET Core rate limiting by client IP, defaulting to
      10 requests per minute and returning `429` when exceeded. Configurable
      through `RateLimiting:Auth:*` and covered by integration tests.
- [ ] **Structured logging / error monitoring.** Only the default ASP.NET
      Core console `ILogger` is configured (see `appsettings.json`'s
      `Logging` section) — no Serilog/Application Insights/Sentry equivalent.
      `ExceptionHandlingMiddleware` does log unhandled exceptions server-side
      and never leaks stack traces to the client (verified — this part is
      solid), but there's nowhere for those logs to go except stdout. Decide
      where the pilot's logs need to land before day one, since debugging a
      remote pilot without them is painful.
- [ ] **Database backups.** No backup/restore process exists or is
      documented anywhere in the repo. A real clinic's records need at least
      a basic automated backup plan before they're the only copy of that
      data.
- [ ] **Health check endpoint.** None exists (`/health` or similar). Useful
      once anything is monitoring uptime; not urgent for a single pilot
      clinic if someone is manually watching it.
- [x] **A deploy story.** ~~There is no Dockerfile...~~ Done: `backend/Dockerfile`,
      `frontend/Dockerfile` + `frontend/nginx.conf`, root `docker-compose.yml`,
      and `docs/DEPLOYMENT.md` walk through standing this up via Docker Compose
      on any host. Still manual (`git pull` + `docker compose up`), no CI/CD.
- [ ] **A way to manage platform-administrator accounts.** Found while
      writing the deploy doc: `GetUsersQuery` scopes by clinic, and platform
      administrators have no `ClinicId` — so they never show up in any
      clinic's Usuarios list, and there's no "list platform admins" endpoint
      at all. The seeded `superadmin@vetplatform.dev` bootstrap account can't
      currently be deactivated, disabled, or even seen from any screen.
      Low risk for a single trusted pilot; needed before more than one
      trusted person touches platform-admin-level access.

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

- No printable/PDF layout for prescriptions yet — a finalized prescription
  can only be viewed on-screen. If the clinic needs to hand patients a
  physical copy, this needs manual workaround until that module ships.
- No audit-log screen. `audit.read.all`/`audit.read.own` permissions exist
  and audit *metadata* (`CreatedAtUtc`/`CreatedByUserId`/etc.) is captured on
  every record, but there's no UI to browse "who did what when" yet.
- No self-service password reset — an Administrador (or platform admin) has
  to create/manage accounts manually via the Usuarios screen; there's no
  "forgot password" email flow.
- Draft consultations/prescriptions on the Dashboard are scoped to *your
  own* records — a vet won't see a colleague's unfinished draft there (this
  is intentional, but worth explaining so it doesn't read as a bug).

## Automated test coverage snapshot

As of this checklist (commit `210c169`): backend 33/33 (2 unit +
31 integration), frontend 35/35 — re-run both before relying on these
numbers, since they move as both agents add coverage. These cover role/permission access
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
