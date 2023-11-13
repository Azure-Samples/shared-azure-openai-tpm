#!/bin/bash

# Variables
source ./00-variables.sh

#!/bin/bash

# Check if service principal exists
echo "Checking if the service principal [$servicePrincipalName] already exists..."
appId=$(az ad sp list \
  --display-name $servicePrincipalName \
  --query [].appId \
  --output tsv)

if [[ -n $appId ]]; then
  echo "Service principal [$servicePrincipalName] already exists."
else
  # Create service principal
  az ad sp create-for-rbac \
    --name "$servicePrincipalName" \
    --role reader \
    --years 5 \
    --scopes /subscriptions/$subscriptionId

  if [[ $? -eq 0 ]]; then
    echo "Service principal [$servicePrincipalName] successfully created."
  else
    echo "Failed to create service principal [$servicePrincipalName]."
    exit 1
  fi

  # Retrieve service principal appId
  echo "Retrieving appId for [$servicePrincipalName] service principal..."
  appId=$(az ad sp list \
    --display-name $servicePrincipalName \
    --query [].appId \
    --output tsv)

  if [[ -n $appId ]]; then
    echo "[$appId] appId  for the [$servicePrincipalName] service principal successfully retrieved"
  else
    echo "Failed to retrieve appId for the [$servicePrincipalName] service principal"
    exit 1
  fi
fi

# Grant get and list permissions on key vault secrets to the service principal
echo "Granting Get and List permissions on secrets in [$keyVaultName] key vault to [$servicePrincipalName] service principal..."
az keyvault set-policy \
  --name $keyVaultName \
  --spn $appId \
  --secret-permissions get list 1>/dev/null

if [[ $? == 0 ]]; then
  echo "Get and List permissions on secrets in [$keyVaultName] key vault successfully granted to [$servicePrincipalName] service principal"
else
  echo "Failed to grant Get and List permissions on secrets in [$keyVaultName] key vault to [$servicePrincipalName] service principal"
  exit
fi

if [[ $? == 0 ]]; then
  echo "Access policy successfully set for the [$servicePrincipalName] service principal on the [$keyVaultName] key vault"
else
  echo "Failed to set the access policy for the [$servicePrincipalName] service principal on the [$keyVaultName] key vault"
fi

for ((i = 0; i < ${#openAiNames[@]}; i++)); do
  openAiName=${openAiNames[$i]}
  openAiResourceGroupName=${openAiResourceGroupNames[$i]}

  # Get the resource id of the Azure OpenAI resource
  openAiId=$(az cognitiveservices account show \
    --name $openAiName \
    --resource-group $openAiResourceGroupName \
    --query id \
    --output tsv)

  if [[ -n $openAiId ]]; then
    echo "Resource id for the [$openAiName] Azure OpenAI resource successfully retrieved"
  else
    echo "Failed to the resource id for the [$openAiName] Azure OpenAI resource"
    exit -1
  fi

  # Assign the Cognitive Services User role on the Azure OpenAI resource to the service principal
  role="Cognitive Services User"
  echo "Checking if the [$servicePrincipalName] service principal has been assigned to [$role] role with [$openAiName] Azure OpenAI resource as a scope..."
  current=$(az role assignment list \
    --assignee $appId \
    --scope $openAiId \
    --query "[?roleDefinitionName=='$role'].roleDefinitionName" \
    --output tsv 2>/dev/null)

  if [[ $current == $role ]]; then
    echo "[$servicePrincipalName] service principal is already assigned to the ["$current"] role with [$openAiName] Azure OpenAI resource as a scope"
  else
    echo "[$servicePrincipalName] service principal is not assigned to the [$role] role with [$openAiName] Azure OpenAI resource as a scope"
    echo "Assigning the [$role] role to the [$servicePrincipalName] service principal with [$openAiName] Azure OpenAI resource as a scope..."

    az role assignment create \
      --assignee $appId \
      --role "$role" \
      --scope $openAiId 1>/dev/null

    if [[ $? == 0 ]]; then
      echo "[$servicePrincipalName] service principal successfully assigned to the [$role] role with [$openAiName] Azure OpenAI resource as a scope"
    else
      echo "Failed to assign the [$servicePrincipalName] service principal to the [$role] role with [$openAiName] Azure OpenAI resource as a scope"
      exit
    fi
  fi
done
