# UAT Test Plan: Hide Unchanged Rows in Inline Child Tables

**This plan is for the Maintainer to run.** The change is contributed from a fork, and
the Azure DevOps half of UAT needs the maintainer's `dev.azure.com/oocx` organization.
The contributor cannot run it. The contributor still delivers `uat-plan.json` and the
rendered `uat-plan.md` so the Maintainer can post them.

## Goal

Confirm that, in real GitHub and Azure DevOps pull request comments, inline child tables
show only added, modified and removed rows by default, show the one-line hidden-count
note, omit the whole section when every child is unchanged, and render the full table
with `--show-unchanged-values`. The change touches markdown output, so UAT applies.

## Required contents of `uat-plan.json`

Path: `docs/features/149-hide-unchanged-child-rows/uat-plan.json` (a real Terraform plan
JSON; rendered to `uat-plan.md` through the normal tfplan2md pipeline). Keep it small so
reviewers can count rows in a PR comment. Use obviously fictitious names, no real
tenants, people or addresses.

Scenarios, each a separate parent resource with a recognisable name:

| Scenario id | Parent | Children | Purpose |
|-------------|--------|----------|---------|
| `routes-separate-mixed` | `azurerm_route_table` update, tags unchanged | Separate `azurerm_route`: 1 create, 1 update, 1 delete, 3 unchanged (no-op) | Specification example; plural note `_3 unchanged routes hidden._` |
| `routes-inline-one-unchanged` | `azurerm_route_table` update | Inline `route` attribute: 1 changed, exactly 1 unchanged | Singular note `_1 unchanged route hidden._` |
| `routes-all-unchanged` | `azurerm_route_table` update, only `tags` change | Inline routes all unchanged | Whole child section omitted |
| `routes-all-unchanged-mixed-sources` | `azurerm_route_table` update | Routes both inline and as separate `azurerm_route`, all unchanged | Warning alone, no heading, table or note |
| `routes-nothing-hidden` | `azurerm_route_table` update | Inline routes, all changed | No note |
| `vnet-subnets` | `azurerm_virtual_network` update | Inline subnets: 1 changed, 2 unchanged | Different pattern, `subnets` label |
| `nsg-inline-rules` | `azurerm_network_security_group` update | Inline security rules: 1 changed, 2 unchanged | NSG inline path (`NsgRenderer`) |
| `nsg-inline-all-unchanged` | `azurerm_network_security_group` update, tags change | Inline rules all unchanged | NSG inline fallback: attribute table only, no empty details block |
| `nsg-separate-rules` | `azurerm_network_security_group` update | Separate `azurerm_network_security_rule`: 1 create, 1 unchanged | NSG merged path |
| `dns-records` | `azurerm_dns_zone` update | At least two record types, 1 changed, 1 unchanged | DNS Records label (`DNS records` noun) |
| `azuread-group-members` | `azuread_group` update | Inline members: 1 added, 1 unchanged, plus one separate `azuread_group_member` | Members label, member-type summary counts |
| `azuredevops-team-administrators` | `azuredevops_team` update | Administrators: 1 changed, 1 unchanged | Administrators label |
| `route-table-create` | `azurerm_route_table` create | 2 inline routes | Create table unaffected (no Change column) |
| `route-table-delete` | `azurerm_route_table` delete | 2 inline routes | Delete table unaffected |
| `route-table-replace` | `azurerm_route_table` replace | 1 changed, 2 unchanged routes | Replace hides unchanged |
| `unchanged-with-code-finding` | `azurerm_network_security_group` with a separate unchanged rule that triggers a code analysis finding | Finding shown in parent section although the row is hidden | Only if the UAT run enables code analysis; otherwise omit and note that TC-53 covers it |

Edge cases the plan must contain:

- A child table where exactly one row remains after hiding.
- Child names containing markdown-sensitive characters (`|`, backticks, `<`, `&`) on
  one changed row, to confirm the table still renders.
- A plan with a resource that has no children, to confirm nothing changes.

The plan must contain at least one scenario for every child label: Routes, Subnets,
Security Rules, DNS Records, Members, Administrators.

## Rendered artifacts

Generate two renderings from the same plan:

1. Default: `tfplan2md docs/features/149-hide-unchanged-child-rows/uat-plan.json --output docs/features/149-hide-unchanged-child-rows/uat-plan.md`
2. With the option, saved next to it as `uat-plan-show-unchanged.md`, using
   `--show-unchanged-values`.

The Developer builds both; the Maintainer posts them. The comprehensive regression
artifacts (`artifacts/comprehensive-demo.md` and `artifacts/comprehensive-demo-simple-diff.md`)
must be regenerated with the `generate-demo-artifacts` skill and posted as well, since
the comprehensive demo snapshot changes on purpose.

## Test steps (Maintainer)

1. Create one GitHub UAT PR and one Azure DevOps UAT PR (run-uat skill), each with the
   default rendering in a PR comment. Azure DevOps needs the oocx organization, so only
   the Maintainer can do this.
2. For each scenario in the default rendering, check the expected result in the table
   below, in both platforms.
3. Post `uat-plan-show-unchanged.md` as a second comment on each PR and confirm that every
   scenario shows its full set of rows including ⏺️ rows, and no hidden-count note.
4. Post the regenerated comprehensive demo and confirm nothing unrelated changed.

## What the Maintainer should look for

| Scenario | Default rendering | With `--show-unchanged-values` |
|----------|-------------------|--------------------------------|
| `routes-separate-mixed` | 3 rows (➕🔄❌), then `_3 unchanged routes hidden._` in italics directly below | 6 rows, no note |
| `routes-inline-one-unchanged` | Changed row only, `_1 unchanged route hidden._` (singular) | All rows, no note |
| `routes-all-unchanged` | Attribute table (tags) only; no `Routes` heading, table or note | Full ⏺️ table |
| `routes-all-unchanged-mixed-sources` | Only the warning line; no heading, table or note | Warning, heading, full table |
| `routes-nothing-hidden` | Table, no note | Same |
| `vnet-subnets`, `dns-records`, `azuread-group-members`, `azuredevops-team-administrators` | Changed rows only, note with the right noun in correct singular or plural | All rows |
| `nsg-inline-rules` | Changed rule only, note `_2 unchanged security rules hidden._` | All rules in the existing format |
| `nsg-inline-all-unchanged` | Normal attribute table, no empty `<details>` block, no rules section | Full rules table |
| `nsg-separate-rules` | Created rule only, `_1 unchanged security rule hidden._` | Both rows |
| create, delete scenarios | Unchanged from before; no Change column | Same |
| replace | Unchanged rows hidden | All rows |

Across all scenarios, look at:

- The note is one italic line and is not rendered as a code block or merged into the table
  (a missing blank line after the table would break Azure DevOps rendering).
- Table rows still align and no stray empty table or heading remains for hidden sections.
- The parent summary line counts (`➕ 1 routes | 🔄 1 routes | ❌ 1 routes`) match the
  visible changed rows and are unchanged from before the feature.
- Azure AD group summary member counts still include the unchanged members.
- Emoji, icons and spacing in the note render the same on both platforms.
- The same output renders in both `<details>` and non-details modes if the Maintainer
  uses another display mode.

Pass criterion: every row in the expectation table holds on both platforms and no
unrelated section of the comprehensive demo has changed.
