# US-PRC-001 Process Group Normalization For Pricing Form

## Status

implemented

## Lane

normal

## Product Contract

Pricing forms that consume `catalog/processgroup` must render the full
process-group list returned by the catalog API, even when the API response
provides the group classifier in `type` instead of `fixedKeyType`.

## Relevant Product Docs

- `docs/product/pricing-and-costs.md`
- `docs/product/operations.md`

## Acceptance Criteria

- The low-value perishable supply creation form shows every process-group option
  returned by `GET /v1/catalog/processgroup?ignorePagination=true`.
- Frontend normalization treats `fixedKeyType`, `type`, or the process-group
  `code` as valid sources for the process-group classifier.
- Existing screens that depend on the same list payload do not regress when the
  backend omits `fixedKeyType`.

## Design Notes

- Commands: none.
- Queries: `GET /v1/catalog/processgroup?ignorePagination=true`.
- API: normalize the response on the frontend and keep the low-value
  perishable supply selector unfiltered.
- Tables: none.
- Domain rules: `DL`, `LC`, and `XL` map to process-group types `1`, `2`, and
  `3`.
- UI surfaces:
  `frontend/src/features/main/pricing/low-value-perishable-supply/form.tsx`
  and `frontend/src/features/main/dashboard/page.tsx`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | none |
| Integration | source inspection of API response shape vs. normalization logic |
| E2E | manual dialog verification in browser |
| Platform | `npm run build` |
| Release | none |

## Harness Delta

Recorded the payload-shape mismatch as a story and matrix row because this fix
touches existing product behavior with only build-level proof available.

## Evidence

- Frontend normalization added in
  `frontend/src/features/main/catalog/process/group/columns.tsx`.
- Pricing form and dashboard now consume normalized process-group data.
