---
name: humans-refactor
description: Autonomous Reforge-guided refactor workflow for the Humans repository. Use when the user wants Codex to target a section such as Shifts, Users, Teams, Tickets, Camps, or Store, create a dedicated worktree/branch from origin/main, run iterative architecture-focused improvements, score each stage with Reforge, gate commits through a read-only architecture review, append score and verdict details to commit messages, push progress, and open a PR when the loop reaches stasis.
---

# Humans Refactor

Run a higher-autonomy refactor pass for one Humans section. This is the v2 tech-debt loop: Reforge supplies deterministic score pressure, and a separate architecture-review pass decides whether the score movement was worth the trade.

## Start

1. Confirm the repo root is a Humans checkout.
2. Read the current request carefully for target section, time budget, branch/PR expectations, and any overridden constraints.
3. Fetch `origin/main`, create `refactor/YYYY-MM-DD-<section>-N`, and attach `.worktrees/refactor-YYYY-MM-DD-<section>-N`.
4. Keep all scratch output under `local/refactor-runs/<run-id>/`.
5. Run `reforge stop` before and after every Reforge call. Do not use daemon mode.

If resuming, continue the existing refactor worktree/branch only when the user clearly refers to it.

## Autonomy Floor

For autonomous or end-of-day runs, do not stop after the first failed implementations. Unless the user gives a shorter budget, work for at least 60 minutes before declaring stasis. If an implementation is rejected, switch search strategy and continue.

Before stasis is allowed, the run must have:

- built a candidate ledger with at least eight researched opportunities
- inspected at least three different categories of opportunity
- attempted or explicitly rejected at least five candidates with concrete reasons
- run Reforge on the target section after any accepted change
- pushed every accepted stage

Two failed patches in a row are a signal that the search strategy is too shallow, not proof that the section has no valuable work.

## Hard Limits

- Avoid database/storage changes unless the user explicitly asks for them: no migrations, no schema configuration changes, no entity persistence-shape changes, no JSON serialization attribute changes.
- Do not revert user changes or unrelated work.
- Do not move debt across sections to win points. Regressions outside the target section count against the change.
- Public members may be removed when they are internal application surface and the call graph is updated. Preserve reflection/external contracts, Razor action routes, serialized DTO contracts, and public APIs that are intentionally consumed outside the repo.
- Prefer the best end-state architecture over the smallest diff. Large cohesive refactors are allowed when the call graph and tests support them.

## Scoring

Capture baseline scores before editing:

```powershell
reforge stop | Out-Null
reforge surface-score --solution Humans.slnx --format Json --all --top-symbols 200 > local\refactor-runs\<run-id>\stage00-all.json
reforge stop | Out-Null
reforge surface-score --solution Humans.slnx --group <Section> --format Json --all --top-symbols 200 > local\refactor-runs\<run-id>\stage00-section.json
reforge stop | Out-Null
```

For each candidate stage, write `stageNN-all.json` and `stageNN-section.json`. Use `scripts/reforge_delta.py` to summarize score movement for commit messages and status updates.

Measure value as:

- target-section total improvement: positive value
- overall total improvement: useful context
- internal-complexity increases: cost unless the architecture-review pass says the trade is justified
- outside-target movement: compute `target improvement + 2 * outside-target improvement`, where improvement means score decrease; an outside-target regression of 5 cancels 10 target points
- score-neutral changes: acceptable only when the reviewer says the architecture clearly improved
- parameter DTOs, input bags, options bags, and command wrappers do not earn credit merely because they reduce public parameter count

## Work Loop

First build a candidate ledger. Do this before editing:

1. Read the target section's Reforge report with `--all --top-symbols 200`.
2. Inspect top symbols and also top rules; do not rely only on the first ten offenders.
3. For key interfaces/services/repositories/controllers, use Reforge queries such as `members`, `dependencies`, `injected`, `callers`, `audit-surface`, `audit-downstream`, and `implementations`.
4. Use `rg` to inspect call sites and duplicate call structures.
5. List candidate ideas with expected score movement, likely files, risk, and architecture rationale.

Search at least these categories before declaring stasis:

