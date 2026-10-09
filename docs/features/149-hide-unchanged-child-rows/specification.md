# Feature: Hide Unchanged Rows in Inline Child Tables

## Overview

Parent-child resource grouping renders the children of a parent resource, such as the
routes of an `azurerm_route_table`, as a table inside the parent's section. On an update
or replace, that table lists every child, including children that do not change (⏺️).
A route table with 10 routes where 2 change produces a table of 10 rows, and the reviewer
has to scan past 8 unchanged rows to find the 2 that matter.

Attribute tables already solve this: unchanged attributes are hidden by default and
`--show-unchanged-values` brings them back. Child tables do not follow that option today,
so the same report hides unchanged attributes but shows unchanged children. This feature
makes inline child tables follow the same rule.

Source request: GitHub issue #679.

## User Goals

- See only the children that are added, removed or modified, so the review effort scales
  with the size of the change, not the size of the parent.
- Have one consistent meaning for `--show-unchanged-values`: without it, unchanged things
  are hidden everywhere in a resource's details; with it, they are shown everywhere.
- Keep the ability to see the full child set when it is needed, for example for an audit
  or to confirm the complete route set.

## Scope

### In Scope

- Every child table rendered by parent-child resource grouping, for every supported
  pattern listed under "Parent-Child Resource Grouping" in `docs/features.md`. This covers
  `azurerm_route_table` routes, and equally `azurerm_virtual_network` subnets,
  `azurerm_network_security_group` security rules, DNS zone records, and the Azure AD and
  Azure DevOps group, team member and administrator tables. The behaviour is the same for
  all of them; it is not specific to route tables.
- Children from both sources: inline children (from an attribute on the parent, such as
  `route`) and separate child resources (such as `azurerm_route`), including a table that
  mixes both.
- Default behaviour (no `--show-unchanged-values`): child rows whose change indicator is
  unchanged (⏺️) are omitted. Added (➕), removed (❌) and modified (🔄) rows are shown
  exactly as today, in the same order relative to each other.
- With `--show-unchanged-values`: child tables are rendered exactly as today, including
  unchanged rows.
- When at least one row was hidden and at least one row remains, a one-line italic note
  directly below the table states how many unchanged children were hidden, for example
  `_8 unchanged routes hidden._`. The note names the children with the same child label
  the table already uses (its heading, for example "routes", "subnets", "security rules",
  "members"), and uses the singular form for exactly one hidden child
  (`_1 unchanged route hidden._`). The note is not shown when no row was hidden, and never
  with `--show-unchanged-values`.
- When every child of a table is unchanged and `--show-unchanged-values` is not set, the
  whole child-table section is omitted: no heading, no table and no note. In particular, no
  hidden-count note is shown for it. The one exception is the warning for a parent whose
  children are managed both inline and as separate resources: it is a configuration
  warning, not a change, so it is still shown, on its own, without the heading, table or
  note.
- The behaviour applies to the parent actions that render a Change column (update and
  replace). Create and delete child tables are unaffected, because they contain no
  unchanged rows.
- The behaviour applies to every render target (Azure DevOps, GitHub, Bitbucket) and every
  details display mode.
- The help text for `--show-unchanged-values`, which today speaks only of attribute values,
  describes that the option also includes unchanged child rows.

### Out of Scope

- Changing the parent resource's summary line. Its child counts (for example
  `➕ 1 routes | 🔄 1 routes | ❌ 1 routes`) already count only changed children and stay
  as they are.
- Adding a hidden-count note to attribute tables. The count note applies to child tables
  only.
- Changing how a child's change indicator is determined, how children are matched between
  before and after, or which columns a child table has.
- Provider-specific semantic tables that are not part of parent-child resource grouping,
  such as the firewall network and application rule collection templates and the Azure
  DevOps build definition and variable group tables. They keep showing unchanged rows.
- Static code analysis findings mapped to a child. Findings shown in the parent section for
  a merged child remain shown even when that child's row is hidden.
- Adding a new command-line option. The existing `--show-unchanged-values` controls this
  behaviour.
- Interaction with `--ignore-azure-id-case-changes` beyond what it does today.

## User Experience

### Default: unchanged routes hidden

A route table update in which one route is added, one modified, one removed and one
unchanged:

