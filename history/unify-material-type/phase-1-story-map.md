# Story Map: Phase 1 - Catalog Merge And UI Consolidation

Dependency diagram: Entry -> S1 Normalize catalog backend behavior for type 1 and 2 -> S2 Collapse FE material pages into the internal page -> S3 Rewrite contract-code and shared entry points -> Exit

## Story Table

| Story | Outcome | Contributes To | Creates | Done |
| --- | --- | --- | --- | --- |
| S1 | Catalog backend requests for type `1` and `2` resolve to the same material dataset and import/export behavior | Unified backend catalog truth | normalization + merged catalog handlers | backend catalog handlers no longer create a user-visible type `1`/`2` split |
| S2 | Navigation, router, and surviving asset UI expose one material screen | Unified user-facing catalog | single FE asset path | `external` is removed and the internal page remains functional for the merged dataset |
| S3 | Contract-code and shared material entry points stop branching by `materialType === 2` | End-to-end usability of unified catalog | one create/import/export/selection flow | no FE shared surface uses `materialType === 2` or imports the removed external material form |

## Manual Bead Decomposition

This repo does not have working `br` tooling in the current environment, so the current-work bead graph is tracked manually.

| Manual Bead | Scope | Owned Surface | Depends On | Validation Status |
| --- | --- | --- | --- | --- |
| MB1 | Patch catalog backend list/import/export logic so incoming type `1` and `2` requests converge to the merged dataset and normalize stored type `2` rows toward type `1` | `backend/.../Catalog/Index/Material/*`, `CatalogController.cs`, migrations | none | pending validating |
| MB2 | Remove `catalog/asset/external`, simplify navigation, and optimize the internal page as the single material screen | `frontend/src/features/main/catalog/router.tsx`, `frontend/src/features/main/layout/constant.tsx`, `frontend/src/features/main/catalog/asset/internal/*`, `frontend/src/features/main/catalog/asset/external/*` | MB1 contract direction confirmed | pending validating |
| MB3 | Rewrite `contract-code/actions.tsx` and shared FE material entry points to use the merged rule with no `materialType === 2` branching | `frontend/src/features/main/catalog/contract-code/actions.tsx`, `frontend/src/features/main/cost/producttion/production/raw-acceptance-report/unresolved/*` | MB2 | pending validating |

## Story-To-Checklist Mapping

- S1 -> MB1
- S2 -> MB2
- S3 -> MB3

## Manual Bead Checklist

- [x] `MB1` Backend catalog merge for type `1` and `2`
- [x] `MB2` Frontend collapse to `asset/internal`
- [x] `MB3` Contract-code and shared FE entry-point rewrite
