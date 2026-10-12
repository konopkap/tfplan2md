using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// End-to-end tests that route table children are shown without their unchanged rows by default.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class HiddenChildRowsRouteTableTests
{
    /// <summary>
    /// Warning text written when children come from inline and separate sources.
    /// </summary>
    private const string MixedWarning = "children managed both inline and as separate resources";

    /// <summary>
    /// Verifies separate routes: changed rows and a note by default, no unchanged route (TC-30).
    /// </summary>
    [Test]
    public void Render_SeparateRoutesDefault_ShowsChangedRowsAndSingularNote()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableSeparate(1), showUnchangedValues: false);

        markdown.Should().Contain("azurerm_route.created").And.Contain("azurerm_route.updated").And.Contain("azurerm_route.deleted");
        markdown.Should().Contain("_1 unchanged route hidden._");
        markdown.Should().NotContain("keep-1");
    }

    /// <summary>
    /// Verifies the option shows the unchanged row and no note (TC-31, TC-45).
    /// </summary>
    [Test]
    public void Render_SeparateRoutesShowUnchanged_ShowsUnchangedRowAndNoNote()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableSeparate(1), showUnchangedValues: true);

        markdown.Should().Contain("| ⏺️ |").And.Contain("keep-1");
        markdown.Should().NotContain("unchanged route");
    }

    /// <summary>
    /// Verifies inline routes use the plural note for several hidden rows (TC-32, TC-43).
    /// </summary>
    [Test]
    public void Render_InlineRoutesDefault_ShowsChangedRowsAndPluralNote()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableInline(2), showUnchangedValues: false);

        markdown.Should().Contain("modified").And.Contain("added").And.Contain("removed");
        markdown.Should().Contain("_2 unchanged routes hidden._");
        markdown.Should().NotContain("keep-1").And.NotContain("keep-2");
    }

    /// <summary>
    /// Verifies a mixed table counts unchanged rows of both sources in one note and keeps the warning (TC-32).
    /// </summary>
    [Test]
    public void Render_MixedSourceRoutesDefault_CountsBothSourcesAndKeepsWarning()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableMixedSources(), showUnchangedValues: false);

        markdown.Should().Contain(MixedWarning);
        markdown.Should().Contain("_2 unchanged routes hidden._");
        markdown.Should().NotContain("inline-keep").And.NotContain("separate-keep");
        markdown.Should().Contain("inline-added").And.Contain("separate-changed");
    }

    /// <summary>
    /// Verifies the remaining rows are the option-on rows without the unchanged ones, in the same order (TC-40).
    /// </summary>
    [Test]
    public void Render_SeparateRoutes_RemainingRowsKeepOptionOnOrder()
    {
        var plan = ChildTableScenarios.RouteTableSeparate(3);

        var defaultRows = TableRows(ChildTableRendering.Render(plan, showUnchangedValues: false));
        var fullRows = TableRows(ChildTableRendering.Render(plan, showUnchangedValues: true));

        defaultRows.Should().Equal(fullRows.Where(row => !row.StartsWith("| ⏺️ |", StringComparison.Ordinal)));
        defaultRows.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies the note follows the last table row after one blank line (TC-41).
    /// </summary>
    [Test]
    public void Render_SeparateRoutesDefault_NoteDirectlyFollowsLastRow()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableSeparate(1), showUnchangedValues: false);

        Regex.IsMatch(markdown, @"\| [^\n]+ \|\n\n_1 unchanged route hidden\._\n\n", RegexOptions.None, TimeSpan.FromSeconds(2)).Should().BeTrue();
    }

    /// <summary>
    /// Verifies a table with all routes unchanged is omitted by default and complete with the option (TC-46, TC-47).
    /// </summary>
    /// <param name="inline">Whether the unchanged routes are inline rather than separate.</param>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void Render_AllRoutesUnchanged_OmitsSectionByDefaultAndShowsItWithOption(bool inline)
    {
        var plan = inline ? ChildTableScenarios.RouteTableAllInlineUnchanged(2) : ChildTableScenarios.RouteTableAllSeparateUnchanged(2);

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().Contain("tags.owner");
        hidden.Should().NotContain("#### Routes").And.NotContain("| Change |").And.NotContain("hidden.");
        shown.Should().Contain("#### Routes").And.Contain("| ⏺️ |");
        shown.Should().NotContain("hidden.");
    }

    /// <summary>
    /// Verifies create and delete parents render identically in both modes (TC-48, TC-49).
    /// </summary>
    /// <param name="create">Whether to use the created route table rather than the deleted one.</param>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void Render_CreateOrDeleteRouteTable_IsIdenticalInBothModes(bool create)
    {
        var plan = create ? ChildTableScenarios.RouteTableCreate() : ChildTableScenarios.RouteTableDelete();

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().Be(shown);
        hidden.Should().Contain("#### Routes").And.NotContain("hidden.");
    }

    /// <summary>
    /// Verifies the parent summary line does not depend on the option (TC-50).
    /// </summary>
    [Test]
    public void Render_SeparateRoutes_SummaryLineIsSameInBothModes()
    {
        var plan = ChildTableScenarios.RouteTableSeparate(2);

        var hidden = SummaryLines(ChildTableRendering.Render(plan, showUnchangedValues: false));
        var shown = SummaryLines(ChildTableRendering.Render(plan, showUnchangedValues: true));

        // The attribute part of the summary legitimately differs with the option; the child counts must not.
        const string childCounts = "➕ 1 routes | 🔄 1 routes | ❌ 1 routes</summary>";
        hidden.Should().ContainSingle().Which.Should().EndWith(childCounts);
        shown.Should().ContainSingle().Which.Should().EndWith(childCounts);
    }

    /// <summary>
    /// Verifies a replaced parent hides unchanged routes.
    /// </summary>
    [Test]
    public void Render_ReplacedRouteTable_HidesUnchangedRoutes()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableReplace(2), showUnchangedValues: false);

        markdown.Should().Contain("_2 unchanged routes hidden._");
        markdown.Should().NotContain("keep-1");
    }

    /// <summary>
    /// Verifies a no-op parent kept for a changed route hides its unchanged routes (TC-29).
    /// </summary>
    [Test]
    public void Render_NoOpRouteTableWithChangedRoute_HidesUnchangedRoutes()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableNoOpParentWithChildChange(), showUnchangedValues: false);

        markdown.Should().Contain("changed");
        markdown.Should().Contain("_1 unchanged route hidden._");
        markdown.Should().NotContain("keep");
    }

    /// <summary>
    /// Verifies a no-op parent with only unchanged routes is still dropped (TC-26).
    /// </summary>
    [Test]
    public void Render_NoOpRouteTableAllUnchanged_IsNotRendered()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableNoOpAllUnchanged(), showUnchangedValues: false);

        markdown.Should().NotContain("azurerm_route_table").And.NotContain("#### Routes");
    }

    /// <summary>
    /// Verifies a mixed-source table with every route unchanged keeps only the warning (TC-52).
    /// </summary>
    [Test]
    public void Render_MixedSourceRoutesAllUnchanged_ShowsOnlyWarningByDefault()
    {
        var plan = ChildTableScenarios.RouteTableMixedAllUnchanged();

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().Contain(MixedWarning);
        hidden.Should().NotContain("#### Routes").And.NotContain("| Change |").And.NotContain("hidden.");
        shown.Should().Contain(MixedWarning).And.Contain("#### Routes").And.Contain("| ⏺️ |");
    }

    /// <summary>
    /// Verifies the child behaviour follows only the show-unchanged option (TC-106).
    /// </summary>
    [Test]
    public void Render_OtherOptions_DoNotChangeChildRows()
    {
        var plan = ChildTableScenarios.RouteTableSeparate(1);
        var options = new Oocx.TfPlan2Md.MarkdownGeneration.ReportModelBuilderOptions(
            ShowUnchangedValues: false,
            HideUnchangedOutputs: true,
            RenderTarget: Oocx.TfPlan2Md.RenderTargets.RenderTarget.GitHub);

        var markdown = ChildTableRendering.Render(plan, showUnchangedValues: false, options);

        markdown.Should().Contain("_1 unchanged route hidden._");
    }

    /// <summary>
    /// Returns the table rows of the markdown, in order.
    /// </summary>
    /// <param name="markdown">The rendered markdown.</param>
    /// <returns>The table rows that start with a change icon.</returns>
    private static List<string> TableRows(string markdown)
    {
        return markdown.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^\| (➕|🔄|❌|♻️|⏺️) \|", RegexOptions.None, TimeSpan.FromSeconds(1)))
            .ToList();
    }

    /// <summary>
    /// Returns the details summary lines of the markdown.
    /// </summary>
    /// <param name="markdown">The rendered markdown.</param>
    /// <returns>The summary lines.</returns>
    private static List<string> SummaryLines(string markdown)
    {
        return markdown.Split('\n').Where(line => line.StartsWith("<summary>", StringComparison.Ordinal)).ToList();
    }
}
