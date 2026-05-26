# Overview

## Current Behavior

The product currently splits material catalog management into two user-facing surfaces: `Vật tư, tài sản` and `Vật tư, tài sản khác`. The backend material catalog list/import/export contract also accepts `MaterialType`, and several operational/reporting flows still map `MaterialType.MaterialOutContract` to `OtherPart`.

## Target Behavior

The product should support only one material catalog type. Existing `MaterialType = 2` records are normalized to `1`, the external asset page is removed, and the retained internal material page remains the only catalog screen. The business distinction `Vật tư, tài sản khác` now lives inside `AssignmentCode` membership rather than `MaterialType`, using separate `materialIds` and `otherMaterialIds` backed by a role enum on `AssignmentCodeMaterial`. Material readers must support multi-group membership through `AssignmentCodeIds`. Operational/reporting flows will later distinguish `Part` versus `OtherPart` by assignment-based rules rather than `MaterialType`.

## Affected Users

- Catalog operators maintaining material and asset master data.
- Production and acceptance-report users who consume downstream part/material classification.
- Report consumers relying on acceptance/export outputs.

## Affected Product Docs

- `docs/product/catalog.md`
- `docs/product/operations.md`
- `docs/product/reporting.md`

## Non-Goals

- Re-architecting catalog modules.
- Introducing a new FE layout pattern.
- Adding automated tests in this story packet.
