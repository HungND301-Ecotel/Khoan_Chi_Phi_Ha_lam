# Phase Contract: Phase 1 - Catalog Merge And UI Consolidation

Entry state: The repo exposes split material catalog behavior between `assets/internal` and `assets/external`, the backend catalog material list/import/export handlers still distinguish client `materialType`, and FE contract-code handling still branches on `materialType === 2`.

Exit state: The repo exposes one material catalog surface based on the optimized internal page, FE catalog and contract-code flows no longer branch on `materialType === 2`, backend catalog handlers treat client requests for type `1` and `2` as one merged dataset, and persisted type `2` material rows are normalized toward the type `1` bucket for catalog behavior.

Demo:

- Open catalog navigation and verify only one `Vật tư, tài sản` destination remains.
- Inspect FE asset and contract-code flows and verify none of them branch on `materialType === 2`.
- Inspect material list/import/export flows and verify requests for catalog type `1` and `2` are served as one merged dataset and one retained FE screen.
- Inspect migration and backend handlers and verify persisted type `2` material data is normalized toward the type `1` catalog bucket.

## Stories

| Story | What Happens | Unlocks | Done |
| --- | --- | --- | --- |
| S1. Normalize catalog backend behavior for type `1` and `2` | BE keeps the domain field for now but rewrites catalog handlers so type `1` and `2` requests/import/export behavior merge into the same dataset | Stable safe backend contract for the merged catalog | CatalogController and catalog material handlers no longer create a user-visible split between type `1` and `2` |
| S2. Collapse FE material pages into the internal page | FE removes `external`, keeps the internal page as the only material catalog screen, and optimizes it for the merged dataset | User-visible catalog matches accepted product truth | No active page or menu path exposes `Vật tư, tài sản khác` |
| S3. Rewrite contract-code and shared FE entry-point logic | Contract-code and shared dialogs stop relying on `materialType === 2` and adopt the merged material rule set | Phase 1 can be demonstrated end to end | Shared material selection and create/import/export entry points resolve with the merged catalog rule |

## Out / Success / Pivot

- Out of scope: acceptance-report, production-output, maintain-equipment, and other operational/reporting behavior rewrites that still depend on `MaterialType` outside the catalog boundary.
- Success signal: catalog and contract-code surfaces are unified without the old type `1`/`2` split remaining in Phase 1 scope.
- Pivot signal: if merging type `1` and `2` at the catalog layer still requires hidden architecture work outside the accepted material/catalog domain, stop and promote that dependency into Phase 2 instead of silently widening execution.
