using AwesomeAssertions;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// End-to-end tests that every parent-child pattern hides unchanged rows by default and names its own child noun.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
public class HiddenChildRowsProviderPatternTests
{
    /// <summary>
    /// Verifies the note names the child type for every pattern, singular for one and plural for several (TC-33 to TC-39, TC-42, TC-43).
    /// </summary>
    /// <param name="scenario">The scenario name.</param>
    /// <param name="note">The expected note.</param>
    /// <param name="hiddenName">A name that must not appear because its row is unchanged.</param>
    [Test]
    [Arguments("vnet-inline-1", "_1 unchanged subnet hidden._", "snet-keep-1")]
    [Arguments("vnet-inline-2", "_2 unchanged subnets hidden._", "snet-keep-2")]
    [Arguments("vnet-separate-1", "_1 unchanged subnet hidden._", "snet-keep-1")]
    [Arguments("vnet-separate-2", "_2 unchanged subnets hidden._", "snet-keep-2")]
    [Arguments("dns-1", "_1 unchanged DNS record hidden._", "keep-1")]
    [Arguments("dns-2", "_2 unchanged DNS records hidden._", "keep-2")]
    [Arguments("nsg-inline-1", "_1 unchanged security rule hidden._", "keep-1")]
    [Arguments("nsg-inline-2", "_2 unchanged security rules hidden._", "keep-2")]
    [Arguments("nsg-separate-1", "_1 unchanged security rule hidden._", "keep-1")]
    [Arguments("nsg-separate-2", "_2 unchanged security rules hidden._", "keep-2")]
    [Arguments("ad-members-1", "_1 unchanged member hidden._", "user-keep-1")]
    [Arguments("ad-members-2", "_2 unchanged members hidden._", "user-keep-2")]
    [Arguments("devops-team-members-2", "_2 unchanged members hidden._", "MemberKeep2")]
    [Arguments("devops-team-admins-1", "_1 unchanged administrator hidden._", "AdminKeep1")]
    [Arguments("devops-team-admins-2", "_2 unchanged administrators hidden._", "AdminKeep2")]
    [Arguments("devops-group-members-1", "_1 unchanged member hidden._", "MemberKeep1")]
    [Arguments("devops-group-members-2", "_2 unchanged members hidden._", "MemberKeep2")]
    public void Render_Default_HidesUnchangedRowsAndNamesChildType(string scenario, string note, string hiddenName)
    {
        var markdown = ChildTableRendering.Render(Plan(scenario), showUnchangedValues: false);

        markdown.Should().Contain(note);
        markdown.Should().NotContain(hiddenName);
        markdown.Should().NotContain("| ⏺️ |");
    }

    /// <summary>
    /// Verifies the option shows every row and writes no note for every pattern (TC-45).
    /// </summary>
    /// <param name="scenario">The scenario name.</param>
    [Test]
    [Arguments("vnet-inline-2")]
    [Arguments("vnet-separate-2")]
    [Arguments("dns-2")]
    [Arguments("nsg-inline-2")]
    [Arguments("nsg-separate-2")]
    [Arguments("ad-members-2")]
    [Arguments("devops-team-members-2")]
    [Arguments("devops-group-members-2")]
    public void Render_ShowUnchanged_ShowsUnchangedRowsAndNoNote(string scenario)
    {
        var markdown = ChildTableRendering.Render(Plan(scenario), showUnchangedValues: true);

        markdown.Should().Contain("| ⏺️ |");
        markdown.Should().NotContain("hidden.");
    }

    /// <summary>
    /// Verifies a table whose rows all changed has no note (TC-44).
    /// </summary>
    [Test]
    public void Render_NothingUnchanged_WritesNoNote()
    {
        foreach (var scenario in new[] { "vnet-inline-0", "vnet-separate-0", "dns-0", "nsg-inline-0", "nsg-separate-0", "ad-members-0", "devops-team-members-0", "devops-group-members-0" })
        {
            ChildTableRendering.Render(Plan(scenario), showUnchangedValues: false).Should().NotContain("hidden.", scenario);
        }
    }

