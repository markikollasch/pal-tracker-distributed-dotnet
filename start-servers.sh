#!/usr/bin/env bash

set -e

function lcase() {
    echo $1 | tr '[:upper:]' '[:lower:]'
}

function startServer() {
    local appDir=Applications/${1}Server
    local dbName=tracker_$(lcase $1)_dev
    local port=$2
    local vcap_services=$(cat vcap_services.json | sed -e "s/<<DB_NAME>>/${dbName}/")
    
    cd $appDir
    
    export Logging__LogLevel__Default=Debug
    export REGISTRATION_SERVER_ENDPOINT=http://localhost:8883/
    export VCAP_SERVICES=${vcap_services}
    export DISABLE_AUTH=true
    
    dotnet run --server.urls "http://*:${port}"
}

trap "kill 0" EXIT
    startServer Registration 8883 &
    startServer Allocations 8881 &
    startServer Backlog 8882 &
    startServer Timesheets 8884 &
wait