using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeochronScreensaver.Core;
using GeochronScreensaver.Infrastructure;
using ThreadingTimer = System.Threading.Timer;

namespace GeochronScreensaver.Services;

/// <summary>
/// Manages weather data fetching and caching for the application.
/// Provides a single point of access for weather information.
/// </summary>
public class WeatherManager
{
    private static WeatherManager? _instance;
    private static readonly object _instanceLock = new();

    private readonly WeatherService _service;
    private readonly WeatherCache _cache;
    private readonly Dictionary<string, WeatherData> _currentWeather = new();
    private readonly object _weatherLock = new();
    private ThreadingTimer? _refreshTimer;
    private bool _isRefreshing;
    private DateTime _lastRefresh = DateTime.MinValue;

    /// <summary>
    /// Gets the singleton instance of the WeatherManager.
    /// </summary>
    public static WeatherManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new WeatherManager();
                }
            }
            return _instance;
        }
    }

    private WeatherManager()
    {
        _service = new WeatherService();
        _cache = new WeatherCache();

        // Load cached weather data
        var cached = _cache.GetAll();
        lock (_weatherLock)
        {
            foreach (var kvp in cached)
            {
                _currentWeather[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Starts automatic weather refresh based on settings.
    /// </summary>
    public void Start()
    {
        // Weather feature disabled - no action needed
    }

    /// <summary>
    /// Stops automatic weather refresh.
    /// </summary>
    public void Stop()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    /// <summary>
    /// Gets the current weather for a city.
    /// Returns cached data if available.
    /// </summary>
    /// <param name="cityName">Name of the city.</param>
    /// <returns>Weather data or null if not available.</returns>
    public WeatherData? GetWeather(string cityName)
    {
        lock (_weatherLock)
        {
            if (_currentWeather.TryGetValue(cityName, out var data))
            {
                return data;
            }
        }

        // Try cache
        return _cache.Get(cityName);
    }

    /// <summary>
    /// Gets weather for all cities.
    /// </summary>
    /// <returns>Dictionary of city names to weather data.</returns>
    public Dictionary<string, WeatherData> GetAllWeather()
    {
        lock (_weatherLock)
        {
            return new Dictionary<string, WeatherData>(_currentWeather);
        }
    }

    /// <summary>
    /// Refreshes weather for all default cities.
    /// </summary>
    public async Task RefreshWeatherAsync()
    {
        if (_isRefreshing)
            return;

        // Weather feature disabled
        return;

        // Don't refresh too frequently
        if (DateTime.UtcNow - _lastRefresh < TimeSpan.FromMinutes(5))
            return;

        _isRefreshing = true;
        try
        {
            var cities = City.GetDefaultCities();
            var weather = await _service.FetchWeatherForCitiesAsync(cities);

            if (weather.Count > 0)
            {
                lock (_weatherLock)
                {
                    foreach (var kvp in weather)
                    {
                        _currentWeather[kvp.Key] = kvp.Value;
                    }
                }

                _cache.UpdateAll(weather);
                _lastRefresh = DateTime.UtcNow;
            }
        }
        catch
        {
            // Silently fail - use cached data
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Called when settings change - updates refresh behavior.
    /// </summary>
    public void OnSettingsChanged()
    {
        // Weather feature disabled - no action needed
    }

    /// <summary>
    /// Clears all weather data and cache.
    /// </summary>
    public void ClearAll()
    {
        lock (_weatherLock)
        {
            _currentWeather.Clear();
        }
        _cache.Clear();
    }
}
