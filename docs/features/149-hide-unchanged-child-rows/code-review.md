# Code Review: Hide Unchanged Rows in Inline Child Tables

## Summary

The change hides unchanged (⏺️) child rows in parent-child tables by default, writes a
hidden-count note, and restores the old output with `--show-unchanged-values`. The core
design is sound and proportionate for an outside PR answering issue #679. It filters once
in `DisplayFilteringStage`, adds a small shared helper for the note and warning, puts the
nouns in provider registrations, and handles the NSG inline-rules path in the provider.
About 250 production lines change.

Three things need fixing before release:

- A child whose action is `forget` is now dropped from the report and counted as
  unchanged. I reproduced this.
- There is an unreachable, untested branch in `NsgMergedSecurityRulesRenderer` that
  duplicates the caller's fallback.
- TC-56 is ticked in `tasks.md`, but no test for it exists.

Reviewer: Claude (fallback, `codex` not installed).

## Verification Results

I ran these commands myself, on this Mac (arm64), at commit `f1e0abc5`:

| Check | Command | Result |
| --- | --- | --- |
| Full suite | `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` | 1612 total, 1606 passed, 6 failed. All 6 failures are `Docker_*` tests (exit 127, an environment problem, known and not caused by this change) |
| Suite with coverage, Release | same runner, `--coverage` cobertura | 1606/1612, same 6 `Docker_*` failures |
| Coverage enforcer | `Oocx.TfPlan2Md.CoverageEnforcer --line-threshold 84.48 --branch-threshold 72.80` | Line 89.72% PASS, branch 81.09% PASS |
| Demo artifacts | `scripts/generate-demo-artifacts.sh` | Regenerated output differs from the committed files only in the header line (commit and timestamp), so the artifacts are current. I restored the files afterwards |
| markdownlint | `scripts/markdownlint.sh artifacts/comprehensive-demo.md` (the file CI lints) | Clean |
| markdownlint | `artifacts/parent-child-resource-grouping-uat.md`, `uat-plan.md`, `uat-plan-show-unchanged.md` | Only MD024 duplicate `Members`/`Administrators` headings. These already exist on `origin/main` (one heading per resource), so this change did not cause them |
| Encoding of `DefaultResourceRenderer.Helpers.cs` (Developer point c) | `file`, `git diff --stat` | UTF-8, 13 lines added and 2 removed, matching the visible hunk. No whole-file rewrite and no line-ending change |
| NBSP in the warning | `od -t x1` on `ChildTableSection.cs` | Bytes `c2 a0` after `⚠️`, so the warning output is byte-identical to before |
| Snapshots | diff of `azuread-group-members.md` and `parent-child-resource-grouping-uat.md` | Each removes one ⏺️ member row and adds `_1 unchanged member hidden._`. Commit `8c17eee1` carries `SNAPSHOT_UPDATE_OK` with that reason. Convincing |

The Developer ran only `DocumentationAlignmentTests` after `f1e0abc5`. The full-suite run
above covers that gap.

I could not verify:

- Rendering in Azure DevOps and GitHub PR UIs (AC 19). That is UAT, and the Maintainer
  runs it for this fork contribution.
- CI status. No PR exists.

## Specification Compliance

