# Deploying a Pilot Instance

This covers running VetPlatform somewhere real (a VPS, a machine at the
clinic, a cloud VM) via Docker Compose. It's the "how" for the deploy item
in `docs/RELEASE_CHECKLIST.md` — read that first for the security/operational
gaps you should know about before pointing this at real patient data.

## Topology

```
                    ┌─────────────────────────┐
 browser ────────▶  │ frontend (nginx, :80)   │
                    │  serves the Angular app │
                    │  proxies /api/* ───────┼───▶ api (.NET, :8080)
                    └─────────────────────────┘         │
                                                         ▼
                                              sqlserver (mssql, :1433)
```

The frontend container serves the built Angular app *and* reverse-proxies
`/api/*` to the API container (see `frontend/nginx.conf`). That means the
browser only ever talks to one origin — the frontend's — which is why
`frontend/src/environments/environment.ts` uses a relative `apiUrl: '/api'`
and why CORS mostly doesn't come into play for normal browser traffic (it
still matters if anything hits the API container's port directly).

## Prerequisites

- Docker and Docker Compose on the host.
- A domain/IP the pilot clinic will actually use, if not just `localhost`.
- 4GB+ RAM available — SQL Server's container is not lightweight.

## First-time setup

1. Copy the environment template and fill in real values:

   ```bash
   cp .env.example .env
   ```

   Edit `.env`: set a strong `SQL_SA_PASSWORD`, generate a real
   `JWT_SIGNING_KEY` (`openssl rand -base64 48`), set `FRONTEND_ORIGIN` to
   wherever this will actually be reached, and pick a real
   `DEMO_ADMIN_PASSWORD` (see step 3 — this is not a throwaway value, treat
   it like any other admin password).

2. Build and start everything:

   ```bash
   docker compose up -d --build
   ```

   The API waits for SQL Server's healthcheck before starting, then runs EF
   Core migrations automatically on boot (`Database.MigrateAsync()` in
   `Program.cs`) — no manual migration step needed.

3. **Bootstrap the first real accounts.** There's no self-registration
   endpoint, so first boot seeds two fixed accounts to get you in the door:
   `superadmin@vetplatform.dev` (platform administrator) and
   `admin@vetplatform.dev` (administrator of a placeholder "Clínica
   Veterinaria Demo" clinic), both using the `DEMO_ADMIN_PASSWORD` you set.
   Once the app is up:

   - Log in as `superadmin@vetplatform.dev`.
   - Create the pilot's real clinic (`POST /api/clinics`, or wire up a UI for
     it later — there isn't one yet).
   - Create the real Administrador account for that clinic
     (`POST /api/users` with the new clinic's id).
   - Log in as that real Administrador and use the Usuarios screen
     (`/users`) to create the rest of the clinic's staff.
   - Deactivate `admin@vetplatform.dev` from the Usuarios screen once the
     real admin account exists — it's now redundant.
   - `superadmin@vetplatform.dev` can't currently be deactivated from any
     screen (platform administrators aren't scoped to a clinic, so they
     don't show up in any clinic's user list — see
     `docs/RELEASE_CHECKLIST.md`). Treat its password as a real production
     secret regardless.

4. Visit `FRONTEND_ORIGIN` and confirm login works end to end.

## Redeploying after a code change

```bash
git pull
docker compose up -d --build
```

Compose rebuilds only the images whose build context changed. The SQL
Server data volume (`vetplatform-sql-data`) persists across this — it's
only wiped by `docker compose down -v`.

## What this does not cover yet

Per `docs/RELEASE_CHECKLIST.md`: automated backups, log aggregation/alerting,
TLS termination (put this behind a reverse proxy like Caddy/Traefik or your
host's existing one for real HTTPS — this setup serves plain HTTP), and CI/CD
(deploys here are a manual `git pull` + `docker compose up`). Fine for a
small, hands-on pilot; not something to leave as-is if this grows past one
clinic.
