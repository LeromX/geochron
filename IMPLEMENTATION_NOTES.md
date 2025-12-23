# Implementation Notes - Geochron Screensaver Phase 1

## Astronomical Calculations

### Sun Position Algorithm
Located in: `src/GeochronScreensaver/Core/SolarCalculator.cs`

The implementation uses a simplified astronomical algorithm suitable for screensaver accuracy:

```csharp
// Julian Date calculation from DateTime
jd = floor(365.25 * (year + 4716)) + floor(30.6001 * (month + 1)) + day + b - 1524.5

// Days since J2000.0 epoch (January 1, 2000, 12:00 UTC)
n = jd - 2451545.0

// Mean longitude of the sun (degrees)
L = (280.460 + 0.9856474 * n) % 360

// Mean anomaly (degrees)
g = (357.528 + 0.9856003 * n) % 360

// Ecliptic longitude (accounts for Earth's elliptical orbit)
lambda = L + 1.915 * sin(g) + 0.020 * sin(2*g)

// Obliquity of the ecliptic (Earth's axial tilt)
epsilon = 23.439 - 0.0000004 * n

// Solar declination (sun's latitude)
declination = asin(sin(epsilon) * sin(lambda))

// Sub-solar longitude (sun's longitude based on UTC time)
longitude = (12 - hours_utc) * 15
```

**Accuracy**: ~0.01° (sufficient for screensaver visualization)

### Day/Night Terminator

The terminator is calculated using great circle distance:

```csharp
// Haversine formula for angular distance
distance = arccos(sin(lat1) * sin(lat2) + cos(lat1) * cos(lat2) * cos(lon2 - lon1))

// Point is in daylight if within 90° of sub-solar point
isDay = distance < 90°

// Smooth transition zone (twilight)
if (distance between 85° and 95°):
    brightness = smoothstep(85°, 95°, distance)
```

## Rendering Architecture

### Layer Composition

The renderer uses a layered approach:

1. **MapLayer** - Base world map
   - Ocean background (dark blue)
   - Continental shapes (green)
   - Grid lines (subtle white)
   - Drawn using WPF vector graphics

2. **TerminatorLayer** - Day/night overlay
   - CPU-based pixel calculation
   - WriteableBitmap for direct pixel access
   - Semi-transparent black overlay (alpha based on solar distance)
   - Sampled rendering (every 2nd pixel) for performance

3. **Overlay Elements** - Added by GeochronRenderer
   - City markers and labels
   - Sub-solar point indicator
   - Information text (UTC time, coordinates)

### Map Projection

Uses Equirectangular (Plate Carrée) projection:

```csharp
// Geographic to screen
x = (longitude + 180) / 360 * width
y = (90 - latitude) / 180 * height

// Screen to geographic
longitude = (x / width) * 360 - 180
latitude = 90 - (y / height) * 180
```

**Pros**: Simple, fast, preserves straight lines for lat/lon grid
**Cons**: Distorts area near poles (acceptable for screensaver)

## Performance Optimizations

### CPU-Based Terminator Rendering

Current approach (Phase 1):
- Samples every 2nd pixel (`stepSize = 2`)
- Fills pixel blocks to avoid gaps
- Processing time: ~50-100ms for 1920x1080 on modern CPU

```csharp
for (int y = 0; y < height; y += stepSize) {
    for (int x = 0; x < width; x += stepSize) {
        // Calculate once per block
        var geoPoint = ProjectToGeo(x, y);
        var brightness = CalculateBrightness(geoPoint, sunPosition);

        // Fill the block
        for (int dy = 0; dy < stepSize; dy++) {
            for (int dx = 0; dx < stepSize; dx++) {
                SetPixel(x + dx, y + dy, brightness);
            }
        }
    }
}
```

**Tuning**: Adjust `stepSize` in TerminatorLayer.cs:
- `stepSize = 1`: Highest quality, slowest (~200ms)
- `stepSize = 2`: Good quality, good performance (~50ms) ← **Current**
- `stepSize = 4`: Lower quality, faster (~15ms)

### Animation Loop

Update strategy:
- Timer interval: 1 second
- Full re-render each frame
- Sun position changes slowly (0.25° per minute at equator)

Future optimization: Only re-render when sun moves significant distance

## Data Structures

### GeoPoint (readonly struct)
```csharp
public readonly struct GeoPoint {
    public double Latitude { get; init; }   // -90 to +90
    public double Longitude { get; init; }  // -180 to +180
}
```
**Design choice**: Readonly struct for value semantics and performance

### SunPosition (class)
```csharp
public class SunPosition {
    public GeoPoint SubSolarPoint { get; set; }
    public double Declination { get; set; }  // -23.5 to +23.5
    public DateTime Timestamp { get; set; }
}
```
**Design choice**: Class with mutable properties for flexibility

