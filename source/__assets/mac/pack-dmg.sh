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
# Signs, notarizes, and packages ExifGlass.app into a distributable DMG (Developer ID / website).
# Run AFTER pack-app.sh.
#
# Prerequisites (one-time):
#   1. Developer ID Application certificate in the login keychain.
#   2. A notarytool keychain profile:
#        xcrun notarytool store-credentials "exifglass-notary" \
#            --apple-id "you@example.com" --team-id "7DV5HBKZ58" \
#            --password "app-specific-password"   # from appleid.apple.com
#
# Override defaults via env: SIGN_IDENTITY, NOTARY_PROFILE.
# -----------------------------------------------------------------------------
set -euo pipefail

SIGN_IDENTITY="${SIGN_IDENTITY:-Developer ID Application: Phap Duong (7DV5HBKZ58)}"
NOTARY_PROFILE="${NOTARY_PROFILE:-exifglass-notary}"

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APP_DIR="$SOURCE_DIR/__artifacts/bundle/osx-arm64/ExifGlass.app"
ENTITLEMENTS_FILE="$SOURCE_DIR/__assets/mac/ExifGlass.entitlements"
BUILD_PROPS_FILE="$SOURCE_DIR/Directory.Build.props"
DMG_STAGING_DIR="$SOURCE_DIR/__artifacts/bundle/osx-arm64/dmg-staging"
OUTPUT_DIR="$SOURCE_DIR/__artifacts/dist"

# --- Sanity checks -----------------------------------------------------------
if [[ ! -d "$APP_DIR" ]]; then
	echo "Error: app bundle not found at $APP_DIR (run pack-app.sh first)." >&2
	exit 1
fi
if [[ ! -f "$ENTITLEMENTS_FILE" ]]; then
	echo "Error: entitlements file not found at $ENTITLEMENTS_FILE" >&2
	exit 1
fi
if ! security find-identity -v -p codesigning | grep -qF "$SIGN_IDENTITY"; then
	echo "Error: signing identity not found in keychain: $SIGN_IDENTITY" >&2
	exit 1
fi
if ! xcrun notarytool history --keychain-profile "$NOTARY_PROFILE" >/dev/null 2>&1; then
	echo "Error: notarytool keychain profile '$NOTARY_PROFILE' not found or invalid." >&2
	echo "       Create it with: xcrun notarytool store-credentials \"$NOTARY_PROFILE\" ..." >&2
	exit 1
fi

EG_VERSION="$(sed -n 's:.*<ExifGlassVersion>\(.*\)</ExifGlassVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EG_VERSION" ]]; then
	echo "Error: could not read ExifGlassVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

DMG_PATH="$OUTPUT_DIR/ExifGlass_${EG_VERSION}_mac-arm64.dmg"
VOLUME_NAME="ExifGlass ${EG_VERSION}"

echo "==> Packaging ExifGlass $EG_VERSION (arm64)"
echo "    Identity : $SIGN_IDENTITY"
echo "    Profile  : $NOTARY_PROFILE"

# --- Strip debug artifacts (they break signing) ------------------------------
echo "==> Removing debug artifacts from bundle"
find "$APP_DIR" -type f -name "*.pdb" -delete
find "$APP_DIR" -type d -name "*.dSYM" -exec rm -rf {} +

# --- Sign nested Mach-O first (inside-out), then the bundle ------------------
# ExifTool is a Perl script in Resources/ (sealed as a resource by the bundle signature), not signed here.
echo "==> Signing nested native libraries"
while IFS= read -r -d '' bin; do
	echo "    sign: ${bin#"$APP_DIR/"}"
	codesign --force --timestamp --options runtime --sign "$SIGN_IDENTITY" "$bin"
done < <(find "$APP_DIR/Contents/MacOS" -type f \( -name "*.dylib" -o -name "*.so" \) -print0)

echo "==> Signing app bundle (hardened runtime + entitlements)"
codesign --force --timestamp --options runtime \
	--entitlements "$ENTITLEMENTS_FILE" \
	--sign "$SIGN_IDENTITY" "$APP_DIR"

echo "==> Verifying code signature"
codesign --verify --deep --strict --verbose=2 "$APP_DIR"

# --- Build the DMG (with an /Applications drag-install shortcut) -------------
echo "==> Building DMG"
rm -rf "$DMG_STAGING_DIR"
mkdir -p "$DMG_STAGING_DIR" "$OUTPUT_DIR"
cp -R "$APP_DIR" "$DMG_STAGING_DIR/"
ln -s /Applications "$DMG_STAGING_DIR/Applications"

rm -f "$DMG_PATH"
hdiutil create -volname "$VOLUME_NAME" -srcfolder "$DMG_STAGING_DIR" \
	-fs HFS+ -format UDZO -ov "$DMG_PATH"
rm -rf "$DMG_STAGING_DIR"

echo "==> Signing DMG"
codesign --force --timestamp --sign "$SIGN_IDENTITY" "$DMG_PATH"

# --- Notarize + staple -------------------------------------------------------
echo "==> Submitting DMG for notarization (this can take a few minutes)"
xcrun notarytool submit "$DMG_PATH" --keychain-profile "$NOTARY_PROFILE" --wait

echo "==> Stapling notarization ticket"
xcrun stapler staple "$DMG_PATH"

echo "==> Validating Gatekeeper acceptance"
spctl --assess --type open --context context:primary-signature --verbose=2 "$DMG_PATH" || true
xcrun stapler validate "$DMG_PATH"

echo ""
echo "Done: $DMG_PATH"
