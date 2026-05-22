# Stories

Stories are the work packets that connect product truth, implementation intent,
and validation evidence.

This repository already contains product code, so stories now serve two roles:

- selected change packets for new work,
- backfill packets that document and validate the current implemented state when
  the earlier story history is incomplete.

## Current Tracked Story

- `docs-backfill-current-state.md`: backfills the harness docs so they describe
  the implemented system accurately.

## Normal Story

Use `docs/templates/story.md` for bounded work such as:

- product behavior changes,
- maintenance slices,
- harness/doc backfills tied to specific code surfaces.

Suggested path for future feature slices:

```text
docs/stories/epics/E01-domain-name/US-001-short-story-title.md
```

## High-Risk Story

Use `docs/templates/high-risk-story/` when the feature intake classifies work as
high-risk.

Suggested path:

```text
docs/stories/epics/E02-risky-domain/US-012-risky-story-title/
  execplan.md
  overview.md
  design.md
  validation.md
```

## Status Flow

```text
planned -> in_progress -> implemented
                  |
                  v
               changed
                  |
                  v
               retired
```

## Source Surfaces

- `docs/templates/story.md`
- `docs/stories/backlog.md`
- `docs/stories/docs-backfill-current-state.md`
