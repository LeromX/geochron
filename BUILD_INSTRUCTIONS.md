# Build Instructions for Geochron Screensaver

## Prerequisites

1. Install .NET 8.0 SDK
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Choose "SDK" for Windows x64

2. Verify installation:
   ```bash
   dotnet --version
   ```
   Should output: 8.0.x or higher

## Quick Start

### Option 1: Using Visual Studio 2022

1. Open `GeochronScreensaver.sln`
2. Press F5 to build and run in debug mode
3. The screensaver will open in a window for testing

### Option 2: Using Command Line

```bash
# Navigate to project directory
cd "C:\1. Code\Geochron"

# Build debug version
dotnet build

# Run the application
dotnet run --project src\GeochronScreensaver\GeochronScreensaver.csproj

# Build release version (creates .scr file)
dotnet build -c Release
```

## Installation as Windows Screensaver

After building in Release mode:

1. Locate the .scr file:
   ```
   src\GeochronScreensaver\bin\Release\net8.0-windows\GeochronScreensaver.scr
   ```

2. Copy to Windows System directory:
   ```bash
   copy src\GeochronScreensaver\bin\Release\net8.0-windows\GeochronScreensaver.scr C:\Windows\System32\
   ```

3. Configure:
   - Right-click desktop → Personalize
   - Click "Lock screen" → "Screen saver settings"
   - Select "Geochron Screensaver" from dropdown
   - Click "Preview" to test

## Testing the Screensaver

You can test different modes without installing:

```bash
# Normal window mode (for debugging)
GeochronScreensaver.exe

# Full screen screensaver mode
GeochronScreensaver.exe /s

# Preview mode (requires window handle)
GeochronScreensaver.exe /p <handle>

# Configuration mode
GeochronScreensaver.exe /c
```

## Troubleshooting

### "dotnet command not found"
- Ensure .NET 8.0 SDK is installed
- Restart your terminal/IDE after installation
- Check PATH environment variable includes dotnet

### Build Errors
Common issues and solutions:

1. **Missing WPF support**
   - Ensure you installed the SDK, not just the runtime
   - WPF is only available on Windows

2. **Unsafe code errors**
   - The project has `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` set
   - This is required for the terminator rendering

3. **XAML compilation errors**
   - Ensure all .xaml and .xaml.cs files are in the same directory
   - Clean and rebuild: `dotnet clean && dotnet build`

### Performance Issues
The MVP uses CPU-based rendering which may be slower on some systems:
- Current implementation samples every 2nd pixel
- Future GPU shader implementation will be much faster
- For now, adjust `stepSize` in `TerminatorLayer.cs` line 40

## Project Files Created

Total: 20 files, ~1176 lines of code

### Core Business Logic (7 files)
- `Program.cs` - Entry point with screensaver mode parsing
- `Core/GeoPoint.cs` - Geographic coordinate model
- `Core/SunPosition.cs` - Sun position data model
- `Core/SolarCalculator.cs` - Astronomical calculations
- `Core/Settings.cs` - Application settings
- `Core/City.cs` - City locations and data

### Rendering Engine (6 files)
- `Rendering/Projections/IMapProjection.cs` - Projection interface
- `Rendering/Projections/EquirectangularProjection.cs` - Map projection
- `Rendering/MapLayer.cs` - World map rendering
- `Rendering/TerminatorLayer.cs` - Day/night overlay (CPU-based)
- `Rendering/GeochronRenderer.cs` - Main compositor

### User Interface (5 files)
- `UI/GeochronControl.xaml` - Main display control
- `UI/GeochronControl.xaml.cs` - Control logic with animation loop
- `UI/ScreensaverWindow.xaml` - Window definition
- `UI/ScreensaverWindow.xaml.cs` - Window logic with exit handling

### Infrastructure (2 files)
- `Infrastructure/NativeMethods.cs` - Win32 API interop
- `App.xaml` / `App.xaml.cs` - WPF application setup

## What Works in Phase 1 MVP

- Real-time sun position calculation using astronomical algorithms
- Dynamic day/night terminator with smooth gradient
- Simplified world map with continents and oceans
- Grid lines (latitude/longitude)
- 15 major world cities with day/night indicators
- Sub-solar point marker (yellow circle)
- UTC time and solar information display
- Screensaver functionality (exit on mouse/keyboard, preview mode)
- Update every second for smooth animation

## Next Development Phase

Phase 2 will add GPU shader-based rendering for better performance.
See main README.md for complete roadmap.
