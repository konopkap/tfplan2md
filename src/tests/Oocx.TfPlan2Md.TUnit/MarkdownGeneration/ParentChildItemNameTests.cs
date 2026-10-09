using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the child item nouns that the hidden-unchanged-rows note uses.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
public class ParentChildItemNameTests
{
    /// <summary>
    /// Verifies every provider registration states non-empty singular and plural nouns (TC-15).
    /// </summary>
    [Test]
    public void Registrations_EveryParentChildRelationship_DefinesSingularAndPluralNouns()
    {
        var relationships = CollectRelationships();

        relationships.Should().NotBeEmpty();
        relationships.Should().OnlyContain(relationship =>
            !string.IsNullOrWhiteSpace(relationship.ItemNameSingular)
            && !string.IsNullOrWhiteSpace(relationship.ItemNamePlural));
    }

    /// <summary>
    /// Verifies each label maps to its exact noun pair (TC-15).
    /// </summary>
    /// <param name="label">The child group label.</param>
    /// <param name="singular">The expected singular noun.</param>
    /// <param name="plural">The expected plural noun.</param>
    [Test]
    [Arguments("Routes", "route", "routes")]
    [Arguments("Subnets", "subnet", "subnets")]
    [Arguments("Security Rules", "security rule", "security rules")]
    [Arguments("DNS Records", "DNS record", "DNS records")]
    [Arguments("Members", "member", "members")]
    [Arguments("Administrators", "administrator", "administrators")]
    public void Registrations_ChildLabel_UsesExpectedNouns(string label, string singular, string plural)
    {
        var forLabel = CollectRelationships().Where(relationship => relationship.ChildGroupLabel == label).ToList();

        forLabel.Should().NotBeEmpty();
        forLabel.Should().OnlyContain(relationship =>
            relationship.ItemNameSingular == singular && relationship.ItemNamePlural == plural);
    }

    /// <summary>
    /// Verifies the merge step copies the registered nouns onto the child group (TC-100, first half).
    /// </summary>
    [Test]
    public void MergeParentChildRelationships_SingleRelationship_CopiesNounsOntoGroup()
    {
        var model = ChildTableRendering.BuildModel(ChildTableScenarios.RouteTableSeparate(), showUnchangedValues: true);

        var group = model.Changes.Single(change => change.Type == "azurerm_route_table").ChildResourceGroups.Single();

        group.ItemNameSingular.Should().Be("route");
        group.ItemNamePlural.Should().Be("routes");
    }

    /// <summary>
    /// Verifies groups with the same label are merged using the first group's nouns (TC-100, second half).
    /// </summary>
    [Test]
    public void MergeGroupsByLabel_SameLabelFromSeveralRecordTypes_TakesNounsFromFirstGroup()
    {
        var model = ChildTableRendering.BuildModel(ChildTableScenarios.DnsZoneRecords(2), showUnchangedValues: true);

        var group = model.Changes.Single(change => change.Type == "azurerm_dns_zone").ChildResourceGroups.Single();

        group.Rows.Should().HaveCountGreaterThan(2, "records of several types merge into one table");
        group.ItemNameSingular.Should().Be("DNS record");
        group.ItemNamePlural.Should().Be("DNS records");
    }

    /// <summary>
    /// Verifies a group has no hidden rows by default (TC-09, model part).
    /// </summary>
    [Test]
    public void ChildResourceGroup_NewGroup_HasZeroHiddenUnchangedCount()
    {
        var group = new ChildResourceGroup
        {
            Label = "Routes",
            Columns = [new ChildTableColumn("Name", "name")],
            Rows = [],
            ItemNameSingular = "route",
            ItemNamePlural = "routes"
        };

        group.HiddenUnchangedCount.Should().Be(0);
    }

    /// <summary>
    /// Collects every relationship that the provider modules register.
    /// </summary>
    /// <returns>The registered relationships.</returns>
    private static List<ParentChildRelationship> CollectRelationships()
    {
        var registry = new RecordingRegistry();
        new AzureRMModule(LargeValueFormat.InlineDiff).RegisterParentChildRelationships(registry);
        new AzureADModule().RegisterParentChildRelationships(registry);
        new AzureDevOpsModule().RegisterParentChildRelationships(registry);
        return registry.Registered;
    }

    /// <summary>
    /// Registry that records every registered relationship so tests can inspect all of them.
    /// </summary>
    private sealed class RecordingRegistry : IParentChildRelationshipRegistry
    {
        /// <summary>
        /// Gets the relationships registered so far.
        /// </summary>
        internal List<ParentChildRelationship> Registered { get; } = [];

        public void Register(ParentChildRelationship relationship) => Registered.Add(relationship);

        public IReadOnlyList<ParentChildRelationship> GetRelationshipsForParent(string parentResourceType) => [];

        public IReadOnlySet<string> GetAllChildResourceTypes() => new HashSet<string>();

        public bool IsChildResourceType(string resourceType) => false;
    }
}
