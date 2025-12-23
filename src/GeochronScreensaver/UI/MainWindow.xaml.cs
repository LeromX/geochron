using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GeochronScreensaver.Core;
using GeochronScreensaver.Infrastructure;
using Microsoft.Win32;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace GeochronScreensaver.UI;

public partial class MainWindow : Window
{
    private GeochronControl? _geochronDisplay;
    private GlobeControl? _globeDisplay;
    private readonly DispatcherTimer _statusTimer;
    private bool _isFullscreen = false;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    public MainWindow()
    {
        InitializeComponent();

        // Set up status bar timer
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();

        // Create the visualization control based on settings
        CreateVisualizationControl();

        // Subscribe to settings changes
        ConfigurationManager.SettingsChanged += OnSettingsChanged;

        // Update status bar initially
        UpdateStatusBar();

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Set up keyboard commands
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, args) => Close()));
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _statusTimer.Stop();
        ConfigurationManager.SettingsChanged -= OnSettingsChanged;
        DisposeCurrentControl();
    }

    private void CreateVisualizationControl()
    {
        DisposeCurrentControl();

        var settings = ConfigurationManager.Current;

        if (settings.RenderMode == RenderModeType.Globe3D)
        {
            _globeDisplay = new GlobeControl();
            DisplayContainer.Content = _globeDisplay;
            // Apply current settings to ensure everything is synchronized
            _globeDisplay.UpdateSettings(settings);
        }
        else
        {
            _geochronDisplay = new GeochronControl();
            DisplayContainer.Content = _geochronDisplay;
            // Apply current settings to ensure everything is synchronized
            _geochronDisplay.UpdateSettings(settings);
        }

        UpdateStatusBar();
    }

    private void DisposeCurrentControl()
    {
        if (_geochronDisplay != null)
        {
            _geochronDisplay.Stop();
            _geochronDisplay.Dispose();
            _geochronDisplay = null;
        }

        if (_globeDisplay != null)
        {
            _globeDisplay.Stop();
            _globeDisplay.Dispose();
            _globeDisplay = null;
        }

        DisplayContainer.Content = null;
    }

    private void OnSettingsChanged(object? sender, Settings newSettings)
    {
        Dispatcher.Invoke(() =>
        {
            var currentMode = _globeDisplay != null ? RenderModeType.Globe3D : RenderModeType.Map2D;

            if (newSettings.RenderMode != currentMode)
            {
                // Render mode changed, recreate the control
                CreateVisualizationControl();
            }
            else
            {
                // Same mode, just update settings on existing control
                _geochronDisplay?.UpdateSettings(newSettings);
                _globeDisplay?.UpdateSettings(newSettings);
            }

            UpdateStatusBar();
        });
    }

    private void StatusTimer_Tick(object? sender, EventArgs e)
    {
        // Status bar update (UTC time removed)
    }

    private void UpdateStatusBar()
    {
        var settings = ConfigurationManager.Current;
        ModeDisplay.Text = $"Mode: {(settings.RenderMode == RenderModeType.Globe3D ? "3D Globe" : "2D Map")}";
        ProjectionDisplay.Text = $"Projection: {settings.ProjectionType}";
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Configure_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void SetAsScreensaver_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the path to our executable
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                System.Windows.MessageBox.Show("Could not determine application path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create .scr file path (copy exe as scr in same directory)
            var scrPath = Path.ChangeExtension(exePath, ".scr");

            // Copy exe to scr if needed
            if (!File.Exists(scrPath) || File.GetLastWriteTime(exePath) > File.GetLastWriteTime(scrPath))
            {
                File.Copy(exePath, scrPath, overwrite: true);
            }

            // Set registry key to use this screensaver
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true))
            {
                if (key != null)
                {
                    key.SetValue("SCRNSAVE.EXE", scrPath);
                    key.SetValue("ScreenSaveActive", "1");
                }
            }

            System.Windows.MessageBox.Show(
                $"Geochron has been set as your screensaver.\n\nYou can configure the wait time in Windows Settings > Personalization > Lock screen > Screen saver settings.",
                "Screensaver Installed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show(
                "Could not set screensaver. Try running the application as administrator.",
                "Permission Denied",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to set screensaver: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    private void SaveGlobePosition_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobePosition();
    }

    private void ResetGlobePosition_Click(object sender, RoutedEventArgs e)
    {
        ResetGlobePosition();
    }

    private void SaveGlobePosition()
    {
        if (_globeDisplay != null)
        {
            _globeDisplay.SaveCurrentPosition();

            // Show brief confirmation
            var (lat, lon) = _globeDisplay.GetCurrentPosition();
            var zoom = _globeDisplay.GetCurrentZoom();
            ModeDisplay.Text = $"Position saved: {lat:F1}, {lon:F1} (zoom {zoom:F2})";

            // Reset status after 3 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, args) =>
            {
                UpdateStatusBar();
                timer.Stop();
            };
            timer.Start();
        }
        else
        {
            System.Windows.MessageBox.Show(
                "Save Position is only available in 3D Globe mode.",
                "Not Available",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ResetGlobePosition()
    {
        var settings = ConfigurationManager.Current;
        settings.GlobeSavedLatitude = null;
        settings.GlobeSavedLongitude = null;
        settings.GlobeSavedZoom = 1.0;
        ConfigurationManager.UpdateSettings(settings, saveImmediately: true);

        ModeDisplay.Text = "Globe position reset to default";

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, args) =>
        {
            UpdateStatusBar();
            timer.Stop();
        };
        timer.Start();
    }

    private void OpenSettings()
    {
        var dialog = new ConfigurationDialog
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            ExitFullscreen();
        }
        else
        {
            EnterFullscreen();
        }
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen) return;

        // Save current state
        _previousWindowState = WindowState;
        _previousWindowStyle = WindowStyle;
        _previousResizeMode = ResizeMode;

        // Enter fullscreen
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
        Topmost = true;

        _isFullscreen = true;
        FullscreenMenuItem.Header = "Exit _Fullscreen";
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen) return;

        // Restore previous state
        Topmost = false;
        WindowState = _previousWindowState;
        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;

        _isFullscreen = false;
        FullscreenMenuItem.Header = "_Fullscreen";
    }

    // Command implementations for keyboard shortcuts
    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Escape when _isFullscreen:
                ExitFullscreen();
                e.Handled = true;
                break;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.OemComma:
                    OpenSettings();
                    e.Handled = true;
                    break;
                case Key.Q:
                    Close();
                    e.Handled = true;
                    break;
                case Key.S:
                    SaveGlobePosition();
                    e.Handled = true;
                    break;
            }
        }
    }
}
