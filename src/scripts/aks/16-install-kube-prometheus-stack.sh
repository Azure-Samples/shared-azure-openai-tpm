#!/bin/bash

# For more information, see:
# https://kubernetes.github.io/ingress-nginx/user-guide/monitoring/#configure-prometheus
# https://github.com/kubernetes/ingress-nginx/tree/main/deploy/grafana/dashboards
# https://prometheus.io/docs/prometheus/latest/configuration/configuration/#scrape_config

# Variables
namespace="prometheus"
release="prometheus"

# Upgrade Helm chart
helm upgrade $release prometheus-community/kube-prometheus-stack \
  --namespace $namespace \
  --set prometheus.prometheusSpec.podMonitorSelectorNilUsesHelmValues=false \
  --set prometheus.prometheusSpec.serviceMonitorSelectorNilUsesHelmValues=false \
  --values kube-prometheus-stack-custom-values.yml

# Get values
helm get values $release --namespace $namespace
