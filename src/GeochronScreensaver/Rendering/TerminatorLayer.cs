using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering.Projections;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.Rendering;

/// <summary>
/// Renders the day/night terminator overlay using CPU-based pixel calculation.
/// </summary>
public class TerminatorLayer
{
    private readonly IMapProjection _projection;
    private readonly double _transitionWidth;
    private readonly double _nightOpacity;
    private WriteableBitmap? _cachedBitmap;
    private int _cachedWidth;
    private int _cachedHeight;

    public TerminatorLayer(IMapProjection projection, double transitionWidth = 5.0, double nightOpacity = 0.7)
    {
        _projection = projection;
        _transitionWidth = transitionWidth;
        _nightOpacity = nightOpacity;
    }

    /// <summary>
    /// Render the terminator overlay to a drawing context.
    /// </summary>
    public void Render(DrawingContext dc, double width, double height, SunPosition sunPosition)
    {
        // Create a bitmap to hold the terminator overlay
        int pixelWidth = (int)Math.Ceiling(width);
        int pixelHeight = (int)Math.Ceiling(height);

        if (pixelWidth <= 0 || pixelHeight <= 0) return;

        // Reuse cached bitmap if dimensions match, otherwise create new one
        if (_cachedBitmap == null || _cachedWidth != pixelWidth || _cachedHeight != pixelHeight)
        {
            _cachedBitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);
            _cachedWidth = pixelWidth;
            _cachedHeight = pixelHeight;
        }

        // Calculate the terminator
        CalculateTerminator(_cachedBitmap, sunPosition);

        // Draw the bitmap
        dc.DrawImage(_cachedBitmap, new Rect(0, 0, width, height));
    }

    private unsafe void CalculateTerminator(WriteableBitmap bitmap, SunPosition sunPosition)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        bitmap.Lock();

        try
        {
            byte* pixels = (byte*)bitmap.BackBuffer.ToPointer();
            int stride = bitmap.BackBufferStride;

            // Sample every few pixels for performance (can be made configurable)
            int stepSize = 2; // Process every 2nd pixel for better performance

            for (int y = 0; y < height; y += stepSize)
            {
                for (int x = 0; x < width; x += stepSize)
                {
                    // Convert pixel to geographic coordinates
                    var screenPoint = new WpfPoint(x, y);
                    var geoPoint = _projection.ProjectToGeo(screenPoint, width, height);

                    // Calculate brightness (0 = night, 1 = day)
                    double brightness = SolarCalculator.GetBrightnessFactor(
                        geoPoint, sunPosition, _transitionWidth);

                    // Calculate night overlay alpha (inverted brightness)
                    byte alpha = (byte)((1.0 - brightness) * _nightOpacity * 255);

                    // Fill the pixel block
                    for (int dy = 0; dy < stepSize && y + dy < height; dy++)
                    {
                        for (int dx = 0; dx < stepSize && x + dx < width; dx++)
                        {
                            int pixelX = x + dx;
                            int pixelY = y + dy;
                            int offset = pixelY * stride + pixelX * 4;

                            // BGRA format: Blue, Green, Red, Alpha
                            pixels[offset + 0] = 0;     // Blue
                            pixels[offset + 1] = 0;     // Green
                            pixels[offset + 2] = 0;     // Red
                            pixels[offset + 3] = alpha; // Alpha
                        }
                    }
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
    }
}
