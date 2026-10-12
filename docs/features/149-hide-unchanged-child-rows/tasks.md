# Tasks: Hide Unchanged Rows in Inline Child Tables

Source: `specification.md` (AC numbers below follow its Success Criteria order),
`architecture.md` (design), `test-plan.md` (TC-nn identifiers). Test names follow
`MethodName_Scenario_ExpectedResult` and are TUnit tests under `src/tests/`.

## Tasks

### Task 1: Capture pre-feature golden baselines
**Priority:** High
**Description:**
Before any production change, record the current output that must not change with
`--show-unchanged-values`, and the output for plans without parent-child resources.
Generate one golden markdown file per fixture used by TC-30 to TC-39 (option on), and one
per existing non-parent-child snapshot fixture, from the unmodified production code. Commit
the golden files in a commit that touches no production code. Developer role.
**Acceptance Criteria:**
- [x] `git diff main -- src/Oocx.TfPlan2Md` is empty at the time the baselines are generated
      (verifies the "pre-change build" premise in the test plan)
- [x] One golden file exists for every fixture in TC-30 to TC-39 and TC-61, rendered with
      `--show-unchanged-values`
- [x] Golden files for every existing non-parent-child fixture exist for TC-64
      (the committed default-mode files under `TestData/Snapshots/` at the pre-feature commit are
      the baselines; Task 18 verifies they stay byte-identical)
- [x] Generating the baselines twice produces byte-identical files
- [x] Commit contains only golden files and fixtures (no `src/` production changes)
**Dependencies:** None

### Task 2: Add child-noun and hidden-count fields to the model and provider registrations
**Priority:** High
**Description:**
Add `HiddenUnchangedCount` (int, default 0) and `ItemNameSingular` / `ItemNamePlural`
(string) to `ChildResourceGroup`. Add the same two nouns as `required` members on
`ParentChildRelationship`. Copy the nouns onto the group in `MergeParentChildRelationships`,
and take them from the first group in `MergeGroupsByLabel`, as `Columns` already is. Set the
nouns in the `AzureRMModule`, `AzureADModule` and `AzureDevOpsModule` registrations, with
the values from architecture.md (Routes/route, Subnets/subnet, Security Rules/security rule,
DNS Records/DNS record, Members/member, Administrators/administrator). Do not derive nouns
from the label. Keep provider nouns out of `MarkdownGeneration/` and `Models`.
**Acceptance Criteria:**
- [x] `ChildResourceGroup` exposes `HiddenUnchangedCount` (default 0), `ItemNameSingular`
      and `ItemNamePlural`
- [x] Every `ParentChildRelationship` registration in the three provider modules defines
      non-empty singular and plural nouns (TC-15), with the exact values listed above
- [x] Merged groups copy the nouns from the relationship (TC-100, first half)
- [x] `MergeGroupsByLabel` takes the nouns from the first group when two groups share a
      label (TC-100, second half)
- [x] Architecture test fails if `MarkdownGeneration/` or `Models` contains the literals
      `route`, `subnet` or `security rule` as provider nouns (TC-101)
- [x] Group-level default (`HiddenUnchangedCount == 0`) is covered (TC-09 model part)
- [x] Existing unit tests pass without changing their expected output
**Dependencies:** None

### Task 3: Add the shared child-table helper to the core renderer
**Priority:** High
**Description:**
Create `ChildTableSection` in `MarkdownGeneration/Rendering/`. It provides
`WriteMixedSourcesWarning(writer)`, which writes the existing warning paragraph text
exactly, followed by a blank line, and `WriteHiddenUnchangedNote(writer, count, singular,
plural)`, which writes nothing for `count == 0`, otherwise one italic line
`_{count} unchanged {noun} hidden._` followed by a blank line, using the singular noun
only when `count == 1`. The helper is added and tested here; the renderers adopt it in
Tasks 6 and 7.
**Acceptance Criteria:**
- [x] `WriteHiddenUnchangedNote` with count 0 writes no output (TC-92)
- [x] Count 1 writes the singular form, for example `_1 unchanged route hidden._` (TC-92)
- [x] Count greater than 1 writes the plural form (TC-92)
- [x] Count 100 writes `_100 unchanged routes hidden._` with the exact integer (TC-103)
- [x] `WriteMixedSourcesWarning` text equals `⚠️ **Warning:** This resource has children
      managed both inline and as separate resources. This configuration will cause
      conflicts.` followed by a blank line (TC-93)
