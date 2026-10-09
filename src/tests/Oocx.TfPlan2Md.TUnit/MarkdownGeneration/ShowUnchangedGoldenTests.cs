using System.IO;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Pins the output of parent-child plans rendered with <c>--show-unchanged-values</c> to golden files captured before child rows were hidden by default.
/// Related feature: docs/features/149-hide-unchanged-child-rows/specification.md.
/// </summary>
/// <remarks>
/// Golden files live in <c>TestData/Golden/</c>, outside <c>TestData/Snapshots/</c>, so that
/// <c>scripts/update-test-snapshots.sh</c> never regenerates them. A missing golden file is written
/// on first run and the test fails until it is committed; an existing one must match exactly.
/// </remarks>
public class ShowUnchangedGoldenTests
{
    /// <summary>
    /// Directory holding the golden files, relative to the test working directory.
    /// </summary>
    private const string GoldenDirectory = "TestData/Golden";

    /// <summary>
    /// Verifies that parent-child plans keep their pre-feature output with <c>--show-unchanged-values</c>.
    /// </summary>
    /// <param name="scenario">The scenario name, also the golden file name.</param>
    [Test]
    [Arguments("route-table-separate-1-unchanged")]
    [Arguments("route-table-separate-3-unchanged")]
    [Arguments("route-table-inline-2-unchanged")]
    [Arguments("route-table-mixed-sources")]
    [Arguments("route-table-all-inline-unchanged")]
    [Arguments("route-table-all-separate-unchanged")]
    [Arguments("route-table-mixed-all-unchanged")]
    [Arguments("route-table-create")]
    [Arguments("route-table-delete")]
    [Arguments("route-table-replace")]
    [Arguments("route-table-noop-parent-child-change")]
    [Arguments("route-table-noop-all-unchanged")]
    [Arguments("vnet-inline-subnets-2-unchanged")]
    [Arguments("vnet-inline-subnets-1-unchanged")]
    [Arguments("vnet-all-inline-subnets-unchanged")]
    [Arguments("vnet-separate-subnets-1-unchanged")]
    [Arguments("vnet-separate-subnets-2-unchanged")]
    [Arguments("dns-zone-records-2-unchanged")]
    [Arguments("dns-zone-records-1-unchanged")]
    [Arguments("nsg-inline-rules-2-unchanged")]
    [Arguments("nsg-inline-rules-1-unchanged")]
    [Arguments("nsg-all-inline-rules-unchanged")]
    [Arguments("nsg-separate-rules-1-unchanged")]
    [Arguments("nsg-separate-rules-2-unchanged")]
    [Arguments("azuread-group-members-2-unchanged")]
    [Arguments("azuread-group-members-1-unchanged")]
    [Arguments("devops-team-members-and-administrators")]
    [Arguments("devops-team-members-and-administrators-1-unchanged")]
    [Arguments("devops-group-members-2-unchanged")]
    [Arguments("devops-group-members-1-unchanged")]
    [Arguments("fixture:azurerm-route-table-inline-routes-plan.json")]
    [Arguments("fixture:azurerm-vnet-inline-subnets-plan.json")]
    [Arguments("fixture:azurerm-vnet-mixed-subnets-plan.json")]
    [Arguments("fixture:azurerm-vnet-separate-subnets-plan.json")]
    [Arguments("fixture:azurerm-nsg-inline-rules-plan.json")]
    [Arguments("fixture:nsg-with-no-op-rules.json")]
    [Arguments("fixture:nsg-with-separate-rule-updates.json")]
    [Arguments("fixture:azurerm-dns-zone-records-plan.json")]
    [Arguments("fixture:azuread-group-members-plan.json")]
    [Arguments("fixture:azuredevops-team-members-plan.json")]
    [Arguments("fixture:azuredevops-group-members-plan.json")]
    [Arguments("fixture:parent-child-resource-grouping-uat-plan.json")]
    public void Render_ShowUnchangedValues_MatchesPreFeatureGolden(string scenario)
    {
        var markdown = ChildTableRendering.Render(LoadPlan(scenario), showUnchangedValues: true);

        AssertMatchesGolden(scenario, markdown);
    }

