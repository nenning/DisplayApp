# DisplayApp

[![CI](https://github.com/nenning/DisplayApp/actions/workflows/ci.yml/badge.svg)](https://github.com/nenning/DisplayApp/actions/workflows/ci.yml)

DisplayApp is a .NET application designed to simplify the process of managing display configurations when connecting to multiple external screens. If you frequently connect to different monitors and are tired of manually positioning them or setting the main display, this app provides a quick and automated solution.

## Features
- Automatically extends the desktop to all connected displays.
- Shows a number on each screen and lets you type the order from left to right — supports any number of displays.
- Bottom-aligns all screens to the primary display.
- Picks the primary display automatically: the **center** screen when there is an odd number of displays, otherwise the **largest** by pixel area.

## Usage
1. Download `DisplayApp.zip` from the [Releases](https://github.com/nenning/DisplayApp/releases) page, extract it, and run `DisplayApp.exe`. No .NET installation required.
2. The app extends the desktop and shows a number overlay on each screen.
3. Type the numbers from left to right, in the order the screens are physically arranged on your desk, and press Enter. Examples for three screens with display 2 on the left, 1 in the middle, and 3 on the right:
   - `2 1 3`
   - `2,1,3`
   - `213` (compact form, single-digit per screen — works for up to 9 displays)

## Prerequisites
- Windows operating system.
- At least two active displays.

## Building from Source
1. Clone the repository:
   ```
   git clone https://github.com/nenning/DisplayApp.git
   ```
2. Build and run:
   ```
   dotnet run
   ```
   Or build a self-contained executable:
   ```
   dotnet publish -c Release -r win-x64 --self-contained
   ```

## Releasing

Run `release.ps1` from the repo root. It auto-increments the patch version, validates the build, commits, tags, and pushes — the GitHub Action then builds and publishes the release automatically.

```powershell
.\release.ps1          # auto-increment patch (e.g. 1.1.0 → 1.1.1)
.\release.ps1 -Version 1.2.0   # explicit version
```

## Contributing
Contributions are welcome! If you encounter issues or have feature requests, feel free to open an issue or submit a pull request.

## License
This project is licensed under the [MIT License](https://opensource.org/licenses/MIT). Feel free to use, modify, and distribute it.

## Acknowledgements
- Uses Windows API functions like `EnumDisplayDevices`, `ChangeDisplaySettingsEx`, and `SetDisplayConfig` for display management.
- Inspired by the need for a streamlined display configuration tool for multi-monitor setups.
