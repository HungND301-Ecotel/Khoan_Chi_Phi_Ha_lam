# Approach: Unify Material Type

## Recommended Approach

Keep the three-phase high-risk rollout, but tighten Phase 2 into a safe role-model packet with an explicit enum on `AssignmentCodeMaterial` and no ambiguous quick-create attachment logic.

Why this is now the smallest believable path:

- The catalog foundation is already in place. The remaining work is about how one unified material can participate in `AssignmentCode` as either main material or `Vật tư, tài sản khác`.
- The user has now supplied a safe boundary: add an enum role to `AssignmentCodeMaterial`, move material readers from singular assignment fields to `AssignmentCodeIds`, and keep quick-create defaulting to normal material creation.
- That boundary removes the previous ambiguous requirement to auto-attach quick-created materials into an `other` role during unresolved handling.

## Why Smaller Modes Are Insufficient

- Not `small_change`: backend persistence, DTOs, queries, commands, migration, FE forms, and reader contracts all change together.
- Not `direct_task`: the previous validation exposed real compile/runtime boundary issues, and this phase still changes persisted relationships.
- Not `spike`: repo evidence is already concrete enough. The remaining task is disciplined scope repair and execution prep.
- Not plain `standard_feature`: the work still changes data shape and public FE/BE contracts.

## Rejected Alternatives

1. Re-introduce `MaterialType = 2` as the persistence signal.
   - Rejected because the user still wants one material catalog type only.
2. Keep singular `AssignmentCodeId` readers and let them point to the "first" group.
   - Rejected because the user now explicitly allows one material to belong to multiple groups.
3. Make quick-create decide whether a new material is `other` by context.
   - Rejected for this phase because the user has now chosen the safer fallback: quick-create defaults to normal material creation.

## Risk Map

| Component | Risk | Reason | Proof Needed |
| --- | --- | --- | --- |
| AssignmentCodeMaterial persistence | HIGH | join entity currently has no role/discriminator | entity + DbContext + migration inspection |
| AssignmentCode API contract | HIGH | current DTOs and commands only carry one material list | source review across DTO/query/command/controller flow |
| Material readers | HIGH | current material list/detail flows still flatten assignment membership | source review of `MaterialsByPaginationSpec` and `GetMaterialByIdQuery` plus FE asset readers |
| Contract-code UX | HIGH | current FE only edits one list | form/detail mapping inspection plus build |
| Quick-create safety | MEDIUM | previous plan lacked explicit write-path semantics | source review confirming default-to-main behavior is enough for this phase |

## File And Order Boundaries

1. Backend role model and contract
   - `AssignmentCodeMaterial`, EF mapping, DTOs, create/update/detail/import/export flow, and migration
2. Material reader contract repair
   - `MaterialsByPaginationSpec`, `GetMaterialByIdQuery`, material DTOs, and FE asset reader/types that still assume singular assignment membership
3. Frontend contract-code restoration
   - `frontend/src/features/main/catalog/contract-code/*` schema, form, and expand/detail handling for `materialIds` vs `otherMaterialIds`
4. Shared quick-create fallback restoration
   - unresolved create chooser and create dialog keep the `Vật tư, tài sản khác` choice visible where required, but quick-create of material defaults to the normal material path instead of contextual role attachment
5. Deferred operational cleanup
   - acceptance/report runtime `Part` / `OtherPart` rewrite and broader `MaterialType` cleanup remain out of this phase

## Validating Questions

- Does the backend now persist role-aware assignment-code membership through an enum on `AssignmentCodeMaterial`?
- Have material list/detail readers been widened from singular assignment fields to `AssignmentCodeIds` so multi-group membership is representable?
- Does `contract-code` now read and write both `materialIds` and `otherMaterialIds` cleanly?
- Do unresolved and quick-create flows now avoid the ambiguous role-attachment problem by defaulting new material creation to the normal material path?

## Code Style Guardrails

- Follow current React form patterns already used in `contract-code`, unresolved dialogs, and catalog forms.
- Follow current backend MediatR, DTO, and EF Core conventions; extend the existing `AssignmentCode` and material-reader flows instead of creating parallel modules.
- Keep the internal material page as the only material catalog screen.
- Do not create tests in this planning packet.
