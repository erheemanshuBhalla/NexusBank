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
var sqlServerName = 'sql-nexusbank-${environment}-${uniqueString(resourceGroup().id)}-v8'
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


// ==========================================
// OBSERVABILITY & LOGGING WORKSPACES (WEEK 8)
// ==========================================

@description('The name of the Log Analytics Workspace')
var logAnalyticsName = 'log-nexus-${environment}-${uniqueString(resourceGroup().id)}'

@description('The name of the Application Insights instance')
var appInsightsName = 'appins-nexus-${environment}-${uniqueString(resourceGroup().id)}'

// 1. The Raw Storage Vault for Logs
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018' // Standard pay-as-you-go telemetry pricing tier
    }
    retentionInDays: 30 // Keep logs for 30 days to avoid extra storage bills
  }
}

// 2. The Intelligent Telemetry Engine
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id // Links App Insights directly to our storage workspace above
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Output the Instrumentation Key and Connection String so our .NET applications can reference it later
output appInsightsConnectionString string = appInsights.properties.ConnectionString

// Output the dynamically generated AKS Cluster Name so the pipeline can read it
output aksClusterName string = aksCluster.name // (Change 'aksCluster' to match your resource symbolic name if it differs)

// ==========================================
// API MANAGEMENT GATEWAY (WEEK 8)
// ==========================================

@description('The name of the API Management Service')
var apimServiceName = 'apim-nexus-${environment}-${uniqueString(resourceGroup().id)}'

@description('The email address associated with the APIM owner')
param apimPublisherEmail string = 'admin@nexusbank.com'

@description('The organization name for APIM')
param apimPublisherName string = 'NexusBank Enterprise'

resource apiManagementService 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimServiceName
  location: location
  sku: {
    name: 'Consumption' // Lightweight, fast deployment, pay-as-you-go pricing tier
    capacity: 0
  }
  properties: {
    publisherEmail: apimPublisherEmail
    publisherName: apimPublisherName
  }
}

// Output the public gateway URL so we know where to send requests
output apimGatewayUrl string = apiManagementService.properties.gatewayUrl

// ==========================================
// APIM API ROUTING CONTRACTS
// ==========================================

// 1. Define the Nexus Ledger API Blueprint inside the Gateway
resource bankApiRoute 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apiManagementService
  name: 'nexus-ledger-api'
  properties: {
    displayName: 'NexusBank Ledger Service'
    description: 'Managed API gateway routing for core banking ledger actions.'
    
    // This defines the public suffix URL path (e.g., https://your-apim.azure-api.net/ledger)
    path: 'ledger'
    protocols: [
      'https'
    ]
    
    // Right now, this maps directly to your active service load balancer address
    // In an enterprise setup, this points to an internal private ingress controller IP
    serviceUrl: 'http://ledger-api.default.svc.cluster.local'
    subscriptionRequired: false // Disabled for initial testing convenience
  }
}

// 2. Define a catch-all route operation so everything under /ledger/* is passed to AKS
resource catchAllOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: bankApiRoute
  name: 'catch-all-requests'
  properties: {
    displayName: 'Route Gateway Traffic'
    method: 'GET'
    urlTemplate: '/*'
    templateParameters: []
  }
}
// ==========================================
// APIM POLICY MANAGEMENT (RATE LIMITING)
// ==========================================

resource bankApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  parent: bankApiRoute
  name: 'policy'
  properties: {
    value: '''<policies>
      <inbound>
        <base />
        <rate-limit calls="10" renewal-period="60" />
      </inbound>
      <backend>
        <base />
      </backend>
      <outbound>
        <base />
      </outbound>
      <on-error>
        <base />
      </on-error>
    </policies>'''
    format: 'rawxml'
  }
}