metadata description = 'Master orchestrator blueprint for Nexus Banking Infrastructure.'

@allowed([
  'dev'
  'prod'
])
@description('The deployment environment tier.')
param environment string

@description('The primary location for all deployed resources.')
param location string = resourceGroup().location

// Centralized naming conventions utilizing our environment parameter
var identityName = 'id-nexus-ledger-${environment}-01'
param keyVaultName string = 'kv-nexusbank-dev-xyz99' // Put your own custom letters/numbers here

// 1. Orchestrate the Identity Module
module appIdentity 'modules/identity.bicep' = {
  name: 'deploy-identity-${environment}'
  params: {
    identityName: identityName
    location: location // ◄ FIXED: Removed the single quotes here!
  }
}

// 2. Orchestrate the Key Vault Module and feed it the identity outputs
module appKeyVault 'modules/keyvault.bicep' = {
  name: 'deploy-keyvault-${environment}'
  params: {
    keyVaultName: keyVaultName
    location: location
    appPrincipalId: appIdentity.outputs.identityPrincipalId // ◄ The link happens here!
  }
}

output deployedKeyVaultUri string = appKeyVault.outputs.kvUri

// Define a unique name for the Container Registry
param acrName string = 'acrnexus${uniqueString(resourceGroup().id)}'

// Create the Azure Container Registry (Basic tier is perfect for dev environments)
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: resourceGroup().location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true // Allows your pipeline to easily authenticate
  }
}

// Output the ACR Login Server url so your GitHub pipeline can see where to push images
output acrLoginServer string = acr.properties.loginServer

param aksClusterName string = 'aks-nexus-${environment}-01'

// Create a cost-optimized, single-node AKS cluster for development
resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-02-01' = {
  name: aksClusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Base'
    tier: 'Free'
  }
  properties: {
    dnsPrefix: 'nexusbank-${environment}'
   agentPoolProfiles: [
      {
        name: 'agentpool'
        count: 1
        vmSize: 'Standard_D2s_v7' // ◄ CHANGED: Replaced B2s with an allowed, cost-effective dev size
        osType: 'Linux'
        mode: 'System'
      }
    ]
  }
}

// Grant the AKS cluster permission to pull images from your ACR vault
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, aksCluster.id, 'AcrPull')
  scope: acr
  properties: {
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull Role ID
    principalType: 'ServicePrincipal'
  }
}
// Define names for the SQL Server and Database (Bumped to -v6)
var sqlServerName = 'sql-nexusbank-${environment}-${uniqueString(resourceGroup().id)}-v6'
var sqlDatabaseName = 'NexusLedgerDb'

// 1. Provision the Azure SQL Server (Targeting westus with standard authentication)
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: 'westus'
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword // ◄ FIXED: Passing the secure param instead
    administrators: null
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// 2. Provision a cost-optimized, serverless development database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: 'westus'
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368
    autoPauseDelay: 60
    minCapacity: any('0.5')
  }
}

// 3. Firewall rule allowing your AKS cluster (and all Azure services) through
resource firewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Output the full server fully-qualified domain name (FQDN) for your connection string
output sqlServerFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName