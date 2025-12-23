using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;

namespace GeochronScreensaver.Rendering.Sources;

/// <summary>
/// Provides satellite map textures from NASA imagery (Blue Marble day, Black Marble night).
/// Falls back to procedural generation if images are not available.
/// </summary>
public class SatelliteMapSource : IMapSource
{
    private BitmapSource? _dayTexture;
    private BitmapSource? _nightTexture;
    private bool _isInitialized;
    private bool _useProceduralFallback;

    // Texture dimensions for procedural fallback
    private const int TextureWidth = 2048;
    private const int TextureHeight = 1024;

    public bool SupportsNightBlending => true;

    /// <summary>
    /// Gets the day texture (illuminated Earth).
    /// </summary>
    public BitmapSource? GetDayTexture()
    {
        EnsureInitialized();
        return _dayTexture;
    }

    /// <summary>
    /// Gets the night texture (Earth at night with city lights).
    /// </summary>
    public BitmapSource? GetNightTexture()
    {
        EnsureInitialized();
        return _nightTexture;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        // Try to load real NASA images
        _dayTexture = LoadImageFromResources("earth_day.jpg");
        _nightTexture = LoadImageFromResources("earth_night.jpg");

        // Fall back to procedural if images not found
        if (_dayTexture == null)
        {
            _useProceduralFallback = true;
            _dayTexture = GenerateDayTexture();
        }

        if (_nightTexture == null)
        {
            _nightTexture = GenerateNightTexture();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Attempts to load an image from the Resources/Maps folder.
    /// </summary>
    private BitmapSource? LoadImageFromResources(string fileName)
    {
        try
        {
            // Try to find the image relative to the executable
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);

            if (assemblyDir == null) return null;

            // Check in Resources/Maps subfolder
            var imagePath = Path.Combine(assemblyDir, "Resources", "Maps", fileName);

            if (!File.Exists(imagePath))
            {
                // Try current directory
                imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Maps", fileName);
            }

            if (!File.Exists(imagePath))
            {
                // Try relative to assembly in development
                var devPath = Path.Combine(assemblyDir, "..", "..", "..", "Resources", "Maps", fileName);
                if (File.Exists(devPath))
                {
                    imagePath = devPath;
                }
            }

            if (File.Exists(imagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
        catch (Exception)
        {
            // Silently fall back to procedural
        }

        return null;
    }

    /// <summary>
    /// Generate a procedural day texture with Earth-like colors.
    /// Used as fallback when real images are not available.
    /// </summary>
    private unsafe WriteableBitmap GenerateDayTexture()
    {
        var bitmap = new WriteableBitmap(TextureWidth, TextureHeight, 96, 96, PixelFormats.Bgr32, null);
        bitmap.Lock();

        try
        {
            byte* pixels = (byte*)bitmap.BackBuffer.ToPointer();
            int stride = bitmap.BackBufferStride;

            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    double lon = (x / (double)TextureWidth) * 360.0 - 180.0;
                    double lat = 90.0 - (y / (double)TextureHeight) * 180.0;

                    bool isLand = IsLandApproximation(lat, lon);

                    WpfColor color;
                    if (isLand)
                    {
                        double latFactor = Math.Abs(lat) / 90.0;
                        byte green = (byte)(100 + 60 * (1.0 - latFactor));
                        byte red = (byte)(60 + 80 * latFactor);
                        byte blue = (byte)(40 + 30 * latFactor);
                        color = WpfColor.FromRgb(red, green, blue);
                    }
                    else
                    {
                        double noise = GenerateNoise(x, y, 0.05) * 0.3;
                        byte blueVal = (byte)Math.Clamp(100 + 100 * noise, 60, 180);
                        byte greenVal = (byte)(blueVal * 0.6);
                        byte redVal = (byte)(blueVal * 0.3);
                        color = WpfColor.FromRgb(redVal, greenVal, blueVal);
                    }

                    int offset = y * stride + x * 4;
                    pixels[offset + 0] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, TextureWidth, TextureHeight));
        }
        finally
        {
            bitmap.Unlock();
        }

        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Generate a procedural night texture with city lights.
    /// Used as fallback when real images are not available.
    /// </summary>
    private unsafe WriteableBitmap GenerateNightTexture()
    {
        var bitmap = new WriteableBitmap(TextureWidth, TextureHeight, 96, 96, PixelFormats.Bgr32, null);
        bitmap.Lock();

        try
        {
            byte* pixels = (byte*)bitmap.BackBuffer.ToPointer();
            int stride = bitmap.BackBufferStride;

            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    double lon = (x / (double)TextureWidth) * 360.0 - 180.0;
                    double lat = 90.0 - (y / (double)TextureHeight) * 180.0;

                    bool isLand = IsLandApproximation(lat, lon);
                    WpfColor color;

                    if (isLand)
                    {
                        double cityLight = GenerateCityLights(lat, lon, x, y);
                        byte intensity = (byte)Math.Clamp(10 + cityLight * 200, 10, 255);
                        color = WpfColor.FromRgb(intensity, (byte)(intensity * 0.9), (byte)(intensity * 0.5));
                    }
                    else
                    {
                        color = WpfColor.FromRgb(5, 8, 15);
                    }

                    int offset = y * stride + x * 4;
                    pixels[offset + 0] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, TextureWidth, TextureHeight));
        }
        finally
        {
            bitmap.Unlock();
        }

        bitmap.Freeze();
        return bitmap;
    }

    private bool IsLandApproximation(double lat, double lon)
    {
        // Africa
        if (lat > -35 && lat < 37 && lon > -20 && lon < 52) return true;
        // Eurasia
        if (lat > 10 && lat < 75 && lon > -10 && lon < 180) return true;
        // North America
        if (lat > 15 && lat < 70 && lon > -170 && lon < -50) return true;
        // South America
        if (lat > -55 && lat < 15 && lon > -85 && lon < -35) return true;
        // Australia
        if (lat > -45 && lat < -10 && lon > 110 && lon < 155) return true;
        // Antarctica
        if (lat < -60) return true;

        return false;
    }

    private double GenerateNoise(int x, int y, double frequency)
    {
        double nx = x * frequency;
        double ny = y * frequency;
        double value = Math.Sin(nx * 12.9898 + ny * 78.233) * 43758.5453;
        return value - Math.Floor(value);
    }

    private double GenerateCityLights(double lat, double lon, int x, int y)
    {
        double light = 0.0;

        // Major city clusters
        light = Math.Max(light, CityLightSpot(lat, lon, 40, -75, 15));  // Eastern US
        light = Math.Max(light, CityLightSpot(lat, lon, 50, 5, 15));    // Western Europe
        light = Math.Max(light, CityLightSpot(lat, lon, 35, 135, 20));  // Japan
        light = Math.Max(light, CityLightSpot(lat, lon, 31, 121, 15));  // Shanghai
        light = Math.Max(light, CityLightSpot(lat, lon, 20, 77, 15));   // India

        double noise = GenerateNoise(x, y, 0.1);
        if (noise > 0.95)
        {
            light = Math.Max(light, (noise - 0.95) * 2);
        }

        return light;
    }

    private double CityLightSpot(double lat, double lon, double centerLat, double centerLon, double radius)
    {
        double distance = Math.Sqrt(
            Math.Pow(lat - centerLat, 2) +
            Math.Pow((lon - centerLon) * Math.Cos(centerLat * Math.PI / 180.0), 2)
        );

        if (distance > radius) return 0.0;

        double factor = 1.0 - (distance / radius);
        return factor * factor;
    }
}
