using System;

namespace GeochronScreensaver.Core;

/// <summary>
/// Calculates the position of the sun and day/night terminator using astronomical algorithms.
/// </summary>
public static class SolarCalculator
{
    private const double J2000Epoch = 2451545.0;
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Calculate the sun's position for a given UTC time.
    /// </summary>
    public static SunPosition GetSunPosition(DateTime utcTime)
    {
        var jd = GetJulianDate(utcTime);
        var declination = CalculateDeclination(jd);
        var subSolarLongitude = CalculateSubSolarLongitude(utcTime);

        var subSolarPoint = new GeoPoint(declination, subSolarLongitude);
        return new SunPosition(subSolarPoint, declination, utcTime);
    }

    /// <summary>
    /// Convert DateTime to Julian Date.
    /// </summary>
    private static double GetJulianDate(DateTime utcTime)
    {
        int year = utcTime.Year;
        int month = utcTime.Month;
        int day = utcTime.Day;

        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        int a = year / 100;
        int b = 2 - a + (a / 4);

        double jd = Math.Floor(365.25 * (year + 4716)) +
                    Math.Floor(30.6001 * (month + 1)) +
                    day + b - 1524.5;

        // Add the time of day
        double dayFraction = (utcTime.Hour +
                             utcTime.Minute / 60.0 +
                             utcTime.Second / 3600.0 +
                             utcTime.Millisecond / 3600000.0) / 24.0;

        return jd + dayFraction;
    }

    /// <summary>
    /// Calculate solar declination using simplified algorithm.
    /// </summary>
    private static double CalculateDeclination(double jd)
    {
        // Days since J2000.0 epoch
        double n = jd - J2000Epoch;

        // Mean longitude of the sun
        double L = (280.460 + 0.9856474 * n) % 360.0;
        if (L < 0) L += 360.0;

        // Mean anomaly
        double g = (357.528 + 0.9856003 * n) % 360.0;
        if (g < 0) g += 360.0;

        double gRad = g * DegreesToRadians;

        // Ecliptic longitude
        double lambda = L + 1.915 * Math.Sin(gRad) + 0.020 * Math.Sin(2 * gRad);
        double lambdaRad = lambda * DegreesToRadians;

        // Obliquity of the ecliptic
        double epsilon = 23.439 - 0.0000004 * n;
        double epsilonRad = epsilon * DegreesToRadians;

        // Declination
        double declination = Math.Asin(Math.Sin(epsilonRad) * Math.Sin(lambdaRad));

        return declination * RadiansToDegrees;
    }

    /// <summary>
    /// Calculate the sub-solar longitude based on UTC time.
    /// The sun is at longitude 0 at 12:00 UTC (solar noon at Greenwich).
    /// </summary>
    private static double CalculateSubSolarLongitude(DateTime utcTime)
    {
        // Hours since midnight UTC
        double hoursUtc = utcTime.Hour +
                         utcTime.Minute / 60.0 +
                         utcTime.Second / 3600.0 +
                         utcTime.Millisecond / 3600000.0;

        // Sub-solar longitude: 0° at 12:00 UTC, moving 15° per hour westward
        double longitude = (12.0 - hoursUtc) * 15.0;

        // Normalize to -180 to +180
        while (longitude > 180.0) longitude -= 360.0;
        while (longitude < -180.0) longitude += 360.0;

        return longitude;
    }

    /// <summary>
    /// Get points along the day/night terminator line.
    /// </summary>
    /// <param name="sunPosition">The current sun position</param>
    /// <param name="numberOfPoints">Number of points to generate along the terminator</param>
    public static GeoPoint[] GetTerminatorPoints(SunPosition sunPosition, int numberOfPoints = 360)
    {
        var points = new GeoPoint[numberOfPoints];
        var subSolar = sunPosition.SubSolarPoint;

        for (int i = 0; i < numberOfPoints; i++)
        {
            double angle = (i * 360.0 / numberOfPoints) * DegreesToRadians;

            // Calculate point 90 degrees away from sub-solar point
            var point = CalculatePointAtDistance(subSolar, angle, 90.0);
            points[i] = point;
        }

        return points;
    }

    /// <summary>
    /// Calculate angular distance between two geographic points.
    /// Returns the angle in degrees (0-180).
    /// </summary>
    public static double CalculateAngularDistance(GeoPoint p1, GeoPoint p2)
    {
        double lat1 = p1.Latitude * DegreesToRadians;
        double lon1 = p1.Longitude * DegreesToRadians;
        double lat2 = p2.Latitude * DegreesToRadians;
        double lon2 = p2.Longitude * DegreesToRadians;

        // Haversine formula for great circle distance
        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return c * RadiansToDegrees;
    }

    /// <summary>
    /// Calculate a point at a given angular distance and bearing from a reference point.
    /// </summary>
    private static GeoPoint CalculatePointAtDistance(GeoPoint origin, double bearingRadians, double distanceDegrees)
    {
        double lat1 = origin.Latitude * DegreesToRadians;
        double lon1 = origin.Longitude * DegreesToRadians;
        double angularDistance = distanceDegrees * DegreesToRadians;

        double lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(angularDistance) +
            Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearingRadians)
        );

        double lon2 = lon1 + Math.Atan2(
            Math.Sin(bearingRadians) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2)
        );

        double latitude = lat2 * RadiansToDegrees;
        double longitude = lon2 * RadiansToDegrees;

        // Normalize longitude
        while (longitude > 180.0) longitude -= 360.0;
        while (longitude < -180.0) longitude += 360.0;

        return new GeoPoint(latitude, longitude);
    }

    /// <summary>
    /// Determine if a point is in daylight (true) or night (false).
    /// </summary>
    public static bool IsInDaylight(GeoPoint point, SunPosition sunPosition)
    {
        double distance = CalculateAngularDistance(point, sunPosition.SubSolarPoint);
        return distance < 90.0; // Day if within 90 degrees of sub-solar point
    }

    /// <summary>
    /// Get the brightness factor for a point (0 = full night, 1 = full day).
    /// Includes a smooth transition zone at the terminator.
    /// </summary>
    public static double GetBrightnessFactor(GeoPoint point, SunPosition sunPosition, double transitionWidth = 5.0)
    {
        double distance = CalculateAngularDistance(point, sunPosition.SubSolarPoint);

        if (distance < 90.0 - transitionWidth)
        {
            return 1.0; // Full day
        }
        else if (distance > 90.0 + transitionWidth)
        {
            return 0.0; // Full night
        }
        else
        {
            // Smooth transition using cosine interpolation
            double t = (distance - (90.0 - transitionWidth)) / (2.0 * transitionWidth);
            return (Math.Cos(t * Math.PI) + 1.0) / 2.0;
        }
    }
}
