# Unify Material Type - Context

**Feature slug:** unify-material-type
**Date:** 2026-05-26
**Exploring session:** complete
**Scope:** Standard
**Domain types:** SEE | CALL | ORGANIZE

## Feature Boundary

Unify the material catalog so the system no longer separates `MaterialType` into two user-facing groups, migrates all existing `MaterialType = 2` records to `1`, removes the external asset surface, and preserves downstream `Part` versus `OtherPart` behavior through `assigmentCodeId` instead of `MaterialType`.

## Locked Decisions

These are fixed. Planning must implement them exactly.

- **D1:** Normalize all existing material data to `MaterialType = 1`, migrate every current `MaterialType = 2` record to `1`, and show all materials on one combined screen and API surface.
  - Rationale: the product no longer wants two separate material groups anywhere in catalog management.
- **D2:** In acceptance-report and report-export flows, stop using `MaterialType` to infer downstream behavior. Use another rule for `Part` versus `OtherPart`.
- **D3:** The downstream rule is: `assigmentCodeId != null` means `Part`; `assigmentCodeId == null` means `OtherPart`.
- **D4:** Remove the frontend external asset page entirely and keep only the current internal asset page as the single material catalog screen.
- **D5:** Remove `materialType` from the catalog material import/export contract and stop supporting `MaterialType = 2` completely in backend contracts.
- **D6:** Even after removing `MaterialType = 2`, the UI must still keep the user-facing choice `Vật tư, tài sản khác` in `contract-code`, unresolved create flows, and quick-create popups.
  - Rationale: the business still needs to classify a material as "other" in the context of a specific `AssignmentCode` or quick-create flow without restoring a second material type.
- **D7:** A material is considered `Vật tư, tài sản khác` in exactly two cases:
  - it does not belong to any `AssignmentCode`
  - or it belongs to the `Vật tư, tài sản khác` bucket inside the relevant `AssignmentCode`
- **D8:** `contract-code` is the main `Nhóm vật tư, tài sản` screen and must explicitly model two separate material lists per `AssignmentCode`:
  - `materialIds`: `Vật tư, tài sản`
  - `otherMaterialIds`: `Vật tư, tài sản khác`
- **D9:** The same material may appear in multiple `AssignmentCode`s and may play different roles across them; for example, it can be `Vật tư, tài sản khác` in one group and `Vật tư, tài sản` in another group.

### Agent's Discretion

The user delegated the exact code-change plan, migration shape, API cleanup order, and FE/BE file selection, as long as the locked decisions above remain unchanged.

## Specific Ideas And References

- The provided screenshot shows the current `Danh muc -> Vat tu, tai san` menu split into `Vat tu, tai san` and `Vat tu, tai san khac`. That split must disappear from the user-facing catalog.
- The later screenshot and clarification change one important point: the catalog remains unified, but `Nhóm vật tư, tài sản` and related quick-create flows still need a user-facing `Vật tư, tài sản khác` selection. That selection is now contextual to `AssignmentCode`, not a second `MaterialType`.

## Existing Code Context

From the quick scout. Downstream agents read these before planning.

### Reusable Assets

