#Requires -Version 7.0
<#
.SYNOPSIS
    Build an MSIX of the ExifGlass Windows head in one of two flavours:
      * SIGNED   (default)  -> a .msix per architecture, for direct download / GitHub
                               Releases (sideload).
      * MSSTORE  (-MsStore) -> a single x64+arm64 .msixbundle, for the Microsoft Store.

.DESCRIPTION
    SIGNED (sideload / GitHub):
        Every payload .exe/.dll ExifGlass owns is Authenticode-signed, then the whole
        .msix is signed. The package Identity/Publisher is set to the EXACT Subject of
        the signing certificate (a hard MSIX requirement); the artwork is rendered from
        the app logo (appxmanifest/Assets). If NO signing certificate is found the
        package is still produced — same identity/artwork — but left UNSIGNED (with a
        warning). Sign it before publishing; an unsigned MSIX cannot be installed.
        Output: __artifacts/bundle/win/ExifGlass_<version>_win-<arch>.msix

    MSSTORE (Microsoft Store):
        The Store re-signs the package on submission, so the bundle is built UNSIGNED.
        It carries the Store-reserved Identity (-MsStoreIdentityName / -MsStorePublisher
        assigned by Partner Center) and its own version (-MsStoreVersion), which is
        independent of <ExifGlassVersion> and MUST be increased for every Store
        submission. Both x64 and arm64 are built and packed into one .msixbundle
        (Windows installs the architecture matching the device).
        Output: __artifacts/bundle/win/ExifGlass_<version>_win-msstore.msixbundle

    Pipeline (per architecture): publish a fresh self-contained NativeAOT build via
    publish.ps1 -> stage <staging>\AppxManifest.xml + \Assets\* + \ExifGlass\<payload>
    -> fill the manifest placeholders -> (signed flavour) Authenticode-sign ExifGlass's
    own payload binaries -> build resources.pri (makepri) -> pack the .msix (makeappx).
    The bundled third-party ExifTool (exiftool.exe + exiftool_files\ Perl runtime) is
    shipped verbatim from exiftool.org and never re-signed.

    makeappx.exe / makepri.exe / signtool.exe are auto-located in the latest installed
    Windows 10/11 SDK; no PATH setup needed.

.PARAMETER Platform
    Target architecture for the signed flavour: x64 (default) or arm64. Ignored with
    -MsStore (a bundle always contains both).

.PARAMETER MsStore
    Build the Microsoft Store x64+arm64 .msixbundle (unsigned, Store identity) instead
    of a signed per-architecture .msix.

.PARAMETER Sign
    (Signed flavour only) Attempt to Authenticode-sign the package + payload. When no
    certificate is found the package is built UNSIGNED instead of failing. Ignored with
    -MsStore (the Store signs its packages itself).

.PARAMETER CertSubject
    Substring of the code-signing certificate Subject to select it from the Current User
    / Local Machine "My" store (passed to signtool /n). Ignored when -CertFile is
    supplied. Default: "Duong Dieu Phap".

.PARAMETER CertFile
    Path to a PFX certificate to sign with instead of a store certificate.

.PARAMETER CertPassword
    Password for -CertFile (if any).

.PARAMETER TimestampUrl
    RFC-3161 timestamp server. Default: http://timestamp.sectigo.com

.PARAMETER PackageVersion
    Override the 4-part package version. Defaults to <ExifGlassVersion>.0 (signed
    flavour, e.g. 2.0.0 -> 2.0.0.0) or -MsStoreVersion (msstore flavour).

.PARAMETER IdentityName
    Signed-flavour package Identity Name. Default: "DuongDieuPhap.ExifGlass".

.PARAMETER PublisherDisplayName
    Human-readable publisher shown in the installer. Default: "Duong Dieu Phap".

.PARAMETER MsStoreIdentityName
    Microsoft-Store-reserved Identity Name (from Partner Center).
    Default: "9662DuongDieuPhap.ExifGlass".

.PARAMETER MsStorePublisher
    Microsoft-Store-assigned Publisher (from Partner Center).
    Default: "CN=29F1B9EC-D220-4DC3-BEDB-01A9CCA51904".

