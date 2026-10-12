using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Filters merged resource changes for display by removing no-op resources
/// and Azure ID case-only changes (when enabled), hides unchanged child rows
/// unless unchanged values are shown, then normalizes module addresses.
/// </summary>
/// <remarks>
/// Filtering occurs after parent-child merging to ensure visual grouping
/// does not affect which resources are displayed.
/// Child rows are filtered last, so everything that needs the full row set (resource-level
/// filters here, summary counts and post-merge callbacks earlier) has already run. Code that runs
/// after this stage sees only visible rows; <see cref="ChildResourceGroup.HiddenUnchangedCount"/> keeps the count.
/// Related features:
/// - docs/features/068-parent-child-resource-grouping/specification.md
/// - docs/features/103-azure-id-case-insensitive-filter/specification.md
/// - docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </remarks>
/// <param name="ignoreAzureIdCaseChanges">Whether resources whose only changes are Azure ID casing are dropped.</param>
/// <param name="showUnchangedValues">Whether unchanged child rows stay in the child tables.</param>
internal class DisplayFilteringStage(bool ignoreAzureIdCaseChanges, bool showUnchangedValues) : IDisplayFilteringStage
{
    private readonly bool _ignoreAzureIdCaseChanges = ignoreAzureIdCaseChanges;
    private readonly bool _showUnchangedValues = showUnchangedValues;

    /// <inheritdoc />
    public DisplayFilteringResult Build(IReadOnlyList<ResourceChangeModel> mergedChanges)
    {
        const string noOpAction = TerraformActions.NoOp;
        const string updateAction = TerraformActions.Update;
        const string unknownAction = TerraformActions.Unknown;

        // Make a mutable working copy for filtering
        var workingChanges = mergedChanges.ToList();

        // Filter out no-op resources from the rendered changes list.
        // No-op resources have no meaningful changes to display.
        // Exception: Preserve no-op parents that have children with actual changes (not just no-op children)
        var afterNoOpFilter = workingChanges
            .Where(c => c.Action != noOpAction || c.CodeAnalysisFindings.Count > 0 || c.ImportId is not null || c.MovedFromAddress is not null || HasChildrenWithChanges(c))
            .ToList();

        // Also filter out update/unknown resources where all attribute changes were suppressed
        // by --ignore-azure-id-case-changes and there is nothing else meaningful to display.
        // Only applies when casing filter is active; when disabled no additional suppression occurs.
        // Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md
        var displayChanges = afterNoOpFilter
            .Where(c => !_ignoreAzureIdCaseChanges
                || c.Action is not (updateAction or unknownAction)
                || c.AttributeChanges.Count > 0
                || c.CodeAnalysisFindings.Count > 0
                || c.ImportId is not null
                || c.MovedFromAddress is not null
                || c.HasWholeResourceUnknownAfterApply
                || HasChildrenWithChanges(c))
            .ToList();

        var filteredResourceCount = afterNoOpFilter.Count - displayChanges.Count;

        if (!_showUnchangedValues)
        {
            HideUnchangedChildRows(displayChanges);
        }

        // SonarAnalyzer S3267: Cannot simplify with LINQ - this loop mutates existing objects
        // Justification: This loop modifies ModuleAddress property for null values, not filtering
#pragma warning disable S3267 // Loops should be simplified using the "Where" LINQ method
        foreach (var c in displayChanges)
        {
            if (c.ModuleAddress is null)
            {
                c.ModuleAddress = string.Empty;
            }
        }
#pragma warning restore S3267

        return new DisplayFilteringResult(displayChanges, filteredResourceCount);
    }

    /// <summary>
    /// Replaces each parent's child groups with copies whose unchanged rows are removed and counted.
    /// </summary>
    /// <param name="displayChanges">The changes that will be rendered.</param>
    /// <remarks>
    /// Create and delete parents are skipped so their output stays exactly as before. Every other action is
    /// filtered, including no-op parents kept for a changed child. Groups with no rows left are kept so
    /// <c>ChildResourceGroups.Count</c>, the mixed-sources flag and the no-attribute-changes rules still work.
    /// </remarks>
    private static void HideUnchangedChildRows(List<ResourceChangeModel> displayChanges)
    {
        foreach (var change in displayChanges.Where(c => c.ChildResourceGroups.Count > 0 && c.Action is not (TerraformActions.Create or TerraformActions.Delete)))
        {
            change.ChildResourceGroups = change.ChildResourceGroups.Select(HideUnchangedRows).ToList();
        }
    }

    /// <summary>
    /// Copies a group without its unchanged rows.
    /// </summary>
    /// <remarks>
    /// Only rows marked <see cref="ChildResourceRow.IsUnchanged"/> are hidden. Children with other actions, such as
    /// forget, also show the unchanged icon but are kept so the report does not silently drop them.
    /// </remarks>
    /// <param name="group">The group to filter; it is not modified.</param>
    /// <returns>A group with the visible rows and the number of rows removed.</returns>
    private static ChildResourceGroup HideUnchangedRows(ChildResourceGroup group)
    {
        var visibleRows = group.Rows.Where(row => !row.IsUnchanged).ToList();
        return group with
        {
            Rows = visibleRows,
            HiddenUnchangedCount = group.HiddenUnchangedCount + (group.Rows.Count - visibleRows.Count)
        };
    }

    /// <summary>
    /// Determines whether a resource has child resources with non-no-op changes.
    /// </summary>
    /// <param name="resource">Resource to check for child changes.</param>
    /// <returns>True if any child has a change indicator other than "Unchanged" (⏺️).</returns>
    /// <remarks>
    /// This method ensures no-op parents are preserved only when their children have real changes,
    /// avoiding the opposite error where no-op parents with only no-op children are displayed.
    /// </remarks>
    private static bool HasChildrenWithChanges(ResourceChangeModel resource)
    {
        if (resource.ChildResourceGroups.Count == 0)
        {
            return false;
        }

        // Check if any child row has a change indicator other than "Unchanged" (⏺️)
        return resource.ChildResourceGroups
            .SelectMany(group => group.Rows)
            .Any(row => row.ChangeIndicator != ActionIcons.Unchanged);
    }
}
