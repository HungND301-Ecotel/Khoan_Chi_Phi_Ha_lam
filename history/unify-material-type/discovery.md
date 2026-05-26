# Discovery: Unify Material Type

## Scout Summary

- Scout source: `node .codex/khuym_status.mjs --json`
- Date: 2026-05-26
- gkg status: repo supported only partially and gkg server not ready, so planning used grep and direct file inspection as fallback.

## Architecture Snapshot

### Frontend

- Catalog routing splits material catalog into `assets/internal` and `assets/external` in `frontend/src/features/main/catalog/router.tsx`.
- Main navigation also exposes the same split under a nested menu in `frontend/src/features/main/layout/constant.tsx`.
- The retained material catalog surface is `frontend/src/features/main/catalog/asset/internal/page.tsx` plus `internal/form.tsx`.
- The removable duplicated surface is `frontend/src/features/main/catalog/asset/external/page.tsx` plus `external/form.tsx`.
- The unresolved acceptance-report create dialog imports both asset forms and still offers a material subtype choice in `frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/unresolved-catalog-create-dialog.tsx`.

### Backend

- `CatalogController` exposes material list/export/import endpoints that currently accept `MaterialType` as query/form input.
- `GetAllMaterialQuery`, `ExportExcelMaterialQuery`, and `ImportMaterialExcelCommand` all carry `MaterialType` through the application layer.
- `EnumCommon.cs` defines `MaterialInContract = 1` and `MaterialOutContract = 2`.
- Downstream production and report flows infer `Part` vs `OtherPart` from `MaterialType.MaterialOutContract` in:
  - `GetAcceptanceReportByIdQuery.cs`
  - `GetProductionOutputDetailQuery.cs`
  - `AcceptanceReportExcelService.cs`
- `GetMaintainUnitPriceEquipmentByIdQuery.cs` also still maps `MaterialOutContract` to `OtherPart`.

## Existing Data Reality

- A previous migration already normalizes invalid material type values to `1`, but it does not collapse valid `2` records into `1`.
- Existing migration: `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Migrators/Migrators.Postgres/Migrations/20260521090000_normalizeMaterialTypeToContractBuckets.cs`

## Constraints

- Preserve the current React feature folder structure and ASP.NET Core layered structure.
- Reuse the current internal material screen instead of inventing a new FE pattern.
- Do not add tests in this task; current proof baseline remains source inspection plus build verification when implementation happens.
- Remove `MaterialType = 2` support completely rather than keeping compatibility shims in backend contracts.

## Warnings

- This is not a local catalog-only change. `MaterialType` leaks into operational and reporting behavior.
- Acceptance-report import logic currently interprets `MaterialOutContract` as a material item instead of a part item, so the downstream classification rule change must be made consistently.
- The repo has both TypeScript and C# surfaces, so manual file inspection was required where gkg coverage is limited.