| Acceptance criterion | Implementation | Proving test |
| --- | --- | --- |
| Default hides ⏺️, keeps ➕🔄❌ | `DisplayFilteringStage.cs:93-114` | `Filter_ShowUnchangedFalse_RemovesUnchangedRows`, `Render_SeparateRoutesDefault_ShowsChangedRowsAndSingularNote` |
| Option on: identical to before | stage skipped when `_showUnchangedValues` (`:64`) | `ShowUnchangedGoldenTests.Render_ShowUnchangedValues_MatchesPreFeatureGolden` (golden files captured from unmodified code in `9f28c825`) |
| Inline, separate and mixed sources | same filter | `Filter_InlineSourceRows_*`, `Filter_SeparateSourceRows_*`, `Filter_MixedSourceGroup_*` |
| Every pattern, at least one non-route | provider nouns in `AzureRMModule`, `AzureADModule`, `AzureDevOpsModule` | `HiddenChildRowsProviderPatternTests` (subnets, AD members, DevOps members/admins, NSG) |
| Relative order kept | `Where` over the list | `Filter_MixedIndicators_PreservesRelativeOrder`, `Render_SeparateRoutes_RemainingRowsKeepOptionOnOrder` |
| Note: count and child label | `ChildTableSection.WriteHiddenUnchangedNote` | `RenderChildResources_Note_UsesGroupNouns` |
| Singular/plural | `ChildTableSection.cs:114` | `RenderChildResources_Hidden_UsesSingularOnlyForOne`, NSG `NsgInlinePath_Nouns_MatchAzureRmModuleRegistration` |
| No note when nothing hidden | `hiddenCount <= 0` guard | `Render_NothingUnchanged_WritesNoNote`, `RenderChildResources_NothingHidden_*` |
| No note with option | count stays 0 | `Render_ShowUnchanged_ShowsUnchangedRowsAndNoNote` |
| All unchanged: section omitted | `DefaultResourceRenderer.Helpers.cs:96-105`, `NsgRenderer` fallbacks | `RenderChildResources_AllRowsHiddenNotMixed_WritesNothing`, `Render_AllRoutesUnchanged_OmitsSectionByDefaultAndShowsItWithOption` |
| All unchanged with option: full table | stage skipped | same test, option branch |
| Create/delete unchanged | stage skips create/delete; NSG inline guards `isCreate`/`isDelete` | `Filter_CreateOrDeleteParent_DoesNotTouchGroups`, `Render_CreateOrDeleteRouteTable_IsIdenticalInBothModes` |
| Summary counts unchanged | counts built before the stage | `Render_SeparateRoutes_SummaryLineIsSameInBothModes`, `Render_AzureAdGroup_SummaryChildCountsAreSameInBothModes` |
| Mixed warning kept when all hidden | `Helpers.cs:99-102` | `RenderChildResources_AllRowsHiddenAndMixed_WritesOnlyWarning`, `Render_MixedSourceRoutesAllUnchanged_ShowsOnlyWarningByDefault` |
| NSG docs updated | `docs/features.md` NSG section | `FeaturesDoc_NsgSection_DescribesUnchangedRulesAsHiddenByDefault` |
| Findings on hidden child still shown | unchanged merge path | `Render_FindingOnHiddenChild_StillAppearsInParentSection` |
| Firewall / build definition / variable group unchanged | not touched | existing snapshot tests pass; `examples/firewall-rules-demo/firewall-rules.md` regenerates identical |
| Help text and docs mention child rows | `HelpTextProvider.cs:33`, README, features.md | `HelpTextProviderTests`, `OptionTable_ShowUnchangedValues_MentionsUnchangedChildRows` |
| Renders in ADO and GitHub | plain italics | markdown validity matrix only (`HiddenChildRowsMarkdownValidityTests`); platform check is UAT |

Every criterion has code and an automated test, except AC 19, which needs UAT.

## What I Tried To Break

- **Child actions other than no-op that map to ⏺️.** `GetChildActionIndicator`
  (`ReportModelBuilder.ParentChildMerging.cs:997-1003`) maps every action other than
  create, update, replace and delete to ⏺️. I built a plan with an updated route table
  and an `azurerm_route` whose action is `forget`. Without the option, the route is in
  the summary (`❌ Destroy 1 azurerm_route`) but not in the details. With
  `--show-unchanged-values` it shows as a ⏺️ row. **Broken:** see Major 1.
- **Stage ordering.** Summary counts, the Azure AD rebuilder and `HasChildrenWithChanges`
  all read rows before `HideUnchangedChildRows` runs (`DisplayFilteringStage.cs:43-67`).
  Held.
