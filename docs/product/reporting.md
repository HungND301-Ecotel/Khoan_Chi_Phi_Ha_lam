# Reporting

Reporting is implemented as a dedicated browser area backed by export-oriented
and read-model APIs from the pricing and production domains.

## Current Report Categories

- Bảng tính đơn giá SCTX và điện năng.
- Bảng hạch toán chi phí dài kỳ.
- Bảng nghiệm thu vật tư và kết chuyển chi phí.
- Bảng quyết toán.
- Bảng thanh toán.
- Báo cáo doanh thu SCTX.

The report router is category-driven, so new categories can be added without
rewriting the entire page shell.

## Current Implementation Notes

- The router still contains placeholder support for categories without a final
  page component.
- A commented-out `raw-acceptance-report` category indicates reporting scope is
  still evolving inside the current codebase.
- Several reports depend on export endpoints implemented under pricing and
  production controllers rather than on a separate report controller.

## Source Surfaces

- `frontend/src/features/main/report/router.tsx`
- `frontend/src/features/main/report/types.ts`
- `frontend/src/features/main/report/`
- `frontend/src/constants/api-enpoint.ts`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/PricingController.cs`
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/ProductionController.cs`
