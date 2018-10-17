#!/bin/sh
dotnet pack ./src/Gizmo/Gizmo.csproj -o ${PWD}
dotnet tool install -g --add-source ${PWD} gizmo