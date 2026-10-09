namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Child item nouns that the AzureRM parent-child registrations and the NSG inline renderer share,
/// so the hidden-rows note reads the same on both paths.
/// Related feature: docs/features/149-hide-unchanged-child-rows/architecture.md.
/// </summary>
internal static class AzureRmChildItemNames
{
    /// <summary>
    /// Singular noun for one network security rule.
    /// </summary>
    internal const string SecurityRuleSingular = "security rule";

    /// <summary>
    /// Plural noun for several network security rules.
    /// </summary>
    internal const string SecurityRulePlural = "security rules";
}
