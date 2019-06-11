dotnet pack .\src\Gizmo\Gizmo.csproj -o %cd%\out
dotnet tool update -g --add-source %cd%\out gizmo