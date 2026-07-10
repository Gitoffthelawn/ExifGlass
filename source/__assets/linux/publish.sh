#!/usr/bin/env bash
#
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
# Release-publishes the Linux head (self-contained NativeAOT; configured in the csproj) for the
# given architecture. NativeAOT can't cross-compile, so run this on Linux (or WSL). Compile the
# self-contained ExifTool first (build-exiftool.sh, or the "build-exiftool-linux-<arch>" VS Code
# task) so the csproj bundles it; otherwise it falls back to the Perl script + lib/.
#
#   ./publish.sh <arch>   <arch>: x64 | arm64  (maps to the linux-<arch> RID)
# Output: __artifacts/publish/linux-<arch>/
# -----------------------------------------------------------------------------
set -euo pipefail

ARCH="${1:?target architecture required: x64 | arm64}"
case "$ARCH" in
  x64|arm64) ;;
  *) echo "ERROR: unsupported architecture '$ARCH' (expected: x64 | arm64)." >&2; exit 1 ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

dotnet publish "$SOURCE_DIR/ExifGlass.Linux/ExifGlass.Linux.csproj" \
  -c Release \
  -r "linux-$ARCH" \
  -o "$SOURCE_DIR/__artifacts/publish/linux-$ARCH"
