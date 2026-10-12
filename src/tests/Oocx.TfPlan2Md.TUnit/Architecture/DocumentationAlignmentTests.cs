using System.Text.RegularExpressions;
using AwesomeAssertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Documentation regression guards for the active provider-architecture guidance.
/// Related feature: docs/features/110-refactoring-opportunities/test-plan.md.
/// </summary>
[Category("Unit")]
public class DocumentationAlignmentTests
{
    [Test]
    public async Task DocumentationFiles_DoNotReferToIProviderModuleAsActiveContract()
    {
        var activeDocs = new[]
        {
            GetRepositoryFilePath("docs", "features.md"),
            GetRepositoryFilePath("docs", "architecture.md")
        };

        foreach (var path in activeDocs)
        {
            var content = await File.ReadAllTextAsync(path);
            content.Should().NotContain("IProviderModule", because: $"{Path.GetFileName(path)} should describe the current provider contract");
        }
    }

    [Test]
    public async Task Adr006_DescribesIProvider_NotIProviderModule()
    {
        var path = GetRepositoryFilePath("docs", "adr-006-dependency-injection.md");
        var content = await File.ReadAllTextAsync(path);

        content.Should().Contain("IProvider", because: "ADR-006 should describe the current provider contract");
        content.Should().NotContain("implement `IProviderModule`", because: "ADR-006 should not instruct contributors to use the deleted contract");
    }

    [Test]
    public async Task ActiveDocumentationFiles_DoNotInstructImplementingIProviderModule()
    {
        var activeDocs = new[]
        {
            GetRepositoryFilePath("docs", "features.md"),
            GetRepositoryFilePath("docs", "adr-006-dependency-injection.md"),
            GetRepositoryFilePath("docs", "architecture.md"),
            GetRepositoryFilePath("CONTRIBUTING.md"),
            GetRepositoryFilePath("README.md")
        };

        var pattern = new Regex(
            "implement.*IProviderModule|IProviderModule.*extend",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        foreach (var path in activeDocs)
        {
            var content = await File.ReadAllTextAsync(path);
            pattern.IsMatch(content).Should().BeFalse(because: $"{Path.GetFileName(path)} should not instruct contributors to use the deleted provider contract");
        }
    }

    /// <summary>
    /// Verifies the NSG section no longer says unchanged security rules are shown for completeness and
    /// says they are hidden by default (TC-70).
    /// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
    /// </summary>
    [Test]
    public async Task FeaturesDoc_NsgSection_DescribesUnchangedRulesAsHiddenByDefault()
    {
        var content = await File.ReadAllTextAsync(GetRepositoryFilePath("docs", "features.md"));
        const string heading = "#### Network Security Groups";
        var start = content.IndexOf(heading, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, because: "features.md documents the NSG renderer");
        var end = content.IndexOf("\n#### ", start + heading.Length, StringComparison.Ordinal);
        var section = end == -1 ? content[start..] : content[start..end];

        section.Should().NotContain("for completeness");
        section.Should().Contain("Hidden by default");
        section.Should().Contain("--show-unchanged-values");
    }

    /// <summary>
    /// Verifies the option tables in README.md and features.md say the option also includes unchanged child rows (TC-72).
    /// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
    /// </summary>
    /// <param name="segment">Path segment of the document, relative to the repository root.</param>
    [Test]
    [Arguments("README.md")]
    [Arguments("docs/features.md")]
    public async Task OptionTable_ShowUnchangedValues_MentionsUnchangedChildRows(string segment)
    {
        var content = await File.ReadAllTextAsync(GetRepositoryFilePath(segment.Split('/')));

        var row = content.Split('\n').Single(line => line.StartsWith("| `--show-unchanged-values`", StringComparison.Ordinal));

        row.Should().Contain("unchanged child rows");
    }

    private static string GetRepositoryFilePath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", Path.Combine(segments)));
    }
}
