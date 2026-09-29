using System.IO;
namespace WebShop.Tools.OpsConsole.Services;

// Wraps the full manual sequence this session actually ran by hand to stand AKS up: provision
// (infra/main.bicep), fetch credentials, install cert-manager if missing, build/push the six
// images, apply k8s/ with real secrets substituted into a scratch copy, then a second pass to
// patch 15-web.yaml's hostname once the ingress IP is known (see k8s/README.md's own "Watching
// it come up" section - unlike ACA, AKS's default domain isn't knowable before the cluster and
// its ingress controller already exist).
public sealed class AksOpsService
{
    private static readonly (string Name, string Dockerfile, string BuildContext)[] DotnetServices =
    [
        ("catalog", "Catalog/Api/Dockerfile", "."),
        ("pricing", "Pricing/Api/Dockerfile", "."),
        ("reviews", "Reviews/Api/Dockerfile", "."),
        ("search", "Search/Api/Dockerfile", "."),
        ("web", "Web/Dockerfile", "."),
    ];

    public async Task<string?> SetupAndDeployAsync(
        string resourceGroup,
        string location,
        string clusterName,
        string acrName,
        string acrResourceGroup,
        string imageTag,
        string embeddingImageTag,
        bool buildAndPushImages,
        string cosmosAccountName,
        string cosmosResourceGroup,
        string postgresPassword,
        string jwtSigningKey,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        log("=== Step 1/8: Resource group ===");
        var groupResult = await ProcessRunner.RunAsync(
            "az", ["group", "create", "--name", resourceGroup, "--location", location],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
        if (!groupResult.Succeeded) return null;

        log("=== Step 2/8: AKS cluster (infra/main.bicep) ===");
        var deployResult = await ProcessRunner.RunAsync(
            "az",
            [
                "deployment", "group", "create",
                "--resource-group", resourceGroup,
                "--template-file", Path.Combine(RepoLocator.InfraDir, "main.bicep"),
                "--parameters", Path.Combine(RepoLocator.InfraDir, "main.bicepparam"),
                "--parameters", $"clusterName={clusterName}",
            ],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
        if (!deployResult.Succeeded) return null;

        log("=== Step 3/8: Fetching cluster credentials ===");
        var credsResult = await ProcessRunner.RunAsync(
            "az",
            ["aks", "get-credentials", "--resource-group", resourceGroup, "--name", clusterName, "--overwrite-existing"],
            log, cancellationToken: cancellationToken);
        if (!credsResult.Succeeded) return null;

        log("=== Step 4/8: cert-manager ===");
        if (!await EnsureCertManagerInstalledAsync(log, cancellationToken))
            return null;

        if (buildAndPushImages)
        {
            log("=== Step 5/8: Build and push images ===");
            if (!await BuildAndPushImagesAsync(acrName, acrResourceGroup, imageTag, embeddingImageTag, log, cancellationToken))
                return null;
        }
        else
        {
            log("=== Step 5/8: Skipped (using already-pushed images) ===");
        }

        log("=== Step 6/8: Preparing manifests with real secrets (scratch copy, never the committed files) ===");
        var scratchDir = K8sManifestPatcher.CopyK8sFolderToScratch();
        log($"Scratch copy: {scratchDir}");

        var cosmosKey = await AzureLookups.GetCosmosPrimaryKeyAsync(cosmosAccountName, cosmosResourceGroup, log, cancellationToken);
        if (cosmosKey is null)
        {
            log($"Failed to fetch the primary key for Cosmos account '{cosmosAccountName}' - check the account/resource group names.");
            return null;
        }

        K8sManifestPatcher.PatchSecrets(scratchDir, postgresPassword, cosmosAccountName, cosmosKey, jwtSigningKey);
        K8sManifestPatcher.PatchImageTags(scratchDir, imageTag, embeddingImageTag);

        log("Applying manifests (kubectl apply -k)...");
        var applyResult = await ProcessRunner.RunAsync(
            "kubectl", ["apply", "-k", scratchDir], log, cancellationToken: cancellationToken);
        if (!applyResult.Succeeded) return null;

        log("Waiting for the Postgres StatefulSet before the app services depend on it...");
        await ProcessRunner.RunAsync(
            "kubectl", ["rollout", "status", "statefulset/postgres", "-n", "webshop", "--timeout=300s"],
            log, cancellationToken: cancellationToken);

        log("=== Step 7/8: Discovering the ingress IP and patching Web's hostname ===");
        var ingressIp = await PollForIngressIpAsync(log, cancellationToken);
        if (ingressIp is null)
        {
            log("Timed out waiting for the app-routing add-on's public IP - the cluster is up, but Web's Ingress hostname needs a manual re-apply once it's assigned.");
            return null;
        }

        var hostname = $"web.{ingressIp}.nip.io";
        log($"Ingress IP: {ingressIp} -> hostname {hostname}");
        K8sManifestPatcher.PatchWebHostname(scratchDir, hostname);
        var webApplyResult = await ProcessRunner.RunAsync(
            "kubectl", ["apply", "-f", Path.Combine(scratchDir, "15-web.yaml")], log, cancellationToken: cancellationToken);
        if (!webApplyResult.Succeeded) return null;

        log("=== Step 8/8: Waiting for rollouts and the TLS certificate ===");
        foreach (var deployment in new[] { "catalog", "pricing", "reviews", "search", "embedding", "web" })
        {
            await ProcessRunner.RunAsync(
                "kubectl", ["rollout", "status", $"deployment/{deployment}", "-n", "webshop", "--timeout=300s"],
                log, cancellationToken: cancellationToken);
        }

        await PollUntilCertificateReadyAsync(log, cancellationToken);

        var url = $"https://{hostname}/";
        log($"Done. {url}");
        return url;
    }

    private static async Task<bool> EnsureCertManagerInstalledAsync(Action<string> log, CancellationToken cancellationToken)
    {
        var (checkResult, _, _) = await ProcessRunner.RunAndCaptureAsync(
            "kubectl", ["get", "namespace", "cert-manager"], cancellationToken: cancellationToken);
        if (checkResult.Succeeded)
        {
            log("cert-manager already installed.");
            return true;
        }

        log("Installing cert-manager...");
        var installResult = await ProcessRunner.RunAsync(
            "kubectl",
            ["apply", "-f", "https://github.com/cert-manager/cert-manager/releases/download/v1.16.2/cert-manager.yaml"],
            log, cancellationToken: cancellationToken);
        if (!installResult.Succeeded) return false;

        foreach (var deployment in new[] { "cert-manager", "cert-manager-cainjector", "cert-manager-webhook" })
        {
            await ProcessRunner.RunAsync(
                "kubectl", ["rollout", "status", $"deployment/{deployment}", "-n", "cert-manager", "--timeout=180s"],
                log, cancellationToken: cancellationToken);
        }

        return true;
    }

    private async Task<bool> BuildAndPushImagesAsync(
        string acrName, string acrResourceGroup, string imageTag, string embeddingImageTag, Action<string> log, CancellationToken cancellationToken)
    {
        var loginResult = await ProcessRunner.RunAsync(
            "az", ["acr", "login", "--name", acrName], log, cancellationToken: cancellationToken);
        if (!loginResult.Succeeded) return false;

        var registry = $"{acrName}.azurecr.io";

        foreach (var (name, dockerfile, context) in DotnetServices)
        {
            var image = $"{registry}/webshop-{name}:{imageTag}";
            log($"Building {image}...");
            var buildResult = await ProcessRunner.RunAsync(
                "docker", ["build", "-t", image, "-f", dockerfile, context],
                log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);
            if (!buildResult.Succeeded) return false;

            log($"Pushing {image}...");
            var pushResult = await ProcessRunner.RunAsync(
                "docker", ["push", image], log, cancellationToken: cancellationToken);
            if (!pushResult.Succeeded) return false;
        }

        var embeddingImage = $"{registry}/webshop-embedding:{embeddingImageTag}";
        var embeddingContext = Path.Combine(RepoLocator.Root, "Search", "EmbeddingServer");
        log($"Building {embeddingImage} (CPU build)...");
        var embeddingBuildResult = await ProcessRunner.RunAsync(
            "docker", ["build", "-t", embeddingImage, "-f", "Dockerfile.embed.cpu", embeddingContext],
            log, workingDirectory: embeddingContext, cancellationToken: cancellationToken);
        if (!embeddingBuildResult.Succeeded) return false;

        log($"Pushing {embeddingImage}...");
        var embeddingPushResult = await ProcessRunner.RunAsync(
            "docker", ["push", embeddingImage], log, cancellationToken: cancellationToken);
        return embeddingPushResult.Succeeded;
    }

    private static async Task<string?> PollForIngressIpAsync(Action<string> log, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var (result, output, _) = await ProcessRunner.RunAndCaptureAsync(
                "kubectl",
                ["get", "svc", "-n", "app-routing-system", "nginx", "-o", "jsonpath={.status.loadBalancer.ingress[0].ip}"],
                cancellationToken: cancellationToken);

            if (result.Succeeded && !string.IsNullOrWhiteSpace(output))
                return output.Trim();

            log("Waiting for the app-routing add-on's public IP to be assigned...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

        return null;
    }

    private static async Task PollUntilCertificateReadyAsync(Action<string> log, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var (result, output, _) = await ProcessRunner.RunAndCaptureAsync(
                "kubectl",
                ["get", "certificate", "-n", "webshop", "web-tls", "-o", "jsonpath={.status.conditions[0].status}"],
                cancellationToken: cancellationToken);

            if (result.Succeeded && output.Trim() == "True")
            {
                log("TLS certificate ready.");
                return;
            }

            log("Waiting for cert-manager to issue the TLS certificate...");
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

        log("Timed out waiting for the certificate - it may still finish shortly; check `kubectl describe certificate -n webshop web-tls`.");
    }
}
