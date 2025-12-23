using System;
using GeochronScreensaver.Core;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.Rendering.Projections;

/// <summary>
/// Simple equirectangular (plate carrée) projection.
/// Maps latitude/longitude directly to x/y coordinates.
/// </summary>
public class EquirectangularProjection : IMapProjection
{
    public WpfPoint ProjectToScreen(GeoPoint point, double width, double height)
    {
        // Longitude -180 to +180 maps to x 0 to width
        double x = (point.Longitude + 180.0) / 360.0 * width;

        // Latitude +90 to -90 maps to y 0 to height
        double y = (90.0 - point.Latitude) / 180.0 * height;

        return new WpfPoint(x, y);
    }

    public GeoPoint ProjectToGeo(WpfPoint screenPoint, double width, double height)
    {
        // Convert screen x to longitude
        double longitude = (screenPoint.X / width * 360.0) - 180.0;

        // Convert screen y to latitude
        double latitude = 90.0 - (screenPoint.Y / height * 180.0);

        return new GeoPoint(latitude, longitude);
    }
}