- interface or repository methods with low caller counts, overlapping behavior, or one implementation
- full-service dependencies that should be read DTO/read-service dependencies
- methods where callers only need one or two scalar values from a heavier service path
- duplicate query/include/call sequences that can become one cohesive read shape
- large service/repository/controller clusters whose methods naturally split by workflow
- stale public methods, DTO fields, or wrappers no longer used by production code
- cross-section reads, entity returns, or persistence joins that bypass canonical read models
- long parameter lists only when the fix is a real domain command/query object, not a parameter bag

Then repeat until stasis:

1. Inspect Reforge top symbols/rules for the target section.
2. Read the surrounding code and call graph before editing.
3. Pick the highest-leverage cohesive improvement, not just the highest scoring rule.
4. Make the change.
5. Run targeted tests and `dotnet build Humans.slnx --disable-build-servers -v q`.
6. Run Reforge after the change.
7. Run the architecture-review gate.
8. If accepted, commit and push. If rework/reject, improve or abandon before committing.

Stasis means the autonomy floor has been met and the remaining ledger is exhausted, blocked by hard limits, score-negative without architectural upside, or too speculative to change without user/product input. A run may stop early only when the user explicitly asks, the repo cannot build for reasons outside the branch, or continuing risks data/schema/contract changes the user did not authorize.

## Architecture Review Gate

Use a separate read-only pass after every candidate stage. Prefer a subagent when multi-agent tools are available; otherwise perform an explicit second-pass review without editing. Read [architecture-reviewer.md](references/architecture-reviewer.md) for the prompt and JSON contract.

Inputs to the reviewer:

- target section and stated objective
- git diff for the uncommitted stage
- Reforge before/after deltas, including surface/internal/combined totals
- weighted target/outside value and any suspicious improvements
- candidate ledger entry and alternatives considered
- build/test results
- notes about behavior or ownership assumptions

Reviewer verdicts:

- `accept`: commit/push.
- `rework`: change the patch and rerun tests/Reforge/review.
- `reject`: abandon the patch or redesign before committing.

Treat Reforge as evidence, not judgment. Good consolidation removes duplicated call structures, uses canonical read DTOs, and replaces interfaces with small data where appropriate. Bad consolidation hides domain verbs behind generic action/mode dispatchers, grows god methods, moves complexity to private helpers, or shifts debt out of the target section.

## Commit Messages

Every accepted commit must include a score and review appendix:

```text
<short imperative subject>

Reforge:
- Target: <Section> <before> -> <after> (<delta>); surface <delta>, internal <delta>
- Overall: <before> -> <after> (<delta>); surface <delta>, internal <delta>
- Weighted value: <target improvement + 2x outside-target improvement>

Architecture-review:
- Verdict: accept
- Grade: good|neutral
- Score-gaming risk: none|low|medium
- Reason: <one or two concise sentences>

Verification:
- <commands run and result>
```

Push after each accepted commit. If a PR does not exist by the time the user asks or the loop reaches stasis, open a draft PR with cumulative score movement, verification, and any commits the reviewer marked as tradeoffs.

## Judgment Rules

- Prefer explicit domain verbs for independent commands.
- Prefer a real state engine for lifecycle transitions.
- Penalize generic action/mode methods that are neither explicit domain verbs nor a real state engine.
- Treat read/query shape objects differently from mutation mode dispatchers; shaped reads are acceptable when they collapse duplicated include/query shapes without growing unbounded.
- Prefer adding one or two scalar properties to an existing DTO over creating a new interface/service/repository path.
- Prefer canonical read services such as `IUserServiceRead`, `ITeamServiceRead`, and section view DTOs for cross-section reads.
- Keep controllers thin, but allow UI-specific finite sorting/filtering/paging at the controller/view boundary.
- Reject parameter-object refactors whose primary effect is hiding a long method signature. Accept a new input/command object only when it has cohesive domain meaning, public readable semantics appropriate to its boundary, validation/invariants, reuse across related flows, or a materially simpler call site.
- Treat `public` input types with mostly `internal` state as a smell on service interfaces; decorators and cross-assembly callers should not need result objects or side channels to recover data the input already contained.

## References

- Use [architecture-reviewer.md](references/architecture-reviewer.md) for the review gate.
- Use [reforge_delta.py](scripts/reforge_delta.py) for score summaries.
