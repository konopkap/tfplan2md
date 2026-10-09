namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Writes the parts of a parent-child table section that every child-table renderer shares.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
/// <remarks>
/// Renderers that build a child table, in core and in providers, call this helper instead of
/// repeating the text, so the warning and the hidden-rows note read the same everywhere.
/// </remarks>
internal static class ChildTableSection
{
    /// <summary>
    /// Writes the warning shown when a resource has children both inline and as separate resources.
    /// </summary>
    /// <param name="writer">The markdown writer.</param>
    internal static void WriteMixedSourcesWarning(MarkdownWriter writer)
    {
        writer.Paragraph("⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.");
        writer.BlankLine();
    }

    /// <summary>
    /// Writes a one-line italic note saying how many unchanged child rows were left out of the table.
    /// </summary>
    /// <param name="writer">The markdown writer.</param>
    /// <param name="hiddenCount">Number of unchanged rows removed; nothing is written for zero.</param>
    /// <param name="singularNoun">Noun used when exactly one row was hidden.</param>
    /// <param name="pluralNoun">Noun used for any other count.</param>
    internal static void WriteHiddenUnchangedNote(MarkdownWriter writer, int hiddenCount, string singularNoun, string pluralNoun)
    {
        if (hiddenCount <= 0)
        {
            return;
        }

        var noun = hiddenCount == 1 ? singularNoun : pluralNoun;
        writer.Paragraph($"_{hiddenCount} unchanged {noun} hidden._");
        writer.BlankLine();
    }
}
