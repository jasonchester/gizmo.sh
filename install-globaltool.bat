dotnet pack ./src/Gizmo/Gizmo.csproj -o %cd%
dotnet tool install -g --add-source %cd% gizmo