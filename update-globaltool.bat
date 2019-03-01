dotnet pack ./src/Gizmo/Gizmo.csproj -o %cd%
dotnet tool update -g --add-source %cd% gizmo