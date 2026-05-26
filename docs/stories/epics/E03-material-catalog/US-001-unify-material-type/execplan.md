# Exec Plan

## Goal

Keep one unified material catalog while restoring contextual `Vật tư, tài sản khác` handling inside `AssignmentCode` through a join-role enum, multi-group-safe material readers, and dual-list contract-code UX.

## Scope

In scope:

- Extend backend assignment-code storage with an enum on `AssignmentCodeMaterial`.
- Extend assignment-code APIs from one flat `materialIds` list to `materialIds` plus `otherMaterialIds`.
- Extend assignment-code import/export with an explicit material-role column.
- Repair backend and FE material readers so they use `AssignmentCodeIds` instead of singular assignment fields.
- Restore role-aware UX in `frontend/src/features/main/catalog/contract-code/*`.
- Keep unresolved / quick-create usable while defaulting new material creation to the normal material path.
- Track the phase with real `br` beads.

Out of scope:

- Reopening the removed external material page.
- Reintroducing `MaterialType = 2`.
- Completing downstream acceptance/report operational rewrites in the same packet.
- Creating tests.

## Risk Classification

Risk flags:

- Data model
- Public contracts
- Existing behavior
- Weak proof
- Multi-domain

Hard gates:

- `AssignmentCodeMaterial` currently has no role/discriminator field.
- Material readers still flatten assignment membership to singular fields today.

## Work Phases

1. Catalog foundation complete.
2. Planning and validation for AssignmentCode role split.
3. Phase 2 implementation after approval and validating.
4. Later operational cleanup of downstream `Part` / `OtherPart` inference.
5. Harness update.

## Stop Conditions

Pause for human confirmation if:

- the assignment-code role split forces broader pricing/reporting contract changes than the current reader surface shows
- preserving existing assignment-code links needs destructive normalization beyond a safe default-role migration
- quick-create still requires contextual role writes after applying the default-to-main fallback
