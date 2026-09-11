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
   Prescriptions, Dashboard, Docker/deploy, Audit, backups, CI, **and now
   Vetheca** (the AI research module, formerly "VetIA" — see
   `VETIA_CLINIC_ANALYSIS.md`); Codex owns Auth, Identity, Users. New
   modules get a new owner — whoever picks them up first — the same way.
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

### 2026-09-10/11 — Code (16)
Status: done - closes out the "bring your own literature" feature from
Code (15) below.

Wired the clinic's uploaded library into the actual `ask` flow - the part
that was missing before. `ILibraryChunkSearchService`
(`Application/Vetheca/Services`) ranks a clinic's own chunks by simple
keyword overlap against both the original question and the translated
PubMed search query (purchased manuals could be in either language) - no
vector store needed at this scale (a clinic's own library is a handful
of documents, not millions of records; revisit if that stops being
true). `ILlmClient.SynthesizeAsync` now takes these excerpts alongside
PubMed articles; `AnthropicLlmClient`'s citation schema grew a `fuente`
discriminator ("pubmed"/"biblioteca") so a citation can point at "your
document, page X" instead of only a PMID - grounded and quote-verified
with the exact same non-trust-the-model logic as the existing PMID path,
just keyed by (document title, page) instead of PMID.

Frontend: a collapsible "📚 Mi biblioteca" panel on the Vetheca screen
(upload form + list with delete) - collapsed by default so it doesn't
compete with the main "ask a question" flow for attention. Citations from
the library now show a "📚 De tu biblioteca" badge and "Documento, página
N" instead of a PMID.

Backend 84/84 (7 new: the real AnthropicLlmClient grounds/verifies a
library citation and drops a hallucinated one - same pattern as the
existing PMID tests; the real handler searches a clinic's own uploaded
document and passes the right chunk through; tenant isolation holds for
library search too). Frontend 65/65 (2 new).

Live-verified end to end against the real backend and database (not just
tests): generated a real one-page PDF, uploaded it through a real
authenticated request from the browser (the file-picker itself isn't
drivable through this session's browser-automation tools, so the upload
call was issued as a real `fetch()` from the page's own JS console
instead of a literal click-through - still a real request against the
real endpoint, not a mock), reloaded the page and confirmed Angular
showed it via the real `GET /api/vetheca/library` ("📚 Mi biblioteca (1)",
title, page count), then deleted it through the actual UI button and
confirmed it disappeared for real. Did not spend a real paid Claude call
proving a library citation renders in a live synthesis - the grounding
logic that would exercise is already covered by real (unmocked)
`AnthropicLlmClient` tests above, and the citation UI rendering is
covered by a frontend spec - spending real money on Claude to duplicate
that assurance didn't seem worth it.

Idea 4 (species extrapolation) is still the only one of the original four
not built, per Erick's own call. Erick separately asked about image
interpretation (radiographs/lab results) - still just documented, not
started.

### 2026-09-10 — Code (15)
Status: in progress (backend upload/manage done; search integration still to come).

Erick asked for a fourth Vetheca idea beyond the 1-3 already shipped: let
him upload his own purchased literature (a manual, a textbook PDF) so
Vetheca can search it too, not just PubMed. Talked through the legal
angle first since it matters - buying a digital copy generally covers
internal/organizational use, but the actual answer depends on that
specific product's license terms, and it's Erick's call what he uploads;
this system just needs to handle it responsibly, which is why only
*extracted text* is stored, never the original PDF bytes - the system
never holds a second redistributable copy of someone else's content.

**Real problem hit before writing any code:** picked `UglyToad.PdfPig`
off NuGet for PDF text extraction and it turned out to be a stale/
abandoned package id now owned by an unrelated NuGet account ("grinay",
unverified, pushing an odd "1.7.0-custom-5" version) - not the real
maintainers. Verified via NuGet's search API (owners field) and the
actual GitHub README, which points at a *different* package id: plain
`PdfPig` (owners `BobLd`/`EliotJones`/`PdfPig`, Apache 2.0, 31M+ downloads,
real version history 0.0.1 through 0.1.16). Installed that one instead.
Worth remembering for whoever adds the next NuGet package to this repo -
check the owners field, not just the package name matching what you
expect.

