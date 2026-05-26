# Story Map: Phase 2 - AssignmentCode Role Split

Dependency diagram: Entry -> S1 Extend backend AssignmentCode persistence and contract -> S2 Repair material reader contract -> S3 Restore role-aware contract-code UX -> S4 Restore safe unresolved / quick-create behavior -> Exit

## Story Table

| Story | Outcome                                                                                                            | Contributes To                                       | Creates                                                            | Done                                                                                                |
| ----- | ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------- | ------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| S1    | AssignmentCode APIs and persistence can represent two contextual material roles through an enum on the join entity | truthful backend contract                            | role-aware assignment-code storage and import/export role contract | backend create/update/detail/import-export paths distinguish `materialIds` from `otherMaterialIds`  |
| S2    | Material list/detail readers can represent multi-group membership safely                                           | unified catalog truth for multi-group materials      | `AssignmentCodeIds`-based reader shape                             | material readers no longer flatten one material to one assignment group                             |
| S3    | Contract-code UI edits and reads both lists cleanly                                                                | main business screen matches clarified product truth | dual-role FE form and detail rendering                             | `Nhóm vật tư, tài sản` no longer relies on one flat `materialIds` list                              |
| S4    | Shared quick-create and unresolved flows stay usable without ambiguous role attachment                             | safe end-to-end flow                                 | default-to-main quick-create behavior                              | material quick-create no longer depends on legacy `MaterialType = 2` or implicit `other` attachment |

## Current Bead Graph

These are real `br` beads for the current phase and should be validated as the current execution surface.

| Bead                                                                  | Scope                                                                                                                                                                                                | Owned Surface                                                                                                                                 | Depends On | Validation Status  |
| --------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ------------------ |
| `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.3` (`B1`) | Extend assignment-code persistence, DTOs, commands, queries, import/export, and migration with an enum role plus `materialIds` / `otherMaterialIds`, including an explicit import/export role column | `backend/.../AssignmentCodes/*`, `AssignmentCodeMaterial.cs`, EF mapping, migrations                                                          | none       | pending validating |
| `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.1` (`B2`) | Repair material reader contract from singular assignment fields to `AssignmentCodeIds` and update FE asset reader types accordingly                                                                  | `backend/.../Catalog/Index/Material/*`, `frontend/src/features/main/catalog/asset/*`                                                          | `B1`       | pending validating |
| `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.4` (`B3`) | Restore dual-list contract-code editing and detail rendering with current FE form patterns                                                                                                           | `frontend/src/features/main/catalog/contract-code/*`                                                                                          | `B2`       | pending validating |
| `khoan_chi_phi_ha_lam-phase-2-assignmentcode-role-split-shb.2` (`B4`) | Restore unresolved / quick-create behavior with default-to-main material creation                                                                                                                    | `frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/*`, related acceptance-report import dialog surfaces | `B3`       | pending validating |

## Story-To-Bead Mapping

- S1 -> B1
- S2 -> B2
- S3 -> B3
- S4 -> B4

## Validation Focus

- Check whether the enum role on `AssignmentCodeMaterial` is sufficient to preserve existing links safely through migration.
- Check whether `AssignmentCodeIds` is enough as the retained reader surface, or whether additional role-aware reader fields are required immediately.
- Check whether the quick-create fallback truly removes the need for contextual assignment-role writes in this phase.
