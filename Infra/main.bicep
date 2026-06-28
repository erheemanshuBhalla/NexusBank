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
var keyVaultName = 'kv-nexus-bank-${environment}-01'

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