- `frontend/src/features/main/catalog/asset/internal/page.tsx` - current single-screen table flow to keep and expand into the only material catalog page.
- `frontend/src/features/main/catalog/asset/internal/form.tsx` - current create/update dialog for `materialType: 1`; likely the base form after removing the second variant.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/Material/Commands/ImportMaterialExcelCommand.cs` - current import handler that scopes work by request `MaterialType`; needs contract and data-scope redesign.

### Established Patterns

- Frontend catalog pages use `DataTable` with `query`, `onExport`, and `onImport` hooks; see `frontend/src/features/main/catalog/asset/internal/page.tsx`.
- The current external material flow is duplicated from the internal flow with `materialType: 2`; see `frontend/src/features/main/catalog/asset/external/page.tsx` and `frontend/src/features/main/catalog/asset/external/form.tsx`.
- Backend catalog material endpoints currently thread `MaterialType` through controller -> query/command -> spec/handler; see `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/CatalogController.cs` and `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/Material/Queries/GetAllMaterialQuery.cs`.
- Downstream production/acceptance code currently infers `Part` versus `OtherPart` from `MaterialType.MaterialOutContract`; that inference must move to `assigmentCodeId`; see `GetAcceptanceReportByIdQuery.cs`, `GetProductionOutputDetailQuery.cs`, and `AcceptanceReportExcelService.cs`.

### Integration Points

- `frontend/src/features/main/catalog/router.tsx` - owns the split `assets/internal` and `assets/external` routes.
- `frontend/src/features/main/layout/constant.tsx` - owns the navigation link that currently points to `/catalogs/assets/external`.
- `frontend/src/features/main/catalog/contract-code/actions.tsx` - current `AssignmentCode` form; today it only stores one flat `materialIds` list and therefore cannot represent `otherMaterialIds` yet.
- `frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/unresolved-catalog-create-dialog.tsx` - imports the external asset form and is coupled to the removed external surface.
- `frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/unresolved-create-dialog.tsx` - owns the current user-facing type chooser and must keep a `Vật tư, tài sản khác` option without reviving `MaterialType = 2`.
- `backend/Ecotel.KCPCMS.BE/src/Core/Domain/Common/Enums/EnumCommon.cs` - defines `MaterialOutContract = 2`.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Dto/Catalog/AssignmentCode/AssignmentCodeDto.cs` - current DTO only exposes `MaterialIds`; likely needs extension to carry `OtherMaterialIds`.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/AssignmentCodes/Commands/CreateAssignmentCodeCommand.cs` - current create path validates a single material list.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/AssignmentCodes/Commands/UpdateAssignmentCodeCommand.cs` - current update path validates a single material list.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/Material/Queries/ExportExcelMaterialQuery.cs` - currently exports by `MaterialType`.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Production/AcceptanceReports/Queries/GetAcceptanceReportByIdQuery.cs` - maps `MaterialOutContract` to `OtherPart`.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Production/ProductionOutputs/Queries/GetProductionOutputDetailQuery.cs` - maps `MaterialOutContract` to `OtherPart`.
- `backend/Ecotel.KCPCMS.BE/src/Infrastructures/Infrastructure/Services/Catalog/AcceptanceReportExcelService.cs` - emits export behavior based on `MaterialType`.

## Canonical References

- `AGENTS.md` - repo operating constraints and source-of-truth order.
- `docs/HARNESS.md` - harness purpose and growth rule.
- `docs/FEATURE_INTAKE.md` - intake lane and risk framing.
- `frontend/src/features/main/catalog/router.tsx` - current catalog routing split.
- `frontend/src/features/main/catalog/asset/internal/page.tsx` - retained FE surface.
- `frontend/src/features/main/catalog/asset/external/page.tsx` - removed FE surface.
- `backend/Ecotel.KCPCMS.BE/src/Presentation/Host/Controllers/Catalog/CatalogController.cs` - current material API contract.
- `backend/Ecotel.KCPCMS.BE/src/Core/Application/Catalog/Index/Material/Commands/ImportMaterialExcelCommand.cs` - current import behavior.
- `backend/Ecotel.KCPCMS.BE/src/Core/Domain/Common/Enums/EnumCommon.cs` - current enum definition.

## Outstanding Questions

### Deferred To Planning

- [ ] Enumerate every BE branch that still treats `MaterialOutContract` specially outside the core material catalog and acceptance/report flows. Planning needs the full removal surface.
- [ ] Define the exact migration/update path for existing rows, including whether one EF migration is enough or whether a one-off normalization step is already partially present and should be superseded.
- [ ] Design the data contract change for `AssignmentCode` so it can persist both `materialIds` and `otherMaterialIds` without breaking dependent pricing/reporting screens that consume `ContractCode`.
- [ ] Verify how quick-create and unresolved-create flows should map a newly created material into `materialIds` versus `otherMaterialIds` for the currently active `AssignmentCode`.

## Handoff Note

CONTEXT.md is the source of truth. Decision IDs are stable. Planning reads locked decisions, code context, canonical references, and deferred-to-planning questions. Validating and reviewing use locked decisions for coverage and UAT.
