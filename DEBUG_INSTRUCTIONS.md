# Globe Rendering Debug Instructions

## What Was Added

Comprehensive debug logging has been added to trace the entire globe rendering pipeline:

### 1. Debug Log File
- **Location**: `%TEMP%\geochron_debug.log`
- Typical path: `C:\Users\[YourUsername]\AppData\Local\Temp\geochron_debug.log`

### 2. Modified Files with Debug Logging

#### ScreensaverWindow.xaml.cs
- Logs when constructor starts
- Logs RenderMode setting value
- Logs GlobeControl creation
- Logs DisplayContainer content assignment
- Logs window size and layout updates
- Logs any exceptions

#### GlobeControl.xaml.cs
- Logs constructor lifecycle
- Logs InitializeComponent completion
- Logs renderer creation
- Logs viewport addition to container
- Logs Loaded event with actual sizes
- Logs Viewport3D details (size, children count, camera)
- Logs size change events
- Logs any exceptions

#### GlobeRenderer.cs
- Logs constructor steps
- Logs viewport creation
- Logs camera configuration
- Logs light setup
- Logs sphere mesh creation with vertex/triangle counts
- Logs earth geometry creation
- Logs final children count
- Logs any exceptions

### 3. Visual Debug Test

#### GlobeControl.xaml
Added a red "GlobeControl Loaded" text at the top of the control to confirm:
- The control is actually being created
- The control is visible on screen
- The control has non-zero size

## Testing Instructions

### Option 1: Run the Test Script (Easiest)

```powershell
cd "C:\1. Code\Geochron"
.\test_globe.ps1
```

This will:
1. Clear any existing debug log
2. Launch the screensaver
3. Wait 2 seconds
4. Display the debug log contents
5. Wait for you to press a key
6. Show updated log

**Close the screensaver window** to exit (press any key or move mouse).

### Option 2: Manual Testing

1. **Clear the debug log**:
   ```powershell
   Remove-Item "$env:TEMP\geochron_debug.log" -ErrorAction SilentlyContinue
   ```

2. **Run the screensaver**:
   ```powershell
   & "C:\1. Code\Geochron\src\GeochronScreensaver\bin\Debug\net8.0-windows\GeochronScreensaver.exe" /s
   ```

3. **View the debug log**:
   ```powershell
   Get-Content "$env:TEMP\geochron_debug.log"
   ```

## What to Look For

### If the globe is working:
- You should see a red "GlobeControl Loaded" text
- You should see a blue/steel-colored sphere
- Log should show normal initialization with no exceptions

### If nothing appears:
Check the debug log for:

1. **Size issues**:
   - `ActualWidth=0, ActualHeight=0` means layout problem
   - `ViewportContainer size=0x0` means container not sizing
   - `Viewport3D size=0x0` means viewport not taking space

2. **Creation issues**:
   - `EXCEPTION in constructor` means object creation failed
   - Look for stack traces after EXCEPTION entries

3. **Content issues**:
   - `Positions: 0, Triangles: 0` means mesh wasn't created
   - `Viewport children count: 0` means nothing was added to viewport

4. **Visibility issues**:
   - `DisplayContainer.Content=NULL` means control wasn't assigned
   - `HasContent=false` means assignment failed

### Expected Log Output (Success)