```markdown
#### Routes

| Change | Name | Address Prefix | Next Hop Type | Next Hop Address | Terraform Resource |
| -------- | -------- | -------- | -------- | -------- | -------------------- |
| ➕ | `🆔 to-firewall` | `🌐 0.0.0.0/0` | `VirtualAppliance` | `🌐 10.200.1.10` | `azurerm_route.separate_route1` |
| 🔄 | `🆔 to-hub` | ... | ... | ... | `azurerm_route.separate_route2` |
| ❌ | `🆔 old-route` | `🌐 172.16.0.0/12` | `None` | - | `azurerm_route.separate_route3` |

_1 unchanged route hidden._
```

The ⏺️ row for `unchanged-route` that appears today is omitted, and the note below the
table says so. With 8 unchanged routes the note reads `_8 unchanged routes hidden._`. The
exact wording is a design detail; the contract is one italic line giving the number of
hidden children, named with the table's child label and correct singular or plural.

### With `--show-unchanged-values`

```bash
tfplan2md --show-unchanged-values plan.json
```

The same table additionally contains:

```markdown
| ⏺️ | `🆔 unchanged-route` | `🌐 192.168.0.0/16` | `VnetLocal` | - | `azurerm_route.separate_route4` |
```

and no hidden-count note is shown. This is the output tfplan2md produces today.

### Every child unchanged

A route table whose tags change but whose routes do not renders its attribute table and
nothing else: there is no `#### Routes` heading, no table and no hidden-count note. With
`--show-unchanged-values`, the full table of ⏺️ rows is rendered as today.

If that route table has routes managed both inline and as separate `azurerm_route`
resources, the mixed-children warning is still rendered, without the heading, table or
note:

```markdown
⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.
```

### Errors

No new error conditions. Plans without parent-child resources produce identical output.

## Success Criteria

- [ ] Without `--show-unchanged-values`, an update to a parent with added, modified,
      removed and unchanged children renders a child table containing only the ➕, 🔄 and
      ❌ rows.
- [ ] With `--show-unchanged-values`, the same plan renders the child table with all rows,
      including ⏺️ rows, identical to the output before this feature.
- [ ] The filtering applies identically to inline children, separate child resources, and
      tables mixing both.
- [ ] The filtering applies to every parent-child pattern listed in `docs/features.md`, not
      only `azurerm_route_table`; at least one non-route pattern is covered by a test.
- [ ] The relative order of the remaining rows is unchanged by the filtering.
- [ ] Without `--show-unchanged-values`, when some rows are hidden and some remain, one
      italic line below the table states the number of hidden children, named with the
      table's child label.
- [ ] The hidden-count note uses the singular form for exactly one hidden child and the
      plural form otherwise, for every child table type.
- [ ] No hidden-count note is shown when no child row was hidden.
- [ ] No hidden-count note is shown with `--show-unchanged-values`.
- [ ] Without `--show-unchanged-values`, when every child is unchanged, the whole child-table
      section is omitted: no heading, no table and no hidden-count note, except for the
      mixed-children warning where one applies.
- [ ] With `--show-unchanged-values`, when every child is unchanged, the full table of ⏺️
      rows is rendered.
- [ ] Create and delete parents render their child tables exactly as before.
- [ ] The parent summary line's child counts are unchanged.
- [ ] The mixed inline-and-separate warning is shown whenever it is shown today, including
      when every child is unchanged; in that case it renders on its own, without the child
      heading, table or hidden-count note.
- [ ] The NSG documentation in `docs/features.md` no longer says that unchanged security
      rules are always shown.
- [ ] Code analysis findings for a merged child are still shown in the parent section when
      that child's row is hidden.
- [ ] Firewall rule collection, build definition and variable group tables render exactly
      as before.
- [ ] The `--show-unchanged-values` help text and its user documentation state that the
      option also includes unchanged child rows.
- [ ] Output renders correctly in Azure DevOps and GitHub pull requests.

## Decisions

The Maintainer answered the questions raised with the first draft of this specification:

1. When every child is unchanged, the whole child-table section is omitted, with no heading,
   no table and no note.
2. A hidden-count note is shown below a child table whenever rows were hidden, using the
   table's child label with correct singular or plural. It is never shown when nothing was
   hidden, when the section is omitted, or with `--show-unchanged-values`.
3. NSG security rules follow the same rule as every other child table; the NSG
   documentation is updated to match.
4. Firewall rule collection, build definition and variable group tables are out of scope.
5. On a parent replace, unchanged children are hidden too, as attribute tables do.
6. The mixed inline-and-separate warning is still shown when every child is unchanged. Only
   the heading, table and hidden-count note are omitted; the warning renders on its own.

## Open Questions and Assumptions

None open.
