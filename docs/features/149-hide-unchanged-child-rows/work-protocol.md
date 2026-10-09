# Work Protocol: 149-hide-unchanged-child-rows

**Work Item:** `docs/features/149-hide-unchanged-child-rows/`
**Workflow Type:** feature
**Created:** 2026-10-09

## Agent Work Log

<!-- Each role appends its entry below on completion. -->

### Requirements Engineer

- **Date:** 2026-10-09
- **Summary:** Specified hiding unchanged (⏺️) rows in all parent-child inline child tables by default, with --show-unchanged-values restoring current output; all-unchanged tables collapse to heading plus one-line note. Five open questions recorded with assumptions.
- **Artifacts Produced:** docs/features/149-hide-unchanged-child-rows/specification.md, state.json, work-protocol.md
- **Problems Encountered:** Spec 046 references feature number 068 in docs/features.md; the 068 folder does not exist (docs link likely stale). docs/features.md NSG section ('unchanged rules shown for completeness') conflicts with this feature; recorded as open question 3.

### Architect

- **Date:** 2026-10-09
- **Summary:** Not contested (arch_contested=false). Filter unchanged child rows in the report model inside DisplayFilteringStage, after summary counts, provider post-merge callbacks and no-op parent checks have used the full row set. ChildResourceGroup records HiddenUnchangedCount and per-provider item nouns, and groups with no remaining rows are kept so the mixed-sources warning can still render. A shared core helper writes the hidden-count note and warning for DefaultResourceRenderer and NsgMergedSecurityRulesRenderer. The NSG inline-rules path in Providers/AzureRM filters its view-model rows itself and falls back to the default renderer when every rule is hidden. No global ADR: this follows the existing model-side filtering convention.
- **Artifacts Produced:** docs/features/149-hide-unchanged-child-rows/architecture.md
- **Problems Encountered:** NsgRenderer's inline-rules path does not render from ChildResourceGroups, so it needs its own filter at render time. DefaultResourceRenderPolicy.IsNoOpParentSecurityRuleScenario depends on Rows.Count == 2 and must use the unfiltered count. The spec scopes filtering to update and replace, but no-op parents also show unchanged rows; recorded as an open question with an assumption.

### Quality Engineer

- **Date:** 2026-10-09
- **Summary:** Test plan mapping all 19 acceptance criteria to stage, renderer (default, merged NSG, NSG inline), registry, integration, snapshot, CLI and docs tests. UAT plan written for the Maintainer to run (fork contribution; Azure DevOps UAT needs the oocx org) and uat-plan.json contents specified.
- **Artifacts Produced:** docs/features/149-hide-unchanged-child-rows/test-plan.md, docs/features/149-hide-unchanged-child-rows/uat-test-plan.md
- **Problems Encountered:** AC 19 (renders correctly in ADO and GitHub PRs) is only automatable as markdown validity; platform check is UAT. Spec says update and replace while architecture filters every action except create and delete; TC-29 covers that assumption.

### Task Planner

- **Date:** 2026-10-09
- **Summary:** Decomposed the specification, architecture and test plan into 19 tasks in tasks.md, ordered baseline first (golden files from unmodified code), then foundations (model nouns and counts, shared note helper, help text), the stage filter and its no-op check, the three renderer paths, docs (Technical Writer owned), docs alignment tests, per-pattern integration tests, edge cases, markdown matrix, snapshot review, and UAT input files built by the Developer (not run). Every AC 1-19 and every defined TC in test-plan.md is mapped; no gaps found. Tasks 9, 10, 11 are for different owners (Developer for help text and tests; Technical Writer for docs).
- **Artifacts Produced:** docs/features/149-hide-unchanged-child-rows/tasks.md, docs/features/149-hide-unchanged-child-rows/work-protocol.md
- **Problems Encountered:** 1) Architecture open question (no-op parents kept for a child change are filtered) is an assumption, not a spec decision; carried into Tasks 4, 5 and 12 and pinned by TC-29/TC-91. 2) Spec wording says update and replace; test-plan observation suggests 'any parent rendering a Change column'. Not changed here. 3) Test plan skips several TC numbers (TC-18, TC-19, TC-57 to TC-59, TC-65 to TC-69, TC-73 to TC-79, TC-89); they are undefined and not tasked. 4) Task 19 asks the Developer to build UAT inputs (uat-plan.json, rendered files); the Developer does not run or post UAT. 5) Branch is a fork contribution; nothing pushed.
