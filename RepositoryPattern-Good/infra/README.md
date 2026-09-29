# WebShop AKS infrastructure

Provisions the AKS cluster WebShop would run on, with the **KEDA** autoscaler add-on and the
**Dapr** cluster extension enabled, plus an Azure Container Registry for the six service images,
a Log Analytics workspace for Container Insights, and the Azure-side destinations for
OpenTelemetry: **Application Insights** (traces/logs) and AKS's **managed-Prometheus** add-on
(metrics), with optional **Azure Managed Grafana** to explore the latter.

This deploys the *cluster*, not the app. Deploying Catalog/Pricing/Reviews/Search/Web/embedding
onto it (Deployments, Services, Dapr component YAML, KEDA ScaledObjects) is a separate step -
see "Next steps" below.

`main.bicep` is just the orchestrator - each resource group (the cluster, the registry, Log
Analytics, the observability pieces) lives in its own file under `modules/`, wired together by
passing outputs between them. See "What's in here" for which module owns what.

## Prerequisites

```bash
az login
az account set --subscription <subscription-id>

# One-time per subscription: the extension used to install Dapr, and the providers backing
# Dapr, managed-Prometheus, Application Insights, and (if enabled) Managed Grafana.
az extension add --name k8s-extension
az provider register --namespace Microsoft.KubernetesConfiguration
az provider register --namespace Microsoft.ContainerService
az provider register --namespace Microsoft.Monitor
az provider register --namespace Microsoft.Insights
az provider register --namespace Microsoft.Dashboard
```

## Deploy

```bash
az group create --name webshop-rg --location westeurope

az deployment group create \
  --resource-group webshop-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam
```

Override anything from `main.bicepparam` inline, e.g. a different region's node size:

```bash
az deployment group create \
  --resource-group webshop-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters systemNodeVmSize=Standard_D4s_v5
```

## After it's up

```bash
az aks get-credentials --resource-group webshop-rg --name webshop-aks

kubectl get pods -n kube-system | grep keda   # keda-operator, keda-operator-metrics-apiserver
kubectl get pods -n dapr-system               # dapr-operator, dapr-sidecar-injector, dapr-placement-server, dapr-sentry
```

### Wiring up the OTel Collector

Application Insights and the managed-Prometheus storage exist after this deploys, but nothing is
actually shipping data to them yet - that needs the in-cluster collector from
`observability/azure/otel-collector.yaml` (a plain Kubernetes manifest; Bicep provisions Azure
resources, not cluster workloads, so this is a separate `kubectl apply`, not part of `az
deployment group create`). That file's own header comment has the exact commands - short version:
grab `appInsightsConnectionString` from this deployment's outputs, put it in a Secret, then
`kubectl apply -f observability/azure/otel-collector.yaml`. Once it's running, point every
service's `OTEL_EXPORTER_OTLP_ENDPOINT` at
`http://otel-collector.observability.svc.cluster.local:4317` (`:4318` for the embedding
container - same http/protobuf-vs-grpc split as `docker-compose.yml` locally).

Metrics take a different path entirely: AKS's managed-Prometheus add-on (enabled via
`azureMonitorProfile.metrics` on the cluster resource) scrapes them directly - nothing to deploy
for that side, it's already active once the cluster exists with `enableManagedPrometheus: true`.

## What's in here

- **AKS cluster** (`modules/aks.bicep`) - a single auto-scaling system node pool (1-4 nodes by
  default), Azure CNI Overlay networking, the managed **KEDA** add-on
  (`workloadAutoScalerProfile.keda`), the managed **app routing** add-on
  (`ingressProfile.webAppRouting`, optional via `enableAppRoutingAddon`) - a managed NGINX Ingress
  controller for `k8s/15-web.yaml`'s Ingress, see `k8s/README.md` for wiring up HTTPS with
  cert-manager on top of it - and OIDC issuer + workload identity enabled so Dapr components/KEDA
  scalers can authenticate to Azure resources without embedded secrets.
