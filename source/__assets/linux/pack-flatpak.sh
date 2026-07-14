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
# Package the Linux head of ExifGlass for Flatpak (linux-x64 only).
#
#   1. Re-publishes a fresh self-contained NativeAOT build so the bundle always
#      matches the current source + ExifGlassVersion.
#   2. Tars the publish output into a release archive (strips debug symbols).
#   3. Writes the download URL + sha256 into the Flatpak manifest.
#   4. If flatpak-builder is available: builds a single-file .flatpak bundle (for
#      direct download / GitHub Releases), optionally GPG-signed, and installs it
#      locally to test.
#
# Run via the "pack-linux-x64-flatpak" VS Code task (it builds the self-contained
# ExifTool first) or directly:  bash __assets/linux/pack-flatpak.sh
# Distribution steps: __assets/linux/flatpak/README.md
#
# Env overrides:
#   RELEASE_TAG=<tag>   tag used to build the GitHub download URL (default: <ExifGlassVersion>)
#   GPG_KEY=<keyid>     sign the .flatpak bundle with this GPG key and embed its public
#                       half so the signature is verifiable on install (optional; the VS
#                       Code task prompts for it). Empty => unsigned bundle.
# -----------------------------------------------------------------------------
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

PUBLISH_DIR="$SOURCE_DIR/__artifacts/publish/linux-x64"
FLATPAK_DIR="$SOURCE_DIR/__assets/linux/flatpak"
# All final artifacts from every platform land directly in __artifacts/bundle/ (the
# .tar.gz + .flatpak sit here). Intermediate build state (staging dirs, the OSTree repo,
# the flatpak-builder cache, the exported public key) goes under __artifacts/staging/
# and is deleted when the script exits, so bundle/ only ever holds shippable files.
OUT_DIR="$SOURCE_DIR/__artifacts/bundle"
STAGING_ROOT="$SOURCE_DIR/__artifacts/staging"
WORK_DIR="$STAGING_ROOT/linux-flatpak"
STATE_DIR="$WORK_DIR/.flatpak-builder"

# Remove the whole staging tree on exit (success or failure) so no scratch dir - not even
# an empty __artifacts/staging/ parent - is left behind.
trap 'rm -rf "$STAGING_ROOT"' EXIT
BUILD_PROPS_FILE="$SOURCE_DIR/Directory.Build.props"
MANIFEST="$FLATPAK_DIR/io.github.d2phap.exifglass.yaml"
APP_ID="io.github.d2phap.exifglass"
LOGO_SVG="$SOURCE_DIR/__assets/__app/logo.svg"
LOGO_PNG="$SOURCE_DIR/__assets/__app/logo_512.png"
# The self-contained ExifTool binary the Linux csproj bundles when present. Required
# for the Flatpak: the org.freedesktop runtime has no system Perl, so the Perl-script
# fallback would not run inside the sandbox.
EXIFTOOL_BIN="$SOURCE_DIR/__assets/exiftool/unix/build/linux-x64/exiftool"

# Signing key for the .flatpak bundle, supplied via the GPG_KEY env var - the VS Code
# "pack-linux-x64-flatpak" task prompts for it, or run: GPG_KEY=<id> bash <script>.
# Empty => unsigned bundle. If the key isn't in the local keyring, the build falls
# back to unsigned (with a warning) rather than failing.
GPG_KEY="${GPG_KEY:-}"

# --- Read version from Directory.Build.props ---
EXIF_VERSION="$(sed -n 's:.*<ExifGlassVersion>\(.*\)</ExifGlassVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$EXIF_VERSION" ]]; then
	echo "Error: could not read ExifGlassVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

# ExifGlass release tags are the bare version (no "v" prefix), matching GitHub Releases.
RELEASE_TAG="${RELEASE_TAG:-$EXIF_VERSION}"
TARBALL_NAME="ExifGlass_${EXIF_VERSION}_linux_x64.tar.gz"
BUNDLE_NAME="ExifGlass_${EXIF_VERSION}_linux-x64.flatpak"
TARBALL_PATH="$OUT_DIR/$TARBALL_NAME"
BUNDLE_PATH="$OUT_DIR/$BUNDLE_NAME"
RELEASE_URL="https://github.com/d2phap/ExifGlass/releases/download/${RELEASE_TAG}/${TARBALL_NAME}"

# --- Require the self-contained ExifTool binary ---
if [[ ! -f "$EXIFTOOL_BIN" ]]; then
	echo "Error: self-contained ExifTool not found at:" >&2
	echo "         $EXIFTOOL_BIN" >&2
	echo "       The Flatpak runtime has no system Perl, so the bundle must embed the" >&2
	echo "       self-contained ExifTool binary. Build it first:" >&2
	echo "           bash __assets/linux/build-exiftool.sh x64" >&2
	echo "       (VS Code task: build-exiftool-linux-x64 - the pack task depends on it.)" >&2
	exit 1
fi

