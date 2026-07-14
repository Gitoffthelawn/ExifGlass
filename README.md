ExifGlass - EXIF Metadata Viewing Tool
===

ExifGlass is a cross-platform EXIF metadata viewing tool, designed to work seamlessly with [ImageGlass - A Fast, Seamless Photo Viewer](https://imageglass.org), but can also be used as a standalone software on your computer. To use ExifGlass, you need to have [ExifTool by Phil Harvey](https://exiftool.org) installed on your system.

With ExifGlass, you can easily access a comprehensive overview of the technical details associated with your images, including camera settings, location data, and more. This tool is particularly useful for professional photographers or anyone interested in the technical aspects of digital photography. Whether you use ExifGlass as a standalone software or in conjunction with ImageGlass, it provides a convenient and efficient way to view and manage the metadata associated with your images.

You can download ExifGlass for free. To support the development of ExifGlass and gain access to future updates, consider purchasing it from the [Microsoft Store](https://apps.microsoft.com/detail/9MX8S9HZ57W8?launch=true&cid=github_readme&mode=full).

[![ExifGlass on Microsoft Store](https://github.com/d2phap/ExifGlass/assets/3154213/ed878a58-cec3-4f74-ac56-c215477f6c61)](https://apps.microsoft.com/detail/9MX8S9HZ57W8?launch=true&cid=github_readme&mode=full)

<a href="https://github.com/d2phap/ExifGlass/releases">
  <img src="https://img.shields.io/github/downloads/d2phap/exifglass/total?color=%23ed604c&label=total%20downloads&style=for-the-badge" /></a>
  
<a href="https://github.com/d2phap/ExifGlass/releases">
  <img src="https://img.shields.io/github/downloads/d2phap/exifglass/latest/total?color=%23ed604c&label=latest%20version&style=for-the-badge" />
</a>


<br/>
<img src="https://raw.githubusercontent.com/d2phap/ExifGlass/refs/heads/main/source/__assets/linux/screenshots/eg_flatpak_2.png" />


## ExifGlass features
| Feature | Free version | [ExifGlass Store](https://apps.microsoft.com/detail/9MX8S9HZ57W8?launch=true&cid=github_readme&mode=full) | 
| -- | -- | -- |
| Reads EXIF metadata | ✅ | ✅ |
| Seamlessly works with [ImageGlass](https://imageglass.org) | ✅ | ✅ |
| Drag-n-drop file to view metadata | ✅ | ✅ |
| Copy metadata | ✅ | ✅ |
| Export as Text, JSON, CSV | ✅ | ✅ |
| Custom ExifTool's command-line arguments | ✅ | ✅ |
| .NET self-contained | ✅ | ✅ |


## Register ExifGlass as external tool in ImageGlass
> [!TIP]
> For ImageGlass 9, please refer to [ImageGlass Docs / ImageGlass tools](https://imageglass.org/docs/imageglass-tools#add-your-tool-to-imageglass).

Launch ImageGlass 10, open Settings > Tools, and click "Add" button. In the "Add external tool" window, fill in the below information:

### 🪟 Windows 10/11
- ID: `Tool_ExifGlass` (you can change the ID)
- Name: `ExifGlass 2` (you can change the tool name)
- Integrated with ImageGlass.SDK: check the box
- Executable: `exifglass` (or the full path of `ExifGlass.exe`)
- Argument: `<file>`
- Hotkeys: optional

### 🍎 macOS
- ID: `Tool_ExifGlass` (you can change the ID)
- Name: `ExifGlass 2` (you can change the tool name)
- Integrated with ImageGlass.SDK: check the box
- Executable: `/Applications/ExifGlass.app`
- Argument: `<file>`
- Hotkeys: optional

### 🐧 Linux
- ID: `Tool_ExifGlass` (you can change the ID)
- Name: `ExifGlass 2` (you can change the tool name)
- Integrated with ImageGlass.SDK: check the box
- Executable: `flatpak`
- Argument: `run io.github.d2phap.exifglass <file>`
- Hotkeys: optional

<img width="800" alt="image" src="https://github.com/user-attachments/assets/28c26471-1858-4928-885c-1b1b1cd34793" />



## Build ExifGlass from source code
- .NET 10.0 and Visual Studio 2022
- Add [ImageGlass.SDK](https://www.nuget.org/packages/ImageGlass.SDK) package (for ImageGlass 10).
- Add [ImageGlass.Tools](https://www.nuget.org/packages/ImageGlass.Tools) package (for ImageGlass 9).


## License
ExifGlass is free for both personal and commercial use, except the Store version. It is released under the terms of [GPLv3](https://github.com/d2phap/ExifGlass/blob/main/LICENSE).

