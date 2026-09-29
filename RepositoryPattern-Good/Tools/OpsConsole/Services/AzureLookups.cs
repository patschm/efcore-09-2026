namespace WebShop.Tools.OpsConsole.Services;

// Small shared lookups used by more than one ops service - kept separate so CosmosOpsService and
// AksOpsService don't each reimplement "how do I get this Cosmos account's key".
public static class AzureLookups
{
    public static async Task<string?> GetCosmosPrimaryKeyAsync(
        string accountName, string resourceGroup, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var (result, output, error) = await ProcessRunner.RunAndCaptureAsync(
            "az",
            [
                "cosmosdb", "keys", "list",
                "--name", accountName,
                "--resource-group", resourceGroup,
                "--query", "primaryMasterKey",
                "-o", "tsv",
            ],
            cancellationToken: cancellationToken);

        if (result.Succeeded && !string.IsNullOrWhiteSpace(output))
            return output.Trim();

        if (!string.IsNullOrWhiteSpace(error))
            log?.Invoke(error.Trim());
        return null;
    }

    public static async Task<string?> GetCosmosConnectionStringAsync(
        string accountName, string resourceGroup, Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var (result, output, error) = await ProcessRunner.RunAndCaptureAsync(
            "az",
            [
                "cosmosdb", "keys", "list",
                "--name", accountName,
                "--resource-group", resourceGroup,
                "--type", "connection-strings",
                "--query", "connectionStrings[0].connectionString",
                "-o", "tsv",
            ],
            cancellationToken: cancellationToken);

        if (result.Succeeded && !string.IsNullOrWhiteSpace(output))
            return output.Trim();

        if (!string.IsNullOrWhiteSpace(error))
            log?.Invoke(error.Trim());
        return null;
    }

    // Backs the editable resource-group dropdowns on every tab - "editable" so a not-yet-created
    // name can still be typed in for Create, but pre-fillable from what's actually already in the
    // current subscription for Restore/Delete/Setup&Deploy against something that already exists.
    public static async Task<IReadOnlyList<string>> ListResourceGroupsAsync(
        Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        var (result, output, error) = await ProcessRunner.RunAndCaptureAsync(
            "az", ["group", "list", "--query", "[].name", "-o", "tsv"], cancellationToken: cancellationToken);

        if (!result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(error))
                log?.Invoke(error.Trim());
            return [];
        }

        return [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
    }
}
