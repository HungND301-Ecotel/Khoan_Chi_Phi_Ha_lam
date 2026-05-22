# Pricing And Costs

The pricing and cost surfaces define operational unit prices, coefficients, and
settlement calculations for multiple mining contexts.

## Pricing Contexts

The frontend navigation and backend endpoints split pricing across:

- Tunneling.
- Trimming.
- Longwall panel.

## Pricing Families

- Material unit prices.
- Tunnel support and drilling material pricing.
- Slide unit prices.
- Maintenance/SCTX unit prices for equipment and parts.
- Electricity unit prices for equipment.
- Low-value perishable supply unit prices.

Each family exposes CRUD plus Excel import/export flows through
`PricingController` and `frontend/src/constants/api-enpoint.ts`.

## Cost Planning And Calculation

Cost and pricing logic extend beyond catalog-like unit prices into operational
calculation surfaces:

- Product unit prices.
- Planned product unit prices by department.
- Actual product unit prices.
- Adjustment product unit prices.
- Planned material cost.
- Planned maintenance cost.
- Planned electricity cost.
- Actual electricity cost.
- Adjustment read models for material, maintenance, and electricity cost.

The browser surfaces under `frontend/src/features/main/cost/` consume these
APIs as operational screens rather than pure catalog maintenance.

## Lump-Sum Final Settlement

The repository includes month and quarter settlement flows with:

- list queries,
- quarter custom-cost management,
- month special-quantity updates,
- carry-forward updates,
- Excel exports for month and quarter reports.

## Source Surfaces

- `frontend/src/features/main/pricing/router.tsx`
- `frontend/src/features/main/cost/router.tsx`
- `frontend/src/features/main/cost/producttion/router.tsx`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/PricingController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Pricing/`
