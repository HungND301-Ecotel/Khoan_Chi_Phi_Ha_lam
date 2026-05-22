# Documentation Map

This directory now serves two purposes:

1. Preserve the collaboration harness.
2. Hold the living product contract for the implemented React + ASP.NET Core +
   PostgreSQL system in this repository.

## Main Files

- `HARNESS.md`: human-agent operating model.
- `FEATURE_INTAKE.md`: intake classification and lane rules.
- `ARCHITECTURE.md`: current implementation shape, boundaries, and runtime
  notes.
- `TEST_MATRIX.md`: current proof map and validation gaps.
- `HARNESS_BACKLOG.md`: harness improvements discovered while working.
- `GLOSSARY.md`: shared terms and language.

## Folders

- `product/`: current product truth for implemented domains.
- `stories/`: selected work packets, evidence, and backlog slices.
- `decisions/`: durable historical decisions and tradeoffs.
- `templates/`: reusable story, decision, intake, and validation formats.

## Current Product Docs

- `product/overview.md`
- `product/catalog.md`
- `product/pricing-and-costs.md`
- `product/operations.md`
- `product/reporting.md`
- `product/access-and-platform.md`

## Current State

- Application code exists in `frontend/` and `backend/Ecotel.KCPCMS.BE/`.
- Build and deployment automation exists through Docker assets, Nginx config,
  and GitHub Actions.
- Product docs are no longer placeholders waiting for a first spec; they should
  track the codebase that already exists.
- Automated behavior proof is still thin. Current validation truth is build
  verification plus source-backed documentation, not comprehensive tests.

## Source Surfaces

- `README.md`
- `AGENTS.md`
- `docs/HARNESS.md`
- `docs/FEATURE_INTAKE.md`
- `docs/decisions/*`
- `frontend/package.json`
- `frontend/src/`
- `backend/Ecotel.KCPCMS.BE/Ecotel.KCPCMS.BE.sln`
- `backend/Ecotel.KCPCMS.BE/src/`
- `backend/Ecotel.KCPCMS.BE/docker-compose.yml`
- `docker-compose.yaml`
- `docker-compose-build.yaml`
- `Makefile`
- `reverse_proxy/`
- `.github/workflows/deploy-release.yml`
