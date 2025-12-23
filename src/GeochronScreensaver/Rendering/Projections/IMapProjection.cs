using GeochronScreensaver.Core;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.Rendering.Projections;

/// <summary>
/// Interface for map projection implementations.
/// Converts between geographic coordinates and screen pixels.
/// </summary>
public interface IMapProjection
{
    /// <summary>
    /// Convert a geographic point to screen coordinates.
    /// </summary>
    /// <param name="point">Geographic point (latitude, longitude)</param>
    /// <param name="width">Width of the display area in pixels</param>
    /// <param name="height">Height of the display area in pixels</param>
    /// <returns>Screen coordinates</returns>
    WpfPoint ProjectToScreen(GeoPoint point, double width, double height);

    /// <summary>
    /// Convert screen coordinates to a geographic point.
    /// </summary>
    /// <param name="screenPoint">Screen coordinates</param>
    /// <param name="width">Width of the display area in pixels</param>
    /// <param name="height">Height of the display area in pixels</param>
    /// <returns>Geographic point</returns>
    GeoPoint ProjectToGeo(WpfPoint screenPoint, double width, double height);
}
