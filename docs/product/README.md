# Product Docs

This directory now contains the current product contract for the implemented
system in this repository. The files are split by domain so future work can
update only the affected area instead of rewriting a single monolithic spec.

## Current Domain Files

- `overview.md`: product summary and top-level domain map.
- `catalog.md`: reference data, coefficients, and setup domains.
- `pricing-and-costs.md`: unit-price, norm, planning, and settlement surfaces.
- `operations.md`: dashboard, production, acceptance report, and long-term
  tracking workflows.
- `reporting.md`: current report pages and export-oriented reporting surfaces.
- `access-and-platform.md`: auth model, system configuration, runtime, and
  deployment notes.

## Update Rule

When behavior changes:

1. Update the affected product doc.
2. Update or create the story packet.
3. Update `docs/TEST_MATRIX.md`.
4. Record a decision if the change affects architecture, scope, risk, or a
   previously settled product rule.

## Source Surfaces

- `frontend/src/features/main/`
- `frontend/src/features/auth/`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/`
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/`
- `backend/Ecotel.KCPCMS.BE/src/Infrastructures/`
