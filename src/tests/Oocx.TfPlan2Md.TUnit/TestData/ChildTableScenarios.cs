using System.Text.Json.Nodes;

namespace Oocx.TfPlan2Md.Tests.TestData;

/// <summary>
/// Plan JSON factories for the parent-child scenarios whose unchanged child rows are hidden by default.
/// Related feature: docs/features/149-hide-unchanged-child-rows/test-plan.md.
/// </summary>
/// <remarks>
/// Every factory takes the number of unchanged children so tests can pin the singular and plural note.
/// All names and addresses are fictitious.
/// </remarks>
internal static class ChildTableScenarios
{
    /// <summary>
    /// Parent route table name referenced by separate route resources.
    /// </summary>
    internal const string RouteTableName = "rt-app";

    /// <summary>
    /// Parent virtual network name referenced by separate subnet resources.
    /// </summary>
    internal const string VnetName = "vnet-spoke";

    /// <summary>
    /// Parent network security group name referenced by separate rule resources.
    /// </summary>
    internal const string NsgName = "nsg-app";

    /// <summary>
    /// Parent DNS zone name referenced by separate record resources.
    /// </summary>
    internal const string DnsZoneName = "example.test";

    /// <summary>
    /// Create action name.
    /// </summary>
    private const string Create = "create";

    /// <summary>
    /// Update action name.
    /// </summary>
    private const string Update = "update";

    /// <summary>
    /// Delete action name.
    /// </summary>
    private const string Delete = "delete";

    /// <summary>
    /// Replace pseudo-action understood by <see cref="PlanJsonBuilder"/>.
    /// </summary>
    private const string Replace = "replace";

    /// <summary>
    /// Builds a route table update with separate <c>azurerm_route</c> children: one created, one updated, one deleted and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged routes.</param>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableSeparate(int unchanged = 1)
    {
        var builder = new PlanJsonBuilder().Add("azurerm_route_table.app", Update, RouteTable("team-a"), RouteTable("team-b"));
        AddSeparateRoutes(builder, unchanged);
        return builder.Build();
    }

    /// <summary>
    /// Builds a route table update with inline <c>route</c> children: one added, one modified, one removed and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged routes.</param>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableInline(int unchanged = 2)
    {
        var before = new JsonArray();
        var after = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            before.Add(Route($"keep-{index}", $"10.1.{index}.0/24", inline: true));
            after.Add(Route($"keep-{index}", $"10.1.{index}.0/24", inline: true));
        }

        before.Add(Route("modified", "10.2.0.0/16", inline: true));
        after.Add(Route("modified", "10.2.0.0/20", inline: true));
        before.Add(Route("removed", "10.3.0.0/16", inline: true));
        after.Add(Route("added", "10.4.0.0/16", inline: true));

