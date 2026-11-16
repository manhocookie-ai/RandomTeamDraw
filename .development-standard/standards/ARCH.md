# ARCH-001 — Architecture Change Governance

## Design qualities
High cohesion, low coupling, explicit dependencies, separation of concerns, readability, testability and low change propagation. Avoid speculative extensibility and forced design patterns. Existing architecture must be analyzed before proposing changes.

## Impact levels (highest matching level wins)
| Level | Meaning | Permission |
|---|---|---|
| L0 | Local implementation changes, existing behavior/contracts preserved | Autonomous within approved task scope |
| L1 | Limited internal restructuring, no external contract/boundary changes | Conditional autonomy within approved scope, with tests |
| L2 | Public API, module boundary, persistent schema, cross-module data flow, security boundary change | Prior approval |
| L3 | Framework/database replacement, system-wide redesign, broad breaking change | Explicit prior approval |

Unknown impact defaults to review, not to autonomous execution. Small diffs can still be L2/L3. Out-of-scope changes are **proposal only**, even if L0.

## Before L2/L3 implementation
Provide: observed problem; root cause; minimum-change alternative; architecture-improvement alternative; option to defer; affected contracts/files/dependencies; migration/rollback; regression tests; recommendation. Wait for explicit approval of a specific scope. Re-request approval if scope expands.

## Architecture fitness checks
Where supported: dependency direction, circular imports, layer boundaries, public API compatibility, migration integrity and regression tests. Tests passing does not alone prove architecture quality. Tools and thresholds are project-specific.