.PARAMETER MsStoreVersion
    4-part version for the Store package. Independent of <ExifGlassVersion> and MUST be
    bumped for every Store submission (the Store rejects a version <= the last accepted).
    Default: "1.10.0.0".

.PARAMETER SkipPublish
    Reuse the existing __artifacts/publish/win-<arch> output instead of re-publishing
    (faster iteration; the package may not reflect uncommitted source changes).

.EXAMPLE
    pwsh __assets/win/pack-msix.ps1 -Platform x64 -Sign
    # Signed x64 .msix for GitHub Releases (cert selected by Subject).

.EXAMPLE
    pwsh __assets/win/pack-msix.ps1 -MsStore
    # Unsigned x64+arm64 .msixbundle for the Microsoft Store.

.EXAMPLE
    pwsh __assets/win/pack-msix.ps1 -Platform x64
    # Unsigned x64 .msix (local testing).
#>

[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Platform = 'x64',

    [switch]$MsStore,

    [switch]$Sign,

    [string]$CertSubject = 'Duong Dieu Phap',
    [string]$CertFile = '',
    [string]$CertPassword = '',
    [string]$TimestampUrl = 'http://timestamp.sectigo.com',

    [string]$PackageVersion = '',

    # Sideload identity (signed build) — Publisher is overwritten with the cert Subject.
    [string]$IdentityName = 'DuongDieuPhap.ExifGlass',
    [string]$PublisherDisplayName = 'Duong Dieu Phap',

    # Microsoft Store identity (unsigned build) — reserved values from Partner Center.
    [string]$MsStoreIdentityName = '9662DuongDieuPhap.ExifGlass',
    [string]$MsStorePublisher = 'CN=29F1B9EC-D220-4DC3-BEDB-01A9CCA51904',
    [string]$MsStoreVersion = '1.10.0.0',

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths ---------------------------------------------------------------------
$SourceDir       = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$BuildProps      = Join-Path $SourceDir 'Directory.Build.props'
$PublishScript   = Join-Path $PSScriptRoot 'publish.ps1'
$GenAssetsScript = Join-Path $PSScriptRoot 'generate-msix-assets.ps1'
$ManifestTpl     = Join-Path $PSScriptRoot 'appxmanifest\AppxManifest.xml'
$AssetsDir       = Join-Path $PSScriptRoot 'appxmanifest\Assets'
$BundleDir       = Join-Path $SourceDir '__artifacts\bundle\win'

# --- Helpers -------------------------------------------------------------------

# Locate a Windows SDK tool (makeappx.exe / makepri.exe / signtool.exe), preferring
# the newest SDK and an x64 host build, falling back to whatever is on PATH.
function Find-SdkTool([string]$Name) {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $roots) {
        $hit = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^10\.' } |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\$Name" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($hit) { return $hit }
    }

    $onPath = Get-Command $Name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    throw "Could not find $Name. Install the Windows 10/11 SDK (includes makeappx, makepri & signtool)."
}

# Read a single <Tag>value</Tag> from Directory.Build.props.
function Get-BuildProp([string]$Tag) {
    $m = Select-String -Path $BuildProps -Pattern "<$Tag>(.*?)</$Tag>" | Select-Object -First 1
    if ($m) { return $m.Matches[0].Groups[1].Value.Trim() }
    return ''
}

# Find a usable signing certificate and report its EXACT Subject DN (needed for the
# manifest Publisher, which must match the signature byte-for-byte) and which store
# it lives in (so signtool searches the same one). Returns @{ Subject; Machine } or
# $null when none is found — the caller then builds an UNSIGNED package.
function Resolve-SigningCert {
    if ($CertFile) {
        if (-not (Test-Path $CertFile)) {
            Write-Warning "Certificate file not found: $CertFile"
            return $null
        }
        try {
            $c = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertFile, $CertPassword)
            return @{ Subject = $c.Subject; Machine = $false }
        }
        catch {
            Write-Warning "Could not load certificate '$CertFile': $($_.Exception.Message)"
            return $null
        }
    }
    foreach ($store in @(
            @{ Path = 'Cert:\CurrentUser\My';  Machine = $false },
            @{ Path = 'Cert:\LocalMachine\My'; Machine = $true })) {
        $c = Get-ChildItem $store.Path -CodeSigningCert -ErrorAction SilentlyContinue |
            Where-Object { $_.Subject -like "*$CertSubject*" -and $_.HasPrivateKey } |
            Select-Object -First 1
        if ($c) { return @{ Subject = $c.Subject; Machine = $store.Machine } }
    }
    return $null
}

# Sign one or more files with signtool (Authenticode, SHA-256, timestamped).
# Returns $true on success, $false on failure (never throws — the caller decides
# whether to continue UNSIGNED).
function Invoke-SignTool([string]$SignTool, [string[]]$Files) {
    if ($Files.Count -eq 0) { return $true }
    $common = @('sign', '/fd', 'SHA256', '/tr', $TimestampUrl, '/td', 'SHA256')
    if ($CertFile) {
        $common += @('/f', $CertFile)
        if ($CertPassword) { $common += @('/p', $CertPassword) }
    }
    else {
        $common += @('/n', $CertSubject, '/a')
        # signtool /n defaults to the CurrentUser store; switch to the machine store
        # when that is where the certificate was found.
        if ($script:useMachineStore) { $common += '/sm' }
    }
    & $SignTool @common @Files
    return ($LASTEXITCODE -eq 0)
}

# Build ONE architecture's .msix (publish -> stage -> manifest -> payload-sign ->
# resources.pri -> pack) and write it to $OutMsixPath. The package itself is NOT
# signed here — the caller signs the final artifact (the .msix in signed mode; the
# msstore .msixbundle is never signed). Reads the flavour-level $identityName,
# $publisher, $pkgVersion, $script:doSign and the located SDK tools from script scope.
function New-MsixPackage([string]$Arch, [string]$OutMsixPath) {
    $rid        = "win-$Arch"
    $publishDir = Join-Path $SourceDir "__artifacts\publish\$rid"
    $stagingDir = Join-Path $SourceDir "__artifacts\staging\$rid-msix"
    $payloadDir = Join-Path $stagingDir 'ExifGlass'

    Write-Host ''
    Write-Host "==> [$Arch] Building MSIX package"

    # 1. Publish a fresh self-contained AOT build via publish.ps1 (kills any running
    #    ExifGlass/exiftool, prepends the VS Installer dir to PATH for ILC's vswhere,
    #    publishes to __artifacts/publish/win-<arch>).
    if ($SkipPublish -and (Test-Path (Join-Path $publishDir 'ExifGlass.exe'))) {
        Write-Host "    reusing publish output: $publishDir"
    }
    else {
        Write-Host "    publishing $rid (Release, AOT, self-contained)"
        if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $PublishScript $Arch
        if ($LASTEXITCODE -ne 0) { throw "publish.ps1 failed for $rid (exit $LASTEXITCODE)." }
    }
    if (-not (Test-Path (Join-Path $publishDir 'ExifGlass.exe'))) {
        throw "Publish did not produce ExifGlass.exe in $publishDir"
    }

    # 2. Stage the layout:  <staging>\AppxManifest.xml + \Assets\* + \ExifGlass\<payload>
    if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
    New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
    Copy-Item -Path (Join-Path $publishDir '*') -Destination $payloadDir -Recurse -Force
    # Drop debug symbols — they bloat the package and are not part of the product.
    Get-ChildItem -Path $payloadDir -Recurse -Include '*.pdb' -File -ErrorAction SilentlyContinue |
        Remove-Item -Force
    Copy-Item -Path $AssetsDir -Destination (Join-Path $stagingDir 'Assets') -Recurse -Force

    # 3. Generate AppxManifest.xml from the template (UTF-8 BOM, as the SDK wants).
    $manifest = (Get-Content -Path $ManifestTpl -Raw).
        Replace('{{IDENTITY_NAME}}', $script:identityName).
        Replace('{{PUBLISHER}}', $script:publisher).
        Replace('{{PUBLISHER_DISPLAY_NAME}}', $PublisherDisplayName).
        Replace('{{VERSION}}', $script:pkgVersion).
        Replace('{{ARCH}}', $Arch)
    $manOut = Join-Path $stagingDir 'AppxManifest.xml'
    [System.IO.File]::WriteAllText($manOut, $manifest, [System.Text.UTF8Encoding]::new($true))

    # 4. Sign ExifGlass's own payload binaries. The bundled ExifTool (exiftool.exe +
    #    its exiftool_files\ Perl runtime) is third-party (exiftool.org) and shipped
    #    verbatim — it is NOT re-signed.
    if ($script:doSign) {
        $binaries = Get-ChildItem -Path $payloadDir -Recurse -Include '*.exe', '*.dll' -File |
            Where-Object { $_.FullName -notmatch '\\exiftool_files\\' -and $_.Name -ne 'exiftool.exe' } |
            Select-Object -ExpandProperty FullName
        Write-Host "    signing $($binaries.Count) payload binary file(s)"
        if (-not (Invoke-SignTool -SignTool $script:signtool -Files $binaries)) {
            Write-Warning "Could not sign payload binaries — the package will be left UNSIGNED."
            $script:doSign = $false
        }
    }

    # 5. Build resources.pri so the manifest's unqualified logo names resolve to the
    #    scale-qualified assets per DPI.
    $priConfig = Join-Path (Split-Path $stagingDir) "$rid-msix.priconfig.xml"
    $priOut    = Join-Path $stagingDir 'resources.pri'
    if (Test-Path $priConfig) { Remove-Item $priConfig -Force }
    & $script:makepri createconfig /cf $priConfig /dq en-US /o
    if ($LASTEXITCODE -ne 0) { throw "makepri createconfig failed (exit $LASTEXITCODE)." }
    & $script:makepri new /pr $stagingDir /cf $priConfig /mn $manOut /of $priOut /o
    if ($LASTEXITCODE -ne 0) { throw "makepri new failed (exit $LASTEXITCODE)." }

    # 6. Pack the .msix.
    New-Item -ItemType Directory -Path (Split-Path $OutMsixPath) -Force | Out-Null
    if (Test-Path $OutMsixPath) { Remove-Item $OutMsixPath -Force }
    & $script:makeappx pack /o /d $stagingDir /p $OutMsixPath
    if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed for $rid (exit $LASTEXITCODE)." }
    Write-Host "    packed: $OutMsixPath"
}

# --- Version -------------------------------------------------------------------
$egVersion = Get-BuildProp 'ExifGlassVersion'
if (-not $egVersion) { throw "Could not read <ExifGlassVersion> from $BuildProps" }

# --- Flavour: identity / publisher / version / signing -------------------------
$script:doSign          = $false
$script:useMachineStore = $false
if ($MsStore) {
    # Microsoft Store: reserved identity, Store version, never signed here.
    $script:identityName = $MsStoreIdentityName
    $script:publisher    = $MsStorePublisher
    $script:pkgVersion   = if ($PackageVersion) { $PackageVersion } else { $MsStoreVersion }
}
else {
    # Sideload / GitHub: plain identity; Publisher = cert Subject when signing.
    $script:identityName = $IdentityName
    if ($PackageVersion) {
        $script:pkgVersion = $PackageVersion
    }
    else {
        $parts = @($egVersion.Split('.'))
        while ($parts.Count -lt 4) { $parts += '0' }
        $script:pkgVersion = ($parts[0..3] -join '.')
    }

    if ($Sign) {
        $cert = Resolve-SigningCert
        if ($cert) {
            $script:publisher       = $cert.Subject
            $script:doSign          = $true
            $script:useMachineStore = $cert.Machine
        }
        else {
            # No usable certificate — build UNSIGNED with a placeholder Publisher. It
            # must be signed before it can install.
            $script:publisher = "CN=$PublisherDisplayName"
            Write-Warning "No signing certificate found — building an UNSIGNED package."
        }
    }
    else {
        $script:publisher = "CN=$PublisherDisplayName"
    }
}

# --- Output artifact -----------------------------------------------------------
if ($MsStore) {
    $outArtifact = Join-Path $BundleDir "ExifGlass_${egVersion}_win-msstore.msixbundle"
    $flavour     = 'MSSTORE (unsigned, Microsoft Store)'
}
else {
    $outArtifact = Join-Path $BundleDir "ExifGlass_${egVersion}_win-$Platform.msix"
    $flavour     = if ($script:doSign) { 'SIGNED (sideload / GitHub)' }
                   elseif ($Sign) { 'UNSIGNED — no certificate found' }
                   else { 'UNSIGNED (no -Sign)' }
}

Write-Host "==> Packing ExifGlass $egVersion as $(if ($MsStore) { 'MSIXBUNDLE (x64 + arm64)' } else { "MSIX ($Platform)" })"
Write-Host "    Flavour   : $flavour"
Write-Host "    Identity  : $($script:identityName)"
Write-Host "    Publisher : $($script:publisher)"
Write-Host "    Version   : $($script:pkgVersion)"
Write-Host "    Output    : $outArtifact"

# --- Ensure the tile/store artwork exists (render it from the logo if not) ------
if (-not (Test-Path (Join-Path $AssetsDir '*.png'))) {
    Write-Host "    assets    : none found — generating from the app logo"
    & pwsh -NoProfile -ExecutionPolicy Bypass -File $GenAssetsScript
    if ($LASTEXITCODE -ne 0) { throw "Asset generation failed (exit $LASTEXITCODE)." }
}

# --- Locate SDK tools ----------------------------------------------------------
$script:makeappx = Find-SdkTool 'makeappx.exe'
$script:makepri  = Find-SdkTool 'makepri.exe'
$script:signtool = if ($script:doSign) { Find-SdkTool 'signtool.exe' } else { '' }
Write-Host "    makeappx  : $($script:makeappx)"
Write-Host "    makepri   : $($script:makepri)"
if ($script:doSign) { Write-Host "    signtool  : $($script:signtool)" }

# --- Build the package(s) ------------------------------------------------------
New-Item -ItemType Directory -Path $BundleDir -Force | Out-Null
if (Test-Path $outArtifact) { Remove-Item $outArtifact -Force }

if ($MsStore) {
    # Build each arch into a clean input dir (makeappx bundle /d wants a folder holding
    # ONLY the packages to bundle), then bundle them. The per-arch packages and the
    # .msixbundle are all left UNSIGNED — the Microsoft Store signs on submission.
    $bundleInput = Join-Path $SourceDir '__artifacts\staging\win-msstore-bundle-input'
    if (Test-Path $bundleInput) { Remove-Item $bundleInput -Recurse -Force }
    New-Item -ItemType Directory -Path $bundleInput -Force | Out-Null

    foreach ($arch in @('x64', 'arm64')) {
        New-MsixPackage -Arch $arch -OutMsixPath (Join-Path $bundleInput "ExifGlass-$arch.msix")
    }

    Write-Host ''
    Write-Host "==> Bundling x64 + arm64 into .msixbundle"
    & $script:makeappx bundle /o /d $bundleInput /bv $script:pkgVersion /p $outArtifact
    if ($LASTEXITCODE -ne 0) { throw "makeappx bundle failed (exit $LASTEXITCODE)." }
    Write-Host "    bundled: $outArtifact"
}
else {
    New-MsixPackage -Arch $Platform -OutMsixPath $outArtifact

    # Sign + verify the final .msix.
    if ($script:doSign) {
        Write-Host ''
        Write-Host "==> Signing the .msix"
        if (Invoke-SignTool -SignTool $script:signtool -Files @($outArtifact)) {
            Write-Host "==> Verifying signature"
            & $script:signtool verify /pa $outArtifact
            if ($LASTEXITCODE -ne 0) { throw "signtool verify failed (exit $LASTEXITCODE)." }
        }
        else {
            Write-Warning "Could not sign the .msix — it has been left UNSIGNED."
            $script:doSign = $false
        }
    }
}

# --- Done ----------------------------------------------------------------------
Write-Host ''
Write-Host 'Done.'
Write-Host "  Package : $outArtifact"
if ($MsStore) {
    Write-Host '  Signed  : no (the Microsoft Store signs it on submission)'
    Write-Host '  Next    : upload to Partner Center (Microsoft Store) as-is.'
    Write-Host '            Do NOT sign this msstore bundle yourself. Bump -MsStoreVersion next submission.'
}
elseif ($script:doSign) {
    Write-Host '  Signed  : yes (payload binaries + package)'
    Write-Host '  Next    : upload to the GitHub release for this version.'
}
else {
    Write-Host '  Signed  : no'
    Write-Host '  Next    : sign the .msix before publishing (an unsigned MSIX cannot be installed).'
}
