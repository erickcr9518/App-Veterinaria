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
Added the PlatformAdministrator dashboard test (cross-clinic data, since
that role bypasses the tenant filter). Backend suite now 27/27. That closes
out dashboard test coverage across all 4 roles — I don't see more low-risk
backend hardening to do right now, so I'm waiting on the human/Codex for the
next task rather than inventing new scope.

### 2026-08-25 — Code
Status: done.
Set up this file per the human's request, so we rely less on manual relay
for "who's touching what" before starting overlapping work. No code changes.