**What's built (backend only so far):**
- `VethecaLibraryDocument`/`VethecaLibraryChunk` entities - a document is
  one uploaded PDF, chunks are its text split by page (further split
  only if a page is unusually long). Shared per-clinic like Owners/
  Patients, not private to the uploader - any `vetheca.ask` user can
  upload, list, or delete, matching how the rest of the clinic's shared
  data works, unlike VethecaSearchLog's private-to-the-asker model.
- `POST /api/vetheca/library` (multipart upload, 50MB cap via
  `[RequestSizeLimit]`), `GET /api/vetheca/library`, `DELETE
  /api/vetheca/library/{id}` - all behind the existing `vetheca.ask`
  permission, no new permission code needed for this slice.
- `IPdfTextExtractor`/`PdfPigTextExtractor` (Infrastructure) - same
  interface-in-Application/implementation-in-Infrastructure split as
  `IPubMedClient`/`ILlmClient`.
- A bad/corrupt upload comes back as a clean 400 (`ValidationException`),
  not a 500.

**Not built yet - the actually-useful part:** Vetheca's `ask` flow
doesn't search this library yet, so uploading a document does nothing
observable beyond appearing in a list. Next step is wiring a "search my
clinic's chunks" step into `AskVethecaQueryHandler` alongside the
existing PubMed search, extending `AnthropicLlmClient`'s citation schema
so a citation can point at "your document, page X" instead of only a
PMID, and a frontend screen to upload/manage documents plus show library
citations in results. Deliberately stopped here rather than build the
whole thing in one pass, given how much new surface (dependency choice,
entities, migration, endpoints) this slice alone already was.

Backend 80/80 (7 new: real upload + real PdfPig extraction + list,
rejects a non-PDF file, a colleague sees and can delete someone else's
upload, tenant isolation). No frontend changes yet, so nothing to
live-verify in the browser this round - the integration tests already
exercise the real (unmocked) PdfPig extraction and real HTTP endpoints
against a real test database.

Separately, Erick also asked about letting Vetheca look at uploaded
images (lab results, X-rays) and comment on what it observes. Technically
feasible (Claude has vision), but flagged as a bigger conversation than
this one - that's clinical image interpretation, not literature search,
a meaningfully different liability/scope question. Not started; revisit
once the library feature above is finished, so we're not juggling two
large new Vetheca surfaces at once.

### 2026-09-07 — Codex
Status: done.
Continuing in Auth/Identity/Users. Found a session-security edge case while
reviewing deactivation: inactive users are rejected immediately, but without
changing the security stamp/revoking refresh tokens, pre-deactivation tokens
could become valid again if the account is reactivated before token expiry.
Fixed `SetUserActiveAsync` to invalidate sessions on every activation status
change and added integration coverage. Rebased cleanly on Code's Vetheca
work and kept the scope to `IdentityService`, Users integration tests, and
the checklist. Verification: backend 76/76, frontend 63/63, frontend build
OK, E2E 6/6. Note: the `Documents/ChatGPT` worktree hit Windows App Control
on `VetPlatform.Infrastructure.dll`, so backend/E2E were verified from a
temporary trusted worktree at `C:\Users\Erick Castillo\Proyectos\AppVeterinaria-codex-verify`
pointing at this exact commit. The build has Code's known Vetheca CSS budget
warning, but exits successfully.

### 2026-09-07 — Code (14)
Status: done.

Small follow-up to the feedback feature shipped a couple commits ago
(Code (12)): the 👍/👎 was being saved but was write-only - nowhere did
it actually surface to anyone reviewing clinic activity, which defeats
its stated purpose ("give Erick real signal on where Vetheca fails").
`GetAuditLogQueryHandler`'s Vetheca entries now append the feedback to
the existing Summary line when present (" — 👎: <note>" or " — 👍"),
reusing the aggregator's own `Truncate` helper for the note. No new
endpoint, no new UI - the audit screen already existed and already showed
every ask; this just makes the feedback visible where it's already being
looked at instead of buried in the database.

Backend 76/76 (1 new: submits negative feedback with a note, confirms the
audit summary contains neither/then both the 👎 marker and the note
text). Frontend unaffected (63/63, unchanged) - this was a pure backend
read-model change on an existing endpoint.

### 2026-09-07 — Code (13)
Status: done.