- [x] The note is a single line matching `^_\d+ unchanged .+ hidden\._$` (supports AC 6, 7)
**Dependencies:** None

### Task 4: Filter unchanged child rows in DisplayFilteringStage
**Priority:** High
**Description:**
Add a `showUnchangedValues` parameter to `DisplayFilteringStage` next to
`ignoreAzureIdCaseChanges`. `ReportModelBuilder.CreateDisplayFilteringStage` passes
`_showUnchangedValues`. After the resource-level filters, and only when the flag is false,
replace each display change's `ChildResourceGroups` with groups whose `Rows` exclude
`ActionIcons.Unchanged`, using `with` on the record and no in-place mutation. Set
`HiddenUnchangedCount` to the number removed. Skip parents whose action is `create` or
`delete`. Filter every other action, including unknown and no-op parents kept for a changed
child, as the architecture open-question assumption states; TC-29 and TC-91 pin that
assumption. Keep groups that end up empty. Keep remaining rows in their original order.
`HasChildrenWithChanges` must still read the unfiltered rows. Document on
`ChildResourceGroup.Rows` and on the stage that rows are filtered after this stage.
**Acceptance Criteria:**
- [x] Update parent with ➕🔄❌⏺️ children yields ➕🔄❌ only, `HiddenUnchangedCount == 1`
      (TC-01)
- [x] 2 changed and 8 unchanged yields 2 rows and `HiddenUnchangedCount == 8` (TC-02)
- [x] `showUnchangedValues: true` leaves groups equal to input (TC-03)
- [x] Inline-source, separate-source and mixed-source groups all filter; `HasMixedSources`
      is preserved for the mixed group (TC-04, TC-05, TC-06)
- [x] Interleaved indicators keep the original order of the remaining rows (TC-07)
- [x] Create and delete parents keep groups unchanged, count 0, even with an injected ⏺️
      row (TC-08)
- [x] All rows unchanged keeps the group with empty `Rows`, the count, `HasMixedSources`
      and `ChildResourceGroups.Count` unchanged (TC-09)
- [x] No-op parent with only unchanged children is still dropped; with one changed child
      it is kept and its unchanged rows hidden (TC-26)
- [x] Replace parent hides unchanged rows (TC-28)
- [x] No-op parent kept for a child change hides unchanged rows (TC-29)
- [x] Input groups and their `Rows` lists are not mutated (TC-90)
- [x] Unknown parent action filters like update (TC-91)
- [x] `ReportModelBuilder` passes the flag through (covered by TC-03 and Task 12 integration)
**Dependencies:** Task 2

### Task 5: Use the full child count in the no-op security-rule check
**Priority:** High
**Description:**
`DefaultResourceRenderPolicy.IsNoOpParentSecurityRuleScenario` checks `Rows.Count == 2`.
Change it to `Rows.Count + HiddenUnchangedCount == 2`, so the `_No attribute changes._`
behaviour is unchanged after filtering.
**Acceptance Criteria:**
- [x] Security-rules group with `Rows.Count == 1` and `HiddenUnchangedCount == 1` is treated
      as the two-row scenario (TC-27)
- [x] `Rows.Count == 1` with `HiddenUnchangedCount == 0` is not treated as the two-row
      scenario (TC-27)
- [x] The existing no-op NSG snapshot still renders `_No attribute changes._` unchanged
      (TC-27)
**Dependencies:** Task 4

### Task 6: Render default child tables with the hidden-count note
**Priority:** High
**Description:**
Change `DefaultResourceRenderer.RenderChildResources` to render each group it receives.
When rows remain: heading, mixed-sources warning (if mixed) via `ChildTableSection`, table
(keep the existing table writer, including its separator row), then the hidden-count note
directly below the table. When no rows remain: no heading, table or note; if
`HasMixedSources`, write only the warning. Replace the warning copy in this renderer with
the helper call. Do not read `IRenderContext.ShowUnchangedValues`.
**Acceptance Criteria:**
- [x] Rows hidden: output order is heading, table, blank line, note (TC-10)
- [x] The note is one italic line with no other text between table and note (TC-11)
- [x] Note uses the group nouns, not nouns derived from the label (TC-12)
- [x] One hidden uses the singular noun; eight hidden and two hidden use the plural (TC-13,
      TC-14)
