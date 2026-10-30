$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\QuietoraFocus\QuietoraFocus.csproj'
$output = Join-Path $PSScriptRoot '..\dist\win-x64'
$zipOutput = Join-Path $PSScriptRoot '..\dist\QuietoraFocus-v4.0-StudentOS-win-x64-portable.zip'

Write-Host 'Publishing Quietora Focus v4.0 Student OS Edition for Windows x64 (Single-File Self-Contained)...' -ForegroundColor Cyan

# Terminate running instances if any
Get-Process -Name "QuietoraFocus" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

# Copy Browser Shield extension alongside dist
$extSrc = Join-Path $PSScriptRoot '..\extensions\chromium'
$extDest = Join-Path $output 'browser-extension'
if (Test-Path $extSrc) {
    Copy-Item $extSrc -Destination $extDest -Recurse -Force
    Write-Host "Bundled Browser Shield extension into $extDest" -ForegroundColor Cyan
}

Write-Host "Published executable to: $output" -ForegroundColor Green

# Generate portable distribution ZIP
if (Test-Path $zipOutput) {
    Remove-Item $zipOutput -Force
}

Write-Host 'Creating portable distribution ZIP...' -ForegroundColor Cyan
Compress-Archive -Path "$output\*" -DestinationPath $zipOutput -CompressionLevel Optimal
Write-Host "Portable package generated: $zipOutput" -ForegroundColor Green
