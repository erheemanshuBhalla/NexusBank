metadata description = 'Creates a User-Assigned Managed Identity for the application pod.'

param identityName string
param location string = resourceGroup().location

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

// We output the Client ID and Principal ID so our master template 
// can pass them to the Key Vault and Kubernetes configurations later!
output identityClientId string = managedIdentity.properties.clientId
output identityPrincipalId string = managedIdentity.properties.principalId
output identityId string = managedIdentity.id