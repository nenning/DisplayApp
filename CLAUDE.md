# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DisplayApp is a .NET 9.0 console application for Windows that automates multi-monitor display configuration. It sets the largest connected screen as primary and positions the laptop display relative to it (left or right).

## Build and Run Commands

**Build the project:**
```bash
dotnet build
```

**Build for release:**
```bash
dotnet build -c Release
```

**Run the application:**
```bash
dotnet run -- -l    # Laptop on left
dotnet run -- -r    # Laptop on right
dotnet run          # Interactive mode
```

**Publish as self-contained executable:**
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Architecture

### Core Components

**Single-file architecture**: The entire application is contained in `Program.cs` - a straightforward console application with no separate class files.

**Windows API Integration**: Uses P/Invoke to call Windows User32.dll functions:
- `EnumDisplayDevices` - Enumerates all display devices
- `EnumDisplaySettings`/`EnumDisplaySettingsEx` - Retrieves display settings
- `ChangeDisplaySettingsEx` - Applies new display configurations
- Uses `DisplaySwitch.exe /extend` to extend displays after configuration

**Display Configuration Flow**:
1. Enumerate all active displays using `GetConnectedDisplays()`
2. Identify largest display (by pixel area) to set as primary
3. Calculate position offsets for laptop display relative to primary
4. Apply settings: primary display first (`SetPrimaryDisplay`), then laptop (`ApplyDisplaySettings`)
5. Call `ExtendDisplays()` to ensure displays are in extended mode

**Coordinate System**: Top-left of primary display is always (0,0). Laptop positioned using negative X offset (left) or positive X offset (right), with Y offset to align bottom edges.

**Key Structures**:
- `DISPLAY_DEVICE`: Windows structure for display device enumeration
- `DEVMODE`: Windows structure containing display settings (resolution, position, flags)

## Platform Requirements

- Windows-only (uses Windows API)
- .NET 9.0 runtime
- Requires at least 2 active displays to function
