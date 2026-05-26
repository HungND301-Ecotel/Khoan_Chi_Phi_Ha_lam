# Phase Plan: Unify Material Type

Mode: `high_risk_feature`

Feature summary: The system keeps one unified material catalog and one retained material page, while the business distinction `Vật tư, tài sản khác` moves into role-aware `AssignmentCode` membership. That requires an enum on `AssignmentCodeMaterial`, multi-group-safe material readers, restored contract-code UX, and a safe quick-create fallback that defaults material creation to the normal path.

## Phase Overview

| Phase | What Changes | Why Now | Demo | Unlocks |
| --- | --- | --- | --- | --- |
| 1. Catalog Foundation | Normalize legacy `MaterialType = 2` rows into `1`, keep one material screen, and remove the external asset surface | This established the single-catalog baseline | One retained `Vật tư, tài sản` page and no maintained external route | Makes later role modeling independent from the old page split |
| 2. AssignmentCode Role Split | Add enum-based role modeling to `AssignmentCodeMaterial`, extend assignment-code APIs to `materialIds` + `otherMaterialIds`, repair material readers to `AssignmentCodeIds`, restore contract-code dual-list UX, and keep quick-create on the normal material path | This is the clarified safe boundary that matches the latest business rule without ambiguous quick-create writes | One `Nhóm vật tư, tài sản` form with two selectors, multi-group-safe material readers, and unresolved material quick-create still working safely | Makes downstream `Part` / `OtherPart` inference independent from legacy material type |
| 3. Downstream Classification Rewrite | Replace operational/reporting `MaterialType` branches with assignment-code-based role inference and remove remaining runtime coupling to `MaterialOutContract` | This depends on Phase 2 defining authoritative per-assignment material roles and reader shapes | Acceptance/report flows still separate `Part` and `OtherPart` correctly without `MaterialType = 2` | Full runtime removal of legacy `MaterialOutContract` behavior |

## Order Check

- Phase 1 remains valid as completed catalog groundwork.
- Phase 2 is now the current phase because the user clarified both the persistence shape and the safe quick-create fallback.
- Phase 3 stays deferred because runtime classification cleanup is broader than the current assignment-code and reader contract change.

## Approval Summary

- Current phase to prepare: Phase 2, because it is now the smallest end-to-end packet that matches the clarified business contract and resolves the last validation failures.
- Picture after Phase 2: the system still has one material catalog, `AssignmentCode` can distinguish main vs other materials through join-role storage, material readers are multi-group-safe, and shared creation flows no longer depend on `MaterialType = 2`.
- Deferred work: acceptance/reporting and other operational code that still infer `OtherPart` from legacy `MaterialType`.

Planning has chosen the smallest work shape. Approve it before execution. Validation for this repaired phase can now run against the real bead graph.
