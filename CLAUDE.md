# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DisplayApp is a .NET 10 console application for Windows (`net10.0-windows`, WinForms enabled) that automates multi-monitor display configuration. It extends the desktop to all connected screens, shows a number overlay on each, and lets the user type the screens' left-to-right order. Screens are then positioned bottom-aligned to the primary.

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
dotnet run
```
The app is fully interactive — there are no command-line flags.

**Publish as self-contained executable:**
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Architecture

### Files

- `Program.cs` — application logic: main flow, display enumeration, overlay UI, user input parsing, positioning math.
- `NativeMethods.cs` — all Win32 interop: P/Invoke signatures (`EnumDisplayDevices`, `EnumDisplaySettings`, `ChangeDisplaySettingsEx`, `SetDisplayConfig`), the `DEVMODE` and `DISPLAY_DEVICE` structs, the `DisplayDeviceStateFlags` enum, and Win32 constants. Imported into `Program.cs` via `using static NativeMethods;`.

### Display Configuration Flow

1. `SetDisplayConfig(SDC_TOPOLOGY_EXTEND | SDC_APPLY)` — extend desktop to all connected displays via Win32 (replaces the previous `DisplaySwitch.exe /extend` subprocess).
2. `GetConnectedDisplaysWithRetry()` — enumerate active displays, retrying until the count is reached **and** all origins are distinct (guards against `EnumDisplaySettings` returning stale, stacked positions immediately after extend).
3. `ShowScreenNumberOverlays()` — on a background STA thread: opaque borderless `Form` per display showing its 1-based index, centered. `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` is set so WinForms coordinates are physical pixels (matching `dmPositionX/dmPelsWidth`). Main thread waits on a `ManualResetEventSlim` that is signaled from `Application.Idle` (after the message pump has drained initial paints).
4. `ReadDisplayOrder()` — prompt for the left-to-right ordering. Accepts space-separated (`2 1 3`), comma-separated (`2,1,3`), or compact-digit (`213`, single-digit per display) input.
5. `CloseOverlays()` — `BeginInvoke(Application.Exit)` on the anchor form, `Join(2000)` the STA thread.
6. `PositionDisplays()` — compute and apply final layout (see below).

### Positioning Algorithm

- **Primary selection**: center display in the user's ordering when count is odd; otherwise largest by pixel area.
- **X positions**: primary anchored at 0; widths accumulated leftward (negative x) and rightward (positive x) from primary.
- **Y positions**: bottom-aligned to primary — `y = primaryHeight - displayHeight`.
- **Apply order**: primary first (Windows requires primary at (0,0)), then all other displays. Final commit via `ChangeDisplaySettingsEx(null, IntPtr.Zero, ...)`.

### Coordinate System

Top-left of primary display is always (0,0). All `dmPositionX/dmPositionY` and `dmPelsWidth/dmPelsHeight` values are physical pixels.

## Implementation Notes

- **Deferred apply pattern**: `SetPrimaryDisplay` and `ApplyDisplaySettings` both pass `CDS_NORESET` to stage changes without applying them. The final `ChangeDisplaySettingsEx(null, IntPtr.Zero, ...)` call commits everything atomically.
- **`ChangeDisplaySettingsEx` overload**: a second P/Invoke signature with `IntPtr lpDevMode` (instead of `ref DEVMODE`) is needed for the null-device commit call — passing `null` through `ref DEVMODE` is not valid.
- **DPI**: `PerMonitorV2` is required so the WinForms overlay positions (computed from `dmPositionX/dmPelsWidth`) end up on the correct monitor on mixed-DPI setups.
- **Overlay timing**: the ready signal must come from `Application.Idle`, not immediately after `Form.Show()`, otherwise the console prompt can appear before the overlays paint.

## Platform Requirements

- Windows-only (uses Win32 API and WinForms)
- .NET 10.0 runtime
- Requires at least 2 active displays to function
