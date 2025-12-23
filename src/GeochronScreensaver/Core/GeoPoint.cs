namespace GeochronScreensaver.Core;

/// <summary>
/// Represents a geographic point with latitude and longitude coordinates.
/// </summary>
public readonly struct GeoPoint
{
    /// <summary>
    /// Latitude in degrees (-90 to +90, negative is South).
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude in degrees (-180 to +180, negative is West).
    /// </summary>
    public double Longitude { get; init; }

    public GeoPoint(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString()
    {
        var latDir = Latitude >= 0 ? "N" : "S";
        var lonDir = Longitude >= 0 ? "E" : "W";
        return $"{Math.Abs(Latitude):F2}°{latDir}, {Math.Abs(Longitude):F2}°{lonDir}";
    }
}
