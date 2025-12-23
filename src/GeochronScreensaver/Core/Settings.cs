using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeochronScreensaver.Core;

/// <summary>
/// Application settings for the Geochron screensaver.
/// </summary>
public class Settings
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GeochronScreensaver");
    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

    // Display settings
    public bool ShowCities { get; set; } = true;
    public bool ShowGridLines { get; set; } = true;
    public bool ShowAnalogClock { get; set; } = false;
    public bool ShowCoordinates { get; set; } = false;

    // Visual settings
    public double NightOverlayOpacity { get; set; } = 0.7;
    public double TerminatorTransitionWidth { get; set; } = 5.0; // Degrees

    // Projection and theme
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MapProjectionType ProjectionType { get; set; } = MapProjectionType.Equirectangular;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeType Theme { get; set; } = ThemeType.Dark;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MapStyleType MapStyle { get; set; } = MapStyleType.Satellite; // Default to satellite

    public bool EnableDayNightBlending { get; set; } = true;

    // Render mode settings
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RenderModeType RenderMode { get; set; } = RenderModeType.Globe3D;

    public double GlobeRotationSpeed { get; set; } = 0.0; // 0 = real-time, degrees per second otherwise
    public bool GlobeAutoRotate { get; set; } = true;

    // Globe saved position (null = use current sun position)
    public double? GlobeSavedLatitude { get; set; } = null;
    public double? GlobeSavedLongitude { get; set; } = null;
    public double GlobeSavedZoom { get; set; } = 1.0;

    // Performance settings
    public int UpdateIntervalMs { get; set; } = 60000; // Update every minute

    // Time simulation settings
    public double TimeSpeedMultiplier { get; set; } = 1.0; // 1.0 = real-time, higher = faster

    // User timezone settings
    public string UserTimezoneId { get; set; } = TimeZoneInfo.Local.Id; // Default to system timezone
    public string UserTimezoneName { get; set; } = "My Time"; // Display name for user's timezone

    /// <summary>
    /// Gets the default settings.
    /// </summary>
    public static Settings Default => new Settings();

    /// <summary>
    /// Load settings from disk, or return default settings if file doesn't exist.
    /// </summary>
    public static Settings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return Default;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<Settings>(json);
            return settings ?? Default;
        }
        catch (Exception)
        {
            // If loading fails, return default settings
            return Default;
        }
    }

    /// <summary>
    /// Save settings to disk.
    /// </summary>
    public void Save()
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(AppDataFolder);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception)
        {
            // Silently fail - settings won't persist but app continues to work
        }
    }
}

/// <summary>
/// Available map projection types.
/// </summary>
public enum MapProjectionType
{
    Equirectangular,
    Mercator
}

/// <summary>
/// Available theme types.
/// </summary>
public enum ThemeType
{
    Dark,
    Light
}

/// <summary>
/// Available map style types.
/// </summary>
public enum MapStyleType
{
    Simplified,
    Satellite
}

/// <summary>
/// Available render mode types.
/// </summary>
public enum RenderModeType
{
    Map2D,
    Globe3D
}
