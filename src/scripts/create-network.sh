#!/bin/bash

# Variables
network_name="openai"

# Check if the openai docker network exists, if not create it
if [[ $(docker network ls --filter name=$network_name -q) == "" ]]; then
  docker network create openai
else
	 echo "$network_name network already exists"
fi