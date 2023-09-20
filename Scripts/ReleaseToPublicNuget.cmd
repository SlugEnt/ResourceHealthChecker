Echo Creates Release Packages

set packages="..\packages\release"

set program="..\src\ResourceHealthChecker"
dotnet msbuild /p:Configuration=Release %program%
dotnet pack -o %packages% %program%

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.FileSystem
set program="..\src\ResourceHealthChecker.FileSystem"
dotnet pack -o %packages% %program%

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.SqlServer
set program="..\src\ResourceHealthChecker.SqlServer"
dotnet pack -o %packages% %program%

REM - Push Locally
REM for %%n in (..\packages\*.nupkg) do  dotnet nuget push -s d:\a_dev\LocalNugetPackages "%%n"

Rem 