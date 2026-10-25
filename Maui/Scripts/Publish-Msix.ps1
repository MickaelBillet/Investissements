<#
.SYNOPSIS
    Builds and signs the installable MSIX package into Maui/artifacts/.

.DESCRIPTION
    Requires the certificate created by New-DevCertificate.ps1. The package is signed by thumbprint from the
    current user's certificate store.
#>
[CmdletBinding()]
param(
    [string] $Subject = 'CN=Mickael Billet',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $certificate) {
    throw "No valid code-signing certificate '$Subject' found. Run Maui\Scripts\New-DevCertificate.ps1 first."
}

$project = Join-Path $PSScriptRoot '..\InvestissementsDashboard.Maui.csproj'

# Windows refuses to install a package whose identity (name + version) matches an installed one but whose content
# differs, so every build needs a higher version. The commit count grows monotonically and stays below the 65535
# limit of an MSIX version part. Rebuilding the same commit with local edits keeps the same number: uninstall first then.
$buildNumber = [int](git -C $PSScriptRoot rev-list --count HEAD)
if ($LASTEXITCODE -ne 0) { throw 'Could not compute the package version from git (git rev-list failed).' }

# Resizetizer copies the version into this generated manifest but its incremental check ignores ApplicationVersion:
# a stale copy would silently package the previous version, so it is regenerated on every build.
Get-ChildItem (Join-Path $PSScriptRoot "..\obj\$Configuration") -Recurse -Filter Package.appxmanifest -ErrorAction SilentlyContinue |
    Where-Object { $_.Directory.Name -eq 'm' -and $_.Directory.Parent.Name -eq 'resizetizer' } |
    Remove-Item -Force

dotnet publish $project -f net10.0-windows10.0.19041.0 -c $Configuration `
    -p:WindowsPackageType=MSIX `
    -p:ApplicationVersion=$buildNumber `
    -p:RuntimeIdentifierOverride=win10-x64 `
    -p:PackageCertificateThumbprint=$($certificate.Thumbprint) | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)." }

# Looked up by version rather than by date, so a package built with the wrong version fails here instead of
# being installed under an already-used version.
$package = Get-ChildItem (Join-Path $PSScriptRoot '..\artifacts') -Recurse -Filter "*.$($buildNumber)_*.msix" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $package) { throw "Publish succeeded but no .msix of build $buildNumber was found under Maui\artifacts." }

Write-Host "Package ready: $($package.FullName)"

# Also returned to the caller (Update-App.ps1); the build output above goes to the host, not to the pipeline.
$package.FullName
