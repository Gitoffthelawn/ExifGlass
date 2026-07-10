# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

ExifGlass is an EXIF-metadata viewer that wraps the command-line **ExifTool** (exiftool.org). It runs standalone (CLI arg, drag-drop, file picker) and plugs into both **ImageGlass 10** (via `ImageGlass.SDK`) and **ImageGlass 9** (via the Windows-only `ImageGlass.Tools`) as an integrated tool that live-updates as the user navigates images.

This directory (`source/`) is a ground-up rewrite on **.NET 10 / Avalonia 12**, built **NativeAOT, Windows-first**. It follows the ImageGlass multi-project layout (`D:\_GITHUB\@d2phap\ImageGlass\source`): a **cross-platform `net10.0` library (`ExifGlass.Lib`) holds all logic + UI**, and a thin **per-platform executable head** references it. Only the Windows head (`ExifGlass.Win32`, `net10.0-windows`, ships `ExifGlass.exe`) exists today — macOS/Linux heads (`ExifGlass.Mac`, `ExifGlass.Linux`) are planned and would sit beside it, referencing the same library. The solution is `ExifGlass.slnx`.

> **Source hygiene:** never reference any prior/"v1"/"old version" in source, comments, or commit messages — write everything as the target design. The repo root contains a `v1/` folder (the previous shipping app) and a v1-era `README.md`; treat both as out of scope for this rewrite and do not import their patterns.

## Commands

All commands run from this `source/` directory.

```bash
dotnet build ExifGlass.slnx -c Debug            # build everything
dotnet test  ExifGlass.slnx -c Debug            # run all xUnit tests (ExifGlass.Lib.Test)
dotnet test  ExifGlass.slnx --filter "FullyQualifiedName~UpdateServiceTests"   # one test class
dotnet test  ExifGlass.slnx --filter "FullyQualifiedName~CheckAsync_Skips_WhenWithinThrottleWindow"  # one test
```

