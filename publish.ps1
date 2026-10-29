<#
.SYNOPSIS
Builds the module and publishes it to the PowerShell Gallery.

.DESCRIPTION
Publish-Module requires the module to be in a directory named like the module,
so the Release build output is staged in artifacts/ProductivityTools.TechBricks
first. The API key is taken from -ApiKey or the PSGALLERY_API_KEY environment
variable (https://www.powershellgallery.com/account/apikeys).

.EXAMPLE
$env:PSGALLERY_API_KEY = '<key>'; ./publish.ps1 -WhatIf
./publish.ps1 -ApiKey '<key>'
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ApiKey = $env:PSGALLERY_API_KEY,
    [string]$Repository = 'PSGallery'
)

$ErrorActionPreference = 'Stop'
$moduleName = 'ProductivityTools.TechBricks'
$root = $PSScriptRoot

if (-not $ApiKey) {
    throw "Provide -ApiKey or set the PSGALLERY_API_KEY environment variable."
}

dotnet build (Join-Path $root 'ProductivityTools.TechBricks.Cmdlet.sln') -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

$output = Join-Path $root 'ProductivityTools.TechBricks.Cmdlet/bin/Release/net8.0'
$staging = Join-Path $root "artifacts/$moduleName"
# Staging is always done (also with -WhatIf), only Publish-Module honours -WhatIf.
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force -WhatIf:$false }
New-Item -ItemType Directory -Path $staging -WhatIf:$false | Out-Null
Get-ChildItem $output -File | Where-Object { $_.Extension -in '.dll', '.psd1' } | Copy-Item -Destination $staging -WhatIf:$false

$manifest = Test-ModuleManifest (Join-Path $staging "$moduleName.psd1")
Write-Host "Publishing $moduleName $($manifest.Version) to $Repository"

Publish-Module -Path $staging -NuGetApiKey $ApiKey -Repository $Repository -WhatIf:$WhatIfPreference