- [x] Nothing hidden writes no note (TC-16)
- [x] Group with `showUnchanged` model (count 0, ⏺️ rows) renders all rows and no note
      (TC-17)
- [x] All rows hidden, not mixed: output contains no child heading, table header or note
      (TC-20, TC-21)
- [x] Group of only ⏺️ rows with count 0 renders heading and every row (TC-22)
- [x] All rows hidden and mixed: output is exactly the warning paragraph (TC-23)
- [x] Rows remain and mixed: order is heading, warning, table, note (TC-24)
- [x] Mixed with nothing hidden: output equals the pre-feature output for that input (TC-25)
- [x] Exactly one remaining changed row plus a note renders (edge case in test plan)
- [x] VNet subnets, DNS records, AD members and DevOps administrators groups with all rows
      hidden omit the section (TC-94)
- [x] Table separator rows are unchanged for this renderer
**Dependencies:** Task 3, Task 4

### Task 7: Render merged NSG security-rule tables with the hidden-count note
**Priority:** High
**Description:**
Apply the same layout rules as Task 6 to `NsgMergedSecurityRulesRenderer.Render`, using the
security-rule nouns from the registration. Replace its copy of the mixed-sources warning
with the helper. Keep this renderer's own separator row format; the two renderers' separators
differ and must not be unified.
**Acceptance Criteria:**
- [x] Separate rules with ➕ and ⏺️ rows show only the changed rows and
      `_N unchanged security rules hidden._` (TC-95)
- [x] One hidden uses `security rule` singular (TC-95)
- [x] All separate rules hidden: the rules section is omitted (TC-96)
- [x] All rules hidden and mixed sources: only the warning (TC-97)
- [x] Model with `showUnchanged` (count 0) renders all rows, no note, separator row
      unchanged (TC-98)
- [x] No-op parent with a separate rule change still renders the changed rule
      (issue 120 regression, TC-99)
**Dependencies:** Task 3, Task 4

### Task 8: Filter inline NSG rules at render time, with fallback
**Priority:** High
**Description:**
In `NsgRenderer` (`Providers/AzureRM/`), on the update path (not create, not delete) and
when `context.ShowUnchangedValues` is false, remove `RuleChanges` entries whose `Change` is
`ActionIcons.Unchanged` and count them. Write the hidden-count note with the security-rule
nouns, taken from the same constants as the `AzureRMModule` registration. If no rule rows
remain, call `base.Render` so the parent's attribute changes still render and no empty
details block appears. Keep the existing `base.Render` fallback for missing rule data.
Row order and formatting of remaining rows must not change.
**Acceptance Criteria:**
- [x] Inline rules update with the default flag omits ⏺️ rules and writes the note
      (TC-80)
- [x] With `--show-unchanged-values`, output is identical to pre-feature output, including
      ⏺️ row order and formatting (TC-81, golden from Task 1)
- [x] One unchanged rule gives `_1 unchanged security rule hidden._`; several give the plural
      (TC-82)
- [x] Nothing unchanged writes no note (TC-83)
- [x] All inline rules unchanged, including a variant where only tags change: output has the
      attribute table, no rules heading, table or note, and no empty details block (TC-84)
- [x] Create and delete paths are unaffected by the filter (TC-85)
- [x] Remaining rule rows keep their original order (TC-86)
- [x] No rule data falls back to the default renderer as today (TC-87)
- [x] Nouns used by this renderer equal the nouns registered for `Security Rules` in
      `AzureRMModule` (TC-88)
**Dependencies:** Task 2, Task 3