        return new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Update, RouteTable("team-a", before), RouteTable("team-b", after))
            .Build();
    }

    /// <summary>
    /// Builds a route table whose routes are managed both inline and as separate resources, with one unchanged route of each kind.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableMixedSources()
    {
        var before = new JsonArray { Route("inline-keep", "10.1.0.0/24", inline: true) };
        var after = new JsonArray
        {
            Route("inline-keep", "10.1.0.0/24", inline: true),
            Route("inline-added", "10.1.1.0/24", inline: true)
        };

        var builder = new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Update, RouteTable("team-a", before), RouteTable("team-b", after))
            .AddNoOp("azurerm_route.separate_keep", Route("separate-keep", "10.5.0.0/24"))
            .Add("azurerm_route.separate_changed", Update, Route("separate-changed", "10.6.0.0/24"), Route("separate-changed", "10.6.0.0/25"));
        return builder.Build();
    }

    /// <summary>
    /// Builds a route table update where only tags change and every inline route is unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged inline routes.</param>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableAllInlineUnchanged(int unchanged = 2)
    {
        var routes = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            routes.Add(Route($"keep-{index}", $"10.1.{index}.0/24", inline: true));
        }

        return new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Update, RouteTable("team-a", routes), RouteTable("team-b", routes))
            .Build();
    }

    /// <summary>
    /// Builds a route table update where only tags change and every separate route is unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged separate routes.</param>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableAllSeparateUnchanged(int unchanged = 2)
    {
        var builder = new PlanJsonBuilder().Add("azurerm_route_table.app", Update, RouteTable("team-a"), RouteTable("team-b"));
        for (var index = 1; index <= unchanged; index++)
        {
            builder.AddNoOp($"azurerm_route.keep_{index}", Route($"keep-{index}", $"10.1.{index}.0/24"));
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a route table update with mixed inline and separate routes that are all unchanged.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableMixedAllUnchanged()
    {
        var routes = new JsonArray { Route("inline-keep", "10.1.0.0/24", inline: true) };
        return new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Update, RouteTable("team-a", routes), RouteTable("team-b", routes))
            .AddNoOp("azurerm_route.separate_keep", Route("separate-keep", "10.5.0.0/24"))
            .Build();
    }

    /// <summary>
    /// Builds a created route table with separate created routes.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableCreate()
    {
        return new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Create, null, RouteTable("team-a"))
            .Add("azurerm_route.first", Create, null, Route("first", "10.1.0.0/24"))
            .Add("azurerm_route.second", Create, null, Route("second", "10.2.0.0/24"))
            .Build();
    }

    /// <summary>
    /// Builds a deleted route table with separate deleted routes.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableDelete()
    {
        return new PlanJsonBuilder()
            .Add("azurerm_route_table.app", Delete, RouteTable("team-a"), null)
            .Add("azurerm_route.first", Delete, Route("first", "10.1.0.0/24"), null)
            .Add("azurerm_route.second", Delete, Route("second", "10.2.0.0/24"), null)
            .Build();
    }

    /// <summary>
    /// Builds a replaced route table with separate children: one updated and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged routes.</param>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableReplace(int unchanged = 2)
    {
        var builder = new PlanJsonBuilder().Add("azurerm_route_table.app", Replace, RouteTable("team-a"), RouteTable("team-b"));
        builder.Add("azurerm_route.changed", Update, Route("changed", "10.2.0.0/16"), Route("changed", "10.2.0.0/20"));
        for (var index = 1; index <= unchanged; index++)
        {
            builder.AddNoOp($"azurerm_route.keep_{index}", Route($"keep-{index}", $"10.1.{index}.0/24"));
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a no-op route table kept in the report because one separate route changes, with one unchanged route.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableNoOpParentWithChildChange()
    {
        return new PlanJsonBuilder()
            .AddNoOp("azurerm_route_table.app", RouteTable("team-a"))
            .Add("azurerm_route.changed", Update, Route("changed", "10.2.0.0/16"), Route("changed", "10.2.0.0/20"))
            .AddNoOp("azurerm_route.keep", Route("keep", "10.1.0.0/24"))
            .Build();
    }

    /// <summary>
    /// Builds a no-op route table whose separate routes are all unchanged.
    /// </summary>
    /// <returns>The plan JSON.</returns>
    internal static string RouteTableNoOpAllUnchanged()
    {
        return new PlanJsonBuilder()
            .AddNoOp("azurerm_route_table.app", RouteTable("team-a"))
            .AddNoOp("azurerm_route.keep_1", Route("keep-1", "10.1.0.0/24"))
            .AddNoOp("azurerm_route.keep_2", Route("keep-2", "10.2.0.0/24"))
            .Build();
    }

    /// <summary>
    /// Builds a virtual network update with inline subnets: one added, one modified, one removed and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged subnets.</param>
    /// <returns>The plan JSON.</returns>
    internal static string VnetInlineSubnets(int unchanged = 2)
    {
        var before = new JsonArray();
        var after = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            before.Add(Subnet($"snet-keep-{index}", $"10.0.{index}.0/24"));
            after.Add(Subnet($"snet-keep-{index}", $"10.0.{index}.0/24"));
        }

        before.Add(Subnet("snet-modified", "10.0.100.0/24"));
        after.Add(Subnet("snet-modified", "10.0.100.0/23"));
        before.Add(Subnet("snet-removed", "10.0.101.0/24"));
        after.Add(Subnet("snet-added", "10.0.102.0/24"));

        return new PlanJsonBuilder()
            .Add("azurerm_virtual_network.spoke", Update, Vnet("team-a", before), Vnet("team-b", after))
            .Build();
    }

    /// <summary>
    /// Builds a virtual network update where only tags change and every inline subnet is unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged subnets.</param>
    /// <returns>The plan JSON.</returns>
    internal static string VnetAllInlineSubnetsUnchanged(int unchanged = 2)
    {
        var subnets = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            subnets.Add(Subnet($"snet-keep-{index}", $"10.0.{index}.0/24"));
        }

        return new PlanJsonBuilder()
            .Add("azurerm_virtual_network.spoke", Update, Vnet("team-a", subnets), Vnet("team-b", subnets))
            .Build();
    }

    /// <summary>
    /// Builds a virtual network update with separate subnets: one created, one updated, one deleted and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged subnets.</param>
    /// <returns>The plan JSON.</returns>
    internal static string VnetSeparateSubnets(int unchanged = 1)
    {
        var builder = new PlanJsonBuilder()
            .Add("azurerm_virtual_network.spoke", Update, Vnet("team-a"), Vnet("team-b"))
            .Add("azurerm_subnet.created", Create, null, SeparateSubnet("snet-created", "10.0.50.0/24"))
            .Add("azurerm_subnet.updated", Update, SeparateSubnet("snet-updated", "10.0.51.0/24"), SeparateSubnet("snet-updated", "10.0.51.0/23"))
            .Add("azurerm_subnet.deleted", Delete, SeparateSubnet("snet-deleted", "10.0.52.0/24"), null);
        for (var index = 1; index <= unchanged; index++)
        {
            builder.AddNoOp($"azurerm_subnet.keep_{index}", SeparateSubnet($"snet-keep-{index}", $"10.0.{index}.0/24"));
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a DNS zone update with separate records of two types: A and CNAME changed, A and TXT unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged records, alternating between A and TXT records.</param>
    /// <returns>The plan JSON.</returns>
    internal static string DnsZoneRecords(int unchanged = 2)
    {
        var builder = new PlanJsonBuilder()
            .Add("azurerm_dns_zone.example", Update, DnsZone("team-a"), DnsZone("team-b"))
            .Add("azurerm_dns_a_record.created", Create, null, DnsARecord("created", "192.0.2.10"))
            .Add("azurerm_dns_cname_record.updated", Update, DnsCnameRecord("updated", "old.example.test"), DnsCnameRecord("updated", "new.example.test"));
        for (var index = 1; index <= unchanged; index++)
        {
            if (index % 2 == 1)
            {
                builder.AddNoOp($"azurerm_dns_a_record.keep_{index}", DnsARecord($"keep-{index}", $"192.0.2.{index}"));
            }
            else
            {
                builder.AddNoOp($"azurerm_dns_txt_record.keep_{index}", DnsTxtRecord($"keep-{index}", "v=spf1 -all"));
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds an NSG update with inline rules: one modified and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged inline rules.</param>
    /// <returns>The plan JSON.</returns>
    internal static string NsgInlineRules(int unchanged = 2)
    {
        var before = new JsonArray();
        var after = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            before.Add(NsgRule($"keep-{index}", 100 + index, "443"));
            after.Add(NsgRule($"keep-{index}", 100 + index, "443"));
        }

        before.Add(NsgRule("modified", 200, "80"));
        after.Add(NsgRule("modified", 200, "8080"));

        return new PlanJsonBuilder()
            .Add("azurerm_network_security_group.app", Update, Nsg("team-a", before), Nsg("team-b", after))
            .Build();
    }

    /// <summary>
    /// Builds an NSG update where only tags change and every inline rule is unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged inline rules.</param>
    /// <returns>The plan JSON.</returns>
    internal static string NsgAllInlineRulesUnchanged(int unchanged = 2)
    {
        var rules = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            rules.Add(NsgRule($"keep-{index}", 100 + index, "443"));
        }

        return new PlanJsonBuilder()
            .Add("azurerm_network_security_group.app", Update, Nsg("team-a", rules), Nsg("team-b", rules))
            .Build();
    }

    /// <summary>
    /// Builds an NSG update with separate rules: one created and the given number unchanged.
    /// </summary>
    /// <param name="unchanged">Number of unchanged separate rules.</param>
    /// <returns>The plan JSON.</returns>
    internal static string NsgSeparateRules(int unchanged = 1)
    {
        var builder = new PlanJsonBuilder()
            .Add("azurerm_network_security_group.app", Update, Nsg("team-a"), Nsg("team-b"))
            .Add("azurerm_network_security_rule.created", Create, null, SeparateNsgRule("created", 300, "22"));
        for (var index = 1; index <= unchanged; index++)
        {
            builder.AddNoOp($"azurerm_network_security_rule.keep_{index}", SeparateNsgRule($"keep-{index}", 100 + index, "443"));
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds an Azure AD group update with inline members (one added, one removed, the given number unchanged) and one separate member created.
    /// </summary>
    /// <param name="unchanged">Number of unchanged inline members.</param>
    /// <returns>The plan JSON.</returns>
    internal static string AzureAdGroupMembers(int unchanged = 2)
    {
        var before = new JsonArray();
        var after = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            before.Add($"user-keep-{index}");
            after.Add($"user-keep-{index}");
        }

        before.Add("user-removed");
        after.Add("user-added");

        return new PlanJsonBuilder()
            .Add("azuread_group.platform", Update, AdGroup(before), AdGroup(after))
            .Add("azuread_group_member.separate", Create, null, new JsonObject { ["group_object_id"] = "group-100", ["member_object_id"] = "user-separate" })
            .Build();
    }

    /// <summary>
    /// Builds an Azure DevOps team update with inline members and administrators, each with one added and the given numbers unchanged.
    /// </summary>
    /// <param name="unchangedMembers">Number of unchanged members.</param>
    /// <param name="unchangedAdministrators">Number of unchanged administrators.</param>
    /// <returns>The plan JSON.</returns>
    internal static string DevOpsTeamMembersAndAdministrators(int unchangedMembers = 2, int unchangedAdministrators = 1)
    {
        var membersBefore = new JsonArray();
        var membersAfter = new JsonArray();
        for (var index = 1; index <= unchangedMembers; index++)
        {
            membersBefore.Add($"aadgp.Uy0.MemberKeep{index}");
            membersAfter.Add($"aadgp.Uy0.MemberKeep{index}");
        }

        membersAfter.Add("aadgp.Uy0.MemberAdded");

        var adminsBefore = new JsonArray();
        var adminsAfter = new JsonArray();
        for (var index = 1; index <= unchangedAdministrators; index++)
        {
            adminsBefore.Add($"aadgp.Uy0.AdminKeep{index}");
            adminsAfter.Add($"aadgp.Uy0.AdminKeep{index}");
        }

        adminsAfter.Add("aadgp.Uy0.AdminAdded");

        return new PlanJsonBuilder()
            .Add("azuredevops_team.platform", Update, DevOpsTeam("Platform Team", membersBefore, adminsBefore), DevOpsTeam("Platform Team", membersAfter, adminsAfter))
            .Build();
    }

    /// <summary>
    /// Builds an Azure DevOps group update with inline members (one added and the given number unchanged) and one separate membership created.
    /// </summary>
    /// <param name="unchanged">Number of unchanged inline members.</param>
    /// <returns>The plan JSON.</returns>
    internal static string DevOpsGroupMembers(int unchanged = 2)
    {
        var before = new JsonArray();
        var after = new JsonArray();
        for (var index = 1; index <= unchanged; index++)
        {
            before.Add($"aadgp.Uy0.MemberKeep{index}");
            after.Add($"aadgp.Uy0.MemberKeep{index}");
        }

        after.Add("aadgp.Uy0.MemberAdded");

        return new PlanJsonBuilder()
            .Add("azuredevops_group.release", Update, DevOpsGroup(before, "Release Team"), DevOpsGroup(after, "Release Team"))
            .Add("azuredevops_group_membership.separate", Create, null, new JsonObject { ["group"] = "aadgp.Uy0.ReleaseTeam", ["member"] = "aadgp.Uy0.MemberSeparate" })
            .Build();
    }

    /// <summary>
    /// Adds the standard separate route children: one created, one updated, one deleted and the given number unchanged.
    /// </summary>
    /// <param name="builder">The plan builder to extend.</param>
    /// <param name="unchanged">Number of unchanged routes.</param>
    private static void AddSeparateRoutes(PlanJsonBuilder builder, int unchanged)
    {
        builder
            .Add("azurerm_route.created", Create, null, Route("created", "10.9.0.0/16"))
            .Add("azurerm_route.updated", Update, Route("updated", "10.2.0.0/16"), Route("updated", "10.2.0.0/20"))
            .Add("azurerm_route.deleted", Delete, Route("deleted", "10.3.0.0/16"), null);
        for (var index = 1; index <= unchanged; index++)
        {
            builder.AddNoOp($"azurerm_route.keep_{index}", Route($"keep-{index}", $"10.1.{index}.0/24"));
        }
    }

    /// <summary>
    /// Builds a route table state.
    /// </summary>
    /// <param name="owner">The owner tag value.</param>
    /// <param name="routes">Optional inline routes.</param>
    /// <returns>The route table state.</returns>
    private static JsonObject RouteTable(string owner, JsonArray? routes = null)
    {
        var state = new JsonObject
        {
            ["name"] = RouteTableName,
            ["location"] = "westeurope",
            ["resource_group_name"] = "rg-network",
            ["tags"] = new JsonObject { ["owner"] = owner }
        };

        if (routes is not null)
        {
            state["route"] = routes.DeepClone();
        }

        return state;
    }

    /// <summary>
    /// Builds a route state.
    /// </summary>
    /// <param name="name">The route name.</param>
    /// <param name="prefix">The address prefix.</param>
    /// <param name="inline">Whether the route is an inline entry, which has no table reference.</param>
    /// <returns>The route state.</returns>
    private static JsonObject Route(string name, string prefix, bool inline = false)
    {
        var state = new JsonObject
        {
            ["name"] = name,
            ["address_prefix"] = prefix,
            ["next_hop_type"] = "VirtualAppliance",
            ["next_hop_in_ip_address"] = "10.0.0.4"
        };

        if (!inline)
        {
            state["route_table_name"] = RouteTableName;
        }

        return state;
    }

    /// <summary>
    /// Builds a virtual network state.
    /// </summary>
    /// <param name="owner">The owner tag value.</param>
    /// <param name="subnets">Optional inline subnets.</param>
    /// <returns>The virtual network state.</returns>
    private static JsonObject Vnet(string owner, JsonArray? subnets = null)
    {
        var state = new JsonObject
        {
            ["name"] = VnetName,
            ["location"] = "westeurope",
            ["resource_group_name"] = "rg-network",
            ["address_space"] = new JsonArray("10.0.0.0/16"),
            ["tags"] = new JsonObject { ["owner"] = owner }
        };

        if (subnets is not null)
        {
            state["subnet"] = subnets.DeepClone();
        }

        return state;
    }

    /// <summary>
    /// Builds an inline subnet entry.
    /// </summary>
    /// <param name="name">The subnet name.</param>
    /// <param name="prefix">The address prefix.</param>
    /// <returns>The subnet state.</returns>
    private static JsonObject Subnet(string name, string prefix)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["address_prefixes"] = new JsonArray(prefix),
            ["security_group"] = "nsg-app",
            ["delegation"] = new JsonArray()
        };
    }

    /// <summary>
    /// Builds a separate subnet resource state.
    /// </summary>
    /// <param name="name">The subnet name.</param>
    /// <param name="prefix">The address prefix.</param>
    /// <returns>The subnet state.</returns>
    private static JsonObject SeparateSubnet(string name, string prefix)
    {
        var state = Subnet(name, prefix);
        state["virtual_network_name"] = VnetName;
        return state;
    }

    /// <summary>
    /// Builds a DNS zone state.
    /// </summary>
    /// <param name="owner">The owner tag value.</param>
    /// <returns>The DNS zone state.</returns>
    private static JsonObject DnsZone(string owner)
    {
        return new JsonObject
        {
            ["name"] = DnsZoneName,
            ["resource_group_name"] = "rg-dns",
            ["tags"] = new JsonObject { ["owner"] = owner }
        };
    }

    /// <summary>
    /// Builds a DNS A record state.
    /// </summary>
    /// <param name="name">The record name.</param>
    /// <param name="address">The record address.</param>
    /// <returns>The record state.</returns>
    private static JsonObject DnsARecord(string name, string address)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["zone_name"] = DnsZoneName,
            ["ttl"] = 3600,
            ["records"] = new JsonArray(address)
        };
    }

    /// <summary>
    /// Builds a DNS CNAME record state.
    /// </summary>
    /// <param name="name">The record name.</param>
    /// <param name="target">The record target.</param>
    /// <returns>The record state.</returns>
    private static JsonObject DnsCnameRecord(string name, string target)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["zone_name"] = DnsZoneName,
            ["ttl"] = 3600,
            ["record"] = target
        };
    }

    /// <summary>
    /// Builds a DNS TXT record state.
    /// </summary>
    /// <param name="name">The record name.</param>
    /// <param name="value">The record text.</param>
    /// <returns>The record state.</returns>
    private static JsonObject DnsTxtRecord(string name, string value)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["zone_name"] = DnsZoneName,
            ["ttl"] = 3600,
            ["record"] = new JsonArray(new JsonObject { ["value"] = value })
        };
    }

    /// <summary>
    /// Builds a network security group state.
    /// </summary>
    /// <param name="owner">The owner tag value.</param>
    /// <param name="rules">Optional inline rules.</param>
    /// <returns>The network security group state.</returns>
    private static JsonObject Nsg(string owner, JsonArray? rules = null)
    {
        var state = new JsonObject
        {
            ["name"] = NsgName,
            ["location"] = "westeurope",
            ["resource_group_name"] = "rg-network",
            ["tags"] = new JsonObject { ["owner"] = owner }
        };

        if (rules is not null)
        {
            state["security_rule"] = rules.DeepClone();
        }

        return state;
    }

    /// <summary>
    /// Builds an inline NSG rule entry.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="priority">The rule priority.</param>
    /// <param name="port">The destination port range.</param>
    /// <returns>The rule state.</returns>
    private static JsonObject NsgRule(string name, int priority, string port)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["priority"] = priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["direction"] = "Inbound",
            ["access"] = "Allow",
            ["protocol"] = "TCP",
            ["source_address_prefix"] = "*",
            ["destination_address_prefix"] = "*",
            ["destination_port_range"] = port
        };
    }

    /// <summary>
    /// Builds a separate NSG rule resource state.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="priority">The rule priority.</param>
    /// <param name="port">The destination port range.</param>
    /// <returns>The rule state.</returns>
    private static JsonObject SeparateNsgRule(string name, int priority, string port)
    {
        var state = NsgRule(name, priority, port);
        state["network_security_group_name"] = NsgName;
        return state;
    }

    /// <summary>
    /// Builds an Azure AD group state.
    /// </summary>
    /// <param name="members">The inline member object IDs.</param>
    /// <returns>The group state.</returns>
    private static JsonObject AdGroup(JsonArray members)
    {
        return new JsonObject
        {
            ["id"] = "group-100",
            ["display_name"] = "Platform Team",
            ["members"] = members.DeepClone()
        };
    }

    /// <summary>
    /// Builds an Azure DevOps team state.
    /// </summary>
    /// <param name="name">The team name.</param>
    /// <param name="members">The inline member descriptors.</param>
    /// <param name="administrators">The inline administrator descriptors.</param>
    /// <returns>The team state.</returns>
    private static JsonObject DevOpsTeam(string name, JsonArray members, JsonArray administrators)
    {
        return new JsonObject
        {
            ["id"] = "team-123",
            ["name"] = name,
            ["project_id"] = "project-001",
            ["members"] = members.DeepClone(),
            ["administrators"] = administrators.DeepClone()
        };
    }

    /// <summary>
    /// Builds an Azure DevOps group state.
    /// </summary>
    /// <param name="members">The inline member descriptors.</param>
    /// <param name="displayName">The group display name.</param>
    /// <returns>The group state.</returns>
    private static JsonObject DevOpsGroup(JsonArray members, string displayName)
    {
        return new JsonObject
        {
            ["descriptor"] = "aadgp.Uy0.ReleaseTeam",
            ["display_name"] = displayName,
            ["members"] = members.DeepClone()
        };
    }
}
