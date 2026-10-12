using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for extracted default-renderer policy decisions.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
[Category("Unit")]
public class DefaultResourceRenderPolicyTests
{
    [Test]
    public async Task ResolvePolicy_NoOpParentSecurityRuleScenario_SetsCompatibilityFlags()
    {
        var change = CreateChange(
            resourceType: "azurerm_network_security_group",
            moduleAddress: null,
            attributes: []);
        change.ChildResourceGroups =
        [
            new ChildResourceGroup
            {
                Label = "Security Rules",
                ItemNameSingular = "security rule",
                ItemNamePlural = "security rules",
                Columns = [new ChildTableColumn("Name", "name")],
                Rows =
                [
                    new ChildResourceRow
                    {
                        ChangeIndicator = "➕",
                        Values = new Dictionary<string, string> { ["name"] = "rule-a" },
                        TerraformResource = "azurerm_network_security_rule.rule_a"
                    },
                    new ChildResourceRow
                    {
                        ChangeIndicator = "➕",
                        Values = new Dictionary<string, string> { ["name"] = "rule-b" },
                        TerraformResource = "azurerm_network_security_rule.rule_b"
                    }
                ]
            }
        ];

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.IsNoOpParentWithChildren.Should().BeTrue();
        policy.UseMultilineDetailsSummary.Should().BeTrue();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the two-row security-rule scenario counts rows that were hidden as unchanged (TC-27).
    /// </summary>
    [Test]
    public void ResolvePolicy_SecurityRuleGroupWithOneRowOneHidden_UsesFullCount()
    {
        var change = CreateChange(resourceType: "azurerm_network_security_group", moduleAddress: null, attributes: []);
        change.ChildResourceGroups = [CreateSecurityRuleGroup(rowCount: 1, hiddenUnchangedCount: 1)];

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.IsNoOpParentWithChildren.Should().BeTrue();
    }

    /// <summary>
    /// Verifies a single visible security rule with nothing hidden is not the two-row scenario (TC-27).
    /// </summary>
    [Test]
    public void ResolvePolicy_SecurityRuleGroupWithOneRowNoneHidden_IsNotTwoRowScenario()
    {
        var change = CreateChange(resourceType: "azurerm_network_security_group", moduleAddress: null, attributes: []);
        change.ChildResourceGroups = [CreateSecurityRuleGroup(rowCount: 1, hiddenUnchangedCount: 0)];

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.IsNoOpParentWithChildren.Should().BeFalse();
    }

    /// <summary>
    /// Creates a security-rules group with the given visible and hidden row counts.
    /// </summary>
    /// <param name="rowCount">Number of visible rows.</param>
    /// <param name="hiddenUnchangedCount">Number of rows hidden as unchanged.</param>
    /// <returns>The security-rules group.</returns>
    private static ChildResourceGroup CreateSecurityRuleGroup(int rowCount, int hiddenUnchangedCount)
    {
        return new ChildResourceGroup
        {
            Label = "Security Rules",
            ItemNameSingular = "security rule",
            ItemNamePlural = "security rules",
            Columns = [new ChildTableColumn("Name", "name")],
            HiddenUnchangedCount = hiddenUnchangedCount,
            Rows = Enumerable.Range(0, rowCount)
                .Select(index => new ChildResourceRow
                {
                    ChangeIndicator = "➕",
                    Values = new Dictionary<string, string> { ["name"] = $"rule-{index}" },
                    TerraformResource = $"azurerm_network_security_rule.rule_{index}"
                })
                .ToList()
        };
    }

    [Test]
    public async Task ResolvePolicy_AzureAdResource_UsesExtraBlankLineBeforeSummary()
    {
        var change = CreateChange(
            resourceType: "azuread_group",
            moduleAddress: null,
            attributes:
            [
                new AttributeChangeModel { Name = "display_name", Before = null, After = "team-a" }
            ]);

        var policy = DefaultResourceRenderPolicy.Resolve(change, CreateContext());

        policy.UseExtraBlankLineBeforeSummary.Should().BeTrue();
        await Task.CompletedTask;
    }

    private static RenderContext CreateContext()
    {
        return new RenderContext(
            showSensitive: false,
            showUnchangedValues: false,
            ignoreAzureIdCaseChanges: true,
            renderTarget: RenderTarget.AzureDevOps,
            detailsDisplayMode: DetailsDisplayMode.Auto);
    }

    private static ResourceChangeModel CreateChange(
        string resourceType,
        string? moduleAddress,
        IReadOnlyList<AttributeChangeModel> attributes,
        string action = "create")
    {
        return new ResourceChangeModel
        {
            Address = $"{resourceType}.main",
            ModuleAddress = moduleAddress,
            Type = resourceType,
            Name = "main",
            ProviderName = "registry.terraform.io/hashicorp/azurerm",
            Action = action,
            ActionSymbol = "➕",
            AttributeChanges = attributes,
            ConfigurationReferences = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
