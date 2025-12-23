# Geochron Screensaver

A beautiful Windows screensaver featuring a 3D rotating Earth with real-time day/night cycle.

![Geochron Preview](docs/preview.png)

---

## Download & Install (Easy)

**No programming knowledge required!**

1. **[Download Geochron-Screensaver-v1.0.zip](https://github.com/LeromX/geochron/releases/latest/download/Geochron-Screensaver-v1.0.zip)**
2. **Extract** the zip file to a folder (e.g., `C:\Geochron`)
3. **Right-click** `GeochronScreensaver.scr` → **Install**
4. Done! Configure wait time in Windows Settings → Personalization → Lock screen → Screen saver

### Tips
- **Run as app**: Double-click the `.scr` file to preview without installing
- **Auto-saves your view**: Just rotate and zoom the globe - your position saves automatically after 2 seconds!
- **Zoom**: Use mouse wheel to zoom in/out
- **Rotate**: Click and drag to spin the globe
- The screensaver will always start with your last saved view

---

## Features

- **3D Globe mode** with realistic day/night blending using NASA Blue Marble textures
- **Mouse interaction** - drag to rotate, scroll wheel to zoom
- **Auto-save position** - your globe view saves automatically for the screensaver
- **Right-click** on any location to see local time
- **Real-time day/night terminator** with smooth twilight gradient
- **Accurate sun position** using astronomical algorithms
- **15 major world cities** showing day/night status
- **Multiple map projections** (Equirectangular, Mercator)
- **Multi-monitor support** - renders on all displays
- **Configuration dialog** with live preview
- **Time acceleration** - speed up time to watch the day/night cycle

## 3D Globe Controls

| Action | Control |
|--------|---------|
| Rotate globe | Left-click + drag |
| Zoom in/out | Mouse wheel |
| Show local time | Right-click on location |
| Save position | Automatic (2 sec after you stop) |
| Reset to default | View → Reset Globe Position |
| Fullscreen | F11 |
| Settings | Ctrl+, |

## Quick Start

### Prerequisites

**.NET 8 SDK** is required to build. [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)

Or install via winget:
```powershell
winget install Microsoft.DotNet.SDK.8
```

### Build & Test

**Option 1: Using build script (easiest)**
```batch
# Run in test window
build.bat run

# Build release .scr file
build.bat release

# Build portable (no .NET needed on target PC)
build.bat portable
```

**Option 2: Using dotnet CLI**
```powershell
# Run for testing (opens window)
dotnet run --project src\GeochronScreensaver

# Test in screensaver mode (fullscreen, ESC to exit)
dotnet run --project src\GeochronScreensaver -- /s

# Test configuration dialog
dotnet run --project src\GeochronScreensaver -- /c

# Build release version
dotnet build -c Release
```

**Option 3: Using PowerShell script**
```powershell
# Debug build
.\build.ps1 -Mode debug

# Release build
.\build.ps1 -Mode release

# Self-contained portable build
.\build.ps1 -Mode portable

# Build and install to System32
.\build.ps1 -Mode install
```

### Install as Windows Screensaver

After building:

1. **Quick install**: Right-click `GeochronScreensaver.scr` → **Install**

2. **Manual install**:
   - Copy `.scr` file to `C:\Windows\System32\`
   - Open **Settings → Personalization → Lock screen → Screen saver settings**
   - Select "GeochronScreensaver"

### Build Outputs

| Command | Output Location | Requires .NET Runtime? |
|---------|-----------------|----------------------|
| `build.bat debug` | `src\...\bin\Debug\net8.0-windows\` | Yes |
| `build.bat release` | `src\...\bin\Release\net8.0-windows\GeochronScreensaver.scr` | Yes |
| `build.bat portable` | `build\portable\GeochronScreensaver.scr` | **No** (self-contained) |

## Command-Line Arguments

| Argument | Mode | Description |
|----------|------|-------------|
| (none) | Test | Opens in a resizable window for testing |
| `/s` | Screensaver | Full-screen mode, exits on mouse/key |
| `/c` | Configure | Opens settings dialog |
| `/p <hwnd>` | Preview | Renders in Windows preview pane |

## Configuration

Settings are saved to `%APPDATA%\GeochronScreensaver\settings.json`

Available options:
- **Show cities** - Display city markers and labels
- **Show grid lines** - Display latitude/longitude grid
- **Night opacity** - Darkness of night regions (0.3-1.0)
- **Terminator width** - Twilight gradient width (2-15°)
- **Map projection** - Equirectangular or Mercator

## Project Structure

```
Geochron/
├── build.bat               # Windows batch build script
├── build.ps1               # PowerShell build script
├── GeochronScreensaver.sln
└── src\GeochronScreensaver\
    ├── Core\               # Solar calculations, settings
    ├── Rendering\          # Map, terminator, projections
    ├── UI\                 # Windows, controls, dialogs
    └── Infrastructure\     # Multi-monitor, Win32 interop
```

## How It Works

### Solar Position Algorithm
- Calculates **Julian Date** from current UTC time
- Computes **solar declination** (seasonal sun angle: -23.5° to +23.5°)
- Determines **sub-solar point** (where sun is directly overhead)

### Day/Night Terminator
- Uses **Haversine formula** for great-circle distances
- Points <90° from sub-solar point are in daylight
- Smooth gradient transition simulates twilight (~5° wide)

## Requirements

- **Build**: .NET 8.0 SDK, Windows 10/11
- **Run (release)**: .NET 8.0 Desktop Runtime
- **Run (portable)**: Windows 10/11 x64 only (no .NET needed)

## License

MIT License - Free to use and modify.
