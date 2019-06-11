#!/bin/sh
dotnet pack ./src/Gizmo/Gizmo.csproj -o ${PWD}/out
dotnet tool install -g --add-source ${PWD}/out gizmo