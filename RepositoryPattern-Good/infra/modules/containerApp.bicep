// One Container App = roughly one k8s/*.yaml Deployment+Service pair, but flatter: ACA folds
// "how many replicas", "what's the image", "is this reachable from outside", and "does it get a
// Dapr sidecar" into one resource instead of three (Deployment/Service/annotation). Called once
// per service from aca.bicep, the same way each k8s/1x-*.yaml file covers one service.
param name string
param location string
param tags object = {}
param environmentId string
param acrLoginServer string
param userAssignedIdentityId string
param image string
param containerPort int
@description('true = reachable from the internet (only Web needs this - see k8s/15-web.yaml\'s Ingress); false = reachable only from other Container Apps in this environment (matching every other service\'s plain ClusterIP Service).')
param externalIngress bool = false

@description('This app\'s Dapr app-id - how every OTHER Container App in this environment invokes it (see BuildingBlocks/Api/DaprServiceInvocation.cs). Unlike AKS, there\'s no dapr.io/enabled annotation or cluster extension to install first - Dapr is a built-in capability of the environment; this block is the entire opt-in.')
param daprAppId string

param cpu string = '0.5'
param memory string = '1Gi'
param minReplicas int = 1
param maxReplicas int = 3

@description('Plain (non-secret) environment variables.')
param env array = []
@description('Secret-backed environment variables: {name, secretName} pairs, where secretName matches an entry in the `secrets` param below.')
param secretEnv array = []
@description('Container App-level secrets, as a {secretName: value} map - an object, not an array, because Bicep\'s @secure() decorator only applies to string/object parameters, not array (an array param here would leave real secret values unredacted in deployment history/what-if output).')
@secure()
param secrets object = {}

param probePath string = '/health'
param startupPeriodSeconds int = 5
param startupFailureThreshold int = 60
param livenessPeriodSeconds int = 10
param readinessPeriodSeconds int = 5

var resolvedSecrets = [for key in items(secrets): {
  name: key.key
  value: key.value
}]

var secretEnvItems = [for e in secretEnv: {
  name: e.name
  secretRef: e.secretName
}]
var resolvedEnv = concat(env, secretEnvItems)

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: resolvedSecrets
      registries: [
        {
          server: acrLoginServer
          identity: userAssignedIdentityId
        }
      ]
      ingress: {
        external: externalIngress
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
      }
      dapr: {
        enabled: true
        appId: daprAppId
        appProtocol: 'http'
        appPort: containerPort
      }
    }
    template: {
      containers: [
        {
          name: name
          image: image
          env: resolvedEnv
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: [
            {
              type: 'Startup'
              httpGet: { path: probePath, port: containerPort }
              periodSeconds: startupPeriodSeconds
              failureThreshold: startupFailureThreshold
            }
            {
              type: 'Liveness'
              httpGet: { path: probePath, port: containerPort }
              periodSeconds: livenessPeriodSeconds
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: probePath, port: containerPort }
              periodSeconds: readinessPeriodSeconds
              failureThreshold: 2
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output fqdn string = externalIngress ? containerApp.properties.configuration.ingress.fqdn : ''
output name string = containerApp.name
