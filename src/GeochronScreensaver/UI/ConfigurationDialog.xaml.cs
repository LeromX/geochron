using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GeochronScreensaver.Core;
using GeochronScreensaver.Infrastructure;

namespace GeochronScreensaver.UI;

public partial class ConfigurationDialog : Window
{
    private Settings _workingSettings;
    private GeochronControl? _geochronPreview;
    private GlobeControl? _globePreview;
    private bool _isInitializing = true;

    public ConfigurationDialog()
    {
        InitializeComponent();

        // Load current settings
        _workingSettings = CloneSettings(ConfigurationManager.Current);

        // Apply settings to UI controls (events are ignored during init)
        LoadSettingsToUI();

        _isInitializing = false;

        // Start preview
        Loaded += (s, e) => UpdatePreview();
    }

    private void LoadSettingsToUI()
    {
        ShowCitiesCheckBox.IsChecked = _workingSettings.ShowCities;
        ShowGridLinesCheckBox.IsChecked = _workingSettings.ShowGridLines;
        NightOpacitySlider.Value = _workingSettings.NightOverlayOpacity;
        TransitionWidthSlider.Value = _workingSettings.TerminatorTransitionWidth;

        // Set render mode
        RenderModeComboBox.SelectedIndex = _workingSettings.RenderMode switch
        {
            RenderModeType.Map2D => 0,
            RenderModeType.Globe3D => 1,
            _ => 0
        };

        // Set map style
        MapStyleComboBox.SelectedIndex = _workingSettings.MapStyle switch
        {
            MapStyleType.Simplified => 0,
            MapStyleType.Satellite => 1,
            _ => 1
        };

        // Set projection
        ProjectionComboBox.SelectedIndex = _workingSettings.ProjectionType switch
        {
            MapProjectionType.Equirectangular => 0,
            MapProjectionType.Mercator => 1,
            _ => 0
        };

        // Set theme
        ThemeComboBox.SelectedIndex = _workingSettings.Theme switch
        {
            ThemeType.Dark => 0,
            ThemeType.Light => 1,
            _ => 0
        };

        // Set update interval (convert ms to seconds)
        UpdateIntervalSlider.Value = _workingSettings.UpdateIntervalMs / 1000.0;

        // Set time speed
        TimeSpeedSlider.Value = _workingSettings.TimeSpeedMultiplier;

        // Populate timezone ComboBox
        PopulateTimezones();

        // Set timezone name
        TimezoneNameTextBox.Text = _workingSettings.UserTimezoneName;
    }

    private void PopulateTimezones()
    {
        TimezoneComboBox.Items.Clear();

        var timezones = TimeZoneInfo.GetSystemTimeZones()
            .OrderBy(tz => tz.BaseUtcOffset)
            .ToList();

        int selectedIndex = 0;
        for (int i = 0; i < timezones.Count; i++)
        {
            var tz = timezones[i];
            var item = new ComboBoxItem
            {
                Content = $"(UTC{tz.BaseUtcOffset:hh\\:mm}) {tz.DisplayName}",
                Tag = tz.Id
            };
            TimezoneComboBox.Items.Add(item);

            if (tz.Id == _workingSettings.UserTimezoneId)
            {
                selectedIndex = i;
            }
        }

        TimezoneComboBox.SelectedIndex = selectedIndex;
    }

