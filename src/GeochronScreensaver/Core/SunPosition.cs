namespace GeochronScreensaver.Core;

/// <summary>
/// Represents the position of the sun at a specific moment in time.
/// </summary>
public class SunPosition
{
    /// <summary>
    /// The sub-solar point - where the sun is directly overhead.
    /// </summary>
    public GeoPoint SubSolarPoint { get; set; }

    /// <summary>
    /// Solar declination in degrees (-23.5 to +23.5).
    /// </summary>
    public double Declination { get; set; }

    /// <summary>
    /// The time this position was calculated for.
    /// </summary>
    public DateTime Timestamp { get; set; }

    public SunPosition(GeoPoint subSolarPoint, double declination, DateTime timestamp)
    {
        SubSolarPoint = subSolarPoint;
        Declination = declination;
        Timestamp = timestamp;
    }

    public override string ToString()
    {
        return $"Sun at {SubSolarPoint}, declination {Declination:F2}° at {Timestamp:u}";
    }
}
