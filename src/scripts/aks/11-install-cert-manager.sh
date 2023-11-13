#!/bin/bash

# Variables
source ./00-variables.sh

# Install cert-manager Helm chart
result=$(helm list -n $certManagerNamespace | grep $certManagerReleaseName | awk '{print $1}')

if [[ -n $result ]]; then
	echo "[$certManagerReleaseName] cert-manager already exists in the $certManagerNamespace namespace"
else
	# Check if the jetstack repository is not already added
	result=$(helm repo list | grep $certManagerRepoName | awk '{print $1}')

	if [[ -n $result ]]; then
		echo "[$certManagerRepoName] Helm repo already exists"
	else
		# Add the jetstack Helm repository
		echo "Adding [$certManagerRepoName] Helm repo..."
		helm repo add $certManagerRepoName $certManagerRepoUrl
	fi

	# Update your local Helm chart repository cache
	echo 'Updating Helm repos...'
	helm repo update

	# Install the cert-manager Helm chart
	echo "Deploying [$certManagerReleaseName] cert-manager to the $certManagerNamespace namespace..."
	helm install $certManagerReleaseName $certManagerRepoName/$certManagerChartName \
		--create-namespace \
		--namespace $certManagerNamespace \
		--set installCRDs=true \
		--set nodeSelector."kubernetes\.io/os"=linux
fi

# Check if the cluster issuer already exists
result=$(kubectl get ClusterIssuer -o json | jq -r '.items[].metadata.name | select(. == "'$clusterIssuer'")')

if [[ -n $result ]]; then
	echo "[$clusterIssuer] cluster issuer already exists"
	exit
else
	# Create the cluster issuer
	echo "[$clusterIssuer] cluster issuer does not exist"
	echo "Creating [$clusterIssuer] cluster issuer..."
	cat $template | yq "(.spec.acme.email)|="\""$email"\" | kubectl apply -f -
fi
