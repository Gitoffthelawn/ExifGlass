# Windows MSIX packaging

Builds an MSIX of **ExifGlass.Win32** for `x64` and `arm64`, for direct download /
GitHub Releases (sideload). Every payload `.exe`/`.dll` ExifGlass owns *and* the
package itself are Authenticode-signed. There is no Microsoft Store flavour.

The identity/publisher is derived from the signing certificate's exact Subject DN,
and the tile artwork is rendered from the app logo (`__assets/__app/logo_512.png`).

## Files

- [`build.ps1`](build.ps1) — Debug build of the Windows head (`build-win-<arch>` tasks).
- [`publish.ps1`](publish.ps1) — Release self-contained NativeAOT publish (`publish-win-<arch>` tasks).
- [`pack-msix.ps1`](pack-msix.ps1) — the packer (PowerShell 7+). Publishes, stages, signs, and packs the `.msix`.
- [`generate-msix-assets.ps1`](generate-msix-assets.ps1) — renders the `appxmanifest/Assets` logo set from [`__assets/__app/logo_512.png`](../__app/logo_512.png).
- [`appxmanifest/AppxManifest.xml`](appxmanifest/AppxManifest.xml) — manifest template with `{{...}}` placeholders filled in at pack time.
- [`appxmanifest/Assets/`](appxmanifest/Assets/) — logo-rendered tile/store artwork (generated).

## Prerequisites

- **Windows 10/11 SDK** — provides `makeappx.exe`, `makepri.exe`, and `signtool.exe`.
  The script auto-locates the newest one under `Windows Kits\10\bin`; no PATH setup needed.
- **.NET 10 SDK** — for `dotnet publish` (the AOT link step needs the VS Installer
  dir on PATH for `vswhere.exe`; `publish.ps1` handles that).
- **Code-signing certificate** — installed in `CurrentUser\My` / `LocalMachine\My`
  with its private key, or supplied as a PFX. Without one, the package is still
  built but left UNSIGNED.

## Usage

Run from VS Code (Terminal → Run Task) or the CLI:

```powershell
# Signed (cert selected by Subject substring "Duong Dieu Phap")
pwsh __assets/win/pack-msix.ps1 -Platform x64   -Sign
pwsh __assets/win/pack-msix.ps1 -Platform arm64 -Sign

# Unsigned (local testing — no certificate lookup)
pwsh __assets/win/pack-msix.ps1 -Platform x64

# Sign with a PFX instead of a store certificate
pwsh __assets/win/pack-msix.ps1 -Platform x64 -Sign -CertFile C:\eg.pfx -CertPassword <pw>

# Reuse an existing publish (faster iteration)
pwsh __assets/win/pack-msix.ps1 -Platform x64 -Sign -SkipPublish
```

VS Code tasks:

- `pack-win-x64-msix`, `pack-win-arm64-msix` — a signed `.msix` per architecture.
- `pack-win-all-msix` — builds both.

Output lands in `__artifacts/bundle/win/`:

- `ExifGlass_<version>_win-x64.msix` / `ExifGlass_<version>_win-arm64.msix`

`<version>` is `<ExifGlassVersion>` from [`Directory.Build.props`](../../Directory.Build.props).

## Notes

- **Version.** The MSIX package version is 4-part (an MSIX requirement):
  `<ExifGlassVersion>` padded to `X.Y.Z.0` (e.g. `2.0.0` → `2.0.0.0`). The output
  file name uses the bare `<ExifGlassVersion>`. Override the package version with
  `-PackageVersion`.
- **Publisher must match the certificate.** The script reads the certificate's exact
  Subject DN and writes it into the manifest `Publisher`; a mismatch makes the
  package un-installable. With no certificate, a placeholder `CN=Duong Dieu Phap`
  is used and the package is left UNSIGNED — sign it (and fix the Publisher to match
  your cert) before publishing.
- **Bundled ExifTool is not re-signed.** The payload's `exiftool.exe` and its
  `exiftool_files\` Perl runtime are shipped verbatim from [exiftool.org](https://exiftool.org);
  only ExifGlass's own binaries are Authenticode-signed. The whole `.msix` is signed
  regardless, which is the trust anchor for installation.
- **Artwork.** `appxmanifest/Assets` is generated from the app logo. Re-run
  `generate-msix-assets.ps1` after changing `__assets/__app/logo_512.png`;
  `pack-msix.ps1` auto-generates it when the folder is empty.
- **File type associations** mirror the Linux `.desktop` `MimeType` set (the image /
  RAW formats ExifTool reads). Editing the list means updating the `<uap:FileType>`
  entries in the manifest template.
- **No certificate?** The package is still produced, just left UNSIGNED (with a
  warning). Sign it before publishing — an unsigned MSIX cannot be installed.
- **Faster iteration.** Pass `-SkipPublish` to reuse an existing
  `__artifacts/publish/win-<arch>` instead of re-publishing.
