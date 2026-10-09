# Architecture: Hide Unchanged Rows in Inline Child Tables

## Decision

Filter unchanged (⏺️) child rows in the report model, inside `DisplayFilteringStage`,
after every consumer of the full row set has run. Record the number of hidden rows on
`ChildResourceGroup` and keep the group, even when no rows remain. Renderers stop
deciding what is visible: each one renders the rows it gets, then a hidden-count note
or, when no rows remain, only the mixed-sources warning. A shared core helper writes
that note and warning. The one renderer that builds its child rows at render time, the
AzureRM NSG inline-rules path, filters its own view-model rows in `Providers/AzureRM/`.

This choice is **not contested**. Unchanged attribute rows are already filtered while
the model is built (`ResourceChangeStage`, `if (!_showUnchangedValues && valuesEqual)`),
and no renderer reads `IRenderContext.ShowUnchangedValues` today. Filtering child rows
in the same place keeps that pattern. The render-time alternative would need the same
filter repeated in three renderers, and a future child-table renderer that left it out
would bring back the inconsistency this feature removes. No global ADR is needed:
filtering in the model is the existing convention, and this design applies it without
changing it.

## Existing implementation

- `ReportModelBuilder.MergeParentChildRelationships` builds one `ChildResourceGroup`
  per child label. Each group holds rows of every indicator, including ⏺️. Inline
  children are ⏺️ when the before and after JSON are equal. Separate children are ⏺️
  when their action is not create, update, replace or delete.
  It then merges groups that share a label, writes the parent summary child counts
  (which already skip ⏺️), and runs provider post-merge callbacks.
- The Azure AD post-merge callback `AzureAdGroupSummaryRebuilder` counts members by
  principal type over **all** `Members` rows, unchanged rows included. That count is
  part of the summary line, which the specification keeps unchanged.
- `DisplayFilteringStage.HasChildrenWithChanges` keeps a no-op parent when any child
  row is not ⏺️. It must see the full row set.
- Three code paths render child tables:
  1. `DefaultResourceRenderer.RenderChildResources` (core) renders virtual network,
     route table, DNS zone, Azure AD group and Azure DevOps group/team children.
  2. `NsgMergedSecurityRulesRenderer` (AzureRM) renders NSG rules when at least one
     separate `azurerm_network_security_rule` was merged in.
  3. `NsgRenderer` (AzureRM), NSG with inline rules only. It ignores the child group
     and renders `NetworkSecurityGroupViewModelFactory.RuleChanges`, which contains ⏺️
     rows. This path produces the "unchanged rules shown for completeness" behaviour
     in `docs/features.md`.
- `MarkdownRenderer.RenderResourceChange`, which renders a single resource, builds its
  `RenderContext` with `showUnchangedValues: false` hard-coded. That works only because
  renderers do not read the flag.
- `DefaultResourceRenderPolicy.IsNoOpParentSecurityRuleScenario` checks
  `Rows.Count == 2` on the security-rules group to work around an old snapshot. That
  check depends on the number of rows.

## Options considered

| Option | Advantages | Costs |
| --- | --- | --- |
| **A. Filter in the model, in `DisplayFilteringStage` (selected)** | Same place and pattern as unchanged attribute filtering. One filter covers every parent-child pattern and both group-based renderers, and future renderers get it without extra work. The stage can be unit-tested without rendering. Renderers stay independent of the flag, so the single-resource render path still works. | The stage must run after every consumer of the full row set (summary counts, the Azure AD rebuilder, `HasChildrenWithChanges`); later readers of `Rows` see filtered data. The `Rows.Count == 2` check must use the full count. The NSG inline path still filters at render time (see below). |
| B. Filter at render time through a shared core helper | `Rows` is the full set everywhere, so stage order does not matter. | Three renderers must call the helper and each must read `ShowUnchangedValues`, which today none does. A renderer that leaves out the call is silently inconsistent, which is the bug this feature fixes. `RenderResourceChange` hard-codes the flag to `false`, so its output would depend on the call site instead of the model. |
| C. Do not create ⏺️ rows during merging | Least code. | Breaks the Azure AD member-type counts, which need unchanged members, and makes `--show-unchanged-values` impossible without a second merge. Rejected. |

Under both A and B the NSG inline path has to filter its view-model rows itself,
because those rows are built at render time from `ResourceChange`, not from the
model. So this path does not separate A from B. Option A leaves it as the only render-time
case. Option B adds two more.

Routing NSG inline rules through `NsgMergedSecurityRulesRenderer`, so that the model filter
would cover them too, was considered and rejected. The view model orders and formats
rows differently from the row extractor, so `--show-unchanged-values` output would
change, against the specification.

## Design

### Model

`ChildResourceGroup` gains:

- `HiddenUnchangedCount` (`int`, default 0), the number of ⏺️ rows removed.
- `ItemNameSingular` and `ItemNamePlural` (`string`), the child noun used in the note,
  for example `route` and `routes`.

`ParentChildRelationship` gains the same two noun properties as `required` members,
so every provider registration states its own noun. The merge step copies them onto
the group, and `MergeGroupsByLabel` takes them from the first group, as it already does
for `Columns`. The nouns are provider data and live in the provider module
registrations (`AzureRMModule`, `AzureADModule`, `AzureDevOpsModule`). That keeps
provider knowledge out of core. Do not derive them from `Label` by lower-casing and
removing a trailing "s". `DNS Records` would become `dns records`, and labels such as
"Policies" would break. Expected values:

