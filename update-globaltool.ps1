dotnet pack ./src/Gizmo/Gizmo.csproj -o ${PWD}/out
dotnet tool update -g --add-source ${PWD}/out gizmo