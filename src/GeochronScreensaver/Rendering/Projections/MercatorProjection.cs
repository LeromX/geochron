using System;
using GeochronScreensaver.Core;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.Rendering.Projections;

/// <summary>
/// Standard Mercator projection.
/// A cylindrical projection that preserves angles but distorts areas near the poles.
/// </summary>
public class MercatorProjection : IMapProjection
{
    // Maximum latitude for Mercator projection (approximately 85.05 degrees)
    private const double MaxLatitude = 85.05112878;

    public WpfPoint ProjectToScreen(GeoPoint point, double width, double height)
    {
        // Clamp latitude to valid Mercator range
        var lat = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, point.Latitude));

        // Longitude -180 to +180 maps to x 0 to width
        double x = (point.Longitude + 180.0) / 360.0 * width;

        // Mercator y-coordinate formula
        var latRad = lat * Math.PI / 180.0;
        var mercatorY = Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0));

        // Normalize to 0-1 range, then scale to screen height
        // The full Mercator range is approximately -π to +π for latitudes -85.05 to +85.05
        var normalizedY = (Math.PI - mercatorY) / (2.0 * Math.PI);
        double y = normalizedY * height;

        return new WpfPoint(x, y);
    }

    public GeoPoint ProjectToGeo(WpfPoint screenPoint, double width, double height)
    {
        // Convert screen x to longitude
        double longitude = (screenPoint.X / width * 360.0) - 180.0;

        // Convert screen y back to Mercator coordinate
        var normalizedY = screenPoint.Y / height;
        var mercatorY = Math.PI - (normalizedY * 2.0 * Math.PI);

        // Inverse Mercator formula
        var latitude = (2.0 * Math.Atan(Math.Exp(mercatorY)) - Math.PI / 2.0) * 180.0 / Math.PI;

        // Clamp to valid range
        latitude = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, latitude));

        return new GeoPoint(latitude, longitude);
    }
}