- **No-op NSG `Rows.Count == 2` heuristic.** It now uses `Rows + HiddenUnchangedCount`. A
  null group gives `null == 2`, which is false, as before. Held. Covered by
  `Render_NoOpNsgWithTwoUnchangedSeparateRules_IsStillDropped`.
- **Input mutation.** The stage uses `with` copies, and `Filter_DoesNotMutateInputGroups`
  pins this. Held.
- **NSG routing.** I traced all three paths:
  - Inline-only groups (`HasExternalResources == false`) go to the view-model path, which
    filters with its own rule and falls back to `base.Render`.
  - Merged groups go to the merged renderer.
  - When merged rows are empty, `NsgRenderer` sends the change to `base.Render` before the
    merged renderer is reached. That makes the merged renderer's own empty-rows branch
    unreachable, and coverage confirms 0 hits on `NsgMergedSecurityRulesRenderer.cs:42-51`.
    See Major 2.
- **Single-resource path (Developer point a).** `MarkdownRenderer.RenderResourceChange`
  hard-codes `showUnchangedValues: false` (`MarkdownRenderer.cs:167`), and
  `NsgRenderer` reads `context.ShowUnchangedValues` (`AzureRmResourceRenderers.cs:251`).
  So a model built with the option on still loses its unchanged inline NSG rules on this
  path, and the fallback then renders the unfiltered ⏺️ group. Only tests call this
  method, so the CLI is not affected. See Major 3.
- **NBSP edit (Developer point c).** Intact, as shown above.
- **Empty `<details>`.** An update with no visible attributes and every child hidden
  writes `_No attribute changes._` through the default renderer. Azure AD uses
  `suppressNoAttributeChangesForNoOpParents`, so it could produce an empty block. I did
  not find an input that reaches that case: such updates are dropped by the
  `--ignore-azure-id-case-changes` filter by default. Untested hypothesis, see
  Suggestions.
- **Unicode and table-breaking names.** Covered by
  `Render_ChildNamesWithSpecialCharacters_KeepTableWellFormed`.
- **Large counts.** TC-103 covers count 100.

## Issues Found

### Blockers

None.

### Major

