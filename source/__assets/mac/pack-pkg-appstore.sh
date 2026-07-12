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
# Signs ExifGlass.app for the Mac App Store and packages it into a .pkg (optionally uploading it).
# Separate pipeline from pack-dmg.sh: App Store identities + sandbox entitlements + embedded
# provisioning profile; NOT notarized (Apple review replaces it). Run AFTER pack-app.sh.
#
# Prerequisites (one-time):
#   1. "Apple Distribution: ..." cert (signs the .app).
#   2. "3rd Party Mac Developer Installer: ..." cert (signs the .pkg).
#   3. A Mac App Store provisioning profile for com.duongdieuphap.exifglass, saved to:
#        __assets/mac/appstore/ExifGlass_AppStore.provisionprofile
#
# Override via env: APP_SIGN_IDENTITY, INSTALLER_SIGN_IDENTITY, PROVISION_PROFILE, UPLOAD.
# To upload: UPLOAD=1 APPLE_ID="you@example.com" APPLE_APP_PASSWORD="app-specific-pw" ...
# -----------------------------------------------------------------------------
set -euo pipefail

APP_SIGN_IDENTITY="${APP_SIGN_IDENTITY:-Apple Distribution: Phap Duong (7DV5HBKZ58)}"
INSTALLER_SIGN_IDENTITY="${INSTALLER_SIGN_IDENTITY:-3rd Party Mac Developer Installer: Phap Duong (7DV5HBKZ58)}"

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
APP_DIR="$SOURCE_DIR/__artifacts/bundle/osx-arm64/ExifGlass.app"
ENTITLEMENTS_FILE="$SOURCE_DIR/__assets/mac/appstore/ExifGlass.AppStore.entitlements"
PROVISION_PROFILE="${PROVISION_PROFILE:-$SOURCE_DIR/__assets/mac/appstore/ExifGlass_AppStore.provisionprofile}"
BUILD_PROPS_FILE="$SOURCE_DIR/Directory.Build.props"
OUTPUT_DIR="$SOURCE_DIR/__artifacts/dist"
UPLOAD="${UPLOAD:-0}"

# --- Sanity checks -----------------------------------------------------------
if [[ ! -d "$APP_DIR" ]]; then
	echo "Error: app bundle not found at $APP_DIR (run pack-app.sh first)." >&2
	exit 1
fi
if [[ ! -f "$ENTITLEMENTS_FILE" ]]; then
	echo "Error: entitlements file not found at $ENTITLEMENTS_FILE" >&2
	exit 1
fi
if [[ ! -f "$PROVISION_PROFILE" ]]; then
	echo "Error: provisioning profile not found at $PROVISION_PROFILE" >&2
	echo "       Create a 'Mac App Store' profile for com.duongdieuphap.exifglass and save it there." >&2
	exit 1
fi
if ! security find-identity -v -p codesigning | grep -qF "$APP_SIGN_IDENTITY"; then
	echo "Error: app signing identity not found in keychain: $APP_SIGN_IDENTITY" >&2
	exit 1
fi
if ! security find-identity -v | grep -qF "$INSTALLER_SIGN_IDENTITY"; then
	echo "Error: installer signing identity not found in keychain: $INSTALLER_SIGN_IDENTITY" >&2
	exit 1
fi

EG_VERSION="$(sed -n 's:.*<ExifGlassVersion>\(.*\)</ExifGlassVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EG_VERSION" ]]; then
	echo "Error: could not read ExifGlassVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

PKG_PATH="$OUTPUT_DIR/ExifGlass_${EG_VERSION}_mac-arm64.pkg"

echo "==> Packaging ExifGlass $EG_VERSION (arm64) for the Mac App Store"
echo "    App identity       : $APP_SIGN_IDENTITY"
echo "    Installer identity : $INSTALLER_SIGN_IDENTITY"

# --- Strip debug artifacts (they break signing) ------------------------------
echo "==> Removing debug artifacts from bundle"
find "$APP_DIR" -type f -name "*.pdb" -delete
find "$APP_DIR" -type d -name "*.dSYM" -exec rm -rf {} +

# --- Embed the provisioning profile + strip quarantine xattrs ----------------
echo "==> Embedding provisioning profile"
cp "$PROVISION_PROFILE" "$APP_DIR/Contents/embedded.provisionprofile"
# Downloaded files carry a quarantine xattr the App Store rejects (ITMS-91109); clear before signing.
xattr -cr "$APP_DIR"

# --- Sign nested Mach-O first (inside-out), then the bundle ------------------
# ExifTool is a Perl script in Resources/ (sealed as a resource by the bundle signature), not signed here.
echo "==> Signing nested native libraries"
while IFS= read -r -d '' bin; do
	echo "    sign: ${bin#"$APP_DIR/"}"
	codesign --force --timestamp --options runtime --sign "$APP_SIGN_IDENTITY" "$bin"
done < <(find "$APP_DIR/Contents/MacOS" -type f \( -name "*.dylib" -o -name "*.so" \) -print0)

echo "==> Signing app bundle (sandbox + entitlements + embedded profile)"
codesign --force --timestamp --options runtime \
	--entitlements "$ENTITLEMENTS_FILE" \
	--sign "$APP_SIGN_IDENTITY" "$APP_DIR"

echo "==> Verifying code signature"
codesign --verify --deep --strict --verbose=2 "$APP_DIR"

# --- Build the App Store installer package -----------------------------------
echo "==> Building signed .pkg"
mkdir -p "$OUTPUT_DIR"
rm -f "$PKG_PATH"
productbuild --component "$APP_DIR" /Applications \
	--sign "$INSTALLER_SIGN_IDENTITY" "$PKG_PATH"

echo ""
echo "Built: $PKG_PATH"

# --- Optionally validate + upload to App Store Connect -----------------------
if [[ "$UPLOAD" != "1" ]]; then
	echo ""
	echo "To submit: set UPLOAD=1 with credentials, or drag the .pkg into Transporter.app."
	exit 0
fi

if [[ -z "${APPLE_ID:-}" || -z "${APPLE_APP_PASSWORD:-}" ]]; then
	echo "Error: UPLOAD=1 requires APPLE_ID and APPLE_APP_PASSWORD (app-specific password)." >&2
	exit 1
fi

echo "==> Validating package with App Store Connect"
xcrun altool --validate-app -f "$PKG_PATH" -t macos \
	--apple-id "$APPLE_ID" --password "$APPLE_APP_PASSWORD"

echo "==> Uploading package to App Store Connect"
xcrun altool --upload-app -f "$PKG_PATH" -t macos \
	--apple-id "$APPLE_ID" --password "$APPLE_APP_PASSWORD"

echo ""
echo "Done: uploaded $PKG_PATH to App Store Connect."
