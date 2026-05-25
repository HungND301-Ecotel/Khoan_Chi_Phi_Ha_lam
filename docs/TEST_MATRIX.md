# Test Matrix

This file maps accepted work packets and documented product behavior to the
proof that currently exists.

The repository already contains product code, but proof is still light. Build
success and source-backed documentation are the current baseline; dedicated unit,
integration, and E2E suites are not yet present as tracked projects.

## Status Values

| Status | Meaning |
| --- | --- |
| planned | Accepted as intended behavior, not implemented |
| in_progress | Actively being built |
| implemented | Implemented and proof exists |
| changed | Contract changed after earlier implementation |
| retired | No longer part of the product contract |

## Matrix

| Story | Contract | Unit | Integration | E2E | Platform | Status | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `docs/stories/docs-backfill-current-state.md` | `docs/ARCHITECTURE.md`, `docs/product/*.md`, `docs/stories/*`, and `docs/README.md` reflect the current React + ASP.NET Core + PostgreSQL system | no | yes | no | yes | implemented | Source inspection plus successful `npm run build` and `dotnet build .\Ecotel.KCPCMS.BE.sln` on 2026-05-22 |
| `docs/stories/fix-process-group-normalization.md` | Pricing forms and dashboard normalize `catalog/processgroup` payloads so process-group options still render, and the low-value perishable supply form shows the full catalog list when the backend returns `type` instead of `fixedKeyType` | no | yes | manual | yes | implemented | Source inspection and successful `npm run build` on 2026-05-25 |

## Evidence Rules

- Unit proof covers pure domain and application rules.
- Integration proof covers backend enforcement, data integrity, provider
  behavior, jobs, or service contracts.
- E2E proof covers user-visible browser flows.
- Platform proof covers shell, deployment, runtime, or release-shape behavior
  that cannot be proven in lower layers.
- A story can be implemented without every proof column if the story packet
  explains why and the available evidence is explicit.