## Coordinate Systems

### Geographic Coordinates
- **Latitude**: -90° (South Pole) to +90° (North Pole)
- **Longitude**: -180° (West) to +180° (East)
- Prime Meridian: 0° longitude (Greenwich)
- Equator: 0° latitude

### Screen Coordinates
- **Origin**: Top-left corner (0, 0)
- **X-axis**: Left to right (0 to width)
- **Y-axis**: Top to bottom (0 to height)

### Time Coordinate
- **UTC**: Universal Time Coordinated
- Sub-solar longitude at solar noon (12:00 UTC) is approximately 0°
- Sun moves westward 15° per hour (360° / 24 hours)

## Simplified Continent Shapes

Phase 1 uses hardcoded polygon approximations:

```csharp
// Africa (11 vertices)
{ (37, -6), (32, 10), (15, 40), ... }

// Eurasia (14 vertices)
{ (70, -10), (70, 30), (75, 80), ... }

// North America (13 vertices)
// South America (11 vertices)
// Australia (8 vertices)
// Antarctica (5 vertices)
```

**Accuracy**: Rough approximation, recognizable continents
**Future**: Replace with actual map images in Phase 4

## City Data

15 major cities included:
- Location coordinates (lat/lon)
- Time zone identifier
- Population (for potential future features)

Cities are rendered with:
- Gold marker if in daylight
- Light blue marker if in darkness
- Name label positioned right of marker

## Screensaver Integration

### Command-Line Modes

Windows screensaver protocol:
- `/s` - Screensaver mode (full screen, exit on input)
- `/c` - Configure (shows settings dialog)
- `/p <hwnd>` - Preview (embedded in settings window)

### Exit Conditions

Screensaver exits when:
- Mouse moves >10 pixels from initial position
- Any mouse button clicked
- Any key pressed

Implementation uses WPF event handlers with threshold detection.

### Preview Mode

Uses Win32 API to embed WPF window:
```csharp
SetParent(hwnd, previewHandle);
SetWindowLong(hwnd, GWL_STYLE, style | WS_CHILD);
GetClientRect(previewHandle, out rect);
```

## Known Limitations (Phase 1)

1. **Performance**: CPU rendering may be slow on high-resolution displays
   - Solution: GPU shader in Phase 2

2. **Map Detail**: Simplified continent shapes lack detail
   - Solution: Real map images in Phase 4

3. **No Configuration**: Settings are hardcoded
   - Solution: Configuration UI in Phase 5

4. **No Time Zones**: Cities don't show local time
   - Solution: Analog clock in Phase 3

5. **Static Cities**: Fixed list of 15 cities
   - Solution: User-configurable cities in Phase 5

## Testing Recommendations

### Visual Verification
1. Check terminator position matches current UTC time
2. Verify cities in correct day/night zones
3. Confirm sub-solar point marker location
4. Check smooth gradient at terminator

### Calculation Verification
Compare with online solar position calculators:
- https://www.esrl.noaa.gov/gmd/grad/solcalc/
- https://www.timeanddate.com/sun/

Expected accuracy: ±0.5° (sufficient for visualization)

### Performance Testing
Monitor frame rendering time:
- Add stopwatch to UpdateRender() method
- Target: <100ms per frame
- Adjust stepSize if needed

## Future Enhancements (Planned Phases)

### Phase 2: GPU Shader Rendering
Replace TerminatorLayer.cs with HLSL compute shader:
- 10-100x performance improvement
- Smooth 60fps animation possible
- Sub-pixel accurate gradients

### Phase 3: Analog Clock
Add 3D rendered clock showing:
- Multiple time zones
- Rotating hour/minute hands
- Day/month indicators

### Phase 4: Real Map Data
Replace polygon approximations with:
- Blue Marble imagery from NASA
- Natural Earth vector data
- High-resolution continent textures

### Phase 5: Configuration UI
Add settings dialog for:
- Custom city list
- Color scheme options
- Update frequency
- Map projection choice
- Enable/disable features

## Code Quality Notes

### Current Status
- All nullable reference types enabled
- Unsafe code allowed (for bitmap manipulation)
- No external dependencies beyond .NET 8 WPF
- ~1176 lines of code across 20 files

### Architecture Patterns
- **Separation of Concerns**: Core logic separated from rendering and UI
- **Interface-Based Design**: IMapProjection allows future projections
- **Composition**: GeochronRenderer composites layers
- **Immutability**: GeoPoint is readonly struct

### Testing Strategy
Phase 1 focuses on visual testing. Future phases should add:
- Unit tests for SolarCalculator
- Integration tests for rendering layers
- Performance benchmarks
