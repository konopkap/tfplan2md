using System.IO;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Edge cases for hidden unchanged child rows: findings, unknown values, options and the no-attribute-changes rules.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class HiddenChildRowsEdgeCaseTests
{
    /// <summary>
    /// Verifies a finding on an unchanged merged child still appears when its row is hidden (TC-53).
    /// </summary>
    [Test]
    public void Render_FindingOnHiddenChild_StillAppearsInParentSection()
    {
        var input = new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools = [],
                Findings =
                [
                    new CodeAnalysisFinding
                    {
                        Message = "Route sends traffic to an unreviewed appliance",
                        SecuritySeverity = 8.0,
                        Locations = [new CodeAnalysisLocation { FullyQualifiedName = "azurerm_route.keep_1" }]
                    }
                ]
            },
            Warnings = [],
            MinimumLevel = null,
            FailOnLevel = null
        };

        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableSeparate(1), showUnchangedValues: false, codeAnalysis: input);

        markdown.Should().NotContain("keep-1 ");
        markdown.Should().Contain("_1 unchanged route hidden._");
        markdown.Should().Contain("Route sends traffic to an unreviewed appliance");
    }

    /// <summary>
    /// Verifies members and subnets whose values are not known yet are not treated as unchanged (TC-102).
    /// </summary>
    /// <param name="fixture">The plan fixture file name.</param>
    [Test]
    [Arguments("azuread-group-members-known-after-apply-plan.json")]
    [Arguments("azurerm-vnet-known-after-apply-plan.json")]
    public void Render_KnownAfterApplyChildren_AreNotHidden(string fixture)
    {
        var plan = File.ReadAllText(Path.Combine("TestData", fixture));

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().NotContain("hidden.").And.NotContain("| ⏺️ |");
        ChangeIconRows(hidden).Should().Equal(ChangeIconRows(shown));
    }

    /// <summary>
    /// Returns the child table rows, which start with a change icon cell.
    /// </summary>
    /// <param name="markdown">The rendered markdown.</param>
    /// <returns>The rows in order.</returns>
    private static List<string> ChangeIconRows(string markdown)
    {
        return markdown.Split('\n')
            .Where(line => line.StartsWith("| ➕ |", StringComparison.Ordinal)
                || line.StartsWith("| 🔄 |", StringComparison.Ordinal)
                || line.StartsWith("| ❌ |", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Verifies the hidden-row rule does not depend on the Azure ID case option (TC-104).
    /// </summary>
    /// <param name="ignoreCaseChanges">The option value.</param>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void Render_IgnoreAzureIdCaseOption_DoesNotChangeHiddenRows(bool ignoreCaseChanges)
    {
        var options = new ReportModelBuilderOptions(ShowUnchangedValues: false, IgnoreAzureIdCaseChanges: ignoreCaseChanges);

        var markdown = ChildTableRendering.Render(ChildTableScenarios.RouteTableSeparate(2), showUnchangedValues: false, options);

        markdown.Should().Contain("_2 unchanged routes hidden._");
        markdown.Should().NotContain("keep-1");
    }

    /// <summary>
    /// Verifies an Azure AD no-op parent kept for a created member still suppresses the no-attribute-changes message (TC-105).
    /// </summary>
    [Test]
    public void Render_AzureAdNoOpParentWithCreatedMember_SuppressionRuleIsSameInBothModes()
    {
        var group = new JsonObject { ["id"] = "group-100", ["display_name"] = "Platform Team", ["members"] = new JsonArray("user-keep-1") };
        var plan = new PlanJsonBuilder()
            .AddNoOp("azuread_group.platform", group)
            .Add("azuread_group_member.created", "create", null, new JsonObject { ["group_object_id"] = "group-100", ["member_object_id"] = "user-new" })
            .Build();

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().NotContain("No attribute changes");
        shown.Should().NotContain("No attribute changes");
        hidden.Should().Contain("user-new").And.Contain("_1 unchanged member hidden._");
        shown.Should().Contain("user-keep-1");
    }

    /// <summary>
    /// Verifies the NSG no-op scenario with two separate rules still renders as before (TC-27).
    /// </summary>
    [Test]
    public void Render_NoOpNsgWithTwoUnchangedSeparateRules_IsStillDropped()
    {
        var markdown = ChildTableRendering.Render(File.ReadAllText("TestData/nsg-with-no-op-rules.json"), showUnchangedValues: false);

        markdown.Should().NotContain("hidden.");
    }

    /// <summary>
    /// Verifies the inline NSG renderer uses the same nouns as the provider registration (TC-88).
    /// </summary>
    [Test]
    public void NsgInlinePath_Nouns_MatchAzureRmModuleRegistration()
    {
        AzureRmChildItemNames.SecurityRuleSingular.Should().Be("security rule");
        AzureRmChildItemNames.SecurityRulePlural.Should().Be("security rules");
    }
}
