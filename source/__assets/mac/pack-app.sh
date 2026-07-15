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
# Assembles ExifGlass.app from the osx-arm64 publish output. Run publish.sh first.
# Output: __artifacts/bundle/ExifGlass.app
# -----------------------------------------------------------------------------
set -euo pipefail

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PUBLISH_DIR="$SOURCE_DIR/__artifacts/publish/osx-arm64"
# The .app is assembled directly in __artifacts/bundle/, the shared output folder for
# every platform's final artifacts.
APP_DIR="$SOURCE_DIR/__artifacts/bundle/ExifGlass.app"
CONTENTS_DIR="$APP_DIR/Contents"
BUILD_PROPS_FILE="$SOURCE_DIR/Directory.Build.props"
ICON_SOURCE_FILE="$SOURCE_DIR/__assets/mac/logo.icns"
INFO_PLIST_TEMPLATE="$SOURCE_DIR/__assets/mac/Info.plist"

if [[ ! -d "$PUBLISH_DIR" ]]; then
	echo "Error: publish output not found at $PUBLISH_DIR (run publish.sh arm64 first)." >&2
	exit 1
fi

EG_VERSION="$(sed -n 's:.*<ExifGlassVersion>\(.*\)</ExifGlassVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EG_VERSION" ]]; then
	echo "Error: could not read ExifGlassVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

# Marketing version (CFBundleShortVersionString): up to 3 integers, e.g. 2.0.0.
# Prefer the explicit <ExifGlassBundleShortVersion>; fall back to ExifGlassVersion minus
# its last segment.
EG_SHORT_VERSION="$(sed -n 's:.*<ExifGlassBundleShortVersion>\(.*\)</ExifGlassBundleShortVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EG_SHORT_VERSION" ]]; then
	EG_SHORT_VERSION="${EG_VERSION%.*}"
	[[ -z "$EG_SHORT_VERSION" ]] && EG_SHORT_VERSION="$EG_VERSION"
fi

# Build number (CFBundleVersion): at most three integers. Prefer the explicit
# <ExifGlassBundleBuild>; fall back to the last segment of ExifGlassVersion.
EG_BUILD="$(sed -n 's:.*<ExifGlassBundleBuild>\(.*\)</ExifGlassBundleBuild>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EG_BUILD" ]]; then
	EG_BUILD="${EG_VERSION##*.}"
	[[ -z "$EG_BUILD" ]] && EG_BUILD="$EG_VERSION"
fi

# Remove any previous bundle (surface a clear message if a prior sudo run left it root-owned).
if [[ -d "$APP_DIR" ]]; then
	if ! rm -rf "$APP_DIR" 2>/dev/null || [[ -d "$APP_DIR" ]]; then
		echo "Error: could not remove the existing bundle at $APP_DIR" >&2
		echo "       It may be root-owned from a previous sudo run: sudo rm -rf \"$APP_DIR\"" >&2
		exit 1
	fi
fi

# Flattened layout: drop the old per-RID subfolder from previous runs so bundle/ isn't
# left carrying a stale bundle/osx-arm64/ tree.
rm -rf "$SOURCE_DIR/__artifacts/bundle/osx-arm64"

mkdir -p "$CONTENTS_DIR/MacOS" "$CONTENTS_DIR/Resources"
cp -R "$PUBLISH_DIR/." "$CONTENTS_DIR/MacOS/"
cp "$ICON_SOURCE_FILE" "$CONTENTS_DIR/Resources/logo.icns"

sed -e "s/\${EG_SHORT_VERSION}/$EG_SHORT_VERSION/g" \
    -e "s/\${EG_BUILD}/$EG_BUILD/g" \
    "$INFO_PLIST_TEMPLATE" > "$CONTENTS_DIR/Info.plist"

chmod +x "$CONTENTS_DIR/MacOS/ExifGlass"
[[ -f "$CONTENTS_DIR/MacOS/exiftool" ]] && chmod +x "$CONTENTS_DIR/MacOS/exiftool"

# codesign rejects non-Mach-O files under Contents/MacOS, so relocate ExifTool (Perl script + lib/)
# and its license into Contents/Resources. The `exiftool` script is symlinked back so the resolver
# (AppContext.BaseDirectory = Contents/MacOS) still finds it; the script's own readlink logic then
# locates lib/ next to the real file in Resources.
if [[ -f "$CONTENTS_DIR/MacOS/exiftool" ]]; then
	mv "$CONTENTS_DIR/MacOS/exiftool" "$CONTENTS_DIR/Resources/exiftool"
	ln -s "../Resources/exiftool" "$CONTENTS_DIR/MacOS/exiftool"
fi
[[ -d "$CONTENTS_DIR/MacOS/lib" ]] && mv "$CONTENTS_DIR/MacOS/lib" "$CONTENTS_DIR/Resources/lib"
[[ -f "$CONTENTS_DIR/MacOS/exiftool.LICENSE.txt" ]] && \
	mv "$CONTENTS_DIR/MacOS/exiftool.LICENSE.txt" "$CONTENTS_DIR/Resources/exiftool.LICENSE.txt"

echo "Created bundle: $APP_DIR"
