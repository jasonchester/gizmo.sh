#!/bin/bash
# dotnet pack src/Gizmo/Gizmo.csproj 
dotnet publish src/Gizmo/Gizmo.csproj -p:PackAsTool=False -o ${PWD}/out/gizmo.win-x64 -r win-x64
dotnet publish src/Gizmo/Gizmo.csproj -p:PackAsTool=False -o ${PWD}/out/gizmo.linux-x64 -r linux-x64
dotnet publish src/Gizmo/Gizmo.csproj -p:PackAsTool=False -o ${PWD}/out/gizmo.osx-x64 -r osx-x64