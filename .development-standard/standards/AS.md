# AS — Anti-Slop Engineering

- AS-001: Do not implement unrequested features or speculative generality.
- AS-002: Search for reusable existing code before introducing new abstractions.
- AS-003: Introduce patterns/interfaces only to solve demonstrated problems; explain trade-offs.
- AS-004: Prefer clear names, small coherent modules and explicit behavior over cleverness.
- AS-005: Do not combine unrelated cleanup with feature work; propose separately.
- AS-006: Do not add redundant comments, boilerplate tests, duplicated docs or fabricated claims.
- AS-007: Tests must verify meaningful acceptance conditions and failure modes.
- AS-008: Do not force existing architecture to accommodate an incompatible requirement through fragile workarounds; escalate per ARCH.

Review questions: What concrete problem does each new component solve? Can existing structures do it? What changed for maintainers? What breaks if requirements change? What evidence supports correctness?
