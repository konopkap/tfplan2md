using System.Text.Json.Nodes;
using AwesomeAssertions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Checks the markdown of default-mode child tables with hidden rows is valid for every render target and details mode.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class HiddenChildRowsMarkdownValidityTests
{
    /// <summary>
    /// Verifies every default scenario parses into well-formed tables and keeps the note outside them (TC-54).
    /// </summary>
    /// <param name="scenario">The scenario number, one for each pattern.</param>
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    public void Render_DefaultScenario_ProducesWellFormedTablesForEveryTargetAndMode(int scenario)
    {
        foreach (var target in Enum.GetValues<RenderTarget>())
        {
            foreach (var mode in Enum.GetValues<DetailsDisplayMode>())
            {
                var options = new ReportModelBuilderOptions(RenderTarget: target, DetailsDisplayMode: mode);
                var markdown = ChildTableRendering.Render(Plans()[scenario], showUnchangedValues: false, options);

                AssertWellFormed(markdown, $"scenario {scenario}, {target}, {mode}");
            }
        }
    }

    /// <summary>
    /// Verifies one scenario shows the same child rows and note for every render target and details mode (TC-55).
    /// </summary>
    [Test]
    public void Render_SeparateRoutes_SameChildRowsAndNoteForEveryTargetAndMode()
    {
        var plan = ChildTableScenarios.RouteTableSeparate(1);
        foreach (var target in Enum.GetValues<RenderTarget>())
        {
            foreach (var mode in Enum.GetValues<DetailsDisplayMode>())
            {
                var options = new ReportModelBuilderOptions(RenderTarget: target, DetailsDisplayMode: mode);
                var markdown = ChildTableRendering.Render(plan, showUnchangedValues: false, options);

                markdown.Should().Contain("_1 unchanged route hidden._", $"{target}, {mode}");
                markdown.Should().Contain("azurerm_route.created").And.Contain("azurerm_route.updated").And.Contain("azurerm_route.deleted");
                markdown.Should().NotContain("keep-1");
            }
        }
    }

    /// <summary>
    /// Verifies child names with table and HTML special characters do not break the table (TC-54).
    /// </summary>
    [Test]
    public void Render_ChildNamesWithSpecialCharacters_KeepTableWellFormed()
    {
        var route = new JsonObject
        {
            ["name"] = "r|1`x<y&z",
            ["route_table_name"] = ChildTableScenarios.RouteTableName,
            ["address_prefix"] = "10.0.0.0/24",
            ["next_hop_type"] = "VnetLocal",
            ["next_hop_in_ip_address"] = null
        };
        var table = new JsonObject { ["name"] = ChildTableScenarios.RouteTableName, ["location"] = "westeurope" };
        var plan = new PlanJsonBuilder()
            .Add("azurerm_route_table.app", "update", table, new JsonObject { ["name"] = ChildTableScenarios.RouteTableName, ["location"] = "northeurope" })
            .Add("azurerm_route.special", "create", null, route)
            .AddNoOp("azurerm_route.keep", new JsonObject { ["name"] = "keep|me", ["route_table_name"] = ChildTableScenarios.RouteTableName, ["address_prefix"] = "10.1.0.0/24", ["next_hop_type"] = "VnetLocal" })
            .Build();

        var markdown = ChildTableRendering.Render(plan, showUnchangedValues: false);

        markdown.Should().Contain("_1 unchanged route hidden._");
        AssertWellFormed(markdown, "special characters");
    }

    /// <summary>
    /// Builds one plan for each parent-child pattern with unchanged children.
    /// </summary>
    /// <returns>The plan JSON documents.</returns>
    private static string[] Plans()
    {
        return
        [
            ChildTableScenarios.RouteTableSeparate(1),
            ChildTableScenarios.RouteTableInline(2),
            ChildTableScenarios.VnetInlineSubnets(2),
            ChildTableScenarios.VnetSeparateSubnets(1),
            ChildTableScenarios.DnsZoneRecords(2),
            ChildTableScenarios.NsgInlineRules(2),
            ChildTableScenarios.NsgSeparateRules(1),
            ChildTableScenarios.AzureAdGroupMembers(2),
            ChildTableScenarios.DevOpsTeamMembersAndAdministrators(2, 1)
        ];
    }

    /// <summary>
    /// Asserts every table has equal column counts per row and every note is a standalone paragraph.
    /// </summary>
    /// <param name="markdown">The rendered markdown.</param>
    /// <param name="context">Text that identifies the scenario in failures.</param>
    private static void AssertWellFormed(string markdown, string context)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(markdown, pipeline);

        var tables = document.Descendants<Table>().ToList();
        tables.Should().NotBeEmpty(context);
        foreach (var table in tables)
        {
            var widths = table.OfType<TableRow>().Select(row => row.Count).Distinct().ToList();
            widths.Should().ContainSingle($"every row of a table has the same number of cells ({context})");
        }

        markdown.Should().NotContain("\n\n\n", context);
        foreach (var line in markdown.Split('\n').Where(line => line.EndsWith("hidden._", StringComparison.Ordinal)))
        {
            line.Should().MatchRegex(@"^_\d+ unchanged .+ hidden\._$", context);
        }
    }
}
