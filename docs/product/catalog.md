# Catalog Domain

The catalog area provides the reference and configuration data that the pricing,
production, and reporting flows depend on.

## Reference Data Surfaces

- Units of measure.
- Departments.
- Process groups and production steps.
- Contract-code groups for materials/assets.
- Materials and external/internal asset records.
- Products.
- Production orders.

## Parameter And Technical Setup

- Passports.
- Hardness/strength values.
- Stone clamp ratios.
- Insert items.
- Technologies.
- Seam faces.
- Support steps.
- Cutting thicknesses.
- Longwall parameters.
- Power values.

## Adjustment And Coefficient Setup

- Adjustment factors.
- Adjustment factor descriptions/interpreters.
- Norm factors.
- Accepted savings-rate configuration.
- Ak-factor configuration.
- Revenue-cost adjustment configuration.

## System Configuration Tied To Catalog Data

- Fixed keys are exposed under the system area but are structurally related to
  the same setup/configuration layer.

## UX And API Pattern

Most catalog surfaces follow the same pattern:

- Paginated list/read endpoint.
- Detail endpoint by id.
- Create and update commands.
- Single-delete and bulk-delete commands.
- Excel import.
- Excel export.

That pattern is visible in both frontend endpoint constants and the backend
`CatalogController`.

## Source Surfaces

- `frontend/src/features/main/catalog/router.tsx`
- `frontend/src/features/main/catalog/process/router.tsx`
- `frontend/src/features/main/catalog/parameter/router.tsx`
- `frontend/src/features/main/catalog/adjustment/router.tsx`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/CatalogController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/System/SystemController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/`
