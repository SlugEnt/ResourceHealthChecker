Echo Creates Release Packages

set packages="..\packages\release"
dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker
dotnet pack -o %packages% ..\src\ResourceHealthChecker

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.FileSystem
dotnet pack -o %packages% ..\src\ResourceHealthChecker.FileSystem

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.SqlServer
dotnet pack -o %packages% ..\src\ResourceHealthChecker.SqlServer

REM for %%n in (..\packages\*.nupkg) do  dotnet nuget push -s d:\a_dev\LocalNugetPackages "%%n"
