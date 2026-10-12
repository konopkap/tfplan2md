using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Providers.MsGraph;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Renders plan JSON through the full provider set, as the CLI does, with deterministic metadata.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
internal static class ChildTableRendering
{
    /// <summary>
    /// Renders plan JSON to markdown.
    /// </summary>
    /// <param name="planJson">The Terraform plan JSON.</param>
    /// <param name="showUnchangedValues">Whether unchanged values and child rows are included.</param>
    /// <param name="options">Optional full option set; overrides <paramref name="showUnchangedValues"/> when supplied.</param>
    /// <returns>The rendered markdown.</returns>
    /// <param name="codeAnalysis">Optional code analysis input.</param>
    internal static string Render(string planJson, bool showUnchangedValues, ReportModelBuilderOptions? options = null, CodeAnalysisInput? codeAnalysis = null)
    {
        var effective = options ?? new ReportModelBuilderOptions(ShowUnchangedValues: showUnchangedValues);
        var registry = CreateProviderRegistry(effective);
        var contributions = registry.CreateContributionSet();
        var model = BuildModel(planJson, effective, registry, contributions, codeAnalysis);
        var renderer = new MarkdownRenderer(providerRegistry: registry, providerContributions: contributions);
        return renderer.Render(model);
    }

    /// <summary>
    /// Builds the report model for plan JSON.
    /// </summary>
    /// <param name="planJson">The Terraform plan JSON.</param>
    /// <param name="showUnchangedValues">Whether unchanged values and child rows are included.</param>
    /// <returns>The report model.</returns>
    internal static ReportModel BuildModel(string planJson, bool showUnchangedValues)
    {
        var options = new ReportModelBuilderOptions(ShowUnchangedValues: showUnchangedValues);
        var registry = CreateProviderRegistry(options);
        return BuildModel(planJson, options, registry, registry.CreateContributionSet());
    }

    /// <summary>
    /// Builds the report model with explicit services.
    /// </summary>
    /// <param name="planJson">The Terraform plan JSON.</param>
    /// <param name="options">The builder options.</param>
    /// <param name="registry">The provider registry.</param>
    /// <param name="contributions">The provider contribution set.</param>
    /// <returns>The report model.</returns>
    /// <param name="codeAnalysis">Optional code analysis input.</param>
    private static ReportModel BuildModel(
        string planJson,
        ReportModelBuilderOptions options,
        ProviderRegistry registry,
        ProviderContributionSet contributions,
        CodeAnalysisInput? codeAnalysis = null)
    {
        var plan = new TerraformPlanParser().Parse(planJson);
        var builder = new ReportModelBuilder(
            options,
            new ReportModelBuilderServices(
                MetadataProvider: TestMetadataProvider.Instance,
                ProviderRegistry: registry,
                ProviderContributions: contributions,
                CodeAnalysisInput: codeAnalysis));
        return builder.Build(plan);
    }

    /// <summary>
    /// Registers every provider module without mappers.
    /// </summary>
    /// <param name="options">The builder options that select the large value format.</param>
    /// <returns>The populated registry.</returns>
    private static ProviderRegistry CreateProviderRegistry(ReportModelBuilderOptions options)
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzApiModule());
        registry.RegisterProvider(new AzureADModule());
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(options.RenderTarget),
            principalMapper: new NullPrincipalMapper()));
        registry.RegisterProvider(new AzureDevOpsModule());
        registry.RegisterProvider(new MsGraphModule());
        return registry;
    }
}
