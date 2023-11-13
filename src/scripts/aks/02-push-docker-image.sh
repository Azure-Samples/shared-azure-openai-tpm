#!/bin/bash

# Variables
source ./00-variables.sh

# Login to ACR
az acr login --name ${acrName,,}

# Retrieve ACR login server. Each container image needs to be tagged with the loginServer name of the registry. 
loginServer=$(az acr show --name ${acrName,,} --query loginServer --output tsv)

# Tag the local container image with the loginServer of ACR
docker tag $containerImageName:$containerImageTag $loginServer/$containerImageName:$containerImageTag

# Push $containerImageName container image to ACR
docker push $loginServer/$containerImageName:$containerImageTag