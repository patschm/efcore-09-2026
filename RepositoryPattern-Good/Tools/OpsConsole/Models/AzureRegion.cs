namespace WebShop.Tools.OpsConsole.Models;

public sealed record AzureRegion(string DisplayName, string Name)
{
    public override string ToString() => DisplayName;
}

// Every Azure region physically in Europe, for the Location dropdowns on every tab - a plain
// TextBox let a typo (or an unavailable region for a given resource type) surface only as a
// cryptic `az` error deep into a deploy. Doesn't include every region ever announced (e.g. a
// brand-new preview region) - update this list if Azure adds one you need.
public static class AzureRegions
{
    public static readonly IReadOnlyList<AzureRegion> Europe =
    [
        new("West Europe (Netherlands)", "westeurope"),
        new("North Europe (Ireland)", "northeurope"),
        new("France Central (Paris)", "francecentral"),
        new("France South (Marseille)", "francesouth"),
        new("Germany West Central (Frankfurt)", "germanywestcentral"),
        new("Germany North (Berlin)", "germanynorth"),
        new("Norway East", "norwayeast"),
        new("Norway West", "norwaywest"),
        new("Sweden Central", "swedencentral"),
        new("Switzerland North (Zurich)", "switzerlandnorth"),
        new("Switzerland West (Geneva)", "switzerlandwest"),
        new("UK South (London)", "uksouth"),
        new("UK West (Cardiff)", "ukwest"),
        new("Poland Central (Warsaw)", "polandcentral"),
        new("Italy North (Milan)", "italynorth"),
        new("Spain Central (Madrid)", "spaincentral"),
    ];
}