- **Dapr** (inline in `main.bicep`, not a module - it's a `Microsoft.KubernetesConfiguration/
  extensions` resource attached to the cluster the module above creates, so it has to live where
  it can reference that) - the managed cluster extension, so Azure handles the control plane's
  lifecycle/upgrades instead of a self-managed `dapr init -k` / Helm install. HA mode is off by
  default (`daprHighAvailability`); turn it on for anything beyond a dev cluster.
- **Azure Container Registry** (`modules/acr.bicep`, optional overall via `enableAcr`) - somewhere
  to push the six images built from `Catalog/Api/Dockerfile`, `Pricing/Api/Dockerfile`,
  `Reviews/Api/Dockerfile`, `Search/Api/Dockerfile`, `Web/Dockerfile`, and
  `Search/EmbeddingServer/Dockerfile.embed.cpu` (the same Dockerfiles `docker-compose.yml` builds
  locally). Creates a new registry by default; set `existingAcrName` (and
  `existingAcrResourceGroupName`, if it's not this deployment's own resource group) to reuse one
  instead - e.g. this training subscription's `psrepoo` registry in resource group `AI-200`,
  which already has the six `webshop-*` images pushed to it (see the commented-out example in
  `main.bicepparam`). Either way, the cluster's kubelet identity gets `AcrPull` on it
  automatically (`modules/acrPullRoleAssignment.bicep`) - deployed at the registry's own resource
  group, which can differ from the cluster's.
- **Log Analytics workspace** (`modules/logAnalytics.bicep`) - backs the cluster's Container
  Insights add-on (`omsagent`).
- **Application Insights** (`modules/appInsights.bicep`, optional via `enableApplicationInsights`,
  workspace-based against the Log Analytics workspace above) - where the in-cluster OTel
  Collector's `azuremonitor` exporter sends traces and logs. See "Wiring up the OTel Collector"
  above.
- **AKS managed-Prometheus** (`modules/managedPrometheus.bicep` for the Azure Monitor Workspace/
  DCE/DCR, plus a Data Collection Rule Association inline in `main.bicep` since it has to attach
  to the cluster; optional overall via `enableManagedPrometheus`) - its own storage (not Log
  Analytics). Handles the metrics signal that Application Insights deliberately doesn't (the
  `azuremonitor` exporter's metrics support is comparatively immature) - each service's own
  `/metrics` still needs a `ServiceMonitor`-style scrape config once the app workloads exist, not
  included here.
- **Azure Managed Grafana** (`modules/grafana.bicep`, optional via `enableManagedGrafana`, off by
  default) - pre-wired to the managed-Prometheus workspace via a `Monitoring Reader` role
  assignment, for exploring those metrics. Off by default purely for cost - Application Insights
  already covers traces/logs without it, and Grafana bills separately.

## Deliberately not in here

- **A managed Postgres service** (e.g. Azure Database for PostgreSQL Flexible Server). Postgres
  itself DOES run on this cluster - see `k8s/04-postgres.yaml` - as a StatefulSet backed by a real
  Azure Managed Disk via AKS's built-in `managed-csi` storage class, not an external managed
  database. That's a reasonable choice for learning (one less Azure resource type to provision
  and pay for separately) but not necessarily for production, where a managed service's automated
  backups/patching/HA usually outweigh running your own.
- **The app workloads themselves.** See `k8s/` for the Deployment/Service manifests for all six
  containers - that folder still deliberately leaves out Dapr `Component`/`Configuration` YAML
  and KEDA `ScaledObject`s, since neither has anything to attach to yet (see `k8s/README.md`'s
  own "Deliberately not in here").

## Cost note

This is sized for a dev/learning cluster (2-6 auto-scaling `Standard_D2s_v5` nodes, Basic ACR,
Dapr HA off, Grafana off) - tear it down with `az group delete --name webshop-rg` when not in use
rather than leaving it running. Application Insights and managed-Prometheus both bill on ingested
volume, which stays low for a cluster this size, but neither is free.
