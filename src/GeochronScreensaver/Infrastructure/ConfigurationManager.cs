using System;
using GeochronScreensaver.Core;

namespace GeochronScreensaver.Infrastructure;

/// <summary>
/// Manages application configuration and settings persistence.
/// </summary>
public static class ConfigurationManager
{
    private static readonly Lazy<Settings> _lazySettings = new Lazy<Settings>(() => Settings.Load());
    private static Settings? _currentSettings;
    private static readonly object _lock = new object();

    /// <summary>
    /// Event raised when settings are updated and saved.
    /// </summary>
    public static event EventHandler<Settings>? SettingsChanged;

    /// <summary>
    /// Gets the current application settings, loading from disk if needed.
    /// Thread-safe.
    /// </summary>
    public static Settings Current
    {
        get
        {
            lock (_lock)
            {
                return _currentSettings ?? _lazySettings.Value;
            }
        }
    }

    /// <summary>
    /// Saves the current settings to disk.
    /// Thread-safe.
    /// </summary>
    public static void Save()
    {
        lock (_lock)
        {
            _currentSettings?.Save();
        }
    }

    /// <summary>
    /// Updates the current settings and optionally saves to disk.
    /// Thread-safe.
    /// </summary>
    public static void UpdateSettings(Settings newSettings, bool saveImmediately = true)
    {
        EventHandler<Settings>? handler = null;
        Settings? settingsToNotify = null;

        lock (_lock)
        {
            _currentSettings = newSettings;
            if (saveImmediately)
            {
                _currentSettings.Save();
                handler = SettingsChanged;
                settingsToNotify = _currentSettings;
            }
        }

        // Invoke event outside lock to prevent potential deadlocks
        handler?.Invoke(null, settingsToNotify!);
    }

    /// <summary>
    /// Resets settings to default values.
    /// Thread-safe.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _currentSettings = Settings.Default;
        }
    }
}
