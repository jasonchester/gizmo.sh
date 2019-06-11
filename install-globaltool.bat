dotnet pack .\src\Gizmo\Gizmo.csproj -o %cd%\out
dotnet tool install -g --add-source %cd%\out gizmo