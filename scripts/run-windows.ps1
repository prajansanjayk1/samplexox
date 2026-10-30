$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\QuietoraFocus\QuietoraFocus.csproj'
Write-Host 'Starting Quietora Focus...' -ForegroundColor Cyan
dotnet run --project $project
if ($LASTEXITCODE -ne 0) { throw 'Build or launch failed. Check .NET 10 SDK and NuGet access.' }
