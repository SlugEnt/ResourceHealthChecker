Echo Creates Debug Packages and pushes to Local Nuget Repo

rem #msbuild /t:pack /p:Configuration=Debug
rem cd ..\src\ResourceHealthChecker\bin\Debug\net6.0
rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker
dotnet pack -o ..\packages ..\src\ResourceHealthChecker

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.FileSystem
dotnet pack -o ..\packages ..\src\ResourceHealthChecker.FileSystem

rem dotnet msbuild /p:Configuration=Debug ..\src\ResourceHealthChecker.SqlServer
dotnet pack -o ..\packages ..\src\ResourceHealthChecker.SqlServer

for %%n in (..\packages\*.nupkg) do  dotnet nuget push -s d:\a_dev\LocalNugetPackages "%%n"
