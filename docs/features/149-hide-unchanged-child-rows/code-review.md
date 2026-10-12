# Code Review: Hide Unchanged Rows in Inline Child Tables

## Summary

This is round 2. Claude reviewed as the fallback reviewer because `codex` is not installed.

Round 1 returned REWORK with three Majors. Three commits since then address them:

- Developer `185160fd`
- Quality Engineer `a2fedead`
- Technical Writer `6fb80577`

All three Majors are resolved and nothing regressed. The change is small, targeted and
proportionate:

- The forget fix adds one `bool` to `ChildResourceRow`.
- It sets the flag at the two places where rows are built.
- The filter now uses the flag instead of the icon.

The Maintainer decided the forget scope and the test footprint, and both are recorded in
`state.json`. One Minor issue is left: a garbled sentence in `docs/architecture.md`. It
does not block release.

## Verification Results

I ran these commands at `6fb80577` on this Mac (arm64):

| Check | Command | Result |
| --- | --- | --- |
| Full suite, Debug | `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` | 1616 total, 1610 passed. The 6 failures are the known local `Docker_*` tests (exit 127). The total is 4 higher than round 1's 1612, matching the 4 new tests |
| Suite with coverage, Release | same runner, `--coverage` cobertura | 1610/1616, same 6 `Docker_*` failures |
| Coverage enforcer | `Oocx.TfPlan2Md.CoverageEnforcer --line-threshold 84.48 --branch-threshold 72.80` | Line 89.75% PASS, branch 81.12% PASS. Round 1 was 89.72% and 81.09% |
| Demo artifacts | `scripts/generate-demo-artifacts.sh` | Every regenerated file differs only in the header line (commit and timestamp). The artifacts are current, and the fix changed no demo output. I restored the files afterwards |
| Forget probe from round 1 | `tfplan2md.dll` (Release) on the scratchpad plan | The forgotten route is now visible as `⏺️ forgotten-route … azurerm_route.forgotten`, with no hidden-count note |
| markdownlint, global docs | `scripts/markdownlint.sh docs/features.md docs/architecture.md`, plus the same files from `origin/main` copied to `.tmp/lint-main/` | Error counts per file and rule are identical on the branch and on `origin/main` (see below). This change adds no new lint errors |

The markdownlint wrapper turns arguments into repo-relative paths with
`realpath --relative-to`, which BSD `realpath` on macOS does not support. Absolute-path
arguments therefore fall through unchanged, the container cannot see them, and it lints
0 files. That is why the Technical Writer's run showed 0 files. Relative paths from the
repository root work.

Per-rule lint counts, the same on `origin/main` and the branch:

| File | Rule counts |
| --- | --- |
| `docs/features.md` | MD007 1, MD031 64, MD032 53, MD033 1, MD036 8, MD049 8 |
| `docs/architecture.md` | MD032 30, MD034 6, MD036 1 |

## Specification Compliance

The criterion-to-code-to-test mapping from round 1 still holds. The fix changes only the
criterion that defines which rows count as unchanged:

| Criterion or decision | Implementation | Proving test |
| --- | --- | --- |
| Hide only truly unchanged rows (Maintainer decision, `state.json`) | `ChildResourceRow.IsUnchanged`. It is set for inline rows with equal JSON (`ReportModelBuilder.ParentChildMerging.cs:305`) and for separate children whose action is `no-op` (`:800`). The filter is in `DisplayFilteringStage.cs:112` | `Render_ForgottenSeparateChild_IsNotHidden` (end to end, using the round-1 probe plan as a fixture) and `Filter_UnchangedIconRowNotMarkedUnchanged_IsKept` (stage unit test) |
| Indicators unchanged | `GetChildActionIndicator` untouched | the forget row still renders `⏺️` (asserted in the test above) |
| TC-56 (amended) | `MarkdownRenderer.RenderResourceChange` unchanged; inline-NSG exception documented in `test-plan.md` | `RenderResourceChange_MergedNsgFromShowUnchangedModel_RendersModelRows`, `RenderResourceChange_InlineNsgFromShowUnchangedModel_HidesUnchangedRules` |
| Docs describe the rule | `docs/features.md` parent-child and NSG sections; `docs/architecture.md` | `DocumentationAlignmentTests` |