    /// <summary>
    /// Resolves the plan JSON for a scenario name.
    /// </summary>
    /// <param name="scenario">A scenario name or a <c>fixture:</c> prefixed test data file name.</param>
    /// <returns>The plan JSON.</returns>
    private static string LoadPlan(string scenario)
    {
        const string fixturePrefix = "fixture:";
        if (scenario.StartsWith(fixturePrefix, StringComparison.Ordinal))
        {
            return File.ReadAllText(Path.Combine("TestData", scenario[fixturePrefix.Length..]));
        }

        return scenario switch
        {
            "route-table-separate-1-unchanged" => ChildTableScenarios.RouteTableSeparate(1),
            "route-table-separate-3-unchanged" => ChildTableScenarios.RouteTableSeparate(3),
            "route-table-inline-2-unchanged" => ChildTableScenarios.RouteTableInline(2),
            "route-table-mixed-sources" => ChildTableScenarios.RouteTableMixedSources(),
            "route-table-all-inline-unchanged" => ChildTableScenarios.RouteTableAllInlineUnchanged(2),
            "route-table-all-separate-unchanged" => ChildTableScenarios.RouteTableAllSeparateUnchanged(2),
            "route-table-mixed-all-unchanged" => ChildTableScenarios.RouteTableMixedAllUnchanged(),
            "route-table-create" => ChildTableScenarios.RouteTableCreate(),
            "route-table-delete" => ChildTableScenarios.RouteTableDelete(),
            "route-table-replace" => ChildTableScenarios.RouteTableReplace(2),
            "route-table-noop-parent-child-change" => ChildTableScenarios.RouteTableNoOpParentWithChildChange(),
            "route-table-noop-all-unchanged" => ChildTableScenarios.RouteTableNoOpAllUnchanged(),
            "vnet-inline-subnets-2-unchanged" => ChildTableScenarios.VnetInlineSubnets(2),
            "vnet-inline-subnets-1-unchanged" => ChildTableScenarios.VnetInlineSubnets(1),
            "vnet-all-inline-subnets-unchanged" => ChildTableScenarios.VnetAllInlineSubnetsUnchanged(2),
            "vnet-separate-subnets-1-unchanged" => ChildTableScenarios.VnetSeparateSubnets(1),
            "vnet-separate-subnets-2-unchanged" => ChildTableScenarios.VnetSeparateSubnets(2),
            "dns-zone-records-2-unchanged" => ChildTableScenarios.DnsZoneRecords(2),
            "dns-zone-records-1-unchanged" => ChildTableScenarios.DnsZoneRecords(1),
            "nsg-inline-rules-2-unchanged" => ChildTableScenarios.NsgInlineRules(2),
            "nsg-inline-rules-1-unchanged" => ChildTableScenarios.NsgInlineRules(1),
            "nsg-all-inline-rules-unchanged" => ChildTableScenarios.NsgAllInlineRulesUnchanged(2),
            "nsg-separate-rules-1-unchanged" => ChildTableScenarios.NsgSeparateRules(1),
            "nsg-separate-rules-2-unchanged" => ChildTableScenarios.NsgSeparateRules(2),
            "azuread-group-members-2-unchanged" => ChildTableScenarios.AzureAdGroupMembers(2),
            "azuread-group-members-1-unchanged" => ChildTableScenarios.AzureAdGroupMembers(1),
            "devops-team-members-and-administrators" => ChildTableScenarios.DevOpsTeamMembersAndAdministrators(2, 2),
            "devops-team-members-and-administrators-1-unchanged" => ChildTableScenarios.DevOpsTeamMembersAndAdministrators(1, 1),
            "devops-group-members-2-unchanged" => ChildTableScenarios.DevOpsGroupMembers(2),
            "devops-group-members-1-unchanged" => ChildTableScenarios.DevOpsGroupMembers(1),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown golden scenario.")
        };
    }

    /// <summary>
    /// Compares rendered markdown to its golden file, writing the file when it does not exist yet.
    /// </summary>
    /// <param name="scenario">The scenario name.</param>
    /// <param name="actual">The rendered markdown.</param>
    private static void AssertMatchesGolden(string scenario, string actual)
    {
        var fileName = scenario.Replace("fixture:", "fixture-", StringComparison.Ordinal).Replace(".json", string.Empty, StringComparison.Ordinal) + ".md";
        var path = Path.GetFullPath(Path.Combine(GoldenDirectory, fileName));
        var normalizedActual = actual.Replace("\r\n", "\n", StringComparison.Ordinal);

        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, normalizedActual);
            Assert.Fail($"Golden file '{fileName}' did not exist and was written to {path}. Review it, commit it, and re-run.");
        }

        var expected = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!string.Equals(expected, normalizedActual, StringComparison.Ordinal))
        {
            Assert.Fail($"Output with --show-unchanged-values differs from golden file '{fileName}' ({path}).");
        }
    }
}
