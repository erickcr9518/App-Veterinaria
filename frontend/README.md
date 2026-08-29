# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.3.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

End-to-end tests use [Playwright](https://playwright.dev/) and drive the
real app against a real backend + database — no mocks. First time only,
install the browser binary:

```bash
npx playwright install chromium
```

Then run the suite:

```bash
npm run e2e
```

Playwright starts the API (`dotnet run` from `../backend/src/VetPlatform.Api`)
and the frontend dev server for you if they aren't already running (see
`playwright.config.ts`), so you need a SQL Server instance reachable via the
API's configured connection string, same as for local development. The
Playwright-started API raises the auth rate-limit ceiling for the test process
so the suite can perform several login/logout calls from the same local IP. If
you reuse an API server that was already running, make sure it has enough
`RateLimiting:Auth:*` headroom or stop it before running `npm run e2e`.
The tests log in as the seeded `admin@vetplatform.dev` account and create their
own fresh owners/patients/staff via the API for isolation — they don't
depend on or modify any other data.

These are deliberately minimal: login (valid/invalid/logout), role-based
access (Administrador vs. Recepcion), and one full clinical workflow smoke
test (consultation draft → finalize → prescription draft → finalize). They
exist to catch "the app doesn't work at all anymore," not to replace the
unit/component specs (`ng test`) or the backend integration tests.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
