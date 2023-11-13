#!/bin/bash

# For more information, see https://azure.github.io/azure-workload-identity/docs/quick-start.html

# Variables
source ./00-variables.sh

# Attach ACR to AKS cluster
if [[ $attachAcr == true ]]; then
  echo "Attaching ACR $acrName to AKS cluster $aksClusterName..."
  az aks update \
    --name $aksClusterName \
    --resource-group $aksResourceGroupName \
    --attach-acr $acrName
fi

# Create the namespace if it doesn't already exists in the cluster
result=$(kubectl get namespace -o jsonpath="{.items[?(@.metadata.name=='$namespace')].metadata.name}")

if [[ -n $result ]]; then
  echo "[$namespace] namespace already exists in the cluster"
else
  echo "[$namespace] namespace does not exist in the cluster"
  echo "creating [$namespace] namespace in the cluster..."
  kubectl create namespace $namespace
fi

if [[ $configurationType == "keyvault" ]]; then
  echo "Selected configuration type is keyvault"
  # Create configmap
  cat $keyVaultConfigMapTemplate |
    yq "(.metadata.name)|="\""$configMapName"\" |
    yq "(.data.aspNetCoreEnvironment)|="\""$aspNetCoreEnvironment"\" |
    yq "(.data.keyVaultName)|="\""$keyVaultName"\" |
    kubectl apply -n $namespace -f -

  # Create deployment
  cat $keyVaultDeploymentTemplate |
    yq "(.spec.template.spec.containers[0].image)|="\""$image"\" |
    yq "(.spec.template.spec.containers[0].imagePullPolicy)|="\""$imagePullPolicy"\" |
    yq "(.spec.template.spec.serviceAccountName)|="\""$serviceAccountName"\" |
    kubectl apply -n $namespace -f -
else
  echo "Selected configuration type is appsettings"
  # Create configmap
  cat $appSettingsConfigMapTemplate |
    yq "(.metadata.name)|="\""$configMapName"\" |
    yq "(.data.aspNetCoreEnvironment)|="\""$aspNetCoreEnvironment"\" |
    kubectl apply -n $namespace -f -

  # Create deployment
  cat $appSettingsDeploymentTemplate |
    yq "(.spec.template.spec.containers[0].image)|="\""$image"\" |
    yq "(.spec.template.spec.containers[0].imagePullPolicy)|="\""$imagePullPolicy"\" |
    yq "(.spec.template.spec.serviceAccountName)|="\""$serviceAccountName"\" |
    yq "(.spec.template.spec.volumes[0].configMap.name)|="\""$configMapName"\" |
    kubectl apply -n $namespace -f -
fi

# Create service
kubectl apply -f $serviceTemplate -n $namespace

# Create HTTP ingress
cat $httpIngressTemplate |
  yq "(.metadata.name)|="\""$httpIngressName"\" |
  yq "(.spec.tls[0].hosts[0])|="\""$httpHostName"\" |
  yq "(.spec.tls[0].secretName)|="\""$httpSecretName"\" |
  yq "(.spec.rules[0].host)|="\""$httpHostName"\" |
  yq "(.spec.rules[0].http.paths[0].backend.service.name)|="\""$httpServiceName"\" |
  yq "(.spec.rules[0].http.paths[0].backend.service.port.number)|=$httpServicePort" |
  kubectl apply -n $namespace -f -

# Create gRPC ingress
cat $grpcIngressTemplate |
  yq "(.metadata.name)|="\""$grpcIngressName"\" |
  yq "(.spec.tls[0].hosts[0])|="\""$grpcHostName"\" |
  yq "(.spec.tls[0].secretName)|="\""$grpcSecretName"\" |
  yq "(.spec.rules[0].host)|="\""$grpcHostName"\" |
  yq "(.spec.rules[0].http.paths[0].backend.service.name)|="\""$grpcServiceName"\" |
  yq "(.spec.rules[0].http.paths[0].backend.service.port.number)|=$grpcServicePort" |
  kubectl apply -n $namespace -f -
