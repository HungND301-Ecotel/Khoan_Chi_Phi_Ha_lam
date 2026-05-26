# Design

## Domain Model

- `MaterialType` stays unified for catalog purposes; this packet does not bring back `MaterialType = 2`.
- The business distinction `Vật tư, tài sản khác` becomes a role on `AssignmentCodeMaterial`.
- `AssignmentCode` therefore exposes two role-aware collections:
  - `materialIds`
  - `otherMaterialIds`
- The join entity `AssignmentCodeMaterial` must gain an enum discriminator so one material can belong to multiple groups with different roles.

## Application Flow

- Assignment-code create/update/detail/import/export flow extends from one material collection to two role-aware collections.
- Assignment-code import/export must add an explicit material-role column so the two lists can round-trip safely.
- Material readers stop publishing singular assignment membership as the authoritative shape and instead retain `AssignmentCodeIds`.
- `contract-code/actions.tsx` becomes the primary FE editor for the two material-role lists.
- Unresolved create and quick-create dialogs keep the user-facing options, but material quick-create safely defaults to the normal material path instead of trying to infer a target `other` role.

## Interface Contract

- Backend assignment-code APIs should expose both `materialIds` and `otherMaterialIds`.
- Backend material list/detail readers should stop relying on singular `AssignmentCodeId` / `AssignmentCode`; `AssignmentCodeIds` becomes the retained safe shape.
- FE `contract-code` form should render two selectors and map them to the two backend lists.
- FE unresolved / quick-create flows should no longer rely on `materialType: 2` semantics when creating materials.

## Data Model

- Existing assignment-code links can be preserved by migrating current links to the main-material enum role by default.
- Unassigned materials remain valid catalog records and can later be attached to any assignment code in either role.
- The persistence model must support the same material appearing in multiple assignment codes with different roles.

## UI / Platform Impact

- The material catalog remains on the internal asset page only.
- `Nhóm vật tư, tài sản` regains a visible `Vật tư, tài sản khác` selector.
- Shared unresolved dialogs remain available, but creating a material from that flow defaults to the standard material path.

## Observability

- No dedicated observability additions are planned in this packet.
- Verification remains build and source-backed, consistent with the current repo proof posture.

## Alternatives Considered

1. Keep singular assignment reader fields and only add role storage in the join table.
   - Rejected because one material can now belong to multiple groups.
2. Let quick-create attach a new material directly into `otherMaterialIds` contextually.
   - Rejected for this phase because the user chose the safer default-to-main behavior.
