# CTX — Context Engineering

## Progressive disclosure
L0: `AGENTS.md` (short routing and invariants).
L1: `docs/project.json` + one active task JSON.
L2: relevant architecture index/sections, linked ADRs, applicable policy.
L3: source, tests, history, logs as justified by findings.

Never bulk-load all documentation by default. Follow task `context.required` then `context.optional` only when useful. Paths constrain initial **reading**, not discovery; edit permissions are separately defined by task scope.

## Suggested soft budgets (not platform limits)
AGENTS.md 300–600 tokens; project metadata 200–400; task 200–600; index 200–400; module design 500–1500; ADR 200–600. Accuracy overrides arbitrary limits. Split by responsibility rather than cutting critical information.

## Freshness
Prefer canonical source over summaries. Summaries are navigation aids, not authoritative replacements. Do not repeatedly summarize summaries. Use targeted search/section reads and confirm referenced documents still exist.

## Measurement
Track input context volume where observable, unnecessary reads, repeated retrievals, stale-reference incidents, task success, rework and failed gates. Optimize total reliable task cost, not token count alone.