Analyzer / style check (IntelliSense-level messages don't fail a normal build):

```bash
dotnet build ExifGlass.slnx -p:EnforceCodeStyleInBuild=true
dotnet format ExifGlass.slnx --verify-no-changes --severity info   # lists IDE/CA/MVVMTK suggestions
```

**AOT publish — the key correctness gate; re-run after any nontrivial change.** On Windows the ILC link step shells out to bare `vswhere.exe`, so prepend the VS Installer dir to `PATH` first (PowerShell), and kill any running instance so the exe copy isn't locked:

```powershell
Get-Process ExifGlass,exiftool -ErrorAction SilentlyContinue | Stop-Process -Force
$env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;" + $env:PATH
dotnet publish ExifGlass.Win32/ExifGlass.Win32.csproj -r win-x64 -c Release
# -> ExifGlass.Win32/bin/Release/net10.0-windows/win-x64/publish/ExifGlass.exe  (~33 MB)
```

A clean publish shows **only** the expected ProDataGrid baseline (`IL2104` + `IL3053` on `Avalonia.Controls.DataGrid.dll`). Any other IL warning — and **any `IL3050`** — is a regression to fix, not suppress.

Run standalone: `ExifGlass.exe "<image>"`. Force a theme for one run without persisting: `ExifGlass.exe /Theme=Dark "<image>"` (see CLI overrides below). Force-kill leaves the `exiftool -stay_open` child orphaned — clean up with `Get-Process ExifGlass,exiftool | Stop-Process -Force`.

## Architecture (the big picture)

Three projects (mirroring the ImageGlass `Lib` + platform-head layout), split so all logic stays testable without a running UI and the Windows-only integration is quarantined to the executable head:

- **`ExifGlass.Lib`** (`net10.0`, `IsAotCompatible=true`, **cross-platform**) — everything shared: all pure logic (ExifTool invocation/parsing, exports, config load/merge, update check, path resolution, version compare) **and** the Avalonia UI (`App`, Views, ViewModels, Services, `Composition`, the `Integration` seam for standalone + ImageGlass 10 hosts, Styles, brand icon assets). The logic files keep their `ExifGlass.Core.*` namespaces (the former Core project was folded in here); the UI uses `ExifGlass.*`. Assembly name `ExifGlass.Lib` (so XAML resources are `avares://ExifGlass.Lib/…`); root namespace `ExifGlass`.
- **`ExifGlass.Win32`** — the **Windows executable head** (`net10.0-windows`, `AssemblyName=ExifGlass` → `ExifGlass.exe`, `PublishAot`, `TrimMode=full`). Holds only `Program.cs`, the ImageGlass 9 host, `app.manifest`, the app icon, and the bundled `exiftool.exe`. It is the **only** project allowed to reference the Windows-only `ImageGlass.Tools` package. Planned `ExifGlass.Mac` / `ExifGlass.Linux` heads would sit beside it and reference the same `ExifGlass.Lib`.
- **`ExifGlass.Lib.Test`** — xUnit over the library (references `ExifGlass.Lib`).

**Central build management** (same as ImageGlass): `Directory.Packages.props` pins every NuGet version (`ManagePackageVersionsCentrally`; csprojs carry versionless `<PackageReference>`s). `Directory.Build.props` single-sources the app version (`<ExifGlassVersion>`) + product identity; `Directory.Build.targets` propagates it to `Version`/`FileVersion`/`InformationalVersion` for every project. `Helpers/AppInfo` reads the *library's* embedded informational version, so it must stay a clean numeric version (no build-metadata suffix) or the update-check comparison breaks.

**Manual composition root, no DI container.** `Composition/AppServices.cs` wires the object graph with plain `new` (fastest startup, zero reflection/trim warnings). `App.OnFrameworkInitializationCompleted` is the bootstrap: parse args → load settings + apply CLI overrides → apply theme → create `MainWindow` + VM → restore window bounds → pick the source host → wire it → `host.Start()`.

**One file-loading seam.** Every entry mode funnels into `MainWindowViewModel.LoadFileAsync(path)` via `Integration/IImageSourceHost`:
- `StandaloneSourceHost` — emits the initial CLI file. (Drag-drop and the picker call `LoadFileAsync` directly.)
- `ImageGlassSourceHost : ToolBase` — the ImageGlass 10 SDK pipe client; `--pipe` on the command line selects it. `OnPhotoChanged` is the live-nav trigger.
- `ImageGlass9SourceHost` (in the Windows head) — wraps `ImageGlass.Tools.ImageGlassTool`; `--ig-tool-pipe-code=<code>` selects it. `IMAGE_LOADING` is the live-nav trigger.

**Host selection is a factory hook, not a hard reference.** The cross-platform `App` (in `ExifGlass.Lib`) cannot name the Windows-only `ImageGlass9SourceHost`, so it exposes `static App.SourceHostFactory` (`Func<StartupOptions, string[], IImageSourceHost?>`). The Windows `Program.Main` sets it to return an `ImageGlass9SourceHost` for `AppMode.ImageGlass9`; returning `null` falls back to the library's built-in standalone / ImageGlass-10 selection. `Helpers/CommandLine.Parse` maps `--pipe` → `ImageGlass`, `--ig-tool-pipe-code=` → `ImageGlass9`.

`LoadFileAsync` owns a **CTS swap**: each load cancels the previous token, so during rapid navigation the newest file always wins and superseded reads are silently discarded (`OperationCanceledException` swallowed).

**ExifTool access.** `Services/ExifToolService` is the seam (`IExifToolService`). Reads go through a persistent **`exiftool -stay_open` daemon** (`ExifToolDaemon`) for fast live navigation; binary extraction and `-ver` validation stay one-shot via `IProcessRunner`/CliWrap. `Helpers/ExifToolCommand` is the single argv builder feeding both the real run and the footer command preview, so they can't drift. The exe path resolves Settings-override → bundled → PATH (`ExifToolPathResolver`), cross-platform.

**Settings.** `ISettingsService` holds a mutable `AppConfig`; layered load is defaults → JSON file → CLI `/Key=Value` overrides. Config path is per-OS (`%LOCALAPPDATA%\ExifGlass\exifglass.config.json` on Windows) via `Helpers/AppPaths`. Persisted **only** on window close (`SaveOnClose`) and after an update check (stamps `LastUpdateCheck`).

## AOT is the through-line — preserve these invariants

NativeAOT forbids runtime codegen and trims unused members, so every choice avoids reflection where a compiled/static path exists. When changing code, keep to these or the native binary breaks silently:

- **Compiled XAML + compiled bindings only** (`EnableAvaloniaXamlCompilation`, `AvaloniaUseCompiledBindingsByDefault`). Windows set `x:DataType`. No `ReflectionBinding`, no runtime `AvaloniaXamlLoader.Load(string)`.
- **`System.Text.Json` source-gen only** — serialize/deserialize through `AppJsonContext` (e.g. `AppJsonContext.Default.UpdateInfo`) and the SDK's `ToolJsonContext`. Never the reflection `JsonSerializer` overloads. Every new persisted/feed type needs a `[JsonSerializable]` entry.
- **Grid is reflection-free**: grouping via `Integration/TagGroupDescription` (a `DataGridGroupDescription`, not the path-based one); header-click sorting handled in `MainWindow.OnSorting` with `DataGridSortDescription.FromComparer` + explicit `Comparer<ExifTagItem>` (never `FromPath`). The row model `ExifTagItem` is `[DynamicallyAccessedMembers(...)]`-annotated so the grid's `Type.GetProperties()` survives trimming.
- **No reflection DI** (no `Microsoft.Extensions.DependencyInjection`/`Ioc`), **no `Microsoft.Extensions.Configuration`** (hand-written layered load), **no reflection for cell copy or config-key mapping** (explicit `switch` / `nameof` comparisons).
- **Expected IL warnings** are pinned to the exact site with `[UnconditionalSuppressMessage("Trimming","IL2026", Justification=...)]` (the DataGrid/`DataGridCollectionView` usage in `MainWindow` and `MainWindowViewModel.BuildGroupedView`) — never a project-wide `NoWarn`. **Never suppress `IL3050`**; it means an expression-based API slipped in.

## ImageGlass 10 SDK integration specifics

`ImageGlassSourceHost` subclasses `ImageGlass.SDK.Tools.ToolBase`:
- The SDK's `OnXxx` hooks are `protected internal virtual`; overriding them from this assembly must use **`protected override`** (not `protected internal` — the `internal` part is inaccessible across assemblies). `ToolId` is `public abstract` → `public override` and must equal the `igconfig.json` entry (`"Tool_ExifGlass"`).
- SDK callbacks fire on the pipe read-loop thread, never the UI thread — **every** `FileRequested`/`CloseRequested` raise marshals via `Dispatcher.UIThread.Post`. `OnPhotoChanged` is a synchronous `void`: never await inline, never let it throw. `RunAsync` blocks in the pipe loop, so `Start()` runs it on a background `Task`.
- The pipe says *which file & when*; ExifTool remains the source of truth (always run the local read on the reported path).

Full details + how to verify the pipe without ImageGlass (a mock `NamedPipeServerStream` host) are in the memory `imageglass-sdk-tool-integration`.

## Conventions

- **Every `.cs` file starts with the GPLv3 header block** (see any existing file); new files must include it.
- **XML doc comments are always multi-line** — never `/// <summary>text</summary>` on one line; break `<summary>` across three lines.
- MVVM uses CommunityToolkit source generators: `[ObservableProperty]` on **partial properties** (`public partial T X { get; set; }`), `[RelayCommand]`. Keep it AOT-safe — generators only, never `Ioc`.
- App version comes from `AssemblyInformationalVersion` via `Helpers/AppInfo` (embedded metadata; cross-platform + single-file/AOT-safe). Never use `Assembly.Location`/`FileVersionInfo` — empty/absent under single-file/AOT and on Linux/macOS. Set the product version through `<ExifGlassVersion>` in `Directory.Build.props` (propagated to all projects by `Directory.Build.targets`) — never per-project.
- **Central package management:** add/upgrade NuGet versions in `Directory.Packages.props`; `<PackageReference>` in a csproj must stay versionless.

## Verifying UI/behavior changes

`dotnet test` covers the logic, but the grid, dialogs, live navigation, and update flow only manifest in the running app. Launch it (Debug exe at `ExifGlass.Win32/bin/Debug/net10.0-windows/ExifGlass.exe`) and screenshot. Notes:
- CLI overrides (`/Theme=Dark`, `/CheckForUpdates=false`, `/WindowWidth=900`, …) are in-memory only; config persists solely on window close, so **force-kill** after screenshotting to avoid mutating the user's real config.
- The NativeAOT window renders on a GPU surface — GDI `CopyFromScreen` returns black; use `PrintWindow(hwnd, hdc, 2)`.
- The persistent daemon means a plain force-kill can orphan `exiftool`; sweep both processes afterward.

The full rewrite plan (locked decisions, phasing M1–M4, risks) lives at `~/.claude/plans/create-the-complete-new-temporal-storm.md`; project-specific gotchas are captured in Claude memory (`imageglass-sdk-tool-integration`, `prodatagrid-aot-warnings`, `avalonia12-window-state-restore`, `aot-publish-link-env`, `aot-crossplatform-app-version`, `exifglass-ui-verify-workflow`).
