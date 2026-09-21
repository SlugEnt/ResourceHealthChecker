# Builds the nuget package for the SlugEnt AD.Protocols
# And Copies it to Local Repository.

$Projects =@(
  'ResourceHealthChecker',
  'ResourceHealthChecker.FileSystem',
  'ResourceHealthChecker.SqlServer'
)

$repo = Read-Host "Which Repository to push to:  L = Local,  N = Nuget Public"
$repo = $repo.ToUpper()

$repository = "L"
if ($repo -eq "N") { $repository = "N"}

Write-Host "Script= $PSScriptRoot"

$scriptPath = $PSScriptRoot


& $Env:SlugEnt_DevScripts\BuildAndDeployToNuget_ver0003.ps1 $Projects $PSScriptRoot $repository