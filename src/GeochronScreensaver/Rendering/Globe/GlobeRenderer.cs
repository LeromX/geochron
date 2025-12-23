using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering.Sources;

namespace GeochronScreensaver.Rendering.Globe;

/// <summary>
/// Renders a 3D rotating Earth globe with day/night texture blending.
/// Uses pure WPF Viewport3D for 3D visualization.
/// Supports manual rotation via mouse drag.
/// </summary>
public class GlobeRenderer
{
    private readonly Viewport3D _viewport;
    private readonly ModelVisual3D _earthModel;
    private readonly GeometryModel3D _earthGeometry;
    private readonly SatelliteMapSource _mapSource;
    private readonly double _transitionWidth;
    private DateTime? _lastUpdateTime;
    private Settings _settings;

    // Manual rotation state
    private double _manualLatitude;
    private double _manualLongitude;

    // Zoom state
    private double _currentZoom = 1.0;
    private const double MinZoom = 0.5;  // Closest zoom (camera at Z = 1.75)
    private const double MaxZoom = 2.0;  // Farthest zoom (camera at Z = 7.0)
    private const double DefaultCameraZ = 3.5;

    public GlobeRenderer(Settings settings)
    {
        _mapSource = new SatelliteMapSource();
        _transitionWidth = settings.TerminatorTransitionWidth;
        _settings = settings;

        // Initialize rotation to current sun position
        var sunPos = SolarCalculator.GetSunPosition(DateTime.UtcNow);
        _manualLatitude = sunPos.SubSolarPoint.Latitude;
        _manualLongitude = sunPos.SubSolarPoint.Longitude;

        // Create the viewport
        _viewport = new Viewport3D
        {
            ClipToBounds = true,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch
        };

        // Configure camera - looking at the globe from a distance
        var camera = new PerspectiveCamera
        {
            Position = new Point3D(0, 0, DefaultCameraZ),
            LookDirection = new Vector3D(0, 0, -1),
            UpDirection = new Vector3D(0, 1, 0),
            FieldOfView = 45
        };
        _viewport.Camera = camera;

        // Add dimmer ambient light
        var ambientLight = new AmbientLight(System.Windows.Media.Color.FromRgb(80, 80, 80));
        var lightModel = new ModelVisual3D { Content = ambientLight };
        _viewport.Children.Add(lightModel);

        // Add directional light pointing at the sphere
        var directionalLight = new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));
        var directionalLightModel = new ModelVisual3D { Content = directionalLight };
        _viewport.Children.Add(directionalLightModel);

        // Create the Earth sphere
        var sphereMesh = EarthSphereGeometry.CreateSphereMesh(radius: 1.0, latDivisions: 60, lonDivisions: 120);

        // Create emissive material with fallback color (blue for Earth)
        var material = new EmissiveMaterial(new SolidColorBrush(Colors.SteelBlue));

        _earthGeometry = new GeometryModel3D
        {
            Geometry = sphereMesh,
            Material = material,
            BackMaterial = material
        };

        _earthModel = new ModelVisual3D { Content = _earthGeometry };
        _viewport.Children.Add(_earthModel);
    }

    /// <summary>
    /// Gets the Viewport3D for hosting in a control.
    /// </summary>
    public Viewport3D GetViewport() => _viewport;

    /// <summary>
    /// Updates the settings used by the renderer.
    /// </summary>
    public void UpdateSettings(Settings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Sets the manual rotation for the globe (from mouse drag).
    /// </summary>
    public void SetManualRotation(double latitude, double longitude)
    {
        _manualLatitude = latitude;
        _manualLongitude = longitude;
    }

    /// <summary>
    /// Sets the camera zoom level. 1.0 = default, less = closer, more = farther.
    /// </summary>
    public void SetCameraZoom(double zoomLevel)
    {
        _currentZoom = Math.Clamp(zoomLevel, MinZoom, MaxZoom);
        UpdateCameraPosition();
    }

    /// <summary>
    /// Gets the current camera zoom level.
    /// </summary>
    public double GetCameraZoom() => _currentZoom;

    /// <summary>
    /// Gets the current rotation latitude.
    /// </summary>
    public double GetRotationLatitude() => _manualLatitude;

    /// <summary>
    /// Gets the current rotation longitude.
    /// </summary>
    public double GetRotationLongitude() => _manualLongitude;

    private void UpdateCameraPosition()
    {
        if (_viewport.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(0, 0, DefaultCameraZ * _currentZoom);
        }
    }

    /// <summary>
    /// Renders the globe for the given UTC time.
    /// Updates the day/night texture based on sun position.
    /// Globe rotation is controlled by SetManualRotation.
    /// </summary>
    public void Render(DateTime utcTime)
    {
        // Update texture if time has changed significantly or not yet initialized
        if (!_lastUpdateTime.HasValue ||
            Math.Abs((utcTime - _lastUpdateTime.Value).TotalMinutes) > 1)
        {
            UpdateTexture(utcTime);
            _lastUpdateTime = utcTime;
        }

        // Apply manual rotation
        RotateGlobe(_manualLongitude, _manualLatitude);
    }

    /// <summary>
    /// Updates the Earth texture with day/night blending based on current time.
    /// </summary>
    private void UpdateTexture(DateTime utcTime)
    {
        var dayTexture = _mapSource.GetDayTexture();
        var nightTexture = _mapSource.GetNightTexture();

        if (dayTexture == null || nightTexture == null)
            return;

        // Create blended texture
        var sunPosition = SolarCalculator.GetSunPosition(utcTime);
        var blendedTexture = CreateBlendedTexture(dayTexture, nightTexture, sunPosition);

        // Apply texture to material
        var textureMaterial = new DiffuseMaterial(new ImageBrush(blendedTexture));
        _earthGeometry.Material = textureMaterial;
        _earthGeometry.BackMaterial = textureMaterial;
    }

    /// <summary>
    /// Creates a blended texture combining day and night textures based on sun position.
    /// </summary>
    private unsafe WriteableBitmap CreateBlendedTexture(
        BitmapSource dayTexture,
        BitmapSource nightTexture,
        SunPosition sunPosition)
    {
        int width = dayTexture.PixelWidth;
        int height = dayTexture.PixelHeight;

        // Resize night texture if dimensions don't match
        if (nightTexture.PixelWidth != width || nightTexture.PixelHeight != height)
        {
            nightTexture = ResizeBitmap(nightTexture, width, height);
        }

        // Create output bitmap
        var blended = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

        // Get pixel data from source textures
        byte[] dayPixels = GetPixelData(dayTexture);
        byte[] nightPixels = GetPixelData(nightTexture);

        blended.Lock();
        try
        {
            byte* outputPixels = (byte*)blended.BackBuffer.ToPointer();
            int stride = blended.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Convert pixel position to geographic coordinates
                    double longitude = (x / (double)width) * 360.0 - 180.0;
                    double latitude = 90.0 - (y / (double)height) * 180.0;

                    var geoPoint = new GeoPoint(latitude, longitude);

                    // Calculate brightness factor (0 = night, 1 = day)
                    double brightness = SolarCalculator.GetBrightnessFactor(
                        geoPoint, sunPosition, _transitionWidth);

                    // Get source colors
                    int srcOffset = y * width * 4 + x * 4;
                    byte dayB = dayPixels[srcOffset + 0];
                    byte dayG = dayPixels[srcOffset + 1];
                    byte dayR = dayPixels[srcOffset + 2];

                    byte nightB = nightPixels[srcOffset + 0];
                    byte nightG = nightPixels[srcOffset + 1];
                    byte nightR = nightPixels[srcOffset + 2];

                    // Blend colors based on brightness
                    byte finalB = (byte)(dayB * brightness + nightB * (1.0 - brightness));
                    byte finalG = (byte)(dayG * brightness + nightG * (1.0 - brightness));
                    byte finalR = (byte)(dayR * brightness + nightR * (1.0 - brightness));

                    // Write pixel
                    int offset = y * stride + x * 4;
                    outputPixels[offset + 0] = finalB;
                    outputPixels[offset + 1] = finalG;
                    outputPixels[offset + 2] = finalR;
                }
            }

            blended.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            blended.Unlock();
        }

        blended.Freeze();
        return blended;
    }

    /// <summary>
    /// Extracts pixel data from a BitmapSource.
    /// </summary>
    private byte[] GetPixelData(BitmapSource bitmap)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int stride = width * 4;

        byte[] pixels = new byte[height * stride];
        var formatConvertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgr32, null, 0);
        formatConvertedBitmap.CopyPixels(pixels, stride, 0);

        return pixels;
    }

    /// <summary>
    /// Resizes a bitmap to the specified dimensions.
    /// </summary>
    private BitmapSource ResizeBitmap(BitmapSource source, int targetWidth, int targetHeight)
    {
        var scaleX = (double)targetWidth / source.PixelWidth;
        var scaleY = (double)targetHeight / source.PixelHeight;

        var transformedBitmap = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));

        var resized = new WriteableBitmap(transformedBitmap);
        resized.Freeze();
        return resized;
    }

    /// <summary>
    /// Rotates the globe so that the specified location faces the camera.
    /// </summary>
    private void RotateGlobe(double targetLongitude, double targetLatitude)
    {
        var transformGroup = new Transform3DGroup();

        // Rotate around Y-axis for longitude
        var longitudeRotation = new RotateTransform3D
        {
            Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -targetLongitude)
        };
        transformGroup.Children.Add(longitudeRotation);

        // Tilt around X-axis for latitude
        var latitudeRotation = new RotateTransform3D
        {
            Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -targetLatitude)
        };
        transformGroup.Children.Add(latitudeRotation);

        _earthModel.Transform = transformGroup;
    }
}
