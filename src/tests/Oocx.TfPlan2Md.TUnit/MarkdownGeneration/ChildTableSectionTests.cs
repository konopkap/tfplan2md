using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the shared warning and hidden-rows note helper for child tables.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
public class ChildTableSectionTests
{
    /// <summary>
    /// Verifies a zero count writes no output (TC-92).
    /// </summary>
    [Test]
    public void WriteHiddenUnchangedNote_ZeroCount_WritesNothing()
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteHiddenUnchangedNote(writer, 0, "route", "routes");

        writer.Build().Should().Be("\n");
    }

    /// <summary>
    /// Verifies a count of one uses the singular noun (TC-92).
    /// </summary>
    [Test]
    public void WriteHiddenUnchangedNote_OneCount_UsesSingular()
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteHiddenUnchangedNote(writer, 1, "route", "routes");

        writer.Raw("END");

        writer.Build().Should().Be("_1 unchanged route hidden._\n\nEND\n");
    }

    /// <summary>
    /// Verifies counts above one use the plural noun (TC-92).
    /// </summary>
    /// <param name="count">The hidden count.</param>
    [Test]
    [Arguments(2)]
    [Arguments(8)]
    public void WriteHiddenUnchangedNote_ManyCount_UsesPlural(int count)
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteHiddenUnchangedNote(writer, count, "route", "routes");

        writer.Raw("END");

        writer.Build().Should().Be($"_{count} unchanged routes hidden._\n\nEND\n");
    }

    /// <summary>
    /// Verifies a large count is written as the exact integer (TC-103).
    /// </summary>
    [Test]
    public void WriteHiddenUnchangedNote_LargeCount_WritesExactNumber()
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteHiddenUnchangedNote(writer, 100, "route", "routes");

        writer.Raw("END");

        writer.Build().Should().Be("_100 unchanged routes hidden._\n\nEND\n");
    }

    /// <summary>
    /// Verifies the note is a single italic line that follows the documented pattern.
    /// </summary>
    [Test]
    public void WriteHiddenUnchangedNote_Count_WritesSingleItalicLine()
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteHiddenUnchangedNote(writer, 3, "DNS record", "DNS records");

        var lines = writer.Build().TrimEnd('\n').Split('\n');
        lines.Should().ContainSingle();
        Regex.IsMatch(lines[0], @"^_\d+ unchanged .+ hidden\._$", RegexOptions.None, TimeSpan.FromSeconds(1)).Should().BeTrue();
        lines[0].Should().Be("_3 unchanged DNS records hidden._");
    }

    /// <summary>
    /// Verifies the mixed-sources warning keeps the text that renderers wrote before the helper existed (TC-93).
    /// </summary>
    [Test]
    public void WriteMixedSourcesWarning_Called_WritesExistingText()
    {
        var writer = new MarkdownWriter();

        ChildTableSection.WriteMixedSourcesWarning(writer);
        writer.Raw("END");

        writer.Build().Should().Be("⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.\n\nEND\n");
    }
}
