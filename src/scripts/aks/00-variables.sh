#!/bin/bash

# Azure Subscription and Tenant
subscriptionId=$(az account show --query id --output tsv)
subscriptionName=$(az account show --query name --output tsv)
tenantId=$(az account show --query tenantId --output tsv)

# Azure Kubernetes Service
aksClusterName="TanAks"
aksResourceGroupName="TanRG"

# Azure Key Vault
keyVaultName="TanKeyVault"
keyVaultSku="Standard"

# Azure Container Registry
acrName="TanAcr"

# Azure Resources
location="EastUS"
resourceGroupName="TanRG"

# Azure Managed Identity
managedIdentityName="TanWorkloadManagedIdentity"

# Test Service Principal
servicePrincipalName="OpenAiTestServicePrincipal"

# Container Images
containerImageName="openaiservice"
containerImageTag="v1"
image="${acrName,,}.azurecr.io/$containerImageName:$containerImageTag"
imagePullPolicy="IfNotPresent" # Always, Never, IfNotPresent

# Kubernetes Service account
namespace="openai"
serviceAccountName="openai-sa"

# Variables for the federated identity name
federatedIdentityName="OpenAiWorkloadFederatedIdentity"

# Azure OpenAI Service
openAiNames=("BetaOpenAI" "AlphaOpenAI")
openAiResourceGroupNames=("TanRG" "BaboRG")

# NGINX
nginxNamespace="ingress-basic"
nginxRepoName="ingress-nginx"
nginxRepoUrl="https://kubernetes.github.io/ingress-nginx"
nginxChartName="ingress-nginx"
nginxReleaseName="nginx-ingress"
nginxReplicaCount=3

# Azure DNS
dnsZoneName="babosbird.com"
dnsZoneResourceGroupName="dnsresourcegroup"
httpSubdomain="BetaOpenAIhttp"
grpcSubdomain="BetaOpenAIgrpc"

# Certificate Manager
certManagerNamespace="cert-manager"
certManagerRepoName="jetstack"
certManagerRepoUrl="https://charts.jetstack.io"
certManagerChartName="cert-manager"
certManagerReleaseName="cert-manager"
email="paolos@microsoft.com"
clusterIssuer="letsencrypt-nginx"
template="cluster-issuer.yml"

# Default Backend
defaultBackendTemplate="default-backend.yml"

# Templates
serviceTemplate="service.yml"
keyVaultDeploymentTemplate="deployment-keyvault.yml"
appSettingsDeploymentTemplate="deployment-appsettings.yml"
keyVaultConfigMapTemplate="configmap-keyvault.yml"
appSettingsConfigMapTemplate="configmap-appsettings.yml"

# ConfigMap values
configMapName="openai"
configurationType="keyvault" # appsettings, keyvault
aspNetCoreEnvironment="Production"

# Ingress
httpIngressName="openai-http"
httpIngressTemplate="ingress-http.yml"
httpSecretName="openai-http-tls"
httpHostName="${httpSubdomain,,}.${dnsZoneName,,}"
httpServiceName="openai"
httpServicePort="80"
grpcIngressName="openai-grpc"
grpcIngressTemplate="ingress-grpc.yml"
grpcSecretName="openai-grpc-tls"
grpcHostName="${grpcSubdomain,,}.${dnsZoneName,,}"
grpcServiceName="openai"
grpcServicePort="6000"


