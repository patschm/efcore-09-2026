using Newtonsoft.Json;

namespace WebShop.Pricing.Infrastructure.Cosmos.Documents;

public sealed class MoneyDocument
{
    [JsonProperty("amount")] public double Amount { get; set; }
    [JsonProperty("currency")] public string Currency { get; set; } = null!;
}
