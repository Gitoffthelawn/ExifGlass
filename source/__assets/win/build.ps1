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
# Debug-builds the Windows head for the given architecture.
#
#   .\build.ps1 <arch>    <arch>: x64 | arm64  (maps to the win-<arch> RID)
# -----------------------------------------------------------------------------
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('x64', 'arm64')]
    [string]$Arch
)

$ErrorActionPreference = 'Stop'
$SourceDir = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path
$Project = Join-Path $SourceDir 'ExifGlass.Win32\ExifGlass.Win32.csproj'

dotnet build $Project `
    -c Debug `
    -r "win-$Arch" `
    --no-incremental
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