## What I Tried To Break

- **Rows built elsewhere without the flag.** `new ChildResourceRow` appears only at
  `ReportModelBuilder.ParentChildMerging.cs:603, 669, 783, 797`. Lines 669 and 783 build
  update rows, line 603 builds the inline row that line 305 then marks, and line 797
  builds the separate row that line 800 marks. Rows are copied only through `with`, which
  keeps the flag. No row type is missed.
- **Inline rows given the unchanged icon for another reason.** The ⏺️ icon is passed only
  on the equal-JSON branch at `:303`, which is the same branch that sets the flag. Held.
- **No-op parent with only a forgotten child.** `HasChildrenWithChanges`
  (`DisplayFilteringStage.cs:139`) still checks the icon, so such a parent is dropped, as
  it was on `origin/main`. This change did not cause it, and it is outside this feature's
  scope.
- **Inline NSG path.** It still filters by the view-model icon
  (`NsgInlineRuleRows.cs:22`). Inline rules have no Terraform action of their own, so the
  icon there only ever means equal before and after values. Held.
- **Dead branch removed.** `NsgMergedSecurityRulesRenderer.Render` has no zero-rows
  branch any more. The only caller's guard (`AzureRmResourceRenderers.cs:224-229`) handles
  that case, and `Render_SeparateNsgRuleCreated_*` and the merged NSG tests still pass.
- **NBSP.** `ChildTableSection.cs:19` now uses `MarkdownHelpers.NonBreakingSpace`, which
  is `" "` (`SemanticFormatting.cs:14`). Snapshots and artifacts are byte-identical,
  so the output did not change.
- **Docs test bounds.** `end == -1` is now handled
  (`DocumentationAlignmentTests.cs`, NSG test).

## Issues Found

### Blockers

None.

### Major

None. All three round-1 Majors are resolved:

1. **Forget child hidden.** Fixed by the `IsUnchanged` flag, tested end to end and at
   stage level, and verified with my probe.
2. **Dead branch in `NsgMergedSecurityRulesRenderer`.** Removed.
3. **TC-56.** Amended by the Quality Engineer and pinned by two tests.

### Minor

1. **Garbled sentence.** `docs/architecture.md:342` reads "...other ⏺️ rows, such as
   `forget` children, stay and records the removed count in
   `ChildResourceGroup.HiddenUnchangedCount`...". The subject of "records" is lost.
   Suggested fix: "...from `ChildResourceGroups` and records the removed count in
   `ChildResourceGroup.HiddenUnchangedCount`, unless `--show-unchanged-values` is set.
   Other ⏺️ rows, such as `forget` children, stay visible." The Technical Writer owns
   this file.

### Suggestions

1. **`fix:` commit type.** `185160fd` is a `fix:` commit for a bug that was never
   released. If the branch is not squashed, Versionize will list it under Bug Fixes. The
   Release Manager may want to squash it or reword it when creating the PR.
2. **Markdownlint wrapper on macOS.** `scripts/markdownlint.sh` lints 0 files when given
   absolute paths on macOS, because BSD `realpath` has no `--relative-to`. The Workflow
   Engineer owns this script.

### Accepted and known, not findings

These will be flagged in the PR:

- The old `website/src/_data/docsPage.js` text.
- The Task 10 tick in `tasks.md`.
- The test footprint, which the Maintainer decided to keep.
- The 6 local `Docker_*` failures.
- The skip-worktree lock files.

## Decision

**APPROVED.** There are no Blockers or Majors, and the full suite and coverage pass apart
from the known local Docker failures. Demo artifacts are current, and the global docs add
no new lint errors. The remaining Minor is a docs wording fix that can go in with the PR.
