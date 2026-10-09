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
