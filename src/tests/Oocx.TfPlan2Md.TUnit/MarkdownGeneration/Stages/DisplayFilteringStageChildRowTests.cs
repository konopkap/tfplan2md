using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.MarkdownGeneration.Stages;

/// <summary>
/// Tests how <see cref="DisplayFilteringStage"/> hides unchanged child rows.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class DisplayFilteringStageChildRowTests
{
    /// <summary>
    /// Verifies unchanged rows are removed and counted (TC-01).
    /// </summary>
    [Test]
    public void Filter_ShowUnchangedFalse_RemovesUnchangedRows()
    {
        var parent = CreateParent("update", CreateGroup(ActionIcons.Add, ActionIcons.Update, ActionIcons.Delete, ActionIcons.Unchanged));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Select(row => row.ChangeIndicator).Should().Equal(ActionIcons.Add, ActionIcons.Update, ActionIcons.Delete);
        group.HiddenUnchangedCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies every removed row is counted (TC-02).
    /// </summary>
    [Test]
    public void Filter_ManyUnchanged_CountsAllRemoved()
    {
        var indicators = new[] { ActionIcons.Add, ActionIcons.Update }.Concat(Enumerable.Repeat(ActionIcons.Unchanged, 8)).ToArray();
        var parent = CreateParent("update", CreateGroup(indicators));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Should().HaveCount(2);
        group.HiddenUnchangedCount.Should().Be(8);
    }

    /// <summary>
    /// Verifies the stage leaves groups alone when unchanged values are shown (TC-03).
    /// </summary>
    [Test]
    public void Filter_ShowUnchangedTrue_LeavesGroupsUntouched()
    {
        var group = CreateGroup(ActionIcons.Add, ActionIcons.Unchanged);
        var parent = CreateParent("update", group);

        var result = Filter(parent, showUnchangedValues: true).ChildResourceGroups.Single();

        result.Should().Be(group);
        result.Rows.Should().HaveCount(2);
        result.HiddenUnchangedCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies a group made of inline rows is filtered (TC-04).
    /// </summary>
    [Test]
    public void Filter_InlineSourceRows_RemovesUnchanged()
    {
        var parent = CreateParent("update", CreateGroup(inlineAttribute: true, ActionIcons.Update, ActionIcons.Unchanged));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Should().ContainSingle().Which.ChangeIndicator.Should().Be(ActionIcons.Update);
        group.HasMixedSources.Should().BeFalse();
    }

    /// <summary>
    /// Verifies a group made of separate resources is filtered (TC-05).
    /// </summary>
    [Test]
    public void Filter_SeparateSourceRows_RemovesUnchanged()
    {
        var parent = CreateParent("update", CreateGroup(inlineAttribute: false, ActionIcons.Add, ActionIcons.Unchanged));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Should().ContainSingle().Which.ChangeIndicator.Should().Be(ActionIcons.Add);
    }

    /// <summary>
    /// Verifies both sources of a mixed group are filtered and the mixed flag survives (TC-06).
    /// </summary>
    [Test]
    public void Filter_MixedSourceGroup_RemovesUnchangedFromBothSources()
    {
        var group = CreateGroup(ActionIcons.Unchanged, ActionIcons.Add, ActionIcons.Unchanged, ActionIcons.Delete) with { HasMixedSources = true };
        var parent = CreateParent("update", group);

        var result = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        result.Rows.Should().HaveCount(2);
        result.HiddenUnchangedCount.Should().Be(2);
        result.HasMixedSources.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the remaining rows keep their relative order (TC-07).
    /// </summary>
    [Test]
    public void Filter_MixedIndicators_PreservesRelativeOrder()
    {
        var parent = CreateParent(
            "update",
            CreateGroup(ActionIcons.Add, ActionIcons.Unchanged, ActionIcons.Update, ActionIcons.Unchanged, ActionIcons.Delete));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Select(row => row.Values["name"]).Should().Equal("row-0", "row-2", "row-4");
    }

    /// <summary>
    /// Verifies create and delete parents are skipped even when a row is unchanged (TC-08).
    /// </summary>
    /// <param name="action">The parent action.</param>
    [Test]
    [Arguments("create")]
    [Arguments("delete")]
    public void Filter_CreateOrDeleteParent_DoesNotTouchGroups(string action)
    {
        var group = CreateGroup(ActionIcons.Add, ActionIcons.Unchanged);
        var parent = CreateParent(action, group);

        var result = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        result.Should().Be(group);
        result.HiddenUnchangedCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies a group whose rows are all unchanged is kept with an empty row list (TC-09).
    /// </summary>
    [Test]
    public void Filter_AllRowsUnchanged_KeepsEmptyGroupWithCountAndMixedFlag()
    {
        var group = CreateGroup(ActionIcons.Unchanged, ActionIcons.Unchanged, ActionIcons.Unchanged) with { HasMixedSources = true };
        var parent = CreateParent("update", group, CreateGroup(ActionIcons.Add));

        var result = Filter(parent, showUnchangedValues: false);

        result.ChildResourceGroups.Should().HaveCount(2);
        result.ChildResourceGroups[0].Rows.Should().BeEmpty();
        result.ChildResourceGroups[0].HiddenUnchangedCount.Should().Be(3);
        result.ChildResourceGroups[0].HasMixedSources.Should().BeTrue();
    }

    /// <summary>
    /// Verifies a no-op parent with only unchanged children is still dropped (TC-26).
    /// </summary>
    [Test]
    public void Filter_NoOpParentWithOnlyUnchangedChildren_IsStillFilteredOutAsNoOp()
    {
        var parent = CreateParent("no-op", CreateGroup(ActionIcons.Unchanged, ActionIcons.Unchanged));

        var result = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false, showUnchangedValues: false).Build([parent]);

        result.DisplayChanges.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies a no-op parent kept for one changed child hides the unchanged rows (TC-26, TC-29).
    /// </summary>
    [Test]
    public void Filter_NoOpParentWithOneChangedChild_IsKeptAndUnchangedRowsHidden()
    {
        var parent = CreateParent("no-op", CreateGroup(ActionIcons.Unchanged, ActionIcons.Update, ActionIcons.Unchanged));

        var result = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false, showUnchangedValues: false).Build([parent]);

        var group = result.DisplayChanges.Should().ContainSingle().Which.ChildResourceGroups.Single();
        group.Rows.Should().ContainSingle().Which.ChangeIndicator.Should().Be(ActionIcons.Update);
        group.HiddenUnchangedCount.Should().Be(2);
    }

    /// <summary>
    /// Verifies a replaced parent hides unchanged rows (TC-28).
    /// </summary>
    [Test]
    public void Filter_ReplaceParent_RemovesUnchangedChildRows()
    {
        var parent = CreateParent("replace", CreateGroup(ActionIcons.Update, ActionIcons.Unchanged));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Should().ContainSingle();
        group.HiddenUnchangedCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies the input groups and their row lists are not changed (TC-90).
    /// </summary>
    [Test]
    public void Filter_DoesNotMutateInputGroups()
    {
        var group = CreateGroup(ActionIcons.Add, ActionIcons.Unchanged);
        var rows = group.Rows;
        var parent = CreateParent("update", group);
        var originalGroups = parent.ChildResourceGroups;

        Filter(parent, showUnchangedValues: false);

        originalGroups.Should().ContainSingle().Which.Should().BeSameAs(group);
        group.Rows.Should().BeSameAs(rows);
        group.Rows.Should().HaveCount(2);
        group.HiddenUnchangedCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies an unknown parent action is filtered like an update (TC-91).
    /// </summary>
    [Test]
    public void Filter_UnknownParentAction_FiltersLikeUpdate()
    {
        var parent = CreateParent("unknown", CreateGroup(ActionIcons.Update, ActionIcons.Unchanged));

        var group = Filter(parent, showUnchangedValues: false).ChildResourceGroups.Single();

        group.Rows.Should().ContainSingle();
        group.HiddenUnchangedCount.Should().Be(1);
    }

    /// <summary>
    /// Runs the stage on one parent and returns the resulting display change.
    /// </summary>
    /// <param name="parent">The parent change.</param>
    /// <param name="showUnchangedValues">Whether unchanged values are shown.</param>
    /// <returns>The single display change.</returns>
    private static ResourceChangeModel Filter(ResourceChangeModel parent, bool showUnchangedValues)
    {
        var stage = new DisplayFilteringStage(ignoreAzureIdCaseChanges: false, showUnchangedValues);
        return stage.Build([parent]).DisplayChanges.Single();
    }

    /// <summary>
    /// Creates a parent resource change with child groups.
    /// </summary>
    /// <param name="action">The parent action.</param>
    /// <param name="groups">The child groups.</param>
    /// <returns>The parent change.</returns>
    private static ResourceChangeModel CreateParent(string action, params ChildResourceGroup[] groups)
    {
        return new ResourceChangeModel
        {
            Address = "test_parent.example",
            Type = "test_parent",
            Name = "example",
            ProviderName = "provider",
            Action = action,
            ActionSymbol = ActionIcons.Update,
            ModuleAddress = string.Empty,
            AttributeChanges = [],
            ChildResourceGroups = groups
        };
    }

    /// <summary>
    /// Creates a child group whose rows have the given change indicators.
    /// </summary>
    /// <param name="indicators">The row change indicators in order.</param>
    /// <returns>The child group.</returns>
    private static ChildResourceGroup CreateGroup(params string[] indicators) => CreateGroup(inlineAttribute: false, indicators);

    /// <summary>
    /// Creates a child group whose rows have the given change indicators.
    /// </summary>
    /// <param name="inlineAttribute">Whether rows come from the inline attribute rather than separate resources.</param>
    /// <param name="indicators">The row change indicators in order.</param>
    /// <returns>The child group.</returns>
    private static ChildResourceGroup CreateGroup(bool inlineAttribute, params string[] indicators)
    {
        return new ChildResourceGroup
        {
            Label = "Routes",
            ItemNameSingular = "route",
            ItemNamePlural = "routes",
            Columns = [new ChildTableColumn("Name", "name")],
            Rows = indicators
                .Select((indicator, index) => new ChildResourceRow
                {
                    ChangeIndicator = indicator,
                    Values = new Dictionary<string, string> { ["name"] = $"row-{index}" },
                    TerraformResource = inlineAttribute ? "route attribute" : $"test_child.row_{index}"
                })
                .ToList()
        };
    }
}
