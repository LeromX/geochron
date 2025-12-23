using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GeochronScreensaver.Core;

namespace GeochronScreensaver.Services;

/// <summary>
/// Service for fetching weather data from the Open-Meteo API.
/// Open-Meteo is completely free with no API key required.
/// </summary>
public class WeatherService
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    /// <summary>
    /// Fetches current weather for multiple cities in a single API call.
    /// </summary>
    /// <param name="cities">List of cities to fetch weather for.</param>
    /// <returns>Dictionary mapping city names to weather data.</returns>
    public async Task<Dictionary<string, WeatherData>> FetchWeatherForCitiesAsync(IEnumerable<City> cities)
    {
        var result = new Dictionary<string, WeatherData>();
        var cityList = cities.ToList();

        if (cityList.Count == 0)
            return result;

        try
        {
            // Build comma-separated coordinate lists
            var latitudes = string.Join(",", cityList.Select(c =>
                c.Location.Latitude.ToString("F4", CultureInfo.InvariantCulture)));
            var longitudes = string.Join(",", cityList.Select(c =>
                c.Location.Longitude.ToString("F4", CultureInfo.InvariantCulture)));

            var url = $"{BaseUrl}?latitude={latitudes}&longitude={longitudes}&current=temperature_2m,weather_code";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            // Check if we got an array (multiple locations) or single object
            var root = data.RootElement;

            // For multiple locations, Open-Meteo returns an array
            if (root.ValueKind == JsonValueKind.Array)
            {
                var weatherArray = root.EnumerateArray().ToList();
                for (int i = 0; i < Math.Min(cityList.Count, weatherArray.Count); i++)
                {
                    var weather = ParseWeatherFromElement(weatherArray[i], cityList[i].Location);
                    if (weather != null)
                    {
                        result[cityList[i].Name] = weather;
                    }
                }
            }
            else
            {
                // Single location returns an object directly
                if (cityList.Count == 1)
                {
                    var weather = ParseWeatherFromElement(root, cityList[0].Location);
                    if (weather != null)
                    {
                        result[cityList[0].Name] = weather;
                    }
                }
                else
                {
                    // Multiple locations but got object - try parsing as array wrapper
                    // Open-Meteo might return different formats based on request
                    for (int i = 0; i < cityList.Count; i++)
                    {
                        var weather = TryParseIndexedWeather(root, i, cityList[i].Location);
                        if (weather != null)
                        {
                            result[cityList[i].Name] = weather;
                        }
                    }
                }
            }
        }
        catch (HttpRequestException)
        {
            // Network error - return empty, caller will use cached data
        }
        catch (TaskCanceledException)
        {
            // Timeout - return empty
        }
        catch (JsonException)
        {
            // Parse error - return empty
        }

        return result;
    }

    private WeatherData? ParseWeatherFromElement(JsonElement element, GeoPoint location)
    {
        try
        {
            if (!element.TryGetProperty("current", out var current))
                return null;

            double temp = 0;
            int weatherCode = 0;

            if (current.TryGetProperty("temperature_2m", out var tempElement))
            {
                temp = tempElement.GetDouble();
            }

            if (current.TryGetProperty("weather_code", out var codeElement))
            {
                weatherCode = codeElement.GetInt32();
            }

            return new WeatherData
            {
                TemperatureCelsius = temp,
                WeatherCode = weatherCode,
                FetchedAt = DateTime.UtcNow,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };
        }
        catch
        {
            return null;
        }
    }

    private WeatherData? TryParseIndexedWeather(JsonElement root, int index, GeoPoint location)
    {
        try
        {
            // Try to parse from arrays within the object
            if (!root.TryGetProperty("current", out var current))
                return null;

            double temp = 0;
            int weatherCode = 0;

            if (current.TryGetProperty("temperature_2m", out var tempArray) &&
                tempArray.ValueKind == JsonValueKind.Array)
            {
                var temps = tempArray.EnumerateArray().ToList();
                if (index < temps.Count)
                    temp = temps[index].GetDouble();
            }
            else if (current.TryGetProperty("temperature_2m", out var tempValue))
            {
                temp = tempValue.GetDouble();
            }

            if (current.TryGetProperty("weather_code", out var codeArray) &&
                codeArray.ValueKind == JsonValueKind.Array)
            {
                var codes = codeArray.EnumerateArray().ToList();
                if (index < codes.Count)
                    weatherCode = codes[index].GetInt32();
            }
            else if (current.TryGetProperty("weather_code", out var codeValue))
            {
                weatherCode = codeValue.GetInt32();
            }

            return new WeatherData
            {
                TemperatureCelsius = temp,
                WeatherCode = weatherCode,
                FetchedAt = DateTime.UtcNow,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches weather for a single location.
    /// </summary>
    public async Task<WeatherData?> FetchWeatherAsync(GeoPoint location)
    {
        try
        {
            var lat = location.Latitude.ToString("F4", CultureInfo.InvariantCulture);
            var lon = location.Longitude.ToString("F4", CultureInfo.InvariantCulture);
            var url = $"{BaseUrl}?latitude={lat}&longitude={lon}&current=temperature_2m,weather_code";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);

            return ParseWeatherFromElement(data.RootElement, location);
        }
        catch
        {
            return null;
        }
    }
}
