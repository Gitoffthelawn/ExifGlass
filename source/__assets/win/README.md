# Windows MSIX packaging

Builds an MSIX of **ExifGlass.Win32**, in two flavours:

| Flavour     | Signed? | Identity / Publisher                    | Destination     | Output                                        |
|-------------|---------|-----------------------------------------|-----------------|-----------------------------------------------|
| **signed**  | Yes     | Plain name + cert Subject as publisher  | GitHub Release  | `ExifGlass_<version>_win-<arch>.msix` (per arch) |
| **msstore** | No      | Store-reserved name + publisher         | Microsoft Store | `ExifGlass_<version>_win-msstore.msixbundle` (x64+arm64) |

The Microsoft Store re-signs packages on submission, so the **msstore** build is a
single unsigned `.msixbundle`. The **signed** build (GitHub) is Authenticode-signed —
every payload `.exe`/`.dll` ExifGlass owns *and* the package itself. Both draw their
tile artwork from the app logo (`__assets/__app/logo_512.png`).

## Files

- [`build.ps1`](build.ps1) — Debug build of the Windows head (`build-win-<arch>` tasks).
- [`publish.ps1`](publish.ps1) — Release self-contained NativeAOT publish (`publish-win-<arch>` tasks).
- [`pack-msix.ps1`](pack-msix.ps1) — the packer (PowerShell 7+). Publishes, stages, signs, and packs.
- [`generate-msix-assets.ps1`](generate-msix-assets.ps1) — renders the `appxmanifest/Assets` logo set from [`__assets/__app/logo_512.png`](../__app/logo_512.png).
- [`appxmanifest/AppxManifest.xml`](appxmanifest/AppxManifest.xml) — manifest template with `{{...}}` placeholders filled in at pack time.
- [`appxmanifest/Assets/`](appxmanifest/Assets/) — logo-rendered tile/store artwork (generated).

## Prerequisites

- **Windows 10/11 SDK** — provides `makeappx.exe`, `makepri.exe`, and `signtool.exe`.
  The script auto-locates the newest one under `Windows Kits\10\bin`; no PATH setup needed.
- **.NET 10 SDK** — for `dotnet publish` (the AOT link step needs the VS Installer
  dir on PATH for `vswhere.exe`; `publish.ps1` handles that). The **msstore** bundle
  builds **both** x64 and arm64, so it needs the arm64 native toolchain (MSVC ARM64
  build tools) on the build machine.
- **Code-signing certificate** (signed flavour only) — installed in `CurrentUser\My` /
  `LocalMachine\My` with its private key, or supplied as a PFX. Without one, the signed
  package is still built but left UNSIGNED.

## Usage

Run from VS Code (Terminal → Run Task) or the CLI:

```powershell
# Signed, per-arch (cert selected by Subject substring "Duong Dieu Phap") — for GitHub
pwsh __assets/win/pack-msix.ps1 -Platform x64   -Sign
pwsh __assets/win/pack-msix.ps1 -Platform arm64 -Sign

# Microsoft Store: one unsigned x64+arm64 .msixbundle
pwsh __assets/win/pack-msix.ps1 -MsStore

# Unsigned per-arch .msix (local testing — no certificate lookup)
pwsh __assets/win/pack-msix.ps1 -Platform x64

# Sign with a PFX instead of a store certificate
pwsh __assets/win/pack-msix.ps1 -Platform x64 -Sign -CertFile C:\eg.pfx -CertPassword <pw>

# Reuse an existing publish (faster iteration)
pwsh __assets/win/pack-msix.ps1 -Platform x64 -Sign -SkipPublish
```

VS Code tasks:

- `pack-win-x64-msix`, `pack-win-arm64-msix` — a signed `.msix` per architecture (GitHub).
- `pack-win-msstore-msixbundle` — one unsigned `.msixbundle` (x64 + arm64) for the Store.

Output lands in `__artifacts/bundle/`:

- `ExifGlass_<version>_win-x64.msix` / `..._win-arm64.msix` — signed, for GitHub.
- `ExifGlass_<version>_win-msstore.msixbundle` — unsigned bundle, for the Store.

`<version>` in the file name is `<ExifGlassVersion>` from [`Directory.Build.props`](../../Directory.Build.props).

### .msix vs .msixbundle

A `.msixbundle` packs the x64 and arm64 packages together; Windows installs the
architecture matching the device, so you publish one file instead of two. The msstore
bundle's per-arch packages and the bundle itself are all unsigned — the Store signs on
submission.

## Notes

- **Version.** Both flavours stamp the same 4-part package version:
  `<Major>.<Minor>.<ExifGlassBundleBuild>.0`, where `<Major>.<Minor>` comes from
  `<ExifGlassBundleShortVersion>` (e.g. short `2.0.0` + build `1` → `2.0.1.0`). The 4th
  (revision) part is `0` because the Store reserves it; the build number lives in the 3rd
  part. The output file name uses the bare `<ExifGlassVersion>`.
  - The Store **rejects a version `<=` the last accepted one**, so **bump
    `<ExifGlassBundleBuild>` for every Store submission** (or re-release without changing
    the app version). Override the whole version with `-PackageVersion` if needed.
- **Publisher must match the certificate (signed).** The script reads the certificate's
  exact Subject DN and writes it into the manifest `Publisher`; a mismatch makes the
  package un-installable. With no certificate, a placeholder `CN=Duong Dieu Phap` is used
  and the package is left UNSIGNED.
- **Store identity (msstore).** `-MsStoreIdentityName` (`9662DuongDieuPhap.ExifGlass`)
  and `-MsStorePublisher` (`CN=29F1B9EC-D220-4DC3-BEDB-01A9CCA51904`) are the values
  Partner Center reserved for ExifGlass; do **not** sign the msstore bundle yourself.
- **Bundled ExifTool is not re-signed.** The payload's `exiftool.exe` and its
  `exiftool_files\` Perl runtime are shipped verbatim from [exiftool.org](https://exiftool.org);
  only ExifGlass's own binaries are Authenticode-signed (signed flavour). The whole
  `.msix` is signed regardless, which is the trust anchor for installation.
- **Artwork.** `appxmanifest/Assets` is generated from the app logo. Re-run
  `generate-msix-assets.ps1` after changing `__assets/__app/logo_512.png`;
  `pack-msix.ps1` auto-generates it when the folder is empty.
- **File type associations** mirror the Linux `.desktop` `MimeType` set (the image /
  RAW formats ExifTool reads). Editing the list means updating the `<uap:FileType>`
  entries in the manifest template.
- **Faster iteration.** Pass `-SkipPublish` to reuse an existing
  `__artifacts/publish/win-<arch>` instead of re-publishing.
