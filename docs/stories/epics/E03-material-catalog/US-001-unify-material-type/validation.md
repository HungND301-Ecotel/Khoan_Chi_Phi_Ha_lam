# Validation

## Proof Strategy

This work currently has light automation in the repo, and the user explicitly does not want new tests created. Validation should therefore use source-backed feasibility checks and existing build commands once implementation starts.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | none planned for this story |
| Integration | verify material migration/update path, API signature changes, and downstream branch inventory by source inspection and implementation checks |
| E2E | manual catalog walkthrough after implementation: one material screen, one import/export flow, no external page |
| Platform | `npm run build`; `dotnet build .\\Ecotel.KCPCMS.BE.sln` |
| Performance | none planned |
| Logs/Audit | none planned |

## Fixtures

- Existing material rows with both `MaterialType = 1` and `MaterialType = 2`.
- Materials with `assigmentCodeId != null`.
- Materials with `assigmentCodeId == null`.

## Commands

Add commands after scripts exist.

```text
npm run build
dotnet build .\Ecotel.KCPCMS.BE.sln
```

## Acceptance Evidence

Add results after verification.
