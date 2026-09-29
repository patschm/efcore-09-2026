param location string
param clusterName string
param dnsPrefix string
param systemNodeVmSize string
param systemNodeCount int
param systemNodeMinCount int
param systemNodeMaxCount int
param enableKeda bool
param enableManagedPrometheus bool
param enableAppRoutingAddon bool
param logAnalyticsWorkspaceId string
param tags object = {}

resource aks 'Microsoft.ContainerService/managedClusters@2024-09-01' = {
  name: clusterName
  location: location
  tags: tags
  identity: {
    // Lets the cluster manage its own supporting resources (load balancers, disks, ACR pull
    // role assignments) without a separate app registration to maintain.
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: dnsPrefix
    agentPoolProfiles: [
      {
        name: 'system'
        mode: 'System'
        osType: 'Linux'
        vmSize: systemNodeVmSize
        count: systemNodeCount
        enableAutoScaling: true
        minCount: systemNodeMinCount
        maxCount: systemNodeMaxCount
        osDiskSizeGB: 128
      }
    ]
    // Azure CNI Overlay - pods get addresses from a private overlay space rather than the VNet,
    // so the subnet doesn't need to be sized for pod IPs. No custom VNet is created here; AKS
    // provisions its own.
    networkProfile: {
      networkPlugin: 'azure'
      networkPluginMode: 'overlay'
      networkPolicy: 'azure'
    }
    // Required for Dapr/KEDA components that authenticate to Azure resources via Workload
    // Identity (federated credentials) rather than embedded secrets.
    oidcIssuerProfile: {
      enabled: true
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
    workloadAutoScalerProfile: {
      keda: {
        enabled: enableKeda
      }
    }
    // The "app routing" add-on: a managed NGINX ingress controller (ingress class
    // webapprouting.kubernetes.azure.com), so getting HTTPS working doesn't first require a
    // separate Helm install of ingress-nginx. Azure runs its control plane and upgrades it
    // alongside the cluster, same story as the Dapr extension below. It only gives you the
    // controller + a public LoadBalancer IP to send traffic at - actual certificates still need
    // cert-manager (see k8s/03-cert-manager-issuers.yaml), which this add-on does not install.
    ingressProfile: {
      webAppRouting: {
        enabled: enableAppRoutingAddon
      }
    }
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceId
        }
      }
    }
    azureMonitorProfile: {
      metrics: {
        enabled: enableManagedPrometheus
      }
    }
  }
}

output name string = aks.name
output id string = aks.id
output kubeletPrincipalId string = aks.properties.identityProfile.kubeletidentity.objectId
output oidcIssuerUrl string = aks.properties.oidcIssuerProfile.issuerURL
