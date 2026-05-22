# Architecture

This repository already implements a browser-based cost-management system with
an ASP.NET Core API and PostgreSQL persistence. The harness docs now describe
that live shape instead of a hypothetical pre-buildout stack.

## Implemented Runtime

- Browser app: React 19 + TypeScript + Vite in `frontend/`.
- API: ASP.NET Core 8 host in
  `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/`.
- Persistence: Entity Framework Core with PostgreSQL migrations in
  `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Persistence/EfCore.Persistence/`
  and `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Migrators/Migrators.Postgres/`.
- Documentation and API exploration: NSwag/Swagger plus `/api/health`.
- Platform packaging: Docker build assets, Nginx reverse proxy configs, and a
  GitHub Actions release workflow.

## Current Repository Shape

```text
frontend/
  src/
    features/
      auth/
      main/
        dashboard/
        catalog/
        pricing/
        cost/
        report/
        system/

backend/Ecotel.KCPCMS.BE/
  src/
    Core/
      Application/
      Domain/
      Shared/
    Infrastructures/
      Infrastructure/
      Persistence/
      Logger/
      Integrates/
      Migrators/
    Presentation/
      Host/
```

The backend layout follows the repository's intended layered split much more
closely than the frontend, which is organized by user-facing feature areas.

## Frontend Shape

Frontend routing is defined under `frontend/src/features/`:

- `auth/`: sign-in flow.
- `main/dashboard/`: summary dashboard with year, process-group, and department
  filters.
- `main/catalog/`: reference data, parameters, coefficients, and production
  order maintenance.
- `main/pricing/`: unit-price and norm management for tunneling, trimming, and
  longwall contexts.
- `main/cost/`: production plans, operational cost views, revenue adjustment,
  and lump-sum settlement screens.
- `main/report/`: report navigation and report pages.
- `main/system/`: fixed-key configuration.

The browser client uses a shared fetch wrapper in `frontend/src/lib/api.ts`
with a consistent response envelope:

```text
{
  success: boolean,
  message: string,
  result: ...
}
```

The app relies on `VITE_API_BASE_URL` plus versioned endpoint constants from
`frontend/src/constants/api-enpoint.ts`.

## Backend Shape

The backend API is controller-based and versioned:

- `VersionedApiController` uses `api/v{version:apiVersion}/[controller]`.
- Lowercase routing is enabled in infrastructure startup.
- Health checks are exposed at `/api/health`.
- Swagger/OpenAPI is configured through the infrastructure layer.

The main controller groups are:

- `CatalogController`: catalog/reference-data CRUD plus import/export.
- `PricingController`: pricing, norm, cost-calculation, and settlement APIs.
- `ProductionController`: production outputs, acceptance reports, long-term
  tracking, and anchor-seed workflows.
- `DashboardController`: dashboard summary read model.
- `SystemController`: fixed-key configuration.
- `TokensController`: token issuance and refresh.

Application work is routed through MediatR commands and queries inside
`src/Core/Application/`. EF Core persistence is wired in infrastructure startup
and initialized during host boot.

## Data, Files, And Jobs

- Database provider selection is abstracted, but the active local/runtime stack
  documented in this repository is PostgreSQL.
- Migrations are generated in the PostgreSQL migrator project.
- Uploaded files are stored under `src/Presentation/Host/Uploads/` and served
  from `/uploads`.
- Quartz is registered in infrastructure startup, so scheduled-job support is
  part of the runtime even though this docs pass does not audit every job.
- Serilog provides structured console logging and optional rolling file logs.

## Access Boundary

The repository contains token issuance, refresh-token handling, and frontend
token storage in local storage. However, the currently active catalog, pricing,
production, dashboard, and system controllers all inherit from
`BaseNoAuthController`, which marks them `[AllowAnonymous]`.

Current documentation should therefore distinguish between:

- Auth scaffolding that exists in code.
- Authorization enforcement that is not yet applied to the primary business
  controllers.

## Runtime And Deployment Notes

- Recommended local database startup lives in
  `backend/Ecotel.KCPCMS.BE/docker-compose.yml` and provisions PostgreSQL plus
  pgAdmin.
- Staging/release image build and push flow lives in `Makefile`,
  `docker-compose-build.yaml`, `reverse_proxy/`, and
  `.github/workflows/deploy-release.yml`.
- Root `docker-compose.yaml` is a legacy local stack that still references a
  Mongo-based service layout and does not match the current PostgreSQL-backed
  app documented elsewhere. This docs pass records that mismatch but does not
  change non-`docs/` files.

## Validation Reality

The repository already supports build verification:

- Frontend: `npm run build`
- Backend: `dotnet build .\Ecotel.KCPCMS.BE.sln`

There are no dedicated frontend test suites, `dotnet test` projects, or other
tracked automated behavior checks in the current tree. Documentation must
therefore treat build success and source traceability as the current proof
baseline, not as a substitute for future unit, integration, or E2E coverage.

## Source Surfaces

- `frontend/package.json`
- `frontend/src/features/index.tsx`
- `frontend/src/features/main/router.tsx`
- `frontend/src/features/main/layout/constant.tsx`
- `frontend/src/features/main/catalog/router.tsx`
- `frontend/src/features/main/pricing/router.tsx`
- `frontend/src/features/main/cost/router.tsx`
- `frontend/src/features/main/report/router.tsx`
- `frontend/src/features/main/system/router.tsx`
- `frontend/src/lib/api.ts`
- `frontend/src/constants/api-enpoint.ts`
- `frontend/src/data/auth/auth-provider.tsx`
- `backend/Ecotel.KCPCMS.BE/Ecotel.KCPCMS.BE.sln`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Program.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Configurations/Startup.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Base/*.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/*.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/System/SystemController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Infrastructure/Startup.cs`
- `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Persistence/EfCore.Persistence/Startup.cs`
- `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Logger/Logging/Serilog/Extensions.cs`
- `backend/Ecotel.KCPCMS.BE/docker-compose.yml`
- `docker-compose.yaml`
- `docker-compose-build.yaml`
- `Makefile`
- `reverse_proxy/nginx_release.conf`
- `reverse_proxy/nginx_staging.conf`
- `.github/workflows/deploy-release.yml`