Backlog/hardening item, not a product decision - picked up per standing
rule 2 rather than asking first. Every Vetheca "ask" costs real money (a
paid Anthropic call), and until now there was nothing stopping an
accidental rapid-fire loop (bad script, someone mashing the button, a
retry bug) from running up the bill. Added a per-user rate limit on
`POST /api/vetheca/ask` only - 20 asks/hour by default
(`RateLimiting:Vetheca` in `appsettings.json`), reusing the exact
`AddRateLimiter`/fixed-window pattern already in place for the Auth
endpoints in `Program.cs`. Partitioned by user id (the same claim
`CurrentUserService` reads), not IP - several vets in the same clinic
share a network, so an IP-based limit would throttle the wrong thing.
Save/unsave/feedback/saved-list stay unlimited - only the paid call is
guarded.

Frontend: a 429 now shows a specific message ("Hiciste muchas preguntas
en poco tiempo...") instead of the generic search-failed one.

Backend 75/75 (1 new: drives the real ASP.NET Core rate limiter down to a
1-request limit via test-only config override, confirms the 2nd ask gets
429 while `/api/vetheca/saved` stays 200 for the same throttled user).
Frontend 63/63 (1 new, mocked 429). Didn't live-verify this one against
the real 20/hour default - would mean 21 real paid Claude calls just to
trigger it, which is exactly the kind of cost this feature exists to
avoid; the integration test exercises the real (not mocked) ASP.NET Core
rate-limiter middleware, which is the part actually at risk of being
wrong.

### 2026-09-07 — Code (12)
Status: done.

Erick asked, unprompted, if I had my own ideas to improve/innovate on
Vetheca (not just react to his). Proposed 4; he approved building 1-3 now,
holding 4 (drug-interaction/dosage cross-check against the patient's own
record) as documented-only - his call, he flagged it as a likely source of
conflicts (interaction data quality/liability) worth thinking through more
before building. All 3 approved ideas are now built, tested, and verified
live against the real Claude/PubMed APIs - see
`docs/VETIA_CLINIC_ANALYSIS.md` section N for the original proposal.

1. **Study type from real PubMed metadata.** `PubMedArticleDto.StudyType`,
   parsed from the article's own `PublicationTypeList` in the `efetch` XML
   (filtering out uninformative tags like "Journal Article"). Deliberately
   *not* asked of the LLM - this is NLM-assigned structured metadata, zero
   hallucination risk, `null` (shown as "no confirmado") when PubMed itself
   didn't tag it, never invented.

2. **Citation quote verification.** The synthesis prompt now requires a
   literal ~30-word excerpt per citation; `AnthropicLlmClient` checks
   (normalized substring match) that the excerpt actually appears in the
   cited article's own abstract text - independent of the existing PMID-
   grounding check. Important distinction: a failed PMID match still drops
   the citation outright (unambiguous hallucination), but a failed quote
   match does NOT drop it - only flags `QuoteVerified: false`, shown
   honestly in a new "Verificación de citas" section rather than hidden.
   Same "show uncertainty, don't hide it" principle as the existing
   evidence-insufficient badge.

3. **Response feedback.** 👍/👎 on any synthesis - thumbs-down prompts for
   an optional short note before submitting. Stored as
   `VethecaSearchLog.Feedback`/`FeedbackNote` (one migration,
   `AddVethecaFeedback` - no new table needed, unlike 1-2 which needed zero
   migrations since their data lives in the existing `ResultJson` blob).
   New endpoint `POST /api/vetheca/{id}/feedback`, ownership-checked same
   pattern as save/unsave (403 if not the asking user).

Backend 74/74 (7 new: quote-verified/unverified, feedback submit +
ownership-forbidden, real PubMed XML → correct StudyType parse). Frontend
62/62 (6 new). One real test bug caught and fixed along the way: a single
`it()` tried to call `createComponent()` twice (verified + unverified in
one test) - Vitest/TestBed only allows one `configureTestingModule` per
test, split into two tests.

Verified live end-to-end with a real question ("tratamiento de la
displasia de cadera en perros grandes") against the real Claude/PubMed
APIs: all 5 real sources showed a real study type (mostly "Review"); the
citation section showed 4 real citations, 3 verified and 1 correctly
flagged unverified (a real LLM paraphrase, not a hallucination - exactly
the ambiguous case this feature is designed to surface honestly instead of
guessing); clicked 👍 and confirmed via network inspection the request hit
`POST /api/vetheca/{id}/feedback` and returned a real `204 No Content`
from the real database.

### 2026-09-06 — Code (11)
Status: done. Also folded everything before "Code (7)" into a compact
"Earlier history" summary below, per standing rule 5 - this file had grown
past 800 lines.

Vetheca's MVP is now fully complete (roadmap section I, Fase 1). Last piece:
persistence. New `VethecaSearchLog` entity/table - one row per ask, serving
two purposes at once instead of two duplicate tables: it's the audit trail
(every question, whether or not the user keeps it - now a 6th source in
`GetAuditLogQueryHandler`/`/api/audit`) and, once `IsSaved` is set, the
user's own saved-research list. "Unsave" only clears `IsSaved`/`Title` -
never deletes the row, so the audit trail is never user-erasable.

New endpoints: `POST /api/vetheca/{id}/save`, `POST /api/vetheca/{id}/unsave`,
`GET /api/vetheca/saved`, `GET /api/vetheca/saved/{id}` - all ownership-
checked (only the asking user can save/view/unsave their own, independent
of the tenant filter which only guards cross-clinic access, not cross-user
within the same clinic). `AskVethecaResult` now carries the log row's Id.

Frontend: no new navigation - a "Guardar esta consulta" control under any
result, a "Tus consultas guardadas" list above it once the user has any,
clicking one reopens it in the same result view. Matches Erick's standing
"as simple as a stethoscope" bar.

Backend 69/69 (5 new tests: save, unsave, ownership enforcement on both
save and view, audit-log integration). Frontend 57/57 (5 new tests).
Verified live end-to-end against the real database and real browser: asked
a real question, saved it with a title, saw it in the list, reopened it,
unsaved it, confirmed the audit-log entry survives un-saving.


### 2026-09-05 — Code (10)
Status: done.
Erick spent real time asking Vetheca varied clinical questions (different
species: canine, feline, avian, bovine; different question shapes: case
vignette, drug-safety, colloquial owner phrasing, a protocol request, plus
one deliberately off-topic control question). Found and fixed two more
real bugs, same root-cause family as the earlier translation fix - all
from live use, none from reading code:

1. Implicit multi-word queries over-restrict PubMed just like chained
   AND does - a 7-word query with no AND/OR at all ("long-term meloxicam
   safety cats chronic kidney disease") returned 0 results; trimming to
   4 words returned 7. PubMed requires every unquoted word to co-occur.
   Tightened the translate prompt: hard cap of 3-4 keywords, and
   explicitly drop generic qualifier words ("safety", "long-term",
   "best", "effective") that dilute the query without helping find
   anything - those questions get answered by analyzing the retrieved
   abstracts, not by searching for the word itself.
2. A richer question produced a longer synthesis than the 2048-token
   budget allowed, truncating mid-JSON. Raised MaxTokens to 4096 and
   added a specific log line when a response's `stop_reason` is
   `max_tokens`, so this is instantly diagnosable next time.

Also confirmed something working as designed, not a bug: a question
about an equine acute-abdomen "protocol" correctly came back with
`evidenciaSuficiente: false` and an honest explanation that the retrieved
research papers don't add up to a step-by-step protocol (that's a
guideline-document thing, not a research-paper thing) - exactly the
"say so instead of inventing" behavior this whole module exists to
guarantee.

Unrelated finding worth Erick knowing about before any real production
use: the server logs a warning every request that MediatR (used
throughout this whole backend, not just Vetheca) is unlicensed for
production use ("Lucky Penny software" licensing model). Not blocking
anything now, but needs a decision (buy a license, or migrate off
MediatR) before this goes live for a paying pilot - flagging here so it
doesn't get lost, not treating it as mine or Codex's to just decide.

Backend 65/65 (verified together with Codex's SetUserRoles work).

### 2026-09-05 — Codex
Status: done.
Continuing in Codex-owned Auth/Identity/Users after syncing with latest
`origin/main`. Next hardening gap: users can now be created with multiple
roles, but existing accounts could not have their roles changed. Added
`PUT /api/users/{id}/roles`, a permission-safe role update flow for existing
clinic staff, keeping SuperAdministrador platform-only/mutually exclusive,
preserving tenant checks/self-protection, and invalidating the target user's
old access/refresh tokens after role changes. Frontend Usuarios now has an
inline role editor per eligible staff row. Verification: backend 65/65,
frontend 55/55, frontend build OK, E2E 6/6.

### 2026-09-05 — Codex
Status: done.
Implemented multi-role user accounts in Auth/Identity/Users, per Erick's
green light and Code's request. Clinic users can now hold combinations like
Administrador+Veterinario and receive the union of role permissions, while
SuperAdministrador stays platform-only and mutually exclusive from clinic
roles. Touched auth/user DTOs, `IIdentityService`, `CurrentUserService`, JWT
role claims, Users create/query handlers, Users UI, shell role display, and
tests. Preserved Code's Vetheca navigation change during rebase. Verification:
backend 59/59, frontend 53/53, frontend build OK, E2E 6/6.

### 2026-09-05 — Code (9)
Status: done.
Vetheca step 4: shipped the actual frontend screen (`/vetheca`, gated by
`vetheca.ask` like everything else) - single question box, synthesis
shown first, sources below. Verified live in the browser logged in as
Administrador.

Found a real bug doing that live verification, not from reading code: a
Spanish question ("manejo dietético de la enfermedad renal crónica en
gatos") returned **zero** PubMed articles, while the identical question in
English returned 5. PubMed's index is almost entirely English-language;
searching it verbatim with Spanish text finds almost nothing. Since this
whole app - and its actual users - are Spanish-speaking, this would have
made Vetheca look broken most of the time. Fixed by adding
`ILlmClient.TranslateToSearchQueryAsync`: Claude converts the question to
an English PubMed search query before searching; falls back to the
original question if translation is unavailable (no key, call fails) -
non-regressive, matches every other safe-degradation path in this module.
Verified live with the real key: same Spanish question now returns 5
articles and a full Spanish synthesis. Backend 55/55, frontend 50/50.

Erick separately asked whether PubMed-only is really enough, or if
Vetheca should search other sources too - see my reply to him directly for
the reasoning; short version: he's right that it's a real limitation, it's
already the documented Fase-1+ plan (Crossref, PMC Open Access), and I
flagged CAB Abstracts as the source that would matter most for veterinary-
specific coverage but it's a paid database like Plumb's/VIN, not free like
PubMed - same licensing story as those.

### 2026-09-05 — Code (8) — request for Codex
Status: proposing a new task, not started by Code (it's Auth/Identity/Users
territory — your area, not Vetheca).

**Request from Erick, came up while setting up Vetheca's rollout:** the
current permission system assigns exactly one role per user
(`roles.FirstOrDefault()` in `IdentityService.BuildAuthenticatedUserAsync`,
and `CurrentUserService.Role` reads a single `ClaimTypes.Role` value), and
role permissions are a fixed global map (`RoleDefaultPermissions.cs`) with
no per-user override. This is a real, current gap, not hypothetical: Erick
is exactly the "owner who is also the treating vet" case — today he can't
have one account that both manages the clinic/staff (Administrador
permissions: `users.manage`, `audit.read.all`, etc.) AND writes
consultations/prescriptions (Veterinario-only permissions). Administrador
literally cannot chart today; Veterinario can't manage staff. There's no
good single-role answer for a small clinic where one person does
everything, sometimes down to reception duties too.

**What's being asked:** let one user account hold multiple roles at once,
with permissions merging (union) across all of them, so an
Administrador+Veterinario account gets everything both roles grant.
Concretely this probably means:
- `BuildAuthenticatedUserAsync` needs to look at *all* of a user's roles
  (not `.FirstOrDefault()`), union their permissions from `RolePermissions`,
  and likely emit multiple `ClaimTypes.Role` claims instead of one.
- `CurrentUserService.Role` (singular) and anything else reading "the"
  role as one string needs to either become a list, or you find a way to
  preserve a single "primary" role for display purposes while permissions
  come from the full set. Worth grepping for every place that reads
  `ClaimTypes.Role`/`CurrentUserService.Role` before deciding the exact
  shape — didn't do that audit myself since this isn't my area.
- The Usuarios screen (frontend) needs a UI to assign multiple roles to
  one user. **Explicit UX requirement from Erick, said almost verbatim,
  worth designing around directly:** never expose raw permission codes to
  an admin setting this up — show simple human-readable role toggles
  instead, e.g. a person's role field becomes multi-select chips like
  "[✓] Veterinario  [✓] Encargado de clínica  [ ] Recepción", not a
  checklist of permission codes like `consultations.write`. He's
  explicit and recurring about this: the whole app needs to be as
  frictionless as "a stethoscope or thermometer" for a working vet, or
  they abandon it — this isn't a one-off ask, treat it as a standing bar
  for any new screen, not just this one.
- `PlatformAdministrator` (cross-tenant bypass logic in
  `ApplicationDbContext`'s query filter) probably needs to stay a special
  case handled separately from ordinary multi-role, since its behavior
  (bypassing the clinic tenant filter entirely) isn't just "more
  permissions" - don't assume it folds cleanly into the same union logic
  without checking.

Not blocking Vetheca — that work continues in parallel. Erick would like
this picked up soon since it affects real usability for him right now, not
hypothetically. If you disagree with the approach or want to scope it
differently, say so in this log rather than starting in a direction Erick
hasn't seen — this is exactly the kind of product/UX-shaping change worth
a quick round-trip before deep implementation, per standing rule 2.

### Earlier history (2026-08-25 – 2026-09-05, folded per standing rule 5)

**VetIA/Vetheca origin (before the entries above):** Erick proposed evolving
this product into "VetIA Clinic" — keep the clinic-management app as a paid
core, add an AI research module on top. Code wrote a full analysis
(`docs/VETIA_CLINIC_ANALYSIS.md`) grounded in the real codebase. An early
"no mature competitor exists" claim was wrong (Erick caught it, unresearched)
— a proper market study found real competitors (Vetgo.ai, and more
importantly Vetesoft/MIAUV, an established Colombian incumbent already in
~30% of the country's clinics). Found 15+ existing products using the
"Vet+IA/AI" name pattern, so the module was renamed **`Vetheca`** (checked,
no conflicts). Decisions resolved through this period: LLM = Anthropic
Claude, no patient-identifiable data sent externally in Fase 1, rollout is
Erick-first (informally protected only by "no frontend screen exists yet" —
this system's RBAC is role-based with no per-user override, a real
limitation flagged for whoever builds the frontend or opens access wider).
Erick separately asked about multi-clinic franchises (SuperAdministrador
sees every clinic platform-wide, not just one franchise's own — documented
as a future `Organization`-layer need, not built) and about single accounts
needing combined roles (Administrador+Veterinario) — the latter became the
request that Codex picked up (see the multi-role entries kept above).

**Everything shipped 2026-08-25 to 2026-08-29, before Vetheca implementation
started:** Users management (`users.manage`, activate/deactivate, platform-
admin visibility), Owners/Appointments/Dashboard QA coverage across all 4
roles, minimal Playwright E2E (login, role access, one full clinical-workflow
smoke test), login lockout, logout revocation, printable prescriptions (a
real print-CSS bug caught via genuine headless-Chromium PDF rendering, not
just visual QA — `100vh` resolving against the print page box, bleeding
background + blank second page), Docker deploy support (Dockerfiles,
compose, DEPLOYMENT.md — unverified locally, no Docker access at the time),
health checks + Serilog structured logging, DB backup/restore scripts
(unverified locally, same reason), self-service password reset, auth rate
limiting, SuperAdministrator account management, GitHub Actions CI (4 jobs:
backend/frontend/e2e/docker) — later confirmed genuinely green including the
docker job (GitHub-hosted runners have Docker, the first real verification
of that whole deploy story), fixing a Node 20→24 mismatch along the way
(Angular CLI 22 requires newer Node than the initial CI pin).

**The most serious bug of the whole project, found by the "full suite
before done" rule (2026-09-04):** Codex's JWT security-stamp hardening
(invalidate access tokens immediately on password change/reset, not just
refresh tokens) looked complete and passed casual testing, but a full
backend suite run turned up 31 of 43 tests failing — essentially every
authenticated endpoint. Root cause: `CurrentUserService` cached
`HttpContext.User` in its constructor; the new `OnTokenValidated` handler
transitively constructed it (via `UserManager` → `ApplicationDbContext`'s
tenant filter) *before* authentication finished, permanently freezing an
empty principal for the rest of the request. Fixed by reading
`HttpContext.User` lazily instead of caching it. Would have broken every
authenticated call in a real deployment; E2E's fresh-login-per-test pattern
never exercised the session-restore path that exposed it. This is the
concrete incident standing rule 4 exists to prevent.

**Frontend dependency hygiene:** `npm audit fix` cleared dev-only
`fast-uri`/`qs` advisories, 0 vulnerabilities in both prod and full audit.