| Label | Singular | Plural |
| --- | --- | --- |
| Routes | route | routes |
| Subnets | subnet | subnets |
| Security Rules | security rule | security rules |
| DNS Records | DNS record | DNS records |
| Members | member | members |
| Administrators | administrator | administrators |

### Filtering (`DisplayFilteringStage`)

- Constructor takes `showUnchangedValues` in addition to `ignoreAzureIdCaseChanges`.
  `ReportModelBuilder.CreateDisplayFilteringStage` passes `_showUnchangedValues`.
- After the resource-level filters (which still use the full row set through
  `HasChildrenWithChanges`), and only when `showUnchangedValues` is false, replace
  each display change's `ChildResourceGroups` with groups whose `Rows` exclude
  `ActionIcons.Unchanged` and whose `HiddenUnchangedCount` holds the number removed.
  Use `with` on the record. Do not mutate the lists in place.
- Skip parents whose action is `create` or `delete`. Their tables have no ⏺️ rows
  today, but skipping them guarantees the "render exactly as before" criterion. Filter
  every other parent action: update, replace, no-op parents kept because a child
  changed, and unknown. See open question below.
- Keep groups that end up with zero rows. Keeping them leaves
  `ChildResourceGroups.Count` unchanged, so the `_No attribute changes._` rule and the
  Azure AD `suppressNoAttributeChangesForNoOpParents` rule behave as before. It also
  carries `HasMixedSources` to the renderer.
- Remaining rows keep their relative order (a `Where` over the existing list).

### Rendering

A new core helper in `MarkdownGeneration/Rendering/`, for example
`ChildTableSection`, owns the text that all three paths share:

- `WriteMixedSourcesWarning(writer)` writes the existing warning paragraph. The
  default and merged-NSG renderers both have a copy of this text today. Moving it here
  removes the duplicate.
- `WriteHiddenUnchangedNote(writer, count, singular, plural)` writes nothing for
  `count == 0`. Otherwise it writes `_{count} unchanged {noun} hidden._` and a blank
  line, with the singular noun for `count == 1`.

Each renderer decides the layout of a group:

- **Rows remain:** heading, warning (if mixed), table, note (if any were hidden). The
  note goes directly below the table, as the specification shows.
- **No rows remain** (all hidden): no heading, table or note. If `HasMixedSources`,
  only the warning.

`DefaultResourceRenderer.RenderChildResources` and
`NsgMergedSecurityRulesRenderer.Render` apply this to the group they receive. Keep
each renderer's table-writing code as it is. The two use different separator rows today,
and unifying them would change snapshots.

**NSG inline path** (`NsgRenderer`, `Providers/AzureRM/`). On the update path (neither
create nor delete), when `context.ShowUnchangedValues` is false, drop
`RuleChanges` entries whose `Change` is `ActionIcons.Unchanged` and count them. Write
the note with the AzureRM security-rule nouns, using the same constants as the
`AzureRMModule` registration. If no rows remain, call `base.Render`. The default
renderer then shows the parent's attribute changes and omits the (also fully hidden)
security-rules group. Without this fallback the section would be an empty details
block, because the NSG view-model path renders no attribute table. The renderer
already falls back to `base.Render` when there is no rule data.

### Dependent fixes

- `DefaultResourceRenderPolicy.IsNoOpParentSecurityRuleScenario` must compare
  `Rows.Count + HiddenUnchangedCount` with 2, so the `_No attribute changes._`
  behaviour does not change.
- `HelpTextProvider` (`--show-unchanged-values`): change the text to say the option
  includes unchanged attribute values and unchanged child rows. The README and
  `docs/features.md` option tables and NSG sections are for the Technical Writer.

### Unaffected by design

- Parent summary line and child counts: computed during merging, before filtering.
- Azure AD member-type summary counts: the post-merge callback runs before filtering.
- Code analysis findings of merged children: `MoveFindingsToParent` copies them to
  the parent during merging, independently of rows.
- Firewall rule collection, build definition and variable group renderers: not
  parent-child groups. Nothing here touches them.
- Render targets and details display modes: the note is plain Markdown italics.

## Testing focus (for the Quality Engineer)

- `DisplayFilteringStage` unit tests: filtering on/off, create/delete untouched, order
  preserved, count recorded, zero-row group retained with `HasMixedSources`, and
  `HasChildrenWithChanges` still evaluated on unfiltered rows.
- Renderer tests for each of the three paths: note singular/plural, no note when nothing
  is hidden, warning-only output when all rows are hidden and sources are mixed, NSG
  inline fallback to the default renderer when all rules are unchanged.
- At least one non-route pattern end to end, as the specification requires.
- Snapshots with `--show-unchanged-values` must not change. Default-mode snapshots
  containing ⏺️ child rows change on purpose.

## Risks

- **Stage-order coupling.** Code that reads `ChildResourceGroups[*].Rows` after
  `DisplayFilteringStage` sees filtered rows. Document this on `ChildResourceGroup.Rows`
  and on the stage. A future consumer that needs the full set must run earlier, or
  use `HiddenUnchangedCount`.
- **Two filter sites.** The NSG inline path filters separately from the model. Its
  tests must cover the same cases as the stage, so the two cannot drift.

## Open question

- The specification names update and replace parents. A no-op parent kept because a
  separate child changed (for example a route table whose `azurerm_route` children
  change) also renders a Change column and can contain ⏺️ rows, which the issue
  example appears to show. **Assumed:** filter every parent action except create and
  delete, so that no-op parents behave the same way. Create and delete tables have no ⏺️
  rows, so for them the result is the same either way.
