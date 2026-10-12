using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a single child resource row in a parent table.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildResourceRow
{
    /// <summary>
    /// Gets the change indicator for the child resource (e.g., "➕", "🔄").
    /// </summary>
    /// <value>The change indicator symbol.</value>
    public required string ChangeIndicator { get; init; }

    /// <summary>
    /// Gets a value indicating whether the child is really unchanged: an inline entry with identical values
    /// before and after, or a separate child whose action is no-op.
    /// </summary>
    /// <value><c>true</c> when the row may be hidden as unchanged; otherwise <c>false</c>.</value>
    /// <remarks>
    /// Kept apart from <see cref="ChangeIndicator"/> because other child actions, such as forget, also show the
    /// unchanged icon but must stay visible.
    /// </remarks>
    public bool IsUnchanged { get; init; }

    /// <summary>
    /// Gets the column values keyed by <see cref="ChildTableColumn.PropertyName"/>.
    /// </summary>
    /// <value>A map of column property names to formatted values.</value>
    public required IReadOnlyDictionary<string, string> Values { get; init; }

    /// <summary>
    /// Gets the Terraform resource label shown in the table.
    /// </summary>
    /// <value>The resource address or inline attribute label.</value>
    public required string TerraformResource { get; init; }

    /// <summary>
    /// Gets the original Terraform resource address for findings attribution.
    /// </summary>
    /// <value>The original resource address, if available.</value>
    public string? OriginalResourceAddress { get; init; }
}
