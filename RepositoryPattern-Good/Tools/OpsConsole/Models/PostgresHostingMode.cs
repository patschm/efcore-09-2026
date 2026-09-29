namespace WebShop.Tools.OpsConsole.Models;

// Mirrors the two ways this repo already runs Postgres: a real managed Azure resource (what
// infra/postgres.bicep provisions) vs. a plain container image (exactly what k8s/04-postgres.yaml
// runs in-cluster on AKS, and what docker-compose.yml's own "postgres-local" container is for
// local dev) - the user asked for "a container option... for example AKS" specifically because
// AKS already runs Postgres this way rather than as a managed service.
public enum PostgresHostingMode
{
    AzureFlexibleServer,
    DockerContainer,
}
