<#
.SYNOPSIS
    Creates the self-signed code-signing certificate used to sign the MSIX package (run once, NOT elevated).

.DESCRIPTION
    The certificate is created in the current user's store; the MSIX is signed by thumbprint, so no .pfx file
    and no password ever exist on disk or in the repository. Only the public .cer is exported, to be trusted
    by Install-Msix.ps1.

    The Subject must match the Publisher declared in Platforms/Windows/Package.appxmanifest.
#>
[CmdletBinding()]
param(
    [string] $Subject = 'CN=Mickael Billet',
    [string] $CerPath = (Join-Path $env:USERPROFILE '.certs\InvestZapto.cer')
)

$ErrorActionPreference = 'Stop'

$existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date) } | Select-Object -First 1
if ($existing) {
    Write-Host "Certificate already exists: $($existing.Thumbprint) (expires $($existing.NotAfter.ToString('yyyy-MM-dd')))"
    $certificate = $existing
}
else {
    $certificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject $Subject -KeyUsage DigitalSignature `
        -FriendlyName 'Suivi des Investissements (MSIX)' -CertStoreLocation Cert:\CurrentUser\My `
        -NotAfter (Get-Date).AddYears(3) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    Write-Host "Certificate created: $($certificate.Thumbprint)"
}

New-Item -ItemType Directory -Force (Split-Path $CerPath) | Out-Null
Export-Certificate -Cert $certificate -FilePath $CerPath | Out-Null
Write-Host "Public certificate exported to $CerPath"
