# Phase Contract: Phase 2 - AssignmentCode Role Split

Entry state: The repo already exposes one unified material catalog screen, but `AssignmentCode` still stores one flat `materialIds` list, `AssignmentCodeMaterial` has no role discriminator, material list/detail readers still flatten assignment membership into singular fields, and `contract-code` plus unresolved flows cannot represent `Vật tư, tài sản khác` safely.

Exit state: `AssignmentCode` persists two distinct role-aware material lists through an enum on `AssignmentCodeMaterial`, material readers expose `AssignmentCodeIds` instead of singular assignment fields, assignment-code import/export carries an explicit material-role column, `contract-code` exposes both `Vật tư, tài sản` and `Vật tư, tài sản khác`, and quick-create of material defaults to the normal material path rather than attempting contextual `other` attachment.

Demo:

- Open `Nhóm vật tư, tài sản` and verify it shows two selectors:
  - `Vật tư, tài sản`
  - `Vật tư, tài sản khác`
- Create or edit one `AssignmentCode` and verify the same material can be stored in multiple groups, with role determined by the `AssignmentCodeMaterial` enum.
- Inspect material list/detail contract and verify singular `AssignmentCodeId` / `AssignmentCode` reader fields are no longer the authoritative shape; `AssignmentCodeIds` is the retained membership surface.
- Inspect assignment-code import/export contract and verify each linked material row carries an explicit role value that can round-trip main vs other membership.
- Open unresolved/quick-create flows and verify material quick-create still works, but defaults to the normal material path instead of auto-creating an `other` assignment-role mapping.

## Stories

| Story | What Happens | Unlocks | Done |
| --- | --- | --- | --- |
| S1. Extend backend AssignmentCode persistence and contract | BE adds an enum role to `AssignmentCodeMaterial` and extends assignment-code create/update/detail/import-export APIs for `materialIds` plus `otherMaterialIds`, including a role column for import/export | truthful data model for the restored UI | assignment-code persistence and APIs distinguish the two lists |
| S2. Repair material reader contract for multi-group membership | BE and FE material readers stop relying on singular assignment fields and use `AssignmentCodeIds` as the safe membership shape | unified material catalog can represent one material in multiple groups | asset/material list-detail flows no longer flatten membership to one group |
| S3. Restore role-aware contract-code UX | FE `contract-code` form and detail view expose two material selectors and map them to the new backend contract | main business screen matches the clarified rule | `Nhóm vật tư, tài sản` no longer collapses both roles into one flat selection |
| S4. Restore safe unresolved / quick-create behavior | shared chooser and popup flows keep the user-facing options but material quick-create defaults to normal material creation | end-to-end flow is safe without ambiguous role attachment | unresolved flows no longer depend on `MaterialType = 2` and no longer require implicit `other` assignment routing |

## Out / Success / Pivot

- Out of scope: acceptance-report, production-output, maintain-equipment, and other operational/reporting rewrites that still infer runtime behavior from `MaterialType`.
- Success signal: one unified material catalog remains, while assignment-code workflows regain explicit `other` classification through role-aware storage instead of `MaterialType = 2`.
- Pivot signal: if pricing/reporting or material readers outside the current phase still require singular assignment membership semantics to compile or behave correctly, absorb them into this phase explicitly instead of leaving hidden breakage.
