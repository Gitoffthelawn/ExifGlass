# ExifGlass - EXIF Metadata Viewing Tool
# Copyright (C) 2023 - 2026 DUONG DIEU PHAP
# Project homepage: https://github.com/d2phap/ExifGlass
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
#
# -----------------------------------------------------------------------------
# Release-publishes the Windows head (self-contained NativeAOT; configured in the csproj) for the
# given architecture.
#
#   .\publish.ps1 <arch>   <arch>: x64 | arm64  (maps to the win-<arch> RID)
# Output: __artifacts\publish\win-<arch>\
#
# NativeAOT's ILC link step shells out to bare `vswhere.exe`, so the VS Installer dir is prepended
# to PATH; any running ExifGlass/exiftool is stopped first so the exe copy isn't locked.
# -----------------------------------------------------------------------------
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('x64', 'arm64')]
    [string]$Arch
)

$ErrorActionPreference = 'Stop'
$SourceDir = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$Project = Join-Path $SourceDir 'ExifGlass.Win32\ExifGlass.Win32.csproj'
$OutDir = Join-Path $SourceDir "__artifacts\publish\win-$Arch"

# Free the output exe (a prior run may have left ExifGlass or its exiftool daemon alive).
Get-Process ExifGlass, exiftool -ErrorAction SilentlyContinue | Stop-Process -Force

# ILC's link step calls bare `vswhere.exe`; make sure the VS Installer dir is on PATH.
$vsInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'
if ((Test-Path $vsInstaller) -and ($env:PATH -notlike "*$vsInstaller*")) {
    $env:PATH = "$vsInstaller;$env:PATH"
}

dotnet publish $Project `
    -c Release `
    -r "win-$Arch" `
    -o $OutDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
