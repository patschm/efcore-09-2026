# WebShop Kubernetes manifests

Plain Kubernetes YAML (no Helm) for running all six containers on the AKS cluster `infra/`
provisions - with one exception: `kustomization.yaml`, which exists solely to let you override the
six `image:` registry/tags from one place instead of hand-editing every Deployment file (see that
file's own comment). Everything else here is applied exactly as written. Deliberately verbose -
every file has a top-of-file explanation of what's new/different about that resource, plus
extensive inline comments on individual fields, since the point of this folder is as much to learn
Kubernetes from as it is to actually run the app.

This is the missing piece both `infra/README.md` and `docker-compose.yml` point at:
`docker-compose.yml` is "run all six containers on your own machine", `infra/` is "here's a
cluster", and this folder is "here's what to actually put on that cluster."

## Files, in the order you'd apply them

| File | What it is |
|---|---|
| `00-namespace.yaml` | The `webshop` namespace everything else lives in |
| `01-secrets.yaml` | Postgres connection strings + the JWT signing key - **edit the placeholders before applying** |
| `02-configmap.yaml` | Non-secret settings shared by all five .NET services |
| `03-cert-manager-issuers.yaml` | ClusterIssuers HTTPS needs - **apply after installing cert-manager itself, see below** |
| `04-postgres.yaml` | Postgres itself: Secret + headless Service + StatefulSet with a Managed-Disk-backed volume |
| `10-catalog.yaml` | Catalog: Deployment + Service (read this one first - the others assume it) |
| `11-pricing.yaml` | Pricing: Deployment + Service |
| `12-reviews.yaml` | Reviews: Deployment + Service (+ the JWT secret) |
| `13-search.yaml` | Search: Deployment + Service (+ calls into embedding) |
| `14-embedding.yaml` | The embedding model server: Deployment + Service (Python, not .NET - see its own comments) |
| `15-web.yaml` | Web: PVC + Deployment + Service + Ingress (the only one exposed outside the cluster, now via HTTPS; the PVC is a shared Data Protection key ring - see its own comment) |
| `kustomization.yaml` | Not applied on its own - lets `kubectl apply -k .` override all six image registries/tags from one place |

## Prerequisites

- The AKS cluster from `infra/` already deployed, with `enableAppRoutingAddon = true` (the
  default) - it provisions the managed NGINX Ingress controller `15-web.yaml`'s Ingress needs.
- cert-manager itself installed - it's cluster-wide infra like the app-routing add-on above, so
  it's a separate one-time `kubectl apply` against the upstream manifest, not part of this folder:
  ```bash
  kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.16.2/cert-manager.yaml
  kubectl get pods -n cert-manager -w   # wait for cert-manager, cainjector, webhook all Running
  ```
- Nothing extra for Postgres - `04-postgres.yaml` runs it in-cluster, backed by a real Azure
  Managed Disk (see that file's own comments). The only thing to actually do here is replace its
  Secret's placeholder password (and the matching one in `01-secrets.yaml`) before applying.
- The six images pushed somewhere the cluster can pull from - see `infra/README.md`'s ACR
  section. These manifests default to `psrepo.azurecr.io/webshop-*:v1`/`:v2` (this training
  subscription's existing registry) - point at a different registry or tags via
  `kustomization.yaml`'s `images:` list (see below) rather than editing each Deployment file.
- The OTel Collector from `observability/azure/otel-collector.yaml` applied (optional - without
  it, every service just logs failed export attempts instead of failing to run; see
  `BuildingBlocks/Api/ObservabilityExtensions.cs`'s own comment on that).

## Deploy

```bash
az aks get-credentials --resource-group <rg> --name <cluster>

kubectl apply -f k8s/00-namespace.yaml

# Edit the placeholder values in k8s/01-secrets.yaml first! Then:
kubectl apply -f k8s/01-secrets.yaml
kubectl apply -f k8s/02-configmap.yaml

# cert-manager (see Prerequisites above) must already be installed before this succeeds:
kubectl apply -f k8s/03-cert-manager-issuers.yaml

# Edit the placeholder password in k8s/04-postgres.yaml first, and match it in 01-secrets.yaml
# above! Then:
kubectl apply -f k8s/04-postgres.yaml
kubectl rollout status statefulset/postgres -n webshop   # wait for it before the app services -
                                                          # see "Restoring the seed data" below

kubectl apply -f k8s/10-catalog.yaml
kubectl apply -f k8s/11-pricing.yaml
kubectl apply -f k8s/12-reviews.yaml
kubectl apply -f k8s/13-search.yaml
kubectl apply -f k8s/14-embedding.yaml
kubectl apply -f k8s/15-web.yaml

# Or, since every file declares its own namespace and Kubernetes doesn't care about apply order
# for anything except "the namespace must exist first" and "a Deployment referencing a Secret/
# ConfigMap key that doesn't exist yet will just sit there failing to start until it does" -
# once you're comfortable with what each file does individually, this is equivalent:
kubectl apply -f k8s/
```

To use the registries/tags from `kustomization.yaml` instead of each file's own hardcoded
defaults (e.g. after bumping a `newTag` there to ship a new version), apply the whole folder
through Kustomize in one shot instead of the file-by-file sequence above:

```bash
kubectl apply -k k8s/

# See exactly what that would send to the API server first, without applying anything -
# useful for confirming an image override actually took effect before committing to it:
kubectl kustomize k8s/ | less
```

## Watching it come up

```bash
kubectl get pods -n webshop -w
# STATUS should move Pending -> ContainerCreating -> Running, then READY should flip to 1/1 once
# the readinessProbe in that service's manifest starts passing (see 10-catalog.yaml's comment on
# the difference between startupProbe/livenessProbe/readinessProbe for what that means).

kubectl get svc -n app-routing-system nginx
# EXTERNAL-IP here (not on the `web` Service anymore - it's ClusterIP now) is the app-routing
# add-on's shared public IP. Point DNS (or a nip.io name - see 15-web.yaml's Ingress comment) at
# it, put that hostname in 15-web.yaml's tls.hosts/rules.host, then re-apply that file.

kubectl get certificate -n webshop web-tls -w
# READY flips True once cert-manager finishes talking to whichever ClusterIssuer 15-web.yaml
# names - typically under a minute for selfsigned-issuer, up to a couple of minutes for either
# letsencrypt-* issuer (it has to wait for its HTTP-01 solver Pod to become reachable first).
# browse to https://<your-host>/ once it's True.
```

## Restoring the seed data

`04-postgres.yaml` starts Postgres completely empty (schemas + tables + rows, none of it - the
`webshop` database itself exists because `POSTGRES_DB` creates it, nothing more). Getting an
existing `pg_dump` SQL file into it is a one-time, imperative operation - there's no Kubernetes
object for "restore this file," so it isn't part of `kubectl apply -f`/`-k` at all:

```bash
# Wait for the StatefulSet's one Pod, postgres-0, to actually be Running first (see "Deploy" above).

# kubectl cp streams the file through the API server into the Pod's filesystem - slow for a large
# dump (tens of seconds to minutes depending on size), but needs nothing extra installed.
kubectl cp "D:\path\to\your-dump.sql" webshop/postgres-0:/tmp/dump.sql

# Runs psql INSIDE the Pod, reading the file that was just copied there - -v ON_ERROR_STOP stops
# at the first failing statement instead of plowing through the rest of a 200MB file and reporting
# success anyway.
kubectl exec -n webshop postgres-0 -- psql -U postgres -d webshop -v ON_ERROR_STOP=1 -f /tmp/dump.sql

# The copy above lives on the same Managed Disk as the database itself now - remove it once the
# restore succeeds rather than paying to keep two copies of the data around.
kubectl exec -n webshop postgres-0 -- rm /tmp/dump.sql
```

This expects a plain-SQL dump (`pg_dump`'s default `-Fp`, i.e. a `.sql` file `psql -f` can read
directly) of a database that already has the `vector` extension dumped into it (see
`04-postgres.yaml`'s comment on why the image is `pgvector/pgvector:pg17`, not plain `postgres`) -
a custom-format dump (`-Fc`, usually `.dump`/`.backup`) needs `pg_restore` inside the Pod instead
of `psql -f`.

## Troubleshooting

**"This form/login intermittently fails with a bare 400, no error page."** Check `web`'s logs for
`Antiforgery... The key {...} was not found in the key ring` - that's ASP.NET Core Data Protection,
not application code (see `Program.cs`'s comment on `AddDataProtection`): a page rendered by one
`web` replica encrypted a token with its own key, and the request landed on the other replica via
the Service's load balancing. If this comes back after `15-web.yaml`'s `web-dataprotection-keys`
PVC already exists, confirm both Pods actually mounted it (`kubectl describe pod -n webshop
<web-pod-name>` - `/keys` should appear under Volumes for both) rather than each falling back to
an ephemeral key ring some other way.

```bash
kubectl describe pod -n webshop <pod-name>   # events at the bottom - image pull errors,
                                              # probe failures, scheduling failures, etc.
kubectl logs -n webshop <pod-name>           # this container's stdout/stderr
kubectl logs -n webshop <pod-name> --previous  # its PREVIOUS run's logs, if it already restarted -
                                                # often the only place to see why it crashed

kubectl describe certificate -n webshop web-tls   # events at the bottom explain WHY it isn't
                                                   # Ready - e.g. "waiting for DNS", or a specific
                                                   # ACME error from Let's Encrypt itself
kubectl describe challenge -n webshop             # only exists while a letsencrypt-* issuance is
                                                   # in progress - the actual HTTP-01 attempt and
                                                   # its result, one level more detailed than the
                                                   # Certificate's own events
kubectl logs -n cert-manager deploy/cert-manager  # the controller's own logs, if neither of the
                                                   # above explains it

kubectl exec -n webshop postgres-0 -- psql -U postgres -d webshop -c '\dn'   # list schemas -
                                                                              # confirms the
                                                                              # restore populated
                                                                              # something
```

## Deliberately not in here

- **KEDA `ScaledObject`s / a `HorizontalPodAutoscaler`.** The cluster has KEDA enabled
  (`infra/modules/aks.bicep`) and it's a good fit for at least the embedding service (see
  `14-embedding.yaml`'s comment on why), but nothing here defines any scaling policy yet - every
  Deployment above just runs a fixed replica count.
- **Dapr sidecar annotations.** The cluster has the Dapr extension installed
  (`infra/main.bicep`), but none of these five .NET services actually use the Dapr SDK/building
  blocks in code today (they talk to each other over plain HTTP, and to Postgres over EF Core) -
  adding `dapr.io/enabled: "true"` would inject a sidecar that does nothing useful yet. Worth
  revisiting if/when a real Dapr building block (pub/sub, state management, ...) gets adopted in
  the application code itself.
- **DNS automation.** cert-manager's HTTP-01 solver (used here) needs the hostname to already
  resolve to the ingress IP before it can issue - it doesn't create DNS records itself. A DNS-01
  solver (e.g. Azure DNS) can prove ownership without a public HTTP endpoint at all and supports
  wildcard certificates, but needs an actual Azure DNS zone wired up - not set up here since this
  project has no domain of its own.
- **A Horizontal Pod Autoscaler's actual metric wiring** (e.g. `kubectl autoscale`) - deliberately
  paired with the KEDA point above; scaling policy is a decision to make once there's real load
  to look at, not something to guess at upfront.