# --- Purge output dirs from earlier layouts ---
# Earlier runs wrote to a per-platform subfolder (bundle/linux-flatpak/) with a sibling
# work dir (bundle/.linux-flatpak-work/). Remove both so the flattened bundle/ doesn't
# carry stale copies. The fresh artifacts are written straight into bundle/ below.
rm -rf "$SOURCE_DIR/__artifacts/bundle/linux-flatpak" \
       "$SOURCE_DIR/__artifacts/bundle/.linux-flatpak-work"
mkdir -p "$OUT_DIR"

# --- Publish a fresh self-contained AOT build ---
# Always re-publish so the bundle matches the current source and ExifGlassVersion. The
# version is baked into the binary; packaging a stale publish dir would ship the wrong
# version and old code. The csproj bundles the ExifTool binary found above.
echo "==> Publishing ExifGlass $EXIF_VERSION (linux-x64, AOT)"
rm -rf "$PUBLISH_DIR"
bash "$SCRIPT_DIR/publish.sh" x64

if [[ ! -x "$PUBLISH_DIR/ExifGlass" ]]; then
	echo "Error: publish did not produce $PUBLISH_DIR/ExifGlass" >&2
	exit 1
fi
if [[ ! -f "$PUBLISH_DIR/exiftool" ]]; then
	echo "Error: publish did not bundle ExifTool at $PUBLISH_DIR/exiftool" >&2
	exit 1
fi

# --- Prepare app-id-named icons from the shared brand assets ---
if [[ -f "$LOGO_SVG" ]]; then
	cp "$LOGO_SVG" "$FLATPAK_DIR/$APP_ID.svg"
fi
if [[ -f "$LOGO_PNG" ]]; then
	cp "$LOGO_PNG" "$FLATPAK_DIR/$APP_ID.png"
fi

# --- Stage the payload and build the tarball ---
# Tar with a single top-level "ExifGlass/" dir so the manifest can use
# strip-components: 1. Exclude debug artifacts that bloat the package.
echo "==> Staging payload (excluding *.dbg / *.pdb)"
STAGE_DIR="$WORK_DIR/stage"
rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR/ExifGlass" "$OUT_DIR"
( cd "$PUBLISH_DIR" && cp -a . "$STAGE_DIR/ExifGlass/" )
find "$STAGE_DIR/ExifGlass" -type f \( -name "*.dbg" -o -name "*.pdb" \) -delete

echo "==> Creating tarball: $TARBALL_NAME"
rm -f "$TARBALL_PATH"
tar -czf "$TARBALL_PATH" -C "$STAGE_DIR" ExifGlass

SHA256="$(sha256sum "$TARBALL_PATH" | cut -d' ' -f1)"
echo "    sha256: $SHA256"

# --- Wire url + sha256 into the manifest ---
echo "==> Updating manifest source (url + sha256)"
sed -i -E "s#^( *)url: https://github.com/d2phap/ExifGlass/releases/download/.*#\1url: ${RELEASE_URL}#" "$MANIFEST"
sed -i -E "s#^( *)sha256: [0-9a-f]{64}#\1sha256: ${SHA256}#" "$MANIFEST"

