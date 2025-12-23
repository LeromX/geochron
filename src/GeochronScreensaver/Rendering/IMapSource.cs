using System.Windows.Media.Imaging;

namespace GeochronScreensaver.Rendering;

/// <summary>
/// Interface for map texture sources.
/// Provides day and night textures for satellite-style map rendering.
/// </summary>
public interface IMapSource
{
    /// <summary>
    /// Gets the day texture (illuminated Earth).
    /// </summary>
    BitmapSource? GetDayTexture();

    /// <summary>
    /// Gets the night texture (Earth at night with city lights).
    /// </summary>
    BitmapSource? GetNightTexture();

    /// <summary>
    /// Indicates whether this map source supports blending between day and night textures.
    /// </summary>
    bool SupportsNightBlending { get; }
}
