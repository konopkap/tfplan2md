using System.Text.Json.Nodes;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Builds minimal Terraform plan JSON documents for parent-child rendering scenarios.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
internal sealed class PlanJsonBuilder
{
    /// <summary>
    /// Collected resource change entries in insertion order.
    /// </summary>
    private readonly JsonArray _changes = [];

    /// <summary>
    /// Adds a resource change to the plan.
    /// </summary>
    /// <param name="address">The Terraform resource address, such as <c>azurerm_route.a</c>.</param>
    /// <param name="action">One of create, update, delete, no-op or replace.</param>
    /// <param name="before">The state before the change, or <c>null</c>.</param>
    /// <param name="after">The state after the change, or <c>null</c>.</param>
    /// <returns>The builder for chaining.</returns>
    internal PlanJsonBuilder Add(string address, string action, JsonNode? before, JsonNode? after)
    {
        var separator = address.IndexOf('.', StringComparison.Ordinal);
        var type = address[..separator];
        var name = address[(separator + 1)..];
        var provider = type[..type.IndexOf('_', StringComparison.Ordinal)];

        JsonArray actions = action switch
        {
            "replace" => ["delete", "create"],
            _ => [action]
        };

        _changes.Add(new JsonObject
        {
            ["address"] = address,
            ["module_address"] = null,
            ["mode"] = "managed",
            ["type"] = type,
            ["name"] = name,
            ["provider_name"] = $"registry.terraform.io/hashicorp/{provider}",
            ["change"] = new JsonObject
            {
                ["actions"] = actions,
                ["before"] = before?.DeepClone(),
                ["after"] = after?.DeepClone(),
                ["after_unknown"] = new JsonObject(),
                ["before_sensitive"] = new JsonObject(),
                ["after_sensitive"] = new JsonObject()
            }
        });

        return this;
    }

    /// <summary>
    /// Adds an unchanged resource whose before and after state are identical.
    /// </summary>
    /// <param name="address">The Terraform resource address.</param>
    /// <param name="state">The state used for before and after.</param>
    /// <returns>The builder for chaining.</returns>
    internal PlanJsonBuilder AddNoOp(string address, JsonNode state) => Add(address, "no-op", state, state);

    /// <summary>
    /// Serializes the plan to JSON text.
    /// </summary>
    /// <returns>The Terraform plan JSON.</returns>
    internal string Build()
    {
        var plan = new JsonObject
        {
            ["format_version"] = "1.2",
            ["terraform_version"] = "1.14.0",
            ["resource_changes"] = _changes.DeepClone()
        };

        return plan.ToJsonString();
    }
}
