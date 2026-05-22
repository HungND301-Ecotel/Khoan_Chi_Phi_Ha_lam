# Access And Platform

This document records the current auth, runtime, and deployment shape visible in
the repository.

## Authentication Surface

- The frontend exposes a sign-in page under `/auth/sign-in`.
- Sign-in posts credentials to `/v1/tokens`.
- Refresh tokens are supported through `/v1/tokens/refresh`.
- The browser stores `token`, `refreshToken`, and
  `refreshTokenExpiryTime` in local storage.

## Current Authorization Reality

The codebase contains both `[Authorize]` and `[AllowAnonymous]` controller
bases, but the active business controllers currently inherit from
`BaseNoAuthController`:

- `CatalogController`
- `PricingController`
- `ProductionController`
- `DashboardController`
- `SystemController`

That means auth scaffolding exists, but the primary business surfaces are not
currently protected by controller-level authorization.

## Runtime Endpoints

- Swagger is exposed from the ASP.NET Core host.
- Health checks are mapped to `/api/health`.
- Uploaded files are served from `/uploads`.
- The frontend expects `VITE_API_BASE_URL` to point at the backend API root.

## Local And Deployment Stack

- Local backend database workflow is centered on
  `backend/Ecotel.KCPCMS.BE/docker-compose.yml`, which provisions PostgreSQL and
  pgAdmin.
- Release image building is defined in `docker-compose-build.yaml`.
- Reverse proxy configuration lives in `reverse_proxy/`.
- Release automation is tracked in `.github/workflows/deploy-release.yml`.

## Known Configuration Mismatch

The root `docker-compose.yaml` still describes an older local stack with
Mongo-based services and names that no longer match the PostgreSQL-backed app
described by the backend compose file and current README. This docs pass records
that mismatch as current repository truth but does not correct non-`docs/`
files.

## Source Surfaces

- `frontend/src/features/auth/router.tsx`
- `frontend/src/features/auth/sign-in/form.tsx`
- `frontend/src/data/auth/auth-provider.tsx`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Base/BaseAuthController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Base/BaseNoAuthController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Common/TokenAuthController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/CatalogController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/PricingController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/ProductionController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/DashboardController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/System/SystemController.cs`
- `backend/Ecotel.KCPCMS.BE/docker-compose.yml`
- `docker-compose.yaml`
- `docker-compose-build.yaml`
- `Makefile`
- `reverse_proxy/`
- `.github/workflows/deploy-release.yml`
