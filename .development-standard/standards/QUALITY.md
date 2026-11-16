# QUALITY — Evidence-Based Completion

Required applicable gates:
1. Scope matches approved request; no unexpected files or behavior.
2. Impact classification recorded; L2/L3 approval evidence present.
3. Build/lint/tests executed where available; record result and command.
4. Schema and reference integrity checks pass for changed structured docs.
5. No newly introduced prohibited dependencies or circular references (when checkable).
6. Relevant canonical docs updated; unchanged docs left untouched.

Statuses: `passed`, `failed`, `not_run`, `not_applicable`. `not_run` requires a reason and cannot be reported as a pass. If a required gate fails or cannot be executed, task is not automatically completed; escalate or obtain explicit exception with rationale. Approval is a recorded human decision, not an inference from silence.
