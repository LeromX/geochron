using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering.Projections;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;

namespace GeochronScreensaver.Rendering;

/// <summary>
/// Renders a satellite-style map with blended day and night textures.
/// Uses per-pixel blending based on solar position to create realistic day/night transitions.
/// </summary>
public class SatelliteMapLayer
{
    private readonly IMapSource _mapSource;
    private readonly IMapProjection _projection;
    private readonly double _transitionWidth;
    private WriteableBitmap? _cachedBitmap;
    private int _cachedWidth;
    private int _cachedHeight;

    public SatelliteMapLayer(IMapSource mapSource, IMapProjection projection, double transitionWidth = 5.0)
    {
        _mapSource = mapSource;
        _projection = projection;
        _transitionWidth = transitionWidth;
    }

    /// <summary>
    /// Render the satellite map with day/night blending.
    /// </summary>
    public void Render(DrawingContext dc, double width, double height, SunPosition sunPosition)
    {
        int pixelWidth = (int)Math.Ceiling(width);
        int pixelHeight = (int)Math.Ceiling(height);

        if (pixelWidth <= 0 || pixelHeight <= 0) return;

        // Get source textures
        var dayTexture = _mapSource.GetDayTexture();
        var nightTexture = _mapSource.GetNightTexture();

        if (dayTexture == null || nightTexture == null) return;

        // Reuse cached bitmap if dimensions match, otherwise create new one
        if (_cachedBitmap == null || _cachedWidth != pixelWidth || _cachedHeight != pixelHeight)
        {
            _cachedBitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgr32, null);
            _cachedWidth = pixelWidth;
            _cachedHeight = pixelHeight;
        }

        // Blend textures based on sun position
        BlendDayNightTextures(_cachedBitmap, dayTexture, nightTexture, sunPosition);

        // Draw the blended bitmap
        dc.DrawImage(_cachedBitmap, new Rect(0, 0, width, height));
    }

    /// <summary>
    /// Blend day and night textures based on solar position.
    /// </summary>
    private unsafe void BlendDayNightTextures(
        WriteableBitmap output,
        BitmapSource dayTexture,
        BitmapSource nightTexture,
        SunPosition sunPosition)
    {
        int outputWidth = output.PixelWidth;
        int outputHeight = output.PixelHeight;

        // Convert source textures to byte arrays
        byte[] dayPixels = GetPixelData(dayTexture);
        byte[] nightPixels = GetPixelData(nightTexture);

        int srcWidth = dayTexture.PixelWidth;
        int srcHeight = dayTexture.PixelHeight;

        output.Lock();

        try
        {
            byte* outputPixels = (byte*)output.BackBuffer.ToPointer();
            int outputStride = output.BackBufferStride;

            // Sample every few pixels for performance (can be adjusted)
            int stepSize = 2;

            for (int y = 0; y < outputHeight; y += stepSize)
            {
                for (int x = 0; x < outputWidth; x += stepSize)
                {
                    // Convert screen pixel to geographic coordinates
                    var screenPoint = new WpfPoint(x, y);
                    var geoPoint = _projection.ProjectToGeo(screenPoint, outputWidth, outputHeight);

                    // Calculate brightness factor (0 = night, 1 = day)
                    double brightness = SolarCalculator.GetBrightnessFactor(
                        geoPoint, sunPosition, _transitionWidth);

                    // Get corresponding pixel from source textures
                    // Convert geo coordinates to texture coordinates (equirectangular)
                    int texX = (int)((geoPoint.Longitude + 180.0) / 360.0 * srcWidth) % srcWidth;
                    int texY = (int)((90.0 - geoPoint.Latitude) / 180.0 * srcHeight);

                    // Clamp to valid range
                    texX = Math.Clamp(texX, 0, srcWidth - 1);
                    texY = Math.Clamp(texY, 0, srcHeight - 1);

                    int srcOffset = texY * srcWidth * 4 + texX * 4;

                    // Get day and night colors (BGR32 format)
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

                    // Fill the pixel block
                    for (int dy = 0; dy < stepSize && y + dy < outputHeight; dy++)
                    {
                        for (int dx = 0; dx < stepSize && x + dx < outputWidth; dx++)
                        {
                            int pixelX = x + dx;
                            int pixelY = y + dy;
                            int offset = pixelY * outputStride + pixelX * 4;

                            outputPixels[offset + 0] = finalB; // Blue
                            outputPixels[offset + 1] = finalG; // Green
                            outputPixels[offset + 2] = finalR; // Red
                        }
                    }
                }
            }

            output.AddDirtyRect(new Int32Rect(0, 0, outputWidth, outputHeight));
        }
        finally
        {
            output.Unlock();
        }
    }

    /// <summary>
    /// Extract pixel data from a BitmapSource.
    /// </summary>
    private byte[] GetPixelData(BitmapSource bitmap)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int stride = width * 4; // 4 bytes per pixel (BGR32)

        byte[] pixels = new byte[height * stride];

        // Convert to BGR32 format if needed
        var formatConvertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgr32, null, 0);
        formatConvertedBitmap.CopyPixels(pixels, stride, 0);

        return pixels;
    }
}
