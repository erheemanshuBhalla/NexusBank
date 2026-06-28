metadata description = 'Deploys an Azure Key Vault with RBAC authorization and assigns access permissions.'

param keyVaultName string
param location string = resourceGroup().location
param appPrincipalId string // Passed in from the identity module output

// Global Azure GUID for the built-in "Key Vault Secrets User" role
// Change this line inside infra/modules/keyvault.bicep:
var secretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6' // ◄ Updated with correct ID digits!

// 1. Deploy the Key Vault
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true // Enforces modern Azure RBAC security model
    enabledForDeployment: true
    enabledForTemplateDeployment: true
  }
}

// 2. Automatically assign the RBAC Role inside the Vault to our Identity
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, appPrincipalId, secretsUserRoleId) // Generates a unique deterministic ID for the assignment
  scope: kv // Appies this permission context strictly inside this Key Vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', secretsUserRoleId)
    principalId: appPrincipalId
    principalType: 'ServicePrincipal' // Managed Identities act as Service Principals in Entra ID
  }
}

output kvUri string = kv.properties.vaultUri