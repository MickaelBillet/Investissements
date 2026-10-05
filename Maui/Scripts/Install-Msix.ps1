<#
.SYNOPSIS
    Trusts the signing certificate and installs the MSIX built by Publish-Msix.ps1.

.DESCRIPTION
    Trusting the certificate writes to the machine store (LocalMachine\TrustedPeople), which requires an
    ELEVATED PowerShell — needed once; later reinstalls of an update work the same way.
#>
[CmdletBinding()]
param(
    [string] $CerPath = (Join-Path $env:USERPROFILE '.certs\InvestZapto.cer')
)

$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw 'Run this script from an elevated PowerShell (Run as administrator): the certificate must be trusted machine-wide.'
}

if (-not (Test-Path $CerPath)) {
    throw "Certificate not found at $CerPath. Run Maui\Scripts\New-DevCertificate.ps1 first."
}

$package = Get-ChildItem (Join-Path $PSScriptRoot '..\artifacts') -Recurse -Filter *.msix -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $package) {
    throw 'No .msix found under Maui\artifacts. Run Maui\Scripts\Publish-Msix.ps1 first.'
}

Import-Certificate -FilePath $CerPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
Write-Host 'Certificate trusted (LocalMachine\TrustedPeople).'

# ForceUpdateFromAnyVersion lets a rebuild with the same version number replace the installed package.
Add-AppxPackage -Path $package.FullName -ForceUpdateFromAnyVersion
Write-Host "Installed: $($package.Name). Look for 'Suivi des Investissements' in the Start menu."
