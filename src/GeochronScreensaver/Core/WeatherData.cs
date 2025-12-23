using System;

namespace GeochronScreensaver.Core;

/// <summary>
/// Represents weather information for a geographic location.
/// </summary>
public class WeatherData
{
    /// <summary>
    /// Temperature in Celsius.
    /// </summary>
    public double TemperatureCelsius { get; set; }

    /// <summary>
    /// WMO weather code indicating conditions (0=clear, 1-3=cloudy, 51-67=rain, 71-77=snow, etc.)
    /// </summary>
    public int WeatherCode { get; set; }

    /// <summary>
    /// When this data was fetched from the API.
    /// </summary>
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// The geographic location this weather data is for.
    /// </summary>
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Gets the temperature formatted as a string with unit.
    /// </summary>
    /// <param name="useCelsius">True for Celsius, false for Fahrenheit.</param>
    /// <returns>Formatted temperature string (e.g., "7°C" or "45°F").</returns>
    public string GetFormattedTemperature(bool useCelsius = true)
    {
        if (useCelsius)
        {
            return $"{Math.Round(TemperatureCelsius)}°C";
        }
        else
        {
            double fahrenheit = TemperatureCelsius * 9.0 / 5.0 + 32.0;
            return $"{Math.Round(fahrenheit)}°F";
        }
    }

    /// <summary>
    /// Checks if the weather data is stale (older than maxAge).
    /// </summary>
    /// <param name="maxAge">Maximum age before data is considered stale.</param>
    /// <returns>True if data is stale.</returns>
    public bool IsStale(TimeSpan maxAge)
    {
        return DateTime.UtcNow - FetchedAt > maxAge;
    }

    /// <summary>
    /// Gets a simple description of the weather condition based on WMO code.
    /// </summary>
    public string GetConditionDescription()
    {
        return WeatherCode switch
        {
            0 => "Clear",
            1 or 2 or 3 => "Cloudy",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rain",
            71 or 73 or 75 => "Snow",
            77 => "Snow grains",
            80 or 81 or 82 => "Showers",
            85 or 86 => "Snow showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm with hail",
            _ => "Unknown"
        };
    }
}
