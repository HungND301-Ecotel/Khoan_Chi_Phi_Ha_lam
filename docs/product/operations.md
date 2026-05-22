# Operations

The operational layer combines dashboard views, production tracking, acceptance
reports, and long-term material workflows.

## Dashboard

The default landing page is a yearly dashboard that filters by:

- process group,
- department,
- year.

It visualizes monthly planned revenue, adjustment revenue, actual cost, and
production volume summaries.

## Production Outputs

Production operations include:

- paginated production-output listing,
- production-output detail,
- create/update flows,
- bulk create/update flows,
- single and bulk deletion.

## Acceptance Reports

Acceptance-report workflows include:

- Excel file upload for report processing,
- create/update flows,
- raw detail retrieval,
- download/export,
- additional-cost read models,
- SCTX revenue lookup by assignment code.

## Long-Term Tracking

The repository already implements long-term material accounting support:

- long-term tracking list and detail,
- allocation-ratio updates,
- department long-term anchor-seed detail,
- long-term anchor-seed import/export,
- anchor-seed update commands.

## Cost Operations UI

The `cost/` browser area exposes:

- production plan management,
- production cost view,
- revenue adjustment view,
- lump-sum final settlement month and quarter pages.

## Source Surfaces

- `frontend/src/features/main/dashboard/page.tsx`
- `frontend/src/features/main/cost/router.tsx`
- `frontend/src/features/main/cost/producttion/router.tsx`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/DashboardController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/ProductionController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Dashboard/`
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Production/`
