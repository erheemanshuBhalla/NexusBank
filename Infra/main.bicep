metadata description = 'Master orchestrator blueprint for Nexus Banking Infrastructure.'

@allowed([
  'dev'
  'prod'
])
@description('The deployment environment tier.')
param environment string

@description('The primary location for all deployed resources.')
param location string = resourceGroup().location

@secure()
@description('The administrator password for the SQL Server.')
param sqlAdminPassword string

// Centralized naming conventions utilizing our environment parameter dynamically
var identityName = 'id-nexus-ledger-${environment}-01'
var keyVaultName = take('kv-nexus-${environment}-${uniqueString(resourceGroup().id)}', 24)

// 1. Orchestrate the Identity Module
module appIdentity 'modules/identity.bicep' = {
  name: 'deploy-identity-${environment}'
  params: {
    identityName: identityName
    location: location
  }
}

// 2. Orchestrate the Key Vault Module and feed it the identity outputs
module appKeyVault 'modules/keyvault.bicep' = {
  name: 'deploy-keyvault-${environment}'
  params: {
    keyVaultName: keyVaultName
    location: location
    appPrincipalId: appIdentity.outputs.identityPrincipalId
  }
}

// Define a unique name for the Container Registry
param acrName string = 'acrnexus${uniqueString(resourceGroup().id)}'

// Create the Azure Container Registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

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
        vmSize: 'Standard_D2s_v6'
        osType: 'Linux'
        mode: 'System'
      }
    ]
  }
}

// Define names for the SQL Server and Database
var sqlServerName = 'sql-nexusbank-${environment}-${uniqueString(resourceGroup().id)}-v6'
var sqlDatabaseName = 'NexusLedgerDb'

// 1. Provision the Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    administrators: null
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// 2. Provision a cost-optimized, serverless development database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
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

// --- GLOBAL OUTPUTS ALIGNED TO TOP-LEVEL SCOPE ---
output deployedKeyVaultUri string = appKeyVault.outputs.kvUri
output acrLoginServer string = acr.properties.loginServer
output sqlServerFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName