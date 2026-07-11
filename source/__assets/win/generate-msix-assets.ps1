#Requires -Version 7.0
<#
.SYNOPSIS
    Render the MSIX tile / store logo set for the signed ExifGlass package from a
    single source image.

.DESCRIPTION
    The Windows MSIX (GitHub / sideload) uses logos rendered from the app brand
    logo (__assets/__app/logo_512.png). This script renders every asset the
    AppxManifest template references — appxmanifest/AppxManifest.xml — into
    appxmanifest/Assets, at every scale/targetsize Windows expects, so makepri can
    resolve the manifest's unqualified logo names per DPI.

    The asset set is defined by $AssetSpec below (base logo name + base size +
    which scale / targetsize variants to emit); there is no external reference
    folder to keep in sync. Sizing rules (per MSIX asset conventions):
      * base size comes from the spec (Square150x150 -> 150x150, StoreLogo -> 50x50).
      * scale-N       -> base * N / 100.
      * targetsize-N  -> N x N (exact; the _altform-unplated / altform-lightunplated
                         variants render the same transparent artwork — plating is a
                         shell concept, not baked into the PNG).
      * Wide* logos    -> the square logo centered on a transparent wide canvas.

    Run this after changing __assets/__app/logo_512.png (or the manifest's logo
    set). script-pack-win-msix.ps1 also auto-runs it when appxmanifest/Assets is
    missing or empty.

.PARAMETER Source
    Source image (square, high-res). Default: __assets/__app/logo_512.png.

.PARAMETER OutDir
    Output folder. Default: appxmanifest/Assets (next to this script).

.EXAMPLE
    pwsh __assets/win/generate-msix-assets.ps1
    # Regenerate __assets/win/appxmanifest/Assets from __assets/__app/logo_512.png.
#>

[CmdletBinding()]
param(
    [string]$Source = '',
    [string]$OutDir = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$SourceDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $Source) { $Source = Join-Path $SourceDir '__assets\__app\logo_512.png' }
if (-not $OutDir) { $OutDir = Join-Path $PSScriptRoot 'appxmanifest\Assets' }

if (-not (Test-Path $Source)) { throw "Source image not found: $Source" }

# The assets the AppxManifest template references. Each spec expands to a set of
# scale (and, for Square44x44, targetsize) qualified files. Keep the base names in
# sync with appxmanifest/AppxManifest.xml.
$AssetSpec = @(
    @{ Base = 'EXIFGLASS-Square44x44Logo'; W = 44; H = 44; Scales = @(100, 125, 150, 200, 400); TargetSizes = @(16, 24, 32, 48, 256) },
    @{ Base = 'EXIFGLASS-Square71x71Logo'; W = 71; H = 71; Scales = @(100, 125, 150, 200, 400); TargetSizes = @() },
    @{ Base = 'EXIFGLASS-Square150x150Logo'; W = 150; H = 150; Scales = @(100, 125, 150, 200, 400); TargetSizes = @() },
    @{ Base = 'EXIFGLASS-Square310x310Logo'; W = 310; H = 310; Scales = @(100, 125, 150, 200, 400); TargetSizes = @() },
    @{ Base = 'EXIFGLASS-Wide310x150Logo'; W = 310; H = 150; Scales = @(100, 125, 150, 200, 400); TargetSizes = @(); Wide = $true },
    @{ Base = 'StoreLogo'; W = 50; H = 50; Scales = @(100, 125, 150, 200, 400); TargetSizes = @() }
)

Add-Type -AssemblyName System.Drawing

# Render the source logo into a WxH transparent PNG at $OutPath. Square canvases are
# filled edge-to-edge; wide canvases get the square logo centered (letterboxed).
function Save-Logo([System.Drawing.Image]$Src, [int]$W, [int]$H, [bool]$Wide, [string]$OutPath) {
    $bmp = [System.Drawing.Bitmap]::new($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        if ($Wide) {
            $side = [Math]::Min($W, $H)
            $x = [int](($W - $side) / 2)
            $y = [int](($H - $side) / 2)
            $g.DrawImage($Src, $x, $y, $side, $side)
        }
        else {
            $g.DrawImage($Src, 0, 0, $W, $H)
        }
    }
    finally {
        $g.Dispose()
    }
    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source).Path)
try {
    if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

    $count = 0
    foreach ($a in $AssetSpec) {
        $isWide = [bool]($a.Contains('Wide') -and $a.Wide)

        # scale-N variants (base size * N / 100).
        foreach ($s in $a.Scales) {
            $w = [int][Math]::Round($a.W * $s / 100.0)
            $h = [int][Math]::Round($a.H * $s / 100.0)
            Save-Logo $src $w $h $isWide (Join-Path $OutDir "$($a.Base).scale-$s.png")
            $count++
        }

        # targetsize-N variants (exact N x N) + the unplated / light-unplated forms.
        foreach ($t in $a.TargetSizes) {
            foreach ($name in @(
                    "$($a.Base).targetsize-$t.png",
                    "$($a.Base).targetsize-${t}_altform-unplated.png",
                    "$($a.Base).altform-lightunplated_targetsize-$t.png")) {
                Save-Logo $src $t $t $false (Join-Path $OutDir $name)
                $count++
            }
        }
    }

    Write-Host "==> Generated $count asset(s) from $Source"
    Write-Host "    -> $OutDir"
}
finally {
    $src.Dispose()
}