# --- Optional: build the .flatpak bundle + install locally via flatpak-builder ---
# Read the runtime version from the manifest so this stays in sync with it.
RUNTIME_VER="$(sed -n "s/^runtime-version: *['\"]\?\([0-9.]*\).*/\1/p" "$MANIFEST" | head -n1)"
RUNTIME_VER="${RUNTIME_VER:-25.08}"
BUNDLE_BUILT=0

if ! command -v flatpak-builder >/dev/null 2>&1; then
	echo "==> flatpak-builder NOT found - skipping bundle build."
	echo "    Install it to build the .flatpak bundle:"
	echo "        sudo apt install flatpak-builder"
elif ! { flatpak info "org.freedesktop.Platform//$RUNTIME_VER" >/dev/null 2>&1 \
		&& flatpak info "org.freedesktop.Sdk//$RUNTIME_VER" >/dev/null 2>&1; }; then
	echo "==> Runtime/SDK $RUNTIME_VER not installed - skipping bundle build."
	echo "    Install them, then re-run this script:"
	echo "        flatpak install -y flathub org.freedesktop.Platform//$RUNTIME_VER org.freedesktop.Sdk//$RUNTIME_VER"
else
	echo "==> Building Flatpak (bundle + local install)"

	# Self-contained staging dir so all manifest sources resolve locally
	# (the committed metadata files + the freshly built tarball).
	LOCAL_DIR="$WORK_DIR/local"
	REPO_DIR="$WORK_DIR/repo"
	rm -rf "$LOCAL_DIR"
	mkdir -p "$LOCAL_DIR"
	cp "$FLATPAK_DIR/$APP_ID.desktop" \
	   "$FLATPAK_DIR/$APP_ID.metainfo.xml" \
	   "$FLATPAK_DIR/$APP_ID.svg" \
	   "$FLATPAK_DIR/$APP_ID.png" \
	   "$LOCAL_DIR/"
	cp "$TARBALL_PATH" "$LOCAL_DIR/app.tar.gz"

	# Local manifest: swap the remote archive source for the local tarball
	# (replace the url line with a path, drop the now-irrelevant sha256 line).
	sed -E -e "s#^( *)url: .*#\1path: app.tar.gz#" \
	       -e "/^ *sha256: [0-9a-f]{64}/d" \
	       "$MANIFEST" > "$LOCAL_DIR/$APP_ID.yaml"

	# --- GPG signing (optional, when GPG_KEY is set) ---
	# Sign the OSTree commit AND embed the matching public key in the bundle, so
	# the origin remote created on `flatpak install <bundle>.flatpak` can actually
	# verify the signature. Without --gpg-keys the embedded signature has nothing
	# to check against and is effectively inert. Unset GPG_KEY => unsigned output.
	GPG_SIGN_ARGS=()    # repo commit signing (flatpak-builder + build-bundle)
	GPG_BUNDLE_ARGS=()  # build-bundle only: signing + embedded public key
	if [[ -z "$GPG_KEY" ]]; then
		echo "==> GPG_KEY empty - building an UNSIGNED bundle."
	elif ! gpg --list-secret-keys "$GPG_KEY" >/dev/null 2>&1; then
		# Don't abort the whole pack (the tarball is already built); just skip signing.
		echo "WARNING: GPG_KEY='$GPG_KEY' is set but no matching SECRET key is in your keyring." >&2
		echo "         Building an UNSIGNED bundle. To sign, generate the key once:" >&2
		echo "             gpg --quick-generate-key \"$GPG_KEY\" default default never" >&2
		echo "         (an EV/code-signing cert is X.509 and cannot be used here - gpg needs its own key)" >&2
	else
		PUBKEY_FILE="$WORK_DIR/$APP_ID.pubkey.gpg"
		echo "==> GPG signing enabled (key: $GPG_KEY) - embedding public key in bundle"
		# Export the public half (binary, what flatpak --gpg-keys expects). Redirect
		# instead of --output to avoid gpg's interactive overwrite prompt on re-runs.
		gpg --export "$GPG_KEY" > "$PUBKEY_FILE"
		GPG_SIGN_ARGS=(--gpg-sign="$GPG_KEY")
		GPG_BUNDLE_ARGS=(--gpg-sign="$GPG_KEY" --gpg-keys="$PUBKEY_FILE")
	fi

	# Build into a repo (for the bundle) and install for the current user (to test).
	# --state-dir keeps the build cache under __artifacts/ instead of the repo root.
	# --disable-cache is REQUIRED: the single module is a local tarball whose name is
	# constant (app.tar.gz) but whose contents change every release. Without it,
	# flatpak-builder matches the cached module build and ships the OLD binary even
	# though the tarball is fresh (--force-clean only wipes the build dir, not the
	# cache) - the .flatpak ends up version-stamped new but containing old code.
	flatpak-builder --state-dir="$STATE_DIR" --user --install --force-clean --disable-cache \
		--repo="$REPO_DIR" "${GPG_SIGN_ARGS[@]}" \
		"$WORK_DIR/build" "$LOCAL_DIR/$APP_ID.yaml"

	# Single-file bundle for direct download / GitHub Releases. --runtime-repo
	# lets installers auto-fetch the freedesktop runtime from Flathub.
	echo "==> Building .flatpak bundle: $BUNDLE_NAME"
	rm -f "$BUNDLE_PATH"
	flatpak build-bundle "${GPG_BUNDLE_ARGS[@]}" \
		--runtime-repo=https://dl.flathub.org/repo/flathub.flatpakrepo \
		"$REPO_DIR" "$BUNDLE_PATH" "$APP_ID"
	BUNDLE_BUILT=1
fi

echo ""
echo "Done."
echo "  Tarball (Flathub source): $TARBALL_PATH"
echo "  sha256                  : $SHA256"
echo "  Manifest url            : $RELEASE_URL"
if [[ "$BUNDLE_BUILT" == "1" ]]; then
	echo "  Bundle (direct install) : $BUNDLE_PATH"
	# PUBKEY_FILE is only set when signing actually happened (key present in keyring).
	# The public key is embedded in the .flatpak itself; the exported copy was scratch
	# and is removed on exit, so point users at gpg rather than a now-deleted file.
	if [[ -n "${PUBKEY_FILE:-}" ]]; then
		echo "  Signed with GPG key     : $GPG_KEY (public key embedded in the bundle)"
		echo "  Publish the fingerprint so users can trust the key:"
		echo "      gpg --fingerprint $GPG_KEY"
	else
		echo "  (unsigned bundle - no usable GPG key)"
	fi
	echo ""
	echo "Installed to your USER flatpak. Test with:"
	echo "    flatpak run $APP_ID [image-path]"
	# A previously double-clicked bundle installs SYSTEM-wide and will shadow this
	# fresh user install (you'd keep testing the old code). Warn if one exists.
	if flatpak --system info "$APP_ID" >/dev/null 2>&1; then
		echo ""
		echo "WARNING: an older SYSTEM-wide install exists and will be launched instead."
		echo "         Remove it so you test this build:"
		echo "             flatpak uninstall --system $APP_ID"
	fi
fi
echo ""
echo "Next: upload both files to the '$RELEASE_TAG' GitHub release, then (optionally)"
echo "      submit the manifest to Flathub (see __assets/linux/flatpak/README.md)."
