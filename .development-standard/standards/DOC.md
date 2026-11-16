# DOC — Documentation & Data Lifecycle

## Ownership
- Project identity/stack/status: `docs/project.json`.
- Milestone definitions: `docs/roadmap.json`.
- Task status, acceptance criteria, dependencies: `docs/tasks/<id>.json` (one task per file).
- Architecture intent and boundaries: `docs/architecture/*.md`.
- Design decisions and rationale: `docs/decisions/ADR-*.md`.
- Major user-facing changes: `docs/changelog.md`.
- Git is the revision history; avoid duplicating it as prose logs.

## Format
JSON for structured facts, Markdown for explanation. JSON has no comments. Use `schemaVersion`, stable IDs, ISO `YYYY-MM-DD` dates, enumerated statuses, relative paths, UTF-8. Validate against JSON Schema and check referenced IDs/paths.

## Updates
Update only documents whose canonical facts changed. Code changes do not imply task completion; acceptance criteria and evidence do. No redundant summaries across files. Do not auto-edit historical ADRs: supersede with a new ADR and links.

## Consistency
Resolve conflicting claims using canonical ownership; flag discrepancies rather than silently overwriting. Changes to schema require versioning and migration plan. Dashboard reads these canonical files, never becomes an alternate source of truth.

## Contract ownership
Contract indexes link to canonical API/interface definitions; never duplicate those definitions. A contract change follows ARCH impact classification.
