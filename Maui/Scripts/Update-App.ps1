<#
.SYNOPSIS
    Builds, trusts and installs (or updates) the MAUI application in one command, from a NORMAL PowerShell.

.DESCRIPTION
    Chains New-DevCertificate.ps1, Publish-Msix.ps1 and the installation, and asks for elevation only when it is
    really needed: importing the signing certificate into LocalMachine\TrustedPeople. Once the certificate is
    trusted (first run, or after a renewal) later runs never prompt: a signed MSIX installs for the current user
    without administrator rights.

    The package version is the commit count of the repository (see Publish-Msix.ps1). Windows refuses a package
    that has the same version as the installed one but a different content (0x80073CFB): the script stops in that
    case and -Reinstall uninstalls first, which ERASES the application data (stored dashboard password).

.PARAMETER Reinstall
    Uninstalls the installed package before installing, so the same version can be installed again.

.PARAMETER Subject
    Subject of the signing certificate; must match the Publisher of Package.appxmanifest.

.EXAMPLE
    .\Maui\Scripts\Update-App.ps1

.EXAMPLE
    .\Maui\Scripts\Update-App.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch] $Reinstall,
    [string] $Subject = 'CN=Mickael Billet',
    [string] $CerPath = (Join-Path $env:USERPROFILE '.certs\InvestZapto.cer')
)

$ErrorActionPreference = 'Stop'

$packageName = 'fr.zapto.invest.maui'

function Get-SigningCertificate {
    Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date) } |
        Sort-Object NotAfter -Descending | Select-Object -First 1
}

# 1. Signing certificate (created once, in the user's store: no elevation needed).
$certificate = Get-SigningCertificate
if (-not $certificate) {
    Write-Host '1/5 No valid signing certificate: creating it (New-DevCertificate.ps1)...'
    if ($PSCmdlet.ShouldProcess($Subject, 'Create the signing certificate')) {
        & (Join-Path $PSScriptRoot 'New-DevCertificate.ps1') -Subject $Subject -CerPath $CerPath
        $certificate = Get-SigningCertificate
        if (-not $certificate) { throw "The certificate '$Subject' could not be created." }
    }
}
else {
    Write-Host "1/5 Signing certificate: $($certificate.Thumbprint) (expires $($certificate.NotAfter.ToString('yyyy-MM-dd')))"
}

# 2. Version check: same version + different content is rejected by Windows (0x80073CFB).
$buildNumber = [int](git -C $PSScriptRoot rev-list --count HEAD)
if ($LASTEXITCODE -ne 0) { throw 'Could not compute the package version from git (git rev-list failed).' }

$installed = Get-AppxPackage -Name $packageName
if ($installed) {
    $installedBuild = ([version] $installed.Version).Revision
    Write-Host "2/5 Installed: $($installed.Version) - package to build: build $buildNumber"

    if ($installedBuild -eq $buildNumber -and -not $Reinstall) {
        Write-Host "Build $buildNumber is already installed. Commit your changes to get a new version, or use -Reinstall" -ForegroundColor Yellow
        Write-Host 'to install the same version again (this erases the application data, e.g. the stored dashboard password).' -ForegroundColor Yellow
        return
    }
}
else {
    Write-Host "2/5 Not installed yet - package to build: build $buildNumber"
}

if ($Reinstall -and $installed) {
    $warning = "Uninstalling $packageName ERASES its data (stored dashboard password, WebView profile). Continue?"
    if ($PSCmdlet.ShouldProcess($packageName, 'Uninstall the installed package')) {
        if (-not $PSCmdlet.ShouldContinue($warning, 'Reinstall')) { Write-Host 'Cancelled.'; return }
        Remove-AppxPackage -Package $installed.PackageFullName
    }
}

# 3. Build and sign the package.
Write-Host '3/5 Building the signed MSIX (Publish-Msix.ps1)...'
$msix = $null
if ($PSCmdlet.ShouldProcess('Maui', 'Publish the signed MSIX')) {
    $msix = & (Join-Path $PSScriptRoot 'Publish-Msix.ps1') -Subject $Subject | Select-Object -Last 1
    if (-not $msix -or -not (Test-Path $msix)) { throw 'Publish-Msix.ps1 did not return the path of the package.' }
}

# 4. Trust the certificate machine-wide: the only step that needs elevation, skipped once done.
# No certificate yet only happens under -WhatIf (it was not created): nothing can be trusted then.
$thumbprint = if ($certificate) { $certificate.Thumbprint } else { '(certificate to create)' }
$trusted = Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object { $_.Thumbprint -eq $thumbprint }
if ($trusted) {
    Write-Host '4/5 Certificate already trusted: no elevation needed.'
}
else {
    Write-Host '4/5 Trusting the certificate (administrator rights requested once)...'
    if ($PSCmdlet.ShouldProcess($thumbprint, 'Trust the certificate in LocalMachine\TrustedPeople (elevated)')) {
        New-Item -ItemType Directory -Force (Split-Path $CerPath) | Out-Null
        Export-Certificate -Cert $certificate -FilePath $CerPath | Out-Null

        $command = "Import-Certificate -FilePath '$CerPath' -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null"
        try {
            Start-Process -FilePath powershell.exe -Verb RunAs -Wait -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $command)
        }
        catch {
            throw "The elevation was refused or failed: the certificate must be trusted machine-wide. $($_.Exception.Message)"
        }

        $trusted = Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object { $_.Thumbprint -eq $thumbprint }
        if (-not $trusted) { throw 'The certificate is still not trusted (LocalMachine\TrustedPeople).' }
    }
}

# 5. Install for the current user; -ForceApplicationShutdown closes a running instance first.
Write-Host '5/5 Installing...'
if ($PSCmdlet.ShouldProcess($packageName, 'Install the package')) {
    Add-AppxPackage -Path $msix -ForceUpdateFromAnyVersion -ForceApplicationShutdown
    $now = Get-AppxPackage -Name $packageName
    Write-Host "Done: $packageName $($now.Version). Look for 'Suivi des Investissements' in the Start menu." -ForegroundColor Green
}
