using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a group of child resources rendered within a parent section.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildResourceGroup
{
    /// <summary>
    /// Gets the display label for the child group.
    /// </summary>
    /// <value>The label shown above the child table.</value>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the column definitions for the table header.
    /// </summary>
    /// <value>The ordered list of table columns.</value>
    public required IReadOnlyList<ChildTableColumn> Columns { get; init; }

    /// <summary>
    /// Gets the row data for each child entry.
    /// </summary>
    /// <value>The ordered list of child rows.</value>
    /// <remarks>
    /// Unchanged rows are removed by <c>DisplayFilteringStage</c> unless unchanged values are shown, so code that
    /// runs after that stage sees only the visible rows. Use <see cref="HiddenUnchangedCount"/> for the full count.
    /// </remarks>
    public required IReadOnlyList<ChildResourceRow> Rows { get; init; }

    /// <summary>
    /// Gets the number of unchanged rows removed from <see cref="Rows"/> before rendering.
    /// </summary>
    /// <value>Zero when nothing was hidden or unchanged values are shown.</value>
    public int HiddenUnchangedCount { get; init; }

    /// <summary>
    /// Gets the singular noun for one child, used in the hidden-rows note.
    /// </summary>
    /// <value>For example <c>route</c>. Provided by the provider registration, never derived from <see cref="Label"/>.</value>
    public required string ItemNameSingular { get; init; }

    /// <summary>
    /// Gets the plural noun for several children, used in the hidden-rows note.
    /// </summary>
    /// <value>For example <c>routes</c>.</value>
    public required string ItemNamePlural { get; init; }

    /// <summary>
    /// Gets a value indicating whether both inline and separate children were detected.
    /// </summary>
    /// <value><c>true</c> when mixed sources were detected; otherwise, <c>false</c>.</value>
    public bool HasMixedSources { get; init; }

    /// <summary>
    /// Gets a value indicating whether the group contains any external (separate) resources.
    /// Used to conditionally show the Terraform Resource column.
    /// </summary>
    public bool HasExternalResources { get; init; }
}
