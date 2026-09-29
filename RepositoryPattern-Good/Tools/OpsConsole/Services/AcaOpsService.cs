using System.IO;
namespace WebShop.Tools.OpsConsole.Services;

// Wraps infra/aca.bicep - see that file's own header comment for what's genuinely different
// about ACA vs AKS (Dapr built into the environment, automatic TLS, a real Postgres Flexible
// Server from the start). This service is deliberately just "create the resource group, run the
// deployment, read back its outputs" - no image build/push step, unlike AksOpsService, because
// ACA's Container Apps just pull whatever tag is already in the registry; this app doesn't
// rebuild images on every deploy (see AksOpsService's own comment on why it does, there).
public sealed class AcaOpsService
{
    public async Task<string?> SetupAndDeployAsync(
        string resourceGroup,
        string location,
        string environmentName,
        string appImageTag,
        string embeddingImageTag,
        string postgresAdminPassword,
        string cosmosAccountName,
        string cosmosResourceGroup,
        string jwtSigningKey,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        var groupResult = await ProcessRunner.RunAsync(
            "az", ["group", "create", "--name", resourceGroup, "--location", location],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
        if (!groupResult.Succeeded)
            return null;

        log($"Fetching connection string for Cosmos account '{cosmosAccountName}'...");
        var cosmosConnectionString = await AzureLookups.GetCosmosConnectionStringAsync(cosmosAccountName, cosmosResourceGroup, log, cancellationToken);
        if (cosmosConnectionString is null)
        {
            log("Failed to fetch the Cosmos account's connection string - check the account/resource group names.");
            return null;
        }

        var deploymentName = $"aca-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var deployResult = await ProcessRunner.RunAsync(
            "az",
            [
                "deployment", "group", "create",
                "--resource-group", resourceGroup,
                "--name", deploymentName,
                "--template-file", Path.Combine(RepoLocator.InfraDir, "aca.bicep"),
                "--parameters", Path.Combine(RepoLocator.InfraDir, "aca.bicepparam"),
                "--parameters", $"environmentName={environmentName}", $"appImageTag={appImageTag}", $"embeddingImageTag={embeddingImageTag}",
            ],
            log,
            environmentVariables: new Dictionary<string, string>
            {
                ["ACA_POSTGRES_ADMIN_PASSWORD"] = postgresAdminPassword,
                ["ACA_COSMOS_CONNECTION_STRING"] = cosmosConnectionString,
                ["ACA_JWT_SIGNING_KEY"] = jwtSigningKey,
            },
            workingDirectory: RepoLocator.Root,
            cancellationToken: cancellationToken);

        if (!deployResult.Succeeded)
            return null;

        log("Deployment succeeded - reading the webUrl output...");
        var (_, webUrl, _) = await ProcessRunner.RunAndCaptureAsync(
            "az",
            [
                "deployment", "group", "show",
                "--resource-group", resourceGroup,
                "--name", deploymentName,
                "--query", "properties.outputs.webUrl.value",
                "-o", "tsv",
            ],
            cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(webUrl) ? null : webUrl.Trim();
    }
}
