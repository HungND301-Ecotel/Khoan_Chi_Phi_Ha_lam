# Product Overview

The repository implements a browser-based internal system for managing
contract-based cost accounting at Hà Lầm. The current product surface spans
reference data, operational pricing, production tracking, settlement, and
reporting.

## Primary User-Facing Areas

- Dashboard: annual summary view with process-group and department filters.
- Catalog: setup and maintenance for reference data used by pricing and
  operations.
- Pricing and norms: unit-price and coefficient maintenance across tunneling,
  trimming, and longwall workflows.
- Cost operations: production plans, production cost views, revenue adjustment,
  and lump-sum settlement.
- Reporting: report pages and export-driven report generation.
- System: fixed-key configuration.

## Core Domains

- Organizational and production reference data.
- Materials, assets, products, and production orders.
- Technical parameters and adjustment coefficients.
- Unit pricing by mining context.
- Production outputs and acceptance reports.
- Long-term material tracking and anchor-seed accounting.
- Settlement and report exports.

## Delivery Shape

- Browser UI: React/Vite app under `frontend/src/features/`.
- API: versioned ASP.NET Core controllers under
  `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/`.
- Database: PostgreSQL via EF Core migrations.
- Imports/exports: Excel workflows on both frontend and backend.

## Current Documentation Limits

- The repository contains implemented code but not a full story history for all
  previously built features.
- Automated tests are not yet present as dedicated frontend or backend test
  projects.
- Platform and product docs should therefore stay explicit about where current
  truth comes from: source inspection, build commands, and runtime config.

## Source Surfaces

- `frontend/src/features/index.tsx`
- `frontend/src/features/main/router.tsx`
- `frontend/src/features/main/layout/constant.tsx`
- `frontend/src/features/main/dashboard/page.tsx`
- `frontend/src/features/main/catalog/router.tsx`
- `frontend/src/features/main/pricing/router.tsx`
- `frontend/src/features/main/cost/router.tsx`
- `frontend/src/features/main/report/router.tsx`
- `frontend/src/features/main/system/router.tsx`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/DashboardController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/CatalogController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/PricingController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/ProductionController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/System/SystemController.cs`