    private void OnTimezoneChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _workingSettings == null || TimezoneComboBox.SelectedItem == null) return;

        if (TimezoneComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tzId)
        {
            _workingSettings.UserTimezoneId = tzId;
        }
    }

    private void OnTimezoneNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing || _workingSettings == null) return;

        _workingSettings.UserTimezoneName = TimezoneNameTextBox.Text;
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        // Ignore events during initialization
        if (_isInitializing || _workingSettings == null) return;

        // Update working settings from UI
        _workingSettings.ShowCities = ShowCitiesCheckBox.IsChecked ?? true;
        _workingSettings.ShowGridLines = ShowGridLinesCheckBox.IsChecked ?? true;
        _workingSettings.NightOverlayOpacity = NightOpacitySlider.Value;
        _workingSettings.TerminatorTransitionWidth = TransitionWidthSlider.Value;

        _workingSettings.RenderMode = RenderModeComboBox.SelectedIndex switch
        {
            0 => RenderModeType.Map2D,
            1 => RenderModeType.Globe3D,
            _ => RenderModeType.Map2D
        };

        _workingSettings.MapStyle = MapStyleComboBox.SelectedIndex switch
        {
            0 => MapStyleType.Simplified,
            1 => MapStyleType.Satellite,
            _ => MapStyleType.Satellite
        };

        _workingSettings.ProjectionType = ProjectionComboBox.SelectedIndex switch
        {
            0 => MapProjectionType.Equirectangular,
            1 => MapProjectionType.Mercator,
            _ => MapProjectionType.Equirectangular
        };

        _workingSettings.Theme = ThemeComboBox.SelectedIndex switch
        {
            0 => ThemeType.Dark,
            1 => ThemeType.Light,
            _ => ThemeType.Dark
        };

        // Update interval (convert seconds to ms)
        _workingSettings.UpdateIntervalMs = (int)(UpdateIntervalSlider.Value * 1000);

        // Time speed (minimum 1x)
        _workingSettings.TimeSpeedMultiplier = Math.Max(1.0, TimeSpeedSlider.Value);

        // Update preview
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            // Create appropriate preview control based on render mode
            if (_workingSettings.RenderMode == RenderModeType.Globe3D)
            {
                // Stop 2D preview if switching modes
                if (_geochronPreview != null)
                {
                    _geochronPreview.Stop();
                    _geochronPreview = null;
                }

                // Create or update globe preview
                if (_globePreview == null)
                {
                    _globePreview = new GlobeControl();
                    PreviewContainer.Content = _globePreview;
                    _globePreview.UpdateSettings(_workingSettings);
                }
                else
                {
                    _globePreview.UpdateSettings(_workingSettings);
                    _globePreview.Start();
                }
            }
            else
            {
                // Stop 3D preview if switching modes
                if (_globePreview != null)
                {
                    _globePreview.Stop();
                    _globePreview = null;
                }

                // Create or update 2D map preview
                if (_geochronPreview == null)
                {
                    _geochronPreview = new GeochronControl();
                    PreviewContainer.Content = _geochronPreview;
                    _geochronPreview.UpdateSettings(_workingSettings);
                }
                else
                {
                    _geochronPreview.UpdateSettings(_workingSettings);
                }
            }
        }
        catch (Exception ex)
        {
            // Show error in preview area
            PreviewContainer.Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Preview error:\n{ex.Message}",
                Foreground = System.Windows.Media.Brushes.Red,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _workingSettings = Settings.Default;
        LoadSettingsToUI();
        UpdatePreview();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Save settings
        ConfigurationManager.UpdateSettings(_workingSettings, saveImmediately: true);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _geochronPreview?.Stop();
        _globePreview?.Stop();
        base.OnClosed(e);
    }

    private static Settings CloneSettings(Settings source)
    {
        return new Settings
        {
            ShowCities = source.ShowCities,
            ShowGridLines = source.ShowGridLines,
            ShowAnalogClock = source.ShowAnalogClock,
            ShowCoordinates = source.ShowCoordinates,
            NightOverlayOpacity = source.NightOverlayOpacity,
            TerminatorTransitionWidth = source.TerminatorTransitionWidth,
            ProjectionType = source.ProjectionType,
            Theme = source.Theme,
            MapStyle = source.MapStyle,
            EnableDayNightBlending = source.EnableDayNightBlending,
            RenderMode = source.RenderMode,
            GlobeRotationSpeed = source.GlobeRotationSpeed,
            GlobeAutoRotate = source.GlobeAutoRotate,
            GlobeSavedLatitude = source.GlobeSavedLatitude,
            GlobeSavedLongitude = source.GlobeSavedLongitude,
            GlobeSavedZoom = source.GlobeSavedZoom,
            UpdateIntervalMs = source.UpdateIntervalMs,
            TimeSpeedMultiplier = source.TimeSpeedMultiplier,
            UserTimezoneId = source.UserTimezoneId,
            UserTimezoneName = source.UserTimezoneName
        };
    }
}

