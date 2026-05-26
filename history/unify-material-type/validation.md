# Validation: Phase 2 - AssignmentCode Role Split

## Reality Gate

```text
REALITY GATE REPORT
Mode: high_risk_feature
Current work: Phase 2 adds an enum role to AssignmentCodeMaterial, extends AssignmentCode to materialIds + otherMaterialIds, repairs material readers to AssignmentCodeIds, restores dual-list contract-code UX, and keeps unresolved material quick-create on the normal material path.
MODE FIT: PASS
REPO FIT: PASS
ASSUMPTIONS: PASS
SMALLER PATH: PASS
PROOF SURFACE: PASS
Decision: ready
Evidence:
- backend/.../Domain/Entities/Index/AssignmentCodeMaterial.cs currently stores only AssignmentCodeId + MaterialId, so adding an enum role is the correct minimal persistence extension
- backend/.../Dto/Catalog/AssignmentCode/AssignmentCodeDto.cs plus create/update/detail/import-export handlers currently assume one material list, and Phase 2 now explicitly absorbs that entire surface
- backend/.../Catalog/Index/Material/Specifications/MaterialsByPaginationSpec.cs and GetMaterialByIdQuery.cs are now explicitly in-scope reader repairs via Story S2 / Bead B2
- frontend/src/features/main/catalog/contract-code/* is explicitly covered by Story S3 / Bead B3
- frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/* plus acceptance-report-editor/import-dialog.tsx are explicitly covered by Story S4 / Bead B4
- frontend/src/features/main/catalog/asset/internal/form.tsx already creates material with assigmentCodeId null and isOtherMaterial false, which matches the approved safe fallback for quick-create
- .beads now contains a real current-phase issue graph rooted at khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb
```

## Feasibility Matrix

| Part / Assumption | Risk | Proof Required | Evidence | Result |
| --- | --- | --- | --- | --- |
| Backend assignment-code persistence must change to support two roles | HIGH | Inspect join entity, DTOs, commands, and EF mapping | `AssignmentCodeMaterial.cs` has no role field today; Phase 2 now explicitly adds an enum role and keeps the rest of the model intact | PASS |
| The current phase plan includes the full backend assignment-code surface that must change | HIGH | Inspect whether create/update/detail/import/export are in scope | `phase-2-contract.md` Story S1 and Bead B1 explicitly include create/update/detail/import/export plus migration | PASS |
| Multi-group material membership is represented safely in material readers | HIGH | Inspect whether singular material reader assumptions are absorbed into the phase | `phase-2-contract.md` Story S2 and Bead B2 now explicitly cover `MaterialsByPaginationSpec.cs`, `GetMaterialByIdQuery.cs`, and FE asset readers via `AssignmentCodeIds` | PASS |
| Assignment-code import/export has an unambiguous two-role contract | HIGH | Check whether the plan defines how import/export round-trips role-aware membership | Phase 2 now explicitly requires an import/export role column in S1, B1, and `design.md` | PASS |
| Contract-code FE changes are covered by current work | HIGH | Inspect FE schema/form/detail surfaces | `frontend/src/features/main/catalog/contract-code/schema.tsx`, `actions.tsx`, and `page.tsx` remain directly targeted by Story S3 / Bead B3 | PASS |
| Quick-create / unresolved flows avoid ambiguous assignment-role writes | MEDIUM | Inspect whether the write path is now explicitly safe | The user-approved fallback is now explicit: material quick-create defaults to the normal material path; existing `AssetInternalForm` already posts `assigmentCodeId: null` and `isOtherMaterial: false` | PASS |
| Dependent pricing/reporting readers of ContractCode can stay outside the current phase | MEDIUM | Inspect live consumers for list-vs-detail coupling | repo scans still show most pricing forms consume only `ContractCode` list fields (`id/code/name/unit/currentPrice`), not assignment-code detail materials | PASS |
| Execution bead graph exists for this current phase | MEDIUM | Check `.beads` for real current-work issues and dependencies | `.beads` now contains root issue `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb` and child beads `.3 -> .1 -> .4 -> .2` | PASS |

## Validation Decision

```text
READY - CURRENT WORK PASSES
```

The repaired Phase 2 boundary is now execution-ready. The persistence change is minimal and explicit, the material reader ambiguity has been absorbed into the same phase, the quick-create path now has a safe fallback, and the current work is represented by a real bead graph in `.beads`.

## Validated Current Beads

- `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.3` - B1 Extend AssignmentCode role persistence and APIs
- `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.1` - B2 Repair material readers for multi-group membership
- `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.4` - B3 Restore contract-code dual-role UI
- `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.2` - B4 Restore safe unresolved and quick-create behavior

## Execution Order

1. `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.3`
2. `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.1`
3. `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.4`
4. `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.2`
