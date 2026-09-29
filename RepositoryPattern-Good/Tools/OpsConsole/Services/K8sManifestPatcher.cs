using System.IO;
using System.Text.RegularExpressions;

namespace WebShop.Tools.OpsConsole.Services;

// Plain text substitution against a SCRATCH COPY of k8s/ - never the committed files (see
// project memory's "comment don't delete"/secret-handling notes: real secret values only ever
// exist in ephemeral copies, the same discipline used when this repo's k8s manifests were applied
// by hand). Deliberately simple line/regex substitution rather than a real YAML parser - these
// files' placeholder tokens are unique, literal strings by design specifically so a plain text
// replace is enough (see k8s/01-secrets.yaml's own comment on why stringData was chosen).
public static partial class K8sManifestPatcher
{
    public static string CopyK8sFolderToScratch()
    {
        var scratchDir = Path.Combine(Path.GetTempPath(), $"webshop-ops-k8s-{Guid.NewGuid():N}");
        Directory.CreateDirectory(scratchDir);
        foreach (var file in Directory.GetFiles(RepoLocator.K8sDir, "*.yaml"))
            File.Copy(file, Path.Combine(scratchDir, Path.GetFileName(file)), overwrite: true);
        return scratchDir;
    }

    public static void PatchSecrets(string scratchDir, string postgresPassword, string cosmosAccountName, string cosmosPrimaryKey, string jwtSigningKey)
    {
        var secretsPath = Path.Combine(scratchDir, "01-secrets.yaml");
        var text = File.ReadAllText(secretsPath);
        text = text.Replace("<POSTGRES_PASSWORD>", postgresPassword);
        text = text.Replace("<COSMOS_ACCOUNT_NAME>", cosmosAccountName);
        text = text.Replace("<COSMOS_ACCOUNT_KEY>", cosmosPrimaryKey);
        text = text.Replace("REPLACE_WITH_A_REAL_RANDOM_SIGNING_KEY", jwtSigningKey);
        File.WriteAllText(secretsPath, text);

        var postgresPath = Path.Combine(scratchDir, "04-postgres.yaml");
        var postgresText = File.ReadAllText(postgresPath);
        postgresText = postgresText.Replace("REPLACE_WITH_A_REAL_PASSWORD", postgresPassword);
        File.WriteAllText(postgresPath, postgresText);
    }

    // kustomization.yaml lists each service as a 3-line { name, newName, newTag } block - walks
    // it tracking which service's block we're currently in (from its `name:` line) so the
    // embedding image gets embeddingImageTag while every other service gets imageTag, without
    // assuming a fixed line order.
    public static void PatchImageTags(string scratchDir, string imageTag, string embeddingImageTag)
    {
        var path = Path.Combine(scratchDir, "kustomization.yaml");
        var lines = File.ReadAllLines(path);
        var isEmbeddingBlock = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var nameMatch = NameLineRegex().Match(lines[i]);
            if (nameMatch.Success)
                isEmbeddingBlock = nameMatch.Groups[1].Value.Contains("webshop-embedding");

            var tagMatch = NewTagLineRegex().Match(lines[i]);
            if (tagMatch.Success)
                lines[i] = $"{tagMatch.Groups["prefix"].Value}{(isEmbeddingBlock ? embeddingImageTag : imageTag)}";
        }

        File.WriteAllLines(path, lines);
    }

    // Rewrites 15-web.yaml's TLS host and Ingress rule host to the real nip.io hostname derived
    // from the app-routing add-on's actual public IP - unknowable until after that IP exists, so
    // this always runs as a second pass, after the first `kubectl apply -k` (see
    // AksOpsService.SetupAndDeployAsync).
    public static void PatchWebHostname(string scratchDir, string hostname)
    {
        var path = Path.Combine(scratchDir, "15-web.yaml");
        var text = File.ReadAllText(path);
        text = HostLineRegex().Replace(text, $"host: {hostname}");
        text = TlsHostLineRegex().Replace(text, $"- {hostname}");
        File.WriteAllText(path, text);
    }

    [GeneratedRegex(@"^\s*-\s*name:\s*(\S+)")]
    private static partial Regex NameLineRegex();

    [GeneratedRegex(@"^(?<prefix>\s*newTag:\s*).+$")]
    private static partial Regex NewTagLineRegex();

    [GeneratedRegex(@"host:\s*\S+")]
    private static partial Regex HostLineRegex();

    [GeneratedRegex(@"-\s*\S+\.nip\.io")]
    private static partial Regex TlsHostLineRegex();
}