```
HH:mm:ss.fff - ScreensaverWindow: Constructor started
HH:mm:ss.fff - ScreensaverWindow: InitializeComponent completed
HH:mm:ss.fff - ScreensaverWindow: Settings loaded, RenderMode=Globe3D
HH:mm:ss.fff - ScreensaverWindow: Creating GlobeControl
HH:mm:ss.fff - GlobeControl constructor started
HH:mm:ss.fff - GlobeControl: InitializeComponent completed
HH:mm:ss.fff - GlobeControl: Settings loaded: RenderMode=Globe3D
HH:mm:ss.fff - GlobeControl: InitializeRenderer: Creating GlobeRenderer
HH:mm:ss.fff - GlobeRenderer: Constructor started
HH:mm:ss.fff - GlobeRenderer: Map source created
HH:mm:ss.fff - GlobeRenderer: Viewport3D created
HH:mm:ss.fff - GlobeRenderer: Camera configured
HH:mm:ss.fff - GlobeRenderer: Ambient light added
HH:mm:ss.fff - GlobeRenderer: Directional light added
HH:mm:ss.fff - GlobeRenderer: Sphere mesh created - Positions: 7260, Triangles: 43200
HH:mm:ss.fff - GlobeRenderer: Earth geometry created
HH:mm:ss.fff - GlobeRenderer: Constructor completed. Viewport children count: 3
HH:mm:ss.fff - GlobeControl: InitializeRenderer: GlobeRenderer created
HH:mm:ss.fff - GlobeControl: InitializeRenderer: Got viewport, children count: 3
HH:mm:ss.fff - GlobeControl: InitializeRenderer: Viewport added to container. Container children: 1
HH:mm:ss.fff - GlobeControl: InitializeRenderer: Initial render completed
HH:mm:ss.fff - GlobeControl: InitializeRenderer completed
HH:mm:ss.fff - GlobeControl: Timer initialized
HH:mm:ss.fff - GlobeControl: Event handlers registered
HH:mm:ss.fff - ScreensaverWindow: GlobeControl created, setting as DisplayContainer.Content
HH:mm:ss.fff - ScreensaverWindow: DisplayContainer.Content set. HasContent=True
HH:mm:ss.fff - ScreensaverWindow: Setting up screensaver mode
HH:mm:ss.fff - ScreensaverWindow: Constructor completed
HH:mm:ss.fff - ScreensaverWindow size changed: 1920x1080
HH:mm:ss.fff - GlobeControl size changed: 1920x1080
HH:mm:ss.fff - GlobeControl: ViewportContainer size: 1920x1080
HH:mm:ss.fff - ScreensaverWindow_Loaded: Window size=1920x1080
HH:mm:ss.fff - ScreensaverWindow_Loaded: DisplayContainer size=1920x1080
HH:mm:ss.fff - ScreensaverWindow_Loaded: DisplayContainer.Content=GlobeControl
HH:mm:ss.fff - GlobeControl_Loaded: ActualWidth=1920, ActualHeight=1080
HH:mm:ss.fff - GlobeControl_Loaded: ViewportContainer size=1920x1080
HH:mm:ss.fff - GlobeControl_Loaded: ViewportContainer children count=1
HH:mm:ss.fff - GlobeControl_Loaded: Viewport3D size=1920x1080
HH:mm:ss.fff - GlobeControl_Loaded: Viewport3D children count=3
HH:mm:ss.fff - GlobeControl_Loaded: Viewport3D camera=SET
HH:mm:ss.fff - GlobeControl_Loaded: Timer started
```

## Next Steps Based on Results

### If you see exceptions:
- Share the exception details
- This indicates a code error that needs fixing

### If sizes are all 0:
- There's a WPF layout issue
- May need to check XAML structure or layout properties

### If everything logs correctly but nothing visible:
- Could be a rendering issue (z-order, transparency, etc.)
- Could be camera positioning
- Could be material/lighting issue

### If the red text shows but no globe:
- The control is rendering
- Problem is specifically with the Viewport3D
- May need to try XAML-based Viewport3D instead of code-based

## Files Modified

1. `src\GeochronScreensaver\UI\ScreensaverWindow.xaml.cs` - Added debug logging
2. `src\GeochronScreensaver\UI\GlobeControl.xaml.cs` - Added debug logging and size tracking
3. `src\GeochronScreensaver\UI\GlobeControl.xaml` - Added red test text
4. `src\GeochronScreensaver\Rendering\Globe\GlobeRenderer.cs` - Added debug logging
5. `test_globe.ps1` - Created test script

All changes are minimal and focused on diagnostics. No functional code was modified.
