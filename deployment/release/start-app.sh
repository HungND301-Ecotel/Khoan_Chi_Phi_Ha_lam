#!/bin/bash
# Start the application in the release environment
# Docker compose for deployment
docker compose -f release-docker-compose.yaml down # always down first
docker compose -f release-docker-compose.yaml pull # always pull the latest images
# run containers in detached mode
nohup docker compose -f release-docker-compose.yaml up  -d > deploy.log 2>&1 & 
# Check built images
docker image prune -f # clean up unused images
docker images