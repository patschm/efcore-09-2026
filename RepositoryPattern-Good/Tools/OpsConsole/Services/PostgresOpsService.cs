using System.IO;
namespace WebShop.Tools.OpsConsole.Services;

// Wraps infra/postgres.bicep - a standalone Azure Database for PostgreSQL Flexible Server,
// independent of both AKS (which runs its own in-cluster Postgres, k8s/04-postgres.yaml) and ACA
// (which provisions its own Flexible Server as part of that deployment) - see this app's project
// memory note on why "Postgres" is its own tab rather than a sub-step of either cluster.
public sealed class PostgresOpsService
{
    public async Task<ProcessResult> CreateAsync(
        string resourceGroup, string location, string serverName, string adminLogin, string adminPassword,
        Action<string> log, CancellationToken cancellationToken = default)
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
                "--template-file", Path.Combine(RepoLocator.InfraDir, "postgres.bicep"),
                "--parameters", Path.Combine(RepoLocator.InfraDir, "postgres.bicepparam"),
                "--parameters", $"serverName={serverName}", $"administratorLogin={adminLogin}",
            ],
            log,
            environmentVariables: new Dictionary<string, string> { ["OPS_POSTGRES_ADMIN_PASSWORD"] = adminPassword },
            workingDirectory: RepoLocator.Root,
            cancellationToken: cancellationToken);
    }

    // psql, not `az postgres flexible-server execute` - the latter only runs a single query
    // string, not an arbitrary multi-statement .sql file. Connection details go through PG*
    // environment variables (PGPASSWORD included) rather than a connection-string argument or
    // psql's own -h/-U flags, so the password never appears in a process's command-line
    // arguments (visible to anything that can list processes on the machine).
    public async Task<ProcessResult> RestoreAsync(
        string serverName, string adminLogin, string adminPassword, string databaseName, string dumpFilePath,
        Action<string> log, CancellationToken cancellationToken = default)
    {
        var fqdn = $"{serverName}.postgres.database.azure.com";
        log($"Restoring '{dumpFilePath}' into {databaseName}@{fqdn}...");

        return await ProcessRunner.RunAsync(
            "psql",
            ["-v", "ON_ERROR_STOP=1", "-f", dumpFilePath],
            log,
            environmentVariables: new Dictionary<string, string>
            {
                ["PGHOST"] = fqdn,
                ["PGPORT"] = "5432",
                ["PGDATABASE"] = databaseName,
                ["PGUSER"] = adminLogin,
                ["PGPASSWORD"] = adminPassword,
                ["PGSSLMODE"] = "require",
            },
            cancellationToken: cancellationToken);
    }

    public async Task<ProcessResult> DeleteAsync(
        string resourceGroup, string serverName, Action<string> log, CancellationToken cancellationToken = default) =>
        await ProcessRunner.RunAsync(
            "az",
            ["postgres", "flexible-server", "delete", "--name", serverName, "--resource-group", resourceGroup, "--yes"],
            log, workingDirectory: RepoLocator.Root, cancellationToken: cancellationToken);

    // The "container" hosting mode - exactly what k8s/04-postgres.yaml runs in-cluster on AKS
    // (same image, pgvector/pgvector:pg17 - the extension real seed data needs, see that file's
    // own comment) and what docker-compose.yml's standalone "postgres-local" container is for
    // local dev, just run directly instead of through either of those. No Azure resource, no
    // `az` calls at all - purely local Docker, for a free/instant alternative to a real Flexible
    // Server while developing or demoing.
    private const string PostgresContainerImage = "pgvector/pgvector:pg17";

    // POSTGRES_PASSWORD goes through a temp --env-file, not a `-e KEY=VALUE` argument, for the
    // same reason psql's restore uses PG* environment variables above - a `docker run -e ...`
    // argument would put the real password in that process's visible command-line arguments.
    public async Task<ProcessResult> CreateContainerAsync(
        string containerName, int port, string volumeName, string adminPassword, string databaseName,
        Action<string> log, CancellationToken cancellationToken = default)
    {
        var envFile = Path.Combine(Path.GetTempPath(), $"webshop-ops-pg-{Guid.NewGuid():N}.env");
        try
        {
            await File.WriteAllTextAsync(envFile, $"POSTGRES_PASSWORD={adminPassword}\nPOSTGRES_DB={databaseName}\n", cancellationToken);

            log($"Pulling {PostgresContainerImage} and starting container '{containerName}' on port {port}...");
            return await ProcessRunner.RunAsync(
                "docker",
                [
                    "run", "-d",
                    "--name", containerName,
                    "--env-file", envFile,
                    "-p", $"{port}:5432",
                    "-v", $"{volumeName}:/var/lib/postgresql/data",
                    PostgresContainerImage,
                ],
                log, cancellationToken: cancellationToken);
        }
        finally
        {
            File.Delete(envFile);
        }
    }

    // Mirrors k8s/README.md's "Restoring the seed data" section exactly, with `docker cp`/`docker
    // exec` standing in for `kubectl cp`/`kubectl exec` - copy the file into the container's
    // filesystem, run psql INSIDE the container against it (so no client-side psql install is
    // needed on the host for this mode, unlike the Azure Flexible Server restore above), then
    // clean up the copy.
    public async Task<ProcessResult> RestoreContainerAsync(
        string containerName, string adminLogin, string databaseName, string dumpFilePath,
        Action<string> log, CancellationToken cancellationToken = default)
    {
        log($"Copying '{dumpFilePath}' into container '{containerName}'...");
        var copyResult = await ProcessRunner.RunAsync(
            "docker", ["cp", dumpFilePath, $"{containerName}:/tmp/dump.sql"], log, cancellationToken: cancellationToken);
        if (!copyResult.Succeeded)
            return copyResult;

        log("Running psql inside the container...");
        var restoreResult = await ProcessRunner.RunAsync(
            "docker",
            ["exec", containerName, "psql", "-U", adminLogin, "-d", databaseName, "-v", "ON_ERROR_STOP=1", "-f", "/tmp/dump.sql"],
            log, cancellationToken: cancellationToken);

        await ProcessRunner.RunAsync("docker", ["exec", containerName, "rm", "/tmp/dump.sql"], log, cancellationToken: cancellationToken);
        return restoreResult;
    }

    public async Task<ProcessResult> DeleteContainerAsync(
        string containerName, bool removeVolume, string volumeName, Action<string> log, CancellationToken cancellationToken = default)
    {
        var removeResult = await ProcessRunner.RunAsync(
            "docker", ["rm", "-f", containerName], log, cancellationToken: cancellationToken);

        if (removeVolume)
        {
            log($"Removing volume '{volumeName}'...");
            await ProcessRunner.RunAsync("docker", ["volume", "rm", volumeName], log, cancellationToken: cancellationToken);
        }

        return removeResult;
    }
}