1. **A forgotten child disappears and is counted as unchanged.**
   - Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/DisplayFilteringStage.cs:108`,
     together with `ReportModelBuilder.ParentChildMerging.cs:1001-1002`.
   - What happens: rows are hidden by their icon, and a separate child with action `forget`
     (Terraform `removed` block) is given the ⏺️ icon. On an update or replace parent, its
     row is now hidden. When it is the only child, the section is omitted, so the report
     says nothing about it even though the summary lists `❌ Destroy 1 azurerm_route`.
   - Reproduced with a plan in the scratchpad: a route table tag change plus a forgotten
     route.
   - Why it matters: before this change the row was visible, with the wrong icon. Now it is
     invisible, against the user goal "see only the children that are added, removed or
     modified".
   - Spec conflict: the spec puts changing indicator logic out of scope. So this needs a
     Maintainer decision before the fix. Record it with
     `scripts/wp-append.sh --question`.
   - Proportionate fix: hide only rows that are really unchanged, meaning inline rows with
     equal JSON and separate children whose action is `no-op`. Do not hide every ⏺️ row.
     For example, filter on the child action, or map non-no-op actions to a different
     icon. Add a test with a `forget` child.
2. **Unreachable, untested duplicate branch.**
   - Location:
     `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/NsgMergedSecurityRulesRenderer.cs:42-52`,
     and its `<remarks>` at `:35-37`.
   - What happens: the only caller, `NsgRenderer.Render`
     (`AzureRmResourceRenderers.cs:224-229`), already sends groups with zero rows to
     `base.Render`. So this branch never runs, and coverage shows 0 hits.
   - Why it matters: the branch also behaves differently from the caller. It would write an
     empty details block with only the warning and drop the parent's attribute changes. So
     it is both dead and misleading. This is the one duplicate code path in the change.
   - Fix: keep one path. Removing the branch and its remark is the smaller change. If the
     merged renderer should own the layout as `architecture.md` describes, remove the
     `NsgRenderer` guard instead and test the branch.
3. **TC-56 is claimed but no test exists, and the code breaks what it describes.**
   - Location: `tasks.md:279-280` ticks "`MarkdownRenderer.RenderResourceChange` renders
     hidden rows and the note consistently with the model and does not read the context
     flag (TC-56)".
   - Evidence: no changed test references `RenderResourceChange`
     (`git diff origin/main...HEAD -G RenderResourceChange -- src/tests` is empty). The
     inline NSG path does read the flag (`AzureRmResourceRenderers.cs:251`).
   - Context: the Developer disclosed the behaviour in the work protocol. But a ticked
     checkbox with no test is false evidence.
   - Fix: this method is test-only, so I do not ask for a production change. The Quality
     Engineer should amend TC-56 to name the inline-NSG exception, and the Developer should
     add a test that pins the actual behaviour. Otherwise, untick the box and record why.

### Minor

1. **The NBSP in the warning is now an invisible literal.**
   `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ChildTableSection.cs:19` contains a
   literal U+00A0. The rest of the codebase writes it as ` `, and so did the two lines
   this replaces. An editor or formatter could silently turn it into a normal space and
   change every warning line in the output. Write `⚠️ **Warning:**`.
2. **The docs overstate the summary counts.** `docs/features.md`, Parent-Child section,
   says the parent summary child counts "include unchanged children". The ➕/🔄/❌ counts
   do not, as the spec's Out of Scope section says. Only the Azure AD member-type badge
   counts unchanged members. Reword this. The Technical Writer owns the file.
3. **A test can fail with the wrong error.**
   `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/DocumentationAlignmentTests.cs`
   (`FeaturesDoc_NsgSection_...`): if the NSG heading becomes the last `####` heading,
   `end` is -1 and `content[start..end]` throws `ArgumentOutOfRangeException` instead of
   failing the assertion. Treat -1 as the end of the content.
4. **Bookkeeping gaps.**
   - Commit `f1e0abc5` (Task 11 tests) has no work-protocol entry.
   - Tasks 10 and 11 are done but still unticked in `tasks.md`.
5. **Stale website text.** `website/src/_data/docsPage.js` still has the old
   `--show-unchanged-values` description. The Technical Writer reported this. It belongs
   to the Web Designer, but it should be fixed before release so the site and the CLI help
   agree.

### Suggestions

1. **Open question (Developer point b): no-op parent kept for a changed child.** I agree
   with the assumption to filter it. The parent renders a Change column, and the issue is
   about scanning past ⏺️ rows. `Render_NoOpRouteTableWithChangedRoute_HidesUnchangedRoutes`
   pins it. It is still unresolved in `state.json`, so close it at the next gate.
2. **Test footprint.** About 45 golden files and a 752-line `ChildTableScenarios.cs` is
   heavy next to about 250 production lines. It is justified by the test plan's byte-identity
   requirement, but the Maintainer may want fewer golden fixtures in an outside PR.
3. **Untested hypothesis: Azure AD empty details block.** The Azure AD renderer suppresses
   `_No attribute changes._` when child groups exist. If an Azure AD group update ever
   reaches the renderer with no visible attributes and every member hidden, the details
   block would be empty. I did not find a reachable input. A test with
   `--no-ignore-azure-id-case-changes` would settle it.

## Decision

**REWORK.**

- No Blockers.
- Three Majors:
  - The forget-child regression (needs a Maintainer decision on scope, then a small fix
    and a test).
  - The dead duplicate branch in `NsgMergedSecurityRulesRenderer`.
  - The unproven TC-56 claim.
- Developer point (d), the full suite: done here. Only the 6 known `Docker_*` tests fail.

Everything else, including scope, layering, snapshots, artifacts, docs alignment and
coverage, is in good shape and proportionate to issue #679.
