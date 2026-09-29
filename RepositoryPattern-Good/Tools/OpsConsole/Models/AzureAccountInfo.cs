namespace WebShop.Tools.OpsConsole.Models;

public sealed record AzureAccountInfo(string SubscriptionName, string SubscriptionId, string UserName);

public sealed record AzureSubscriptionInfo(string Name, string Id, bool IsDefault);
