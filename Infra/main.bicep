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
    location: location
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