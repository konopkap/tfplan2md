using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests how <see cref="DefaultResourceRenderer"/> renders child tables whose unchanged rows were hidden.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class DefaultResourceRendererChildRowsTests
{
    /// <summary>
    /// Warning text shared with <see cref="ChildTableSection"/>.
    /// </summary>
    private const string Warning = "⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.";

    /// <summary>
    /// Verifies the note sits directly below the table after one blank line (TC-10, TC-11).
    /// </summary>
    [Test]
    public void RenderChildResources_RowsHidden_WritesNoteDirectlyBelowTable()
    {
        var markdown = Render(CreateGroup(hidden: 8, "➕", "🔄"));

        markdown.Should().Contain("#### Routes");
        markdown.Should().MatchRegex(@"\| 🔄 \| `row-1` \|\n\n_8 unchanged routes hidden\._\n");
    }

    /// <summary>
    /// Verifies the note uses the nouns on the group (TC-12).
    /// </summary>
    [Test]
    public void RenderChildResources_Note_UsesGroupNouns()
    {
        var markdown = Render(CreateGroup(hidden: 3, "➕") with { ItemNameSingular = "DNS record", ItemNamePlural = "DNS records" });

        markdown.Should().Contain("_3 unchanged DNS records hidden._");
    }

    /// <summary>
    /// Verifies singular for one hidden row and plural for several (TC-13, TC-14).
    /// </summary>
    /// <param name="hidden">The hidden count.</param>
    /// <param name="expected">The expected note.</param>
    [Test]
    [Arguments(1, "_1 unchanged route hidden._")]
    [Arguments(2, "_2 unchanged routes hidden._")]
    [Arguments(8, "_8 unchanged routes hidden._")]
    public void RenderChildResources_Hidden_UsesSingularOnlyForOne(int hidden, string expected)
    {
        Render(CreateGroup(hidden, "➕")).Should().Contain(expected);
    }

    /// <summary>
    /// Verifies no note is written when nothing was hidden (TC-16, TC-17).
    /// </summary>
    [Test]
    public void RenderChildResources_NothingHidden_WritesNoNoteAndKeepsUnchangedRows()
    {
        var markdown = Render(CreateGroup(hidden: 0, "➕", "⏺️"));

        markdown.Should().NotContain("unchanged");
        markdown.Should().Contain("| ⏺️ | `row-1` |");
    }

    /// <summary>
    /// Verifies a group with every row hidden writes no heading, table or note (TC-20, TC-21).
    /// </summary>
    [Test]
    public void RenderChildResources_AllRowsHiddenNotMixed_WritesNothing()
    {
        var markdown = Render(CreateGroup(hidden: 3));

        markdown.Should().NotContain("#### Routes");
        markdown.Should().NotContain("| Change |");
        markdown.Should().NotContain("hidden");
    }

    /// <summary>
    /// Verifies a group of only unchanged rows with no hidden count renders in full (TC-22).
    /// </summary>
    [Test]
    public void RenderChildResources_OnlyUnchangedRowsNothingHidden_RendersFullTable()
    {
        var markdown = Render(CreateGroup(hidden: 0, "⏺️", "⏺️"));

        markdown.Should().Contain("#### Routes");
        markdown.Should().Contain("| ⏺️ | `row-0` |");
        markdown.Should().Contain("| ⏺️ | `row-1` |");
    }

    /// <summary>
    /// Verifies a mixed group with every row hidden writes only the warning (TC-23).
    /// </summary>
    [Test]
    public void RenderChildResources_AllRowsHiddenAndMixed_WritesOnlyWarning()
    {
        var markdown = Render(CreateGroup(hidden: 2) with { HasMixedSources = true });

        markdown.Should().Contain(Warning);
        markdown.Should().NotContain("#### Routes");
        markdown.Should().NotContain("| Change |");
        markdown.Should().NotContain("hidden");
    }

    /// <summary>
    /// Verifies the order heading, warning, table, note when rows remain (TC-24).
    /// </summary>
    [Test]
    public void RenderChildResources_RowsRemainAndMixed_WritesWarningThenTableThenNote()
    {
        var markdown = Render(CreateGroup(hidden: 1, "➕") with { HasMixedSources = true });

        var heading = markdown.IndexOf("#### Routes", StringComparison.Ordinal);
        var warning = markdown.IndexOf(Warning, StringComparison.Ordinal);
        var table = markdown.IndexOf("| Change |", StringComparison.Ordinal);
        var note = markdown.IndexOf("_1 unchanged route hidden._", StringComparison.Ordinal);

        heading.Should().BeGreaterThan(-1);
        warning.Should().BeGreaterThan(heading);
        table.Should().BeGreaterThan(warning);
        note.Should().BeGreaterThan(table);
    }

    /// <summary>
    /// Verifies mixed sources with nothing hidden still show the warning (TC-25).
    /// </summary>
    [Test]
    public void RenderChildResources_MixedSourcesNothingHidden_ShowsWarningAndNoNote()
    {
        var markdown = Render(CreateGroup(hidden: 0, "➕") with { HasMixedSources = true });

        markdown.Should().Contain(Warning);
        markdown.Should().NotContain("hidden");
    }

    /// <summary>
    /// Verifies one remaining changed row still renders with the note.
    /// </summary>
    [Test]
    public void RenderChildResources_OneRowRemains_RendersRowAndNote()
    {
        var markdown = Render(CreateGroup(hidden: 4, "🔄"));

        markdown.Should().Contain("| 🔄 | `row-0` |");
        markdown.Should().Contain("_4 unchanged routes hidden._");
    }

    /// <summary>
    /// Verifies the table separator row is the one this renderer always wrote.
    /// </summary>
    [Test]
    public void RenderChildResources_Table_KeepsSeparatorRow()
    {
        Render(CreateGroup(hidden: 1, "➕")).Should().Contain("| Change | Name |\n| -------- | -------- |\n");
    }

    /// <summary>
    /// Renders a parent holding one child group.
    /// </summary>
    /// <param name="group">The child group.</param>
    /// <returns>The rendered markdown.</returns>
    private static string Render(ChildResourceGroup group)
    {
        var change = new ResourceChangeModel
        {
            Address = "test_parent.example",
            Type = "test_parent",
            Name = "example",
            ProviderName = "provider",
            Action = "update",
            ActionSymbol = "🔄",
            ModuleAddress = string.Empty,
            AttributeChanges = [],
            ChildResourceGroups = [group]
        };
        var context = new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);
        var writer = new MarkdownWriter();

        new DefaultResourceRenderer().Render(writer, change, context);

        return writer.Build();
    }

    /// <summary>
    /// Creates a routes group with the given visible row indicators and hidden count.
    /// </summary>
    /// <param name="hidden">Number of rows hidden as unchanged.</param>
    /// <param name="indicators">Change indicators of the visible rows.</param>
    /// <returns>The group.</returns>
    private static ChildResourceGroup CreateGroup(int hidden, params string[] indicators)
    {
        return new ChildResourceGroup
        {
            Label = "Routes",
            ItemNameSingular = "route",
            ItemNamePlural = "routes",
            Columns = [new ChildTableColumn("Name", "name")],
            HiddenUnchangedCount = hidden,
            Rows = indicators
                .Select((indicator, index) => new ChildResourceRow
                {
                    ChangeIndicator = indicator,
                    Values = new Dictionary<string, string> { ["name"] = $"row-{index}" },
                    TerraformResource = $"test_child.row_{index}"
                })
                .ToList()
        };
    }
}
