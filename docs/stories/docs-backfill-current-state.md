# US-000 Docs Backfill Current State

## Status

implemented

## Lane

normal

## Product Contract

`docs/` must describe the implemented React + ASP.NET Core + PostgreSQL system
that already exists in this repository. The docs must stop claiming the product
is unimplemented and must backfill product domains, architecture, story/backlog
context, and validation reality without changing application code.

## Relevant Product Docs

- `docs/ARCHITECTURE.md`
- `docs/product/overview.md`
- `docs/product/catalog.md`
- `docs/product/pricing-and-costs.md`
- `docs/product/operations.md`
- `docs/product/reporting.md`
- `docs/product/access-and-platform.md`

## Acceptance Criteria

- Top-level docs no longer describe the repository as pre-implementation.
- `docs/product/` contains real domain docs tied to current source surfaces.
- `docs/stories/*` and `docs/TEST_MATRIX.md` reflect the backfill task and the
  current proof baseline.
- Any codebase/doc mismatch that cannot be fixed within `docs/` is documented
  explicitly.

## Design Notes

- Commands: documentation-only edits under `docs/`.
- Queries: source inspection across frontend routes, backend controllers, and
  runtime config.
- API: versioned controller APIs summarized from endpoint constants and
  controller methods.
- Tables: PostgreSQL-backed runtime documented from EF Core persistence and
  backend compose assets.
- Domain rules: preserve harness history; document current auth/runtime
  mismatches honestly.
- UI surfaces: dashboard, catalog, pricing, cost, reporting, auth, and system
  areas.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | None currently available in the repository |
| Integration | `dotnet build .\Ecotel.KCPCMS.BE.sln` succeeds |
| E2E | None currently available in the repository |
| Platform | `npm run build` succeeds and docs trace to runtime/deploy files |
| Release | Final docs sweep removes stale pre-implementation claims |

## Harness Delta

- Backfilled `docs/product/` with real domain docs.
- Replaced pre-implementation language in docs that had become false.
- Recorded the root `docker-compose.yaml` versus PostgreSQL runtime mismatch as
  an unresolved documented issue because this task cannot edit non-`docs/`
  files.

## Evidence

- `npm run build` succeeded in `frontend` on 2026-05-22.
- `dotnet build .\Ecotel.KCPCMS.BE.sln` succeeded in
  `backend\Ecotel.KCPCMS.BE` on 2026-05-22.
- Initial in-sandbox validation failures were environment-related:
  Vite build access restrictions inside the sandbox and blocked NuGet network
  access for backend restore. Final proof was collected with elevated build
  execution only; no source files outside `docs/` were changed.
