# Story Backlog

The current codebase is large enough that future implementation work should be
sliced from the domains that already exist instead of pretending the product is
still unspecced.

## Current Candidate Epics

| Epic | Description | Status |
| --- | --- | --- |
| E01-catalog-reference-data | Units, departments, process groups, materials, products, parameters, coefficients, and setup screens | unsliced |
| E02-pricing-and-norms | Unit-price management for tunneling, trimming, and longwall contexts | unsliced |
| E03-production-and-long-term-tracking | Production outputs, acceptance reports, long-term material tracking, anchor-seed workflows | unsliced |
| E04-cost-planning-and-settlement | Production plans, revenue adjustment, cost calculations, and lump-sum settlement | unsliced |
| E05-reporting-and-dashboard | Dashboard views, export-driven reports, and reporting UX cleanup | unsliced |
| E06-access-system-and-platform | Auth enforcement, fixed-key configuration, runtime consistency, and deployment/docs hardening | unsliced |

## Selected Work

| Story | Reason selected | Status |
| --- | --- | --- |
| `docs/stories/docs-backfill-current-state.md` | Current docs still described the product as pre-implementation even though the system already exists | implemented |

## Source Surfaces

- `frontend/src/features/main/`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/`
- `docs/product/*.md`
