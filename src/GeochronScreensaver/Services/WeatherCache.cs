using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GeochronScreensaver.Core;

namespace GeochronScreensaver.Services;

/// <summary>
/// Caches weather data in memory and persists to disk.
/// </summary>
public class WeatherCache
{
    private readonly Dictionary<string, WeatherData> _cache = new();
    private readonly object _lock = new();
    private readonly string _cacheFilePath;
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromHours(24);

    public WeatherCache()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GeochronScreensaver");

        Directory.CreateDirectory(appDataPath);
        _cacheFilePath = Path.Combine(appDataPath, "weather_cache.json");

        LoadFromDisk();
    }

    /// <summary>
    /// Gets cached weather for a city.
    /// </summary>
    /// <param name="cityName">Name of the city.</param>
    /// <returns>Cached weather data or null if not found.</returns>
    public WeatherData? Get(string cityName)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(cityName, out var data))
            {
                // Return even stale data - let caller decide what to do
                return data;
            }
            return null;
        }
    }

    /// <summary>
    /// Stores weather data in cache.
    /// </summary>
    /// <param name="cityName">Name of the city.</param>
    /// <param name="data">Weather data to cache.</param>
    public void Set(string cityName, WeatherData data)
    {
        lock (_lock)
        {
            _cache[cityName] = data;
        }
    }

    /// <summary>
    /// Updates cache with multiple weather entries and saves to disk.
    /// </summary>
    /// <param name="weatherData">Dictionary of city names to weather data.</param>
    public void UpdateAll(Dictionary<string, WeatherData> weatherData)
    {
        lock (_lock)
        {
            foreach (var kvp in weatherData)
            {
                _cache[kvp.Key] = kvp.Value;
            }
        }

        SaveToDisk();
    }

    /// <summary>
    /// Gets all cached weather data.
    /// </summary>
    /// <returns>Dictionary of city names to weather data.</returns>
    public Dictionary<string, WeatherData> GetAll()
    {
        lock (_lock)
        {
            return new Dictionary<string, WeatherData>(_cache);
        }
    }

    /// <summary>
    /// Removes entries older than maxAge.
    /// </summary>
    /// <param name="maxAge">Maximum age for cache entries.</param>
    public void PurgeStale(TimeSpan? maxAge = null)
    {
        var age = maxAge ?? DefaultMaxAge;
        var keysToRemove = new List<string>();

        lock (_lock)
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsStale(age))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        if (keysToRemove.Count > 0)
        {
            SaveToDisk();
        }
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
        }

        try
        {
            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }
        }
        catch
        {
            // Ignore file deletion errors
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return;

            var json = File.ReadAllText(_cacheFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, WeatherData>>(json);

            if (data != null)
            {
                lock (_lock)
                {
                    foreach (var kvp in data)
                    {
                        // Only load non-stale entries
                        if (!kvp.Value.IsStale(DefaultMaxAge))
                        {
                            _cache[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
        }
        catch
        {
            // If cache is corrupted, delete it
            try
            {
                File.Delete(_cacheFilePath);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private void SaveToDisk()
    {
        try
        {
            Dictionary<string, WeatherData> dataToSave;
            lock (_lock)
            {
                dataToSave = new Dictionary<string, WeatherData>(_cache);
            }

            var json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_cacheFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
