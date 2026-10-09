# Test Plan: Hide Unchanged Rows in Inline Child Tables

## Overview

Without `--show-unchanged-values`, child tables of parent-child resource grouping omit
unchanged (⏺️) rows and show a hidden-count note. With the option, output is byte-for-byte
what it is today. The design (architecture.md) has three code paths and two filter sites,
so the plan tests each path and each filter site separately and then proves the behaviour
end to end for more than one pattern.

All tests are TUnit, named `MethodName_Scenario_ExpectedResult`, fully automated. The
Developer writes them; this plan only specifies them. Test data lives under
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/`. Existing fixtures to extend or reuse:
`azurerm-route-table-inline-routes-plan.json`, `azurerm-vnet-inline-subnets-plan.json`,
`azurerm-vnet-mixed-subnets-plan.json`, `azurerm-nsg-inline-rules-plan.json`,
`nsg-with-no-op-rules.json`, `nsg-with-separate-rule-updates.json`,
`azurerm-dns-zone-records-plan.json`, `azuread-group-members-plan.json`,
`azuredevops-team-members-plan.json`, `azuredevops-group-members-plan.json`.
New fixtures are needed where no existing plan has an unchanged child next to changed
ones (route table with separate `azurerm_route` children; all-unchanged and mixed-source
cases).

Test layers:

- **Unit, stage:** `Stages/DisplayFilteringStageTests.cs` (extend).
- **Unit, model/provider:** `ReportModelBuilderParentChild*Tests.cs`,
  `ParentPostMergeCallbackTests.cs`, `AzureAdGroupSummaryRebuilderTests.cs`,
  `DefaultResourceRenderPolicyTests.cs`, `ParentChildRelationshipRegistryTests.cs`.
- **Unit, renderer:** `DefaultResourceRendererScenarioTests.cs`,
  `Providers/AzureRM/NsgMergedSecurityRuleRenderingTests.cs`, plus a new NSG inline
  renderer test class.
- **Integration (pipeline):** `MarkdownRenderer` from plan JSON to markdown, both option
  values.
- **Snapshot:** existing snapshot tests; baselines change on purpose only where listed.
- **CLI:** `CLI/HelpTextProviderTests.cs`.

## Test Coverage Matrix

AC numbering follows the order of the Success Criteria list in specification.md.

| # | Acceptance criterion | Test cases | Type |
|---|----------------------|-----------|------|
| 1 | Default: update with ➕🔄❌⏺️ children shows only ➕🔄❌ rows | TC-01, TC-02, TC-30 | Unit stage, integration |
| 2 | `--show-unchanged-values`: all rows, identical to pre-feature output | TC-03, TC-31, TC-60, TC-61 | Unit stage, integration, snapshot |
| 3 | Same for inline, separate and mixed child sources | TC-04, TC-05, TC-06, TC-32 | Unit renderer, integration |
| 4 | Applies to every pattern in `docs/features.md`; at least one non-route test | TC-33 to TC-39 | Integration |
| 5 | Relative order of remaining rows unchanged | TC-07, TC-40 | Unit stage, integration |
| 6 | Note below table with hidden count and child label | TC-10, TC-11, TC-12, TC-41 | Unit renderer, integration |
| 7 | Singular for exactly one, plural otherwise, every child table type | TC-13, TC-14, TC-15, TC-42, TC-43 | Unit renderer, registry, integration |
| 8 | No note when no row hidden | TC-16, TC-44 | Unit renderer, integration |
| 9 | No note with `--show-unchanged-values` | TC-17, TC-45 | Unit renderer, integration |
| 10 | All children unchanged, default: whole section omitted (heading, table, note) | TC-20, TC-21, TC-46 | Unit renderer, integration |
| 11 | All children unchanged, option set: full ⏺️ table | TC-22, TC-47 | Integration |
| 12 | Create and delete parents unchanged | TC-08, TC-48, TC-49 | Unit stage, integration |
| 13 | Parent summary child counts unchanged | TC-50, TC-51 | Unit model, integration |
| 14 | Mixed-sources warning shown whenever shown today, alone when all hidden | TC-23, TC-24, TC-25, TC-52 | Unit renderer, integration |
| 15 | NSG docs no longer say unchanged rules are always shown | TC-70 | Docs check (automated grep) |
| 16 | Code analysis findings of a merged child still shown when row hidden | TC-53 | Integration |
| 17 | Firewall, build definition, variable group tables unchanged | TC-62, TC-63 | Snapshot, regression |
| 18 | Help text and user docs say option includes unchanged child rows | TC-71, TC-72 | CLI unit, docs check |
| 19 | Renders correctly in Azure DevOps and GitHub PRs | TC-54 (markdown validity), UAT | Integration plus UAT (maintainer) |

Supporting design requirements from architecture.md (not separate ACs, but each guards an
AC above and is traceable to it):

| Design point | Guards AC | Test cases |
|--------------|-----------|-----------|
| `HiddenUnchangedCount` and nouns on group | 6, 7 | TC-09, TC-14, TC-15 |
| `HasChildrenWithChanges` runs on unfiltered rows | 1, 12 | TC-26 |
| Azure AD member-type counts use unfiltered rows | 13 | TC-51 |
| `IsNoOpParentSecurityRuleScenario` uses `Rows.Count + HiddenUnchangedCount` | 10 | TC-27 |
| NSG inline path filters its own rows and falls back to default renderer | 1, 2, 6, 10 | TC-80 to TC-87 |
| Parent replace and no-op parent kept for child change | 1 (Decision 5) | TC-28, TC-29 |
| Provider nouns live in provider modules, not core | 7 | TC-15, TC-88 |

## Test Cases

### Stage: DisplayFilteringStage

Unit tests build a `ReportModel`/display change with `ChildResourceGroup`s directly, so
the stage is exercised without rendering.

- **TC-01** `Filter_ShowUnchangedFalse_RemovesUnchangedRows`. Group rows ➕🔄❌⏺️, stage
  with `showUnchangedValues: false`. Expect rows ➕🔄❌ only; `HiddenUnchangedCount == 1`.
- **TC-02** `Filter_ManyUnchanged_CountsAllRemoved`. 2 changed, 8 unchanged. Expect 2
  rows, `HiddenUnchangedCount == 8`.
- **TC-03** `Filter_ShowUnchangedTrue_LeavesGroupsUntouched`. Same input with
  `showUnchangedValues: true`. Expect all rows, `HiddenUnchangedCount == 0`, groups equal
  to input.
- **TC-04** `Filter_InlineSourceRows_RemovesUnchanged`. Rows whose source is the inline
  attribute.
- **TC-05** `Filter_SeparateSourceRows_RemovesUnchanged`. Rows from separate resources
  (⏺️ from a non-changing action).
- **TC-06** `Filter_MixedSourceGroup_RemovesUnchangedFromBothSources`. One group with
  both sources; `HasMixedSources` stays true.
- **TC-07** `Filter_MixedIndicators_PreservesRelativeOrder`. Interleaved ➕⏺️🔄⏺️❌; expect
  ➕🔄❌ in original order.
- **TC-08** `Filter_CreateOrDeleteParent_DoesNotTouchGroups`. Parent action create and
  delete; groups returned unchanged, count 0, even if a ⏺️ row were injected (proves the
  skip rule, not just the absence of ⏺️).
- **TC-09** `Filter_AllRowsUnchanged_KeepsEmptyGroupWithCountAndMixedFlag`. Expect group
  retained with `Rows` empty, `HiddenUnchangedCount == n`, `HasMixedSources` preserved,
  `ChildResourceGroups.Count` unchanged.
- **TC-26** `Filter_NoOpParentWithOnlyUnchangedChildren_IsStillFilteredOutAsNoOp` and
  `Filter_NoOpParentWithOneChangedChild_IsKeptAndUnchangedRowsHidden`. Proves
  `HasChildrenWithChanges` reads the full row set before row filtering: the parent with
  one changed child is retained; the parent with only ⏺️ children is dropped exactly as
  today.
- **TC-27** `IsNoOpParentSecurityRuleScenario_TwoRowsOneHidden_UsesFullCount`. A security
  rules group with `Rows.Count == 1` and `HiddenUnchangedCount == 1` is treated as the
  two-row scenario; `Rows.Count == 1, Hidden == 0` is not. The `_No attribute changes._`
  output for the existing no-op NSG snapshot stays the same.
- **TC-28** `Filter_ReplaceParent_RemovesUnchangedChildRows` (Decision 5).
- **TC-29** `Filter_NoOpParentKeptForChildChange_RemovesUnchangedChildRows` (architecture
  open-question assumption: every action except create and delete is filtered).
- **TC-90** `Filter_DoesNotMutateInputGroups`. The input group list and its `Rows` are
  unchanged after filtering (record `with`, no in-place edit).
- **TC-91** `Filter_UnknownParentAction_FiltersLikeUpdate`.

### Renderer: DefaultResourceRenderer (core)

Direct renderer tests with a prepared model.

- **TC-10** `RenderChildResources_RowsHidden_WritesNoteDirectlyBelowTable`. Expect
  heading, table, then a blank line, one line `_N unchanged routes hidden._`, and a
  blank line; no other text between the table and the note.
- **TC-11** `RenderChildResources_Note_IsSingleItalicLine`. Matches
  `^_\d+ unchanged .+ hidden\._$` on its own line.
- **TC-12** `RenderChildResources_Note_UsesGroupNouns`. Group with custom nouns renders
  those nouns (not derived from the label).
- **TC-13** `RenderChildResources_OneHidden_UsesSingularNoun`. `_1 unchanged route hidden._`.
- **TC-14** `RenderChildResources_ManyHidden_UsesPluralNoun`. 8 hidden gives
  `_8 unchanged routes hidden._`; 2 hidden gives the plural too. (Zero is TC-16.)
- **TC-16** `RenderChildResources_NothingHidden_WritesNoNote`.
- **TC-17** `RenderChildResources_ShowUnchangedTrueModel_WritesNoNote` (group with count 0
  and ⏺️ rows renders all rows, no note).
- **TC-20** `RenderChildResources_AllRowsHiddenNotMixed_WritesNothing`. Output contains no
  child heading, no table header, no note.
- **TC-21** `RenderChildResources_AllRowsHidden_DoesNotWriteHiddenCountNote`.
- **TC-22** `RenderChildResources_AllRowsUnchangedWithShownModel_RendersFullTable`. Group
  of only ⏺️ rows and count 0 renders heading and every row, no note.
- **TC-23** `RenderChildResources_AllRowsHiddenAndMixed_WritesOnlyWarning`. Exactly the
  warning paragraph; no heading, table or note.
- **TC-24** `RenderChildResources_RowsRemainAndMixed_WritesWarningThenTableThenNote`.
  Order: heading, warning, table, note.
- **TC-25** `RenderChildResources_MixedSourcesWithNothingHidden_WarningShownLikeToday`.
  Output equal to the pre-feature output for that input.
- **TC-92** `ChildTableSection_WriteHiddenUnchangedNote_ZeroCount_WritesNothing` and
  `..._OneCount_UsesSingular`, `..._ManyCount_UsesPlural` (core helper unit tests).
- **TC-93** `ChildTableSection_WriteMixedSourcesWarning_WritesExistingText`. Text equals
  `⚠️ **Warning:** This resource has children managed both inline and as separate
  resources. This configuration will cause conflicts.` followed by a blank line.
- **TC-94** `RenderChildResources_VnetSubnetsAllHidden_OmitsSection` and the same for DNS
  records, AD members, DevOps administrators (one case per label in TC-15 table).

### Renderer: NsgMergedSecurityRulesRenderer

- **TC-95** `Render_SeparateRulesWithUnchangedRows_ShowsOnlyChangedRowsAndNote`. Uses a
  model with ➕ and ⏺️ rules; note reads `_N unchanged security rules hidden._` (singular
  `security rule` for 1).
- **TC-96** `Render_AllSeparateRulesHidden_OmitsRulesSection`.
- **TC-97** `Render_AllRulesHiddenAndMixedSources_WritesOnlyWarning`.
- **TC-98** `Render_ShowUnchangedModel_RendersAllRowsNoNote`; table separator row format
  unchanged (the two renderers' differing separators must not be unified).
- **TC-99** `Render_NoOpParentWithSeparateRuleChange_StillShowsChangedRule`. The issue 120
  regression (no-op parent, created, updated, deleted separate rule) keeps rendering.

### Renderer: NsgRenderer inline path (second filter site)

Mirror of the stage cases; the architecture risk is that the two sites drift.

- **TC-80** `Render_InlineRulesUpdateDefault_OmitsUnchangedRulesAndWritesNote`.
- **TC-81** `Render_InlineRulesUpdateWithShowUnchanged_RendersAllRulesNoNote`. Output
  identical to pre-feature output, including ordering and formatting of ⏺️ rows.
- **TC-82** `Render_InlineRulesOneUnchanged_NoteUsesSingular` (`_1 unchanged security rule
  hidden._`) and `_ManyUnchanged_NoteUsesPlural`.
- **TC-83** `Render_InlineRulesNothingUnchanged_WritesNoNote`.
- **TC-84** `Render_InlineRulesAllUnchanged_FallsBackToDefaultRenderer`. Output has the
  parent attribute changes, no security-rules heading, table or note, and is not an empty
  details block. Includes a variant where the only parent change is tags.
- **TC-85** `Render_InlineRulesCreate_UnaffectedByFilter`. Create and delete paths render
  exactly as before.
- **TC-86** `Render_InlineRulesRowOrder_PreservedAfterFilter`.
- **TC-87** `Render_NoRuleData_FallsBackToDefaultRenderer` (existing behaviour kept).
- **TC-88** `NsgInlinePath_Nouns_MatchAzureRmModuleRegistration`. The nouns the inline
  renderer uses equal the nouns registered for `Security Rules` in `AzureRMModule`
  (single source of constants).

### Registry and provider data

- **TC-15** `Registrations_EveryParentChildRelationship_DefinesSingularAndPluralNouns`.
  For every relationship in `AzureRMModule`, `AzureADModule`, `AzureDevOpsModule`, both
  nouns are non-empty. Plus exact expected values: Routes route/routes, Subnets
  subnet/subnets, Security Rules security rule/security rules, DNS Records DNS
  record/DNS records, Members member/members, Administrators
  administrator/administrators.
- **TC-100** `MergeGroupsByLabel_SameLabel_TakesNounsFromFirstGroup` and
  `MergeParentChildRelationships_CopiesNounsOntoGroup`.
- **TC-101** Architecture test: core (`MarkdownGeneration/`, `Models`) contains no
  hard-coded provider nouns (`route`, `subnet`, `security rule`); extend
  `ArchitectureBoundaryTests`.

### Integration: plan JSON to markdown

Run the real pipeline (`MarkdownRenderer` or CLI entry) twice per scenario, with and
without `ShowUnchangedValues`.

- **TC-30** Route table update with separate `azurerm_route` children: add, modify,
  remove, 1 unchanged (the specification example). Default output contains the three
  changed rows and `_1 unchanged route hidden._`, and not the unchanged route name.
- **TC-31** Same plan with the option: contains the ⏺️ row for the unchanged route and no
  note; whole output equals the pre-feature output for that plan.
- **TC-32** Route table with inline `route` attribute children (add, modify, remove, 2
  unchanged) and a mixed inline plus separate table (warning, changed rows, note counts
  unchanged rows of both sources together).
- **TC-33** `azurerm_virtual_network` with inline subnets: note names `subnets`.
- **TC-34** `azurerm_network_security_group` with separate rules, and TC-35 NSG with
  inline rules only (the "unchanged rules for completeness" case): note names
  `security rules`.
- **TC-36** `azurerm_dns_zone` with record children (use at least two record types).
- **TC-37** `azuread_group` with inline members and with separate `azuread_group_member`.
- **TC-38** `azuredevops_team` members and administrators; TC-39 `azuredevops_group`
  members.

  TC-33 to TC-39 each assert: only changed rows, correct note noun, and option on gives
  full table. This satisfies "at least one non-route pattern" many times over.
- **TC-40** Row order in rendered table equals the order of the option-on table with the
  ⏺️ rows removed (compare programmatically by deleting ⏺️ rows from the option-on
  output).
- **TC-41** Note position: the line directly after the last table row (after one blank
  line) is the note.
- **TC-42** Singular noun per pattern: one unchanged child each for routes, subnets,
  security rules, DNS records, members, administrators gives the singular noun from the
  TC-15 table. TC-43 plural with 2 unchanged for the same six.
- **TC-44** Nothing hidden (all children changed) gives no note. TC-45 no note with the
  option on, for every pattern above.
- **TC-46** Route table where only tags change and all routes are unchanged: output has
  the attribute table, no `Routes` heading, no table, no note. TC-47 same plan with the
  option: full ⏺️ table.
- **TC-48** Create parent with children renders identical to the current snapshot (child
  table has no Change column). TC-49 delete parent likewise.
- **TC-50** Parent summary line child counts (for example `➕ 1 routes | 🔄 1 routes | ❌
  1 routes`) are identical with and without the option and identical to the pre-feature
  text. TC-51 Azure AD group summary member-type counts include unchanged members in both
  modes (counts equal to the pre-feature summary).
- **TC-52** All children unchanged plus routes managed both inline and separately:
  default output has the warning and no heading, table or note; option output has the
  warning, heading and full table.
- **TC-53** A separate child with a code analysis finding whose row is hidden: the finding
  still appears in the parent section. Use a fixture with a security finding on an
  unchanged merged child.
- **TC-54** Rendered markdown for each TC-30 to TC-39 default scenario passes the
  existing markdown lint and validation fixtures (`MarkdownLintFixture`,
  `MarkdownValidationTests`) for AzureDevOps, GitHub and Bitbucket render targets, and in
  each details display mode. This is the automated part of "renders correctly"; the PR UI
  check is UAT.
- **TC-55** Render target and details-mode matrix: one scenario (TC-30) rendered for
  every render target and every details display mode contains the same child rows and
  note.
- **TC-56** `MarkdownRenderer.RenderResourceChange` (single resource path, flag hard-coded
  to false) renders hidden rows and the note consistently with the model; it must not
  throw and must not read the context flag.

### Snapshots and regressions

- **TC-60** Run every existing snapshot test with `ShowUnchangedValues` on where a
  snapshot test already uses it; baselines must not change.
- **TC-61** For each fixture used in TC-30 to TC-39, assert option-on output equals a
  checked-in pre-feature baseline (golden file generated from the pre-change build on
  `main`, committed before the filtering is implemented).
- **TC-62** Snapshots expected to change on purpose, because they contain ⏺️ child rows
  today: `azuread-group-members.md`, `parent-child-resource-grouping-uat.md`,
  `comprehensive-demo.md`, `comprehensive-demo-full.md`. For each, the diff must be
  limited to removed ⏺️ child rows, added hidden-count notes, and removed child sections.
  Developer states the reviewed diff in the work protocol. `firewall-rules.md` (contains ⏺️
  from firewall rule collection tables) must **not** change.
- **TC-63** Build definition and variable group renderer tests
  (`BuildDefinitionViewModelFactoryTests`, variable group tests) and firewall rule
  collection template tests pass unchanged and with the option off still contain their ⏺️
  rows.
- **TC-64** Plans without any parent-child resource render byte-identical output to the
  pre-feature output (run over the existing non-parent-child snapshot fixtures).

### CLI and docs

- **TC-71** `HelpTextProvider_ShowUnchangedValuesOption_MentionsChildRows`. The help
  line for `--show-unchanged-values` mentions attribute values and unchanged child rows
  (assert on the words `child` and `unchanged`). Existing help snapshot or help tests
  updated accordingly.
- **TC-72** Docs check test or script step: `README.md` and `docs/features.md` option
  tables for `--show-unchanged-values` mention child rows (string assertion on the file
  contents, run as a TUnit test in the existing docs-consistency tests if present,
  otherwise as part of the Developer's pre-PR checklist; recorded in the work protocol).
- **TC-70** `docs/features.md` NSG section no longer contains the phrases that say
  unchanged security rules are always shown or shown for completeness; it states the
  rule hides them by default. (Automated string assertion as for TC-72.)

## Edge Cases and Error Conditions

| Case | Expected | Test |
|------|----------|------|
| Exactly one unchanged child | Singular noun | TC-13, TC-42, TC-82 |
| Zero unchanged children | No note | TC-16, TC-44, TC-83 |
| All children unchanged, default | No heading, table, note | TC-20, TC-46, TC-84 |
| All unchanged, mixed sources | Warning only | TC-23, TC-52 |
| All unchanged, option set | Full ⏺️ table | TC-22, TC-47 |
| Exactly one changed row remains | Table with one row plus note | TC-10 (variant with one remaining row) |
| Single child group (1 row, ⏺️), nothing else | Section omitted | TC-20 |
| Parent replace | Hidden like update | TC-28 |
| No-op parent kept for child change | Hidden, same as update | TC-29 |
| No-op parent, all children unchanged | Parent dropped as today | TC-26 |
| Create, delete parents | Unchanged output | TC-08, TC-48, TC-49 |
| Child group with unknown/`known after apply` members | No crash; unknown rows are not ⏺️ and stay visible | TC-102 `Filter_KnownAfterApplyMembers_AreNotTreatedAsUnchanged` using `azuread-group-members-known-after-apply-plan.json` and `azurerm-vnet-known-after-apply-plan.json` |
| Two groups with same label merged | Counts and nouns correct after merge | TC-100 |
| Parent with several child groups (for example DevOps team members and administrators) | Each group filtered and noted independently | TC-38 |
| Hidden count large (for example 100) | Correct integer, no formatting error | TC-103 `WriteHiddenUnchangedNote_LargeCount_WritesExactNumber` |
| Child names with markdown special characters | Table rows unchanged by the feature; note contains no user data | TC-54 |
| `--ignore-azure-id-case-changes` set | Rows ⏺️ because of case-only change are hidden like other ⏺️ rows; unchanged otherwise | TC-104 |
| `_No attribute changes._` rule | Same output as today for no-op NSG and Azure AD no-op parents | TC-27, TC-105 `Render_AzureAdNoOpParentAllMembersHidden_SuppressionRuleUnchanged` |
| `--show-unchanged-values` combined with other options (`--hide-unchanged-outputs`, `--large-value-format`) | Child behaviour depends only on `--show-unchanged-values` | TC-106 |

## Specification observations

- AC 19 ("renders correctly in Azure DevOps and GitHub pull requests") cannot be
  automated beyond markdown validity (TC-54). The platform check is UAT.
- The specification says the behaviour applies to "update and replace" parents, while the
  architecture filters every parent action except create and delete (no-op parents kept
  for a changed child). This is not a defect in testability, but the specification wording
  should say "any parent that renders a Change column". Tests TC-29 cover the architecture's
  assumption; if the Maintainer disagrees, TC-29 flips.
- The criterion "No hidden-count note ... when the section is omitted" is only implied by
  the AC list (AC 10 covers it). Covered by TC-21.
