using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Renderers;

/// <summary>
/// Writes the inline security-rule table rows of an NSG and drops the unchanged ones.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
internal static class NsgInlineRuleRows
{
    /// <summary>
    /// Removes rules marked unchanged.
    /// </summary>
    /// <param name="ruleChanges">The update rows, in plan order.</param>
    /// <returns>The remaining rows in their original order.</returns>
    internal static List<SecurityRuleChangeRowViewModel> WithoutUnchanged(IReadOnlyList<SecurityRuleChangeRowViewModel> ruleChanges)
    {
        return ruleChanges.Where(rule => rule.Change != ActionIcons.Unchanged).ToList();
    }

    /// <summary>
    /// Writes the table rows for the action being rendered.
    /// </summary>
    /// <param name="writer">The markdown writer.</param>
    /// <param name="viewModel">The NSG view model.</param>
    /// <param name="isCreate">Whether the NSG is created, which lists the after rules.</param>
    /// <param name="isDelete">Whether the NSG is deleted, which lists the before rules.</param>
    /// <param name="ruleChanges">The update rows to write for any other action.</param>
    internal static void Write(
        MarkdownWriter writer,
        NetworkSecurityGroupViewModel viewModel,
        bool isCreate,
        bool isDelete,
        IReadOnlyList<SecurityRuleChangeRowViewModel> ruleChanges)
    {
        if (isCreate)
        {
            foreach (var rule in viewModel.AfterRules)
            {
                writer.TableRow([ActionIcons.Add, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }

            return;
        }

        if (isDelete)
        {
            foreach (var rule in viewModel.BeforeRules)
            {
                writer.TableRow([ActionIcons.Delete, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
            }

            return;
        }

        foreach (var rule in ruleChanges)
        {
            writer.TableRow([rule.Change, rule.Name, rule.Priority, rule.Direction, rule.Access, rule.Protocol, rule.SourceAddresses, rule.SourcePorts, rule.DestinationAddresses, rule.DestinationPorts, rule.Description]);
        }
    }
}
