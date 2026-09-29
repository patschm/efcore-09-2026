using System.IO;
namespace WebShop.Tools.OpsConsole.Services;

// Wraps infra/cosmos.bicep (create) and Tools/CosmosMigration (restore) - see project memory's
// "project-cosmos-persistence" for what that migration tool actually does and the two data-
// fidelity bugs it had to work around. This service doesn't reimplement any of that logic, only
// invokes it the same way it was run manually all session.
public sealed class CosmosOpsService
{
    public async Task<ProcessResult> CreateAsync(
        string resourceGroup, string location, string accountName, Action<string> log, CancellationToken cancellationToken = default)
    {
        var groupResult = await ProcessRunner.RunAsync(
            "az", ["group", "create", "--name", resourceGroup, "--location", location],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
        if (!groupResult.Succeeded)
            return groupResult;

        return await ProcessRunner.RunAsync(
            "az",
            [
                "deployment", "group", "create",
                "--resource-group", resourceGroup,
                "--template-file", Path.Combine(RepoLocator.InfraDir, "cosmos.bicep"),
                "--parameters", Path.Combine(RepoLocator.InfraDir, "cosmos.bicepparam"),
                "--parameters", $"accountName={accountName}",
            ],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
    }

    // Real secret values (the account's key, embedded in the connection string) are fetched and
    // handed to the migration tool via an environment variable only - never passed through `log`,
    // matching this project's own "never print/commit a real secret" discipline (see project
    // memory's Dapr/Cosmos secret-handling notes).
    public async Task<ProcessResult> RestoreAsync(
        string resourceGroup, string accountName, string dumpFolder, Action<string> log, CancellationToken cancellationToken = default)
    {
        log($"Fetching connection string for Cosmos account '{accountName}'...");
        var connectionString = await AzureLookups.GetCosmosConnectionStringAsync(accountName, resourceGroup, log, cancellationToken);
        if (connectionString is null)
        {
            log("Failed to fetch the Cosmos account's connection string - check the account/resource group names above.");
            return new ProcessResult(1);
        }

        log("Connection string retrieved. Running the import...");
        return await ProcessRunner.RunAsync(
            "dotnet",
            ["run", "--project", RepoLocator.CosmosMigrationProject, "-c", "Release", "--", "--import", dumpFolder],
            log,
            environmentVariables: new Dictionary<string, string> { ["WEBSHOP_COSMOS_CONNECTION"] = connectionString },
            workingDirectory: RepoLocator.Root,
            cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> DeleteAsync(
        string resourceGroup, string accountName, Action<string> log, CancellationToken cancellationToken = default) =>
        await ProcessRunner.RunAsync(
            "az",
            ["cosmosdb", "delete", "--name", accountName, "--resource-group", resourceGroup, "--yes"],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
}
