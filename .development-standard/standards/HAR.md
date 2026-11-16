# HAR — Agent Harness & Execution Lifecycle

## Phases
1. Discover: read task and project metadata, inspect relevant code.
2. Plan: identify scope, contracts, dependencies, impact level and tests.
3. Approve: stop for L2/L3, out-of-scope changes or unresolved high-risk ambiguity.
4. Implement: smallest coherent change; isolate unrelated modifications.
5. Verify: schema, build, lint, relevant tests, architecture gates, diff review.
6. Synchronize: update canonical task/architecture/ADR/changelog only if warranted.
7. Report: changes, impact level, executed commands, pass/fail/not_run, risks, next steps.

## Controls
Tool permissions and approvals are enforced by execution environment; this document alone is not a security boundary. Prefer scoped access and reviewable commits/PRs. Do not claim a gate passed without evidence. A failed gate blocks completion; unavailable checks are `not_run`, not `passed`.

## Recovery
Record a compact checkpoint for interrupted work: task ID, phase, decisions/approvals, changed files, test results, unresolved issues, next action. Checkpoint is transient operational state, not a duplicate canonical task ledger. Use Git to recover actual diffs.

## Planning modes
- **Lightweight:** L0/local, low-risk work: short scope + verification notes; no separate planning artifact required.
- **Formal:** L2/L3, cross-boundary or high-risk work: alternatives, contracts, approval reference, rollback and verification plan before edits.

## Onboarding
Run `skills/assess-project/SKILL.md` as a read-only assessment. Apply approved changes incrementally; never assume a blank-slate TypeScript or other stack.