    /// <summary>
    /// Verifies inline NSG rules with only tags changing show the attribute table and no rules section or empty block (TC-84).
    /// </summary>
    [Test]
    public void Render_NsgAllInlineRulesUnchangedWithTagChange_FallsBackToAttributeTable()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.NsgAllInlineRulesUnchanged(2), showUnchangedValues: false);

        markdown.Should().Contain("tags.owner");
        markdown.Should().NotContain("#### Security Rules").And.NotContain("| Change |").And.NotContain("hidden.");
        markdown.Should().NotContain("<br>\n\n</details>");
    }

    /// <summary>
    /// Verifies subnets and security rules with every row unchanged omit the section by default and show it with the option (TC-94).
    /// </summary>
    [Test]
    public void Render_AllSubnetsUnchanged_OmitsSectionByDefault()
    {
        var plan = ChildTableScenarios.VnetAllInlineSubnetsUnchanged(2);

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        hidden.Should().NotContain("#### Subnets").And.NotContain("hidden.");
        shown.Should().Contain("#### Subnets").And.Contain("| ⏺️ |");
    }

    /// <summary>
    /// Verifies the Azure AD summary counts members of every kind in both modes (TC-51).
    /// </summary>
    [Test]
    public void Render_AzureAdGroup_SummaryChildCountsAreSameInBothModes()
    {
        var plan = ChildTableScenarios.AzureAdGroupMembers(2);

        var hidden = ChildTableRendering.Render(plan, showUnchangedValues: false);
        var shown = ChildTableRendering.Render(plan, showUnchangedValues: true);

        MemberSegments(hidden).Should().Equal(MemberSegments(shown));
        MemberSegments(hidden).Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies several child groups of one parent are filtered and noted independently (TC-38).
    /// </summary>
    [Test]
    public void Render_DevOpsTeam_NotesMembersAndAdministratorsSeparately()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.DevOpsTeamMembersAndAdministrators(3, 1), showUnchangedValues: false);

        markdown.Should().Contain("_3 unchanged members hidden._").And.Contain("_1 unchanged administrator hidden._");
        markdown.Should().Contain("#### Members").And.Contain("#### Administrators");
    }

    /// <summary>
    /// Verifies a separate NSG rule that is the only change still shows next to the parent attributes (TC-34, TC-99).
    /// </summary>
    [Test]
    public void Render_SeparateNsgRuleCreated_ShowsCreatedRuleAndNote()
    {
        var markdown = ChildTableRendering.Render(ChildTableScenarios.NsgSeparateRules(1), showUnchangedValues: false);

        markdown.Should().Contain("#### Security Rules").And.Contain("azurerm_network_security_rule.created");
        markdown.Should().NotContain("keep-1");
    }

    /// <summary>
    /// Returns the plan JSON for a scenario code such as <c>vnet-inline-2</c>.
    /// </summary>
    /// <param name="scenario">The scenario code: pattern and unchanged count.</param>
    /// <returns>The plan JSON.</returns>
    private static string Plan(string scenario)
    {
        var separator = scenario.LastIndexOf('-');
        var pattern = scenario[..separator];
        var count = int.Parse(scenario[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture);

        return pattern switch
        {
            "vnet-inline" => ChildTableScenarios.VnetInlineSubnets(count),
            "vnet-separate" => ChildTableScenarios.VnetSeparateSubnets(count),
            "dns" => ChildTableScenarios.DnsZoneRecords(count),
            "nsg-inline" => ChildTableScenarios.NsgInlineRules(count),
            "nsg-separate" => ChildTableScenarios.NsgSeparateRules(count),
            "ad-members" => ChildTableScenarios.AzureAdGroupMembers(count),
            "devops-team-members" => ChildTableScenarios.DevOpsTeamMembersAndAdministrators(count, 0),
            "devops-team-admins" => ChildTableScenarios.DevOpsTeamMembersAndAdministrators(0, count),
            "devops-group-members" => ChildTableScenarios.DevOpsGroupMembers(count),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario.")
        };
    }

    /// <summary>
    /// Extracts the summary segments that mention members.
    /// </summary>
    /// <param name="markdown">The rendered markdown.</param>
    /// <returns>The member count segments of the summary line.</returns>
    private static List<string> MemberSegments(string markdown)
    {
        return markdown.Split('\n')
            .Where(line => line.StartsWith("<summary>", StringComparison.Ordinal))
            .SelectMany(line => line.Split(" | "))
            .Where(segment => segment.Contains("member", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
