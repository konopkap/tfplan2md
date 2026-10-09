using System.IO;
using System.Text.RegularExpressions;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Assertions.Exceptions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Architecture;

/// <summary>
/// Keeps provider-specific child nouns out of the core markdown generation code.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
public class ChildItemNameBoundaryTests
{
    /// <summary>
    /// Matches a string literal that is exactly one of the provider child nouns.
    /// </summary>
    private static readonly Regex ProviderNounLiteral = new(
        "\"(route|routes|subnet|subnets|security rule|security rules)\"",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Verifies core markdown generation code contains no hard-coded provider child nouns (TC-101).
    /// </summary>
    [Test]
    public void MarkdownGeneration_Sources_DoNotHardCodeProviderChildNouns()
    {
        var directory = Path.Combine(DemoPaths.RepositoryRoot, "src", "Oocx.TfPlan2Md", "MarkdownGeneration");
        var violations = Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => (path, line, number: index + 1))
                .Where(entry => ProviderNounLiteral.IsMatch(entry.line)))
            .Select(entry => $"{Path.GetFileName(entry.path)}:{entry.number}: {entry.line.Trim()}")
            .ToList();

        if (violations.Count > 0)
        {
            throw new AssertionException(
                "Provider child nouns belong in the provider module registrations, not in MarkdownGeneration:\n"
                + string.Join('\n', violations));
        }
    }
}
