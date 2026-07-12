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
# Release-publishes the macOS head (self-contained NativeAOT; configured in the csproj). NativeAOT
# can't cross-compile, so run this on Apple Silicon. Compile the self-contained ExifTool first
# (build-exiftool.sh, or the "build-exiftool-mac-arm64" VS Code task) so the csproj bundles it.
#
#   ./publish.sh arm64      (arm64 is the only supported macOS arch)
# Output: __artifacts/publish/osx-<arch>/
# -----------------------------------------------------------------------------
set -euo pipefail

ARCH="${1:?target architecture required: arm64}"
case "$ARCH" in
  arm64) ;;
  *) echo "ERROR: unsupported architecture '$ARCH' (expected: arm64)." >&2; exit 1 ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

dotnet publish "$SOURCE_DIR/ExifGlass.Mac/ExifGlass.Mac.csproj" \
  -c Release \
  -r "osx-$ARCH" \
  -o "$SOURCE_DIR/__artifacts/publish/osx-$ARCH"