### Task 9: Update the `--show-unchanged-values` help text
**Priority:** Medium
**Description:**
Change the help line for `--show-unchanged-values` in
`src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` (currently "Include unchanged attribute values
in tables.") so it says the option also includes unchanged child rows. Update the existing
help tests and any help snapshot that contains the line. No new option is added.
**Acceptance Criteria:**
- [x] Help line for `--show-unchanged-values` mentions attribute values and unchanged child
      rows (TC-71)
- [x] No other help line changes (TC-71)
- [x] Existing help tests pass after the expected text is updated
**Dependencies:** None

### Task 10: Update user documentation for hidden child rows
**Priority:** Medium
**Owner:** Technical Writer (docs are not Developer-owned; Developer must not edit these files)
**Description:**
Update `README.md` and `docs/features.md` so that: the `--show-unchanged-values` option
tables say the option also includes unchanged child rows; the parent-child resource grouping
section describes the default hidden-count note and the omitted section for fully unchanged
children; and the NSG section no longer says unchanged security rules are always shown or
shown "for completeness". The NSG section states that unchanged rules are hidden by default.
Wording must match the help text from Task 9. The broken link to feature 068 noted in the
work protocol is not part of this task.
**Acceptance Criteria:**
- [ ] `README.md` and `docs/features.md` option entries for `--show-unchanged-values` mention
      unchanged child rows (AC 18)
- [ ] `docs/features.md` NSG section no longer contains the phrases "shown for completeness"
      or any wording that says unchanged security rules are always shown (AC 15)
- [ ] `docs/features.md` NSG section states that unchanged security rules are hidden by
      default and shown with `--show-unchanged-values` (AC 15)
- [ ] Parent-child section describes the hidden-count note and the omitted section (supports
      AC 6, AC 10)
- [ ] No Terraform-doc or generated block changed (no `BEGIN_TF_DOCS` edits in this task)
**Dependencies:** Task 9

### Task 11: Add documentation alignment tests for hidden child rows
**Priority:** Medium
**Description:**
Extend `src/tests/Oocx.TfPlan2Md.TUnit/Architecture/DocumentationAlignmentTests.cs`, which
already checks documentation consistency, with string assertions on `README.md` and
`docs/features.md`: the NSG phrases are gone (TC-70) and the option entries mention child
rows (TC-72). Assertions check the file contents and fail on the old wording.
**Acceptance Criteria:**
- [ ] TC-70: the test fails when `docs/features.md` contains "shown for completeness" in the
      NSG section and passes after Task 10
- [ ] TC-72: the test fails when the `--show-unchanged-values` option entry in `README.md` or
      `docs/features.md` lacks a mention of child rows, and passes after Task 10
- [ ] Test names follow `MethodName_Scenario_ExpectedResult`
**Dependencies:** Task 10

### Task 12: Route-table integration tests (separate, inline, replace, create/delete)
**Priority:** High
**Description:**
Add route-table plan fixtures under `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` where no
existing fixture has an unchanged child next to changed ones: a route table with separate
`azurerm_route` children (1 add, 1 update, 1 delete, 1 unchanged, as in the spec example),
and inline `route` children with 2 unchanged. Run the full pipeline (`MarkdownRenderer`)
with and without the option. Cover the create, delete, replace and no-op-with-tag-only
cases. Also cover the single-resource path, which hard-codes the flag to false.
**Acceptance Criteria:**
- [x] Default output for the separate-children plan contains the three changed rows and
      `_1 unchanged route hidden._`, and not the unchanged route name (TC-30)
- [x] Option-on output equals the golden baseline from Task 1 and has the ⏺️ row and no note
      (TC-31)
- [x] Inline plus separate mixed table: warning, changed rows, and a note whose count includes
      unchanged rows of both sources (TC-32)
- [x] Remaining rows are in the same order as the option-on table with ⏺️ rows removed
      (TC-40)
- [x] Note directly follows the last table row after one blank line (TC-41)
- [x] Route table with only tags changing and all routes unchanged: no `Routes` heading,
      table or note; option on gives the full ⏺️ table (TC-46, TC-47)
- [x] Create and delete route tables render identical to pre-feature output (TC-48, TC-49)
- [x] Parent summary line child counts are identical in both modes and to pre-feature text
      (TC-50)
- [x] Replace parent hides unchanged routes (AC 1, Decision 5)
- [x] Mixed-sources table with all children unchanged renders the warning on its own (TC-52)
- [x] `MarkdownRenderer.RenderResourceChange` renders hidden rows and the note consistently
      with the model and does not read the context flag (TC-56)
**Dependencies:** Task 1, Task 4, Task 6

### Task 13: VNet subnet and DNS record integration tests
**Priority:** Medium
**Description:**
Cover `azurerm_virtual_network` inline subnets and `azurerm_dns_zone` records (at least two
record types) through the full pipeline, with and without the option. Use the existing
fixtures `azurerm-vnet-inline-subnets-plan.json` and `azurerm-dns-zone-records-plan.json`
where they have an unchanged child next to changed ones; add a fixture where they do not.
**Acceptance Criteria:**
- [x] Subnets default output shows only changed subnets and the note names `subnets` (TC-33)
- [x] One unchanged subnet gives `_1 unchanged subnet hidden._`; two or more give the plural
      (TC-42, TC-43)
- [x] DNS records default output shows only changed records and the note names `DNS records`
      with correct singular/plural (TC-36, TC-42, TC-43)
- [x] No note when nothing is hidden for either pattern (TC-44)
- [x] No note with the option on for either pattern; full table rendered (TC-45)
- [x] Option-on output equals the golden baseline from Task 1 (TC-61)
**Dependencies:** Task 1, Task 6

### Task 14: NSG integration tests (inline rules, separate rules)
**Priority:** Medium
**Description:**
Cover `azurerm_network_security_group` through the full pipeline for inline rules only
(`NsgRenderer` path), for separate `azurerm_network_security_rule` children (merged path),
and for inline rules where all rules are unchanged and only tags change (fallback). Use the
existing fixtures `azurerm-nsg-inline-rules-plan.json`, `nsg-with-no-op-rules.json` and
`nsg-with-separate-rule-updates.json` where they fit; add fixtures where they do not.
**Acceptance Criteria:**
- [x] Inline rules: 1 changed and 2 unchanged gives the changed rule only and
      `_2 unchanged security rules hidden._` (TC-35)
- [x] Separate rules with 1 create and 1 unchanged gives the created rule and
      `_1 unchanged security rule hidden._` (TC-34)
- [x] Note names `security rules` with singular and plural correct (TC-42, TC-43)
- [x] Inline rules all unchanged with a tag change: attribute table only, no empty `<details>`
      block, no rules section (TC-84 at integration level)
- [x] With the option on, all rules render in the existing format, no note (TC-45, TC-81)
- [x] Option-on output equals the golden baseline from Task 1 (TC-61)
**Dependencies:** Task 1, Task 7, Task 8

### Task 15: Azure AD group and Azure DevOps integration tests
**Priority:** Medium
**Description:**
Cover `azuread_group` members (inline members plus a separate `azuread_group_member`),
`azuredevops_team` members and administrators, and `azuredevops_group` members through the
full pipeline. Use the existing fixtures `azuread-group-members-plan.json`,
`azuredevops-team-members-plan.json` and `azuredevops-group-members-plan.json`, and add
an administrators fixture if none has an unchanged administrator next to changed ones.
Verify that the Azure AD member-type summary counts still include unchanged members in both
modes.
**Acceptance Criteria:**
- [x] Azure AD members default output shows only changed members with `_N unchanged members
      hidden._` (TC-37, TC-42, TC-43)
- [x] DevOps team administrators show only the changed administrator and the note names
      `administrators` with singular/plural correct (TC-38, TC-42, TC-43)
- [x] DevOps group members render with only changed rows and the note (TC-39)
- [x] Azure AD summary member-type counts include unchanged members in default and option-on
      modes, equal to the pre-feature summary (TC-51)
- [x] No note when nothing is hidden and no note with the option on, for each pattern
      (TC-44, TC-45)
- [x] Option-on output equals the golden baseline from Task 1 for each fixture (TC-61)
**Dependencies:** Task 1, Task 2, Task 6

### Task 16: Edge-case and code-analysis integration tests
**Priority:** Medium
**Description:**
Cover the remaining edge cases: a merged child with a code analysis finding whose row is
hidden; a `known after apply` member or subnet, which must not be treated as unchanged; the
`--ignore-azure-id-case-changes` option; the Azure AD no-op suppression rule; and the
combination of `--show-unchanged-values` with other options. Add fixtures only where no
existing one covers the case (for example `azurerm-vnet-known-after-apply-plan.json` and
`azuread-group-members-known-after-apply-plan.json`).
**Acceptance Criteria:**
- [x] Finding on an unchanged merged child still appears in the parent section when the row
      is hidden (TC-53)
- [x] `known after apply` members and subnets are not ⏺️ and stay visible (TC-102)
- [x] A case-only change under `--ignore-azure-id-case-changes` is hidden like other ⏺️ rows
      (TC-104)
- [x] Azure AD no-op parent with all members hidden keeps the `_No attribute changes._`
      suppression unchanged (TC-105)
- [x] Child behaviour depends only on `--show-unchanged-values`, not on
      `--hide-unchanged-outputs` or `--large-value-format` (TC-106)
**Dependencies:** Task 4, Task 6, Task 7, Task 8, Task 15

### Task 17: Markdown validity and render-target matrix
**Priority:** Medium
**Description:**
Run the default-mode scenarios from TC-30 to TC-39 through the existing markdown lint and
validation fixtures (`MarkdownLintFixture`, `MarkdownValidationTests`) for the Azure DevOps,
GitHub and Bitbucket render targets, in every details display mode. Run one scenario
(TC-30) through the full target and mode matrix and assert the same child rows and note in
each. The rendering check in a real PR UI is UAT, not part of this task.
**Acceptance Criteria:**
- [x] Each default scenario from TC-30 to TC-39 passes markdown lint and validation for
      Azure DevOps, GitHub and Bitbucket (TC-54)
- [x] The TC-30 scenario yields the same child rows and note for every render target and
      details display mode (TC-55)
- [x] Child names with `|`, backticks, `<` or `&` do not break the table (test plan edge case,
      TC-54)
**Dependencies:** Task 12, Task 13, Task 14, Task 15

### Task 18: Snapshot and regression review
**Priority:** High
**Description:**
Run every existing snapshot test and the golden comparisons. Snapshots that contain ⏺️ child
rows in default mode change on purpose: `azuread-group-members.md`,
`parent-child-resource-grouping-uat.md`, `comprehensive-demo.md`,
`comprehensive-demo-full.md`. For each, check that the diff contains only removed ⏺️ child
rows, added hidden-count notes and removed child sections, and record the reviewed diff in
the work protocol. `firewall-rules.md` must not change. Confirm that option-on snapshots and
non-parent-child snapshots are byte-identical to Task 1 baselines. Use
`scripts/update-test-snapshots.sh` only for the four intended snapshots.
**Acceptance Criteria:**
- [x] Every existing snapshot test with `--show-unchanged-values` passes without baseline
      change (TC-60)
- [x] The four intended snapshots differ only by removed ⏺️ child rows, added hidden-count
      notes and removed child sections; the reviewed diff summary is in the work protocol
      (TC-62)
- [x] `firewall-rules.md` is unchanged (TC-62)
- [x] Build definition and variable group tests, and firewall rule collection template tests,
      pass unchanged and still contain their ⏺️ rows with the option off (TC-63)
- [x] Plans without parent-child resources render byte-identical to Task 1 baselines (TC-64)
- [x] Option-on output for every TC-30 to TC-39 fixture equals its Task 1 baseline (TC-61)
**Dependencies:** Task 1, Task 6, Task 7, Task 8

### Task 19: Prepare UAT input files (Developer builds, does not run UAT)
**Priority:** Medium
**Description:**
Build the UAT input files specified in `uat-test-plan.md`: `uat-plan.json` (scenarios as
listed in that plan, with fictitious names only), the default rendering `uat-plan.md`, and
`uat-plan-show-unchanged.md` rendered with `--show-unchanged-values`. Regenerate
`artifacts/comprehensive-demo.md` and `artifacts/comprehensive-demo-simple-diff.md` with the
`generate-demo-artifacts` skill. The Developer does not create UAT PRs, post comments or
judge the rendering; the Maintainer runs UAT as the UAT plan states.
**Acceptance Criteria:**
- [x] `uat-plan.json` contains one scenario per row of the UAT plan table, including the
      edge cases listed there, and includes at least one scenario for each child label
      (Routes, Subnets, Security Rules, DNS Records, Members, Administrators)
- [x] `uat-plan.json` contains no real tenant, person or address data
- [x] `uat-plan.md` and `uat-plan-show-unchanged.md` are generated from `uat-plan.json`
      through the normal pipeline
- [x] Regenerated comprehensive demo artifacts differ from the previous ones only as Task 18
      lists
- [x] The unchanged-code-analysis scenario is either included or explicitly omitted with a
      note in the work protocol
**Dependencies:** Task 18

## Implementation Order

1. **Task 1** first. The golden baselines must come from unmodified production code, so
   they must be committed before any production change in Tasks 2 to 8.
2. **Tasks 2, 3 and 9** next, in any order, because each is a foundation with no
   dependencies. Task 2 supplies the nouns and count fields that the filter and every
   renderer need. Task 3 supplies the shared note and warning helper. Task 9 is independent
   help text.
3. **Task 4** (stage filter) after Task 2. It is the single model-side filter that every
   renderer relies on, so its tests anchor the rest. **Task 5** follows directly, because it
   depends on the filtered count.
4. **Tasks 6, 7 and 8** (renderers: default, merged NSG, inline NSG). They depend on the
   stage and helper, and they are the three paths the test plan names. Task 8 can run in
   parallel with 6 and 7.
5. **Task 10** (technical writer docs) after Task 9 so wording matches the help text,
   then **Task 11** (docs alignment tests) after Task 10.
6. **Tasks 12 to 15** (integration by pattern: route tables, VNet and DNS, NSG, Azure AD and
   DevOps). These need the renderers and the baselines. They can run in parallel with each
   other.
7. **Task 16** (edge cases) after the pattern tests, because its cases reuse those fixtures.
   **Task 17** (markdown validity and render-target matrix) after 12 to 15.
8. **Task 18** (snapshot review) after all renderers, once the snapshot diffs are final.
9. **Task 19** (UAT inputs) last, because its files and the regenerated demo artifacts must
   reflect the final output.

## Coverage Check

Reverse mapping of the specification's acceptance criteria (AC) and the test plan's
scenarios (TC) to tasks. Every AC and every TC defined in `test-plan.md` appears here.

| AC | Tasks |
|----|-------|
| 1 Default hides unchanged, shows ➕🔄❌ | 4, 12 |
| 2 Option on renders identical output | 1, 4, 8, 12, 18 |
| 3 Inline, separate and mixed sources | 4, 6, 12 |
| 4 Every parent-child pattern, one non-route | 13, 14, 15 |
| 5 Relative order preserved | 4, 12 |
| 6 Note below table with count and child label | 3, 6, 12 |
| 7 Singular and plural per table type | 2, 6, 13, 14, 15 |
| 8 No note when nothing hidden | 6, 13, 14, 15 |
| 9 No note with option on | 6, 12, 13, 14, 15 |
| 10 All unchanged: section omitted | 6, 12 |
| 11 All unchanged, option on: full table | 6, 12 |
| 12 Create and delete unchanged | 4, 12 |
| 13 Summary child counts unchanged | 12, 15 |
| 14 Mixed warning shown whenever it is today | 6, 7, 12 |
| 15 NSG docs updated | 10, 11 |
| 16 Code-analysis findings still shown | 16 |
| 17 Firewall, build definition, variable group unchanged | 18 |
| 18 Help text and docs mention child rows | 9, 10, 11 |
| 19 Renders correctly in ADO and GitHub | 17 (automated markdown validity); UAT run by the Maintainer per `uat-test-plan.md`, inputs in 19 |

| TC | Tasks |
|----|-------|
| TC-01 to TC-09 | 4 (TC-09 model part in 2) |
| TC-10 to TC-17, TC-20 to TC-25 | 6 |
| TC-26 to TC-29 | 4 |
| TC-27 | 5 |
| TC-30 to TC-32 | 12 |
| TC-33 | 13 |
| TC-34, TC-35 | 14 |
| TC-36 | 13 |
| TC-37 to TC-39 | 15 |
| TC-40, TC-41 | 12 |
| TC-42, TC-43 | 13, 14, 15 (routes in 12) |
| TC-44, TC-45 | 12 to 15 |
| TC-46 to TC-52 | 12 (TC-51 in 15) |
| TC-53 | 16 |
| TC-54, TC-55 | 17 |
| TC-56 | 12 |
| TC-60, TC-62, TC-63, TC-64 | 18 |
| TC-61 | 1 (baseline), 13, 14, 15, 18 |
| TC-70, TC-72 | 11 |
| TC-71 | 9 |
| TC-80 to TC-87 | 8 |
| TC-88 | 8 |
| TC-90, TC-91 | 4 |
| TC-92, TC-93, TC-103 | 3 |
| TC-94 | 6 |
| TC-95 to TC-99 | 7 |
| TC-100, TC-101 | 2 |
| TC-102, TC-104, TC-105, TC-106 | 16 |

Gaps: none. Open assumption carried into Tasks 4, 5 and 12: no-op parents kept for a child
change are filtered (architecture open question; TC-29 and TC-91 pin it). Test plan
numbering skips TC-18, TC-19, TC-57 to TC-59, TC-65 to TC-69, TC-73 to TC-79 and TC-89, so
those IDs are not defined and not tasked.
