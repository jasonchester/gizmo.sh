dotnet pack ./src/Gizmo/Gizmo.csproj -o ${PWD}
dotnet tool update -g --add-source ${PWD} gizmo