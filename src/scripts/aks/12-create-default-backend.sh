#!/bin/bash

# Variables
source ./00-variables.sh

# Deploy manifest
kubectl apply -f $defaultBackendTemplate -n $nginxNamespace