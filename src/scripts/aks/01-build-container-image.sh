#!/bin/bash

#Variables
source ./00-variables.sh

cd ../../openairestapi
docker build -t $containerImageName:$containerImageTag -f Dockerfile .