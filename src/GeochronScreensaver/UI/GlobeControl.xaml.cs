using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GeochronScreensaver.Core;
using GeochronScreensaver.Infrastructure;
using GeochronScreensaver.Rendering.Globe;
using GeochronScreensaver.Services;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace GeochronScreensaver.UI;

/// <summary>
/// UserControl that hosts the 3D globe visualization with mouse rotation.
/// </summary>
public partial class GlobeControl : WpfUserControl, IDisposable
{
    private GlobeRenderer? _renderer;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _animationTimer; // Fast timer for momentum animation
    private bool _disposed;
    private Settings _settings;

    // Mouse rotation state
    private bool _isDragging;
    private WpfPoint _lastMousePosition;
    private double _rotationLatitude;
    private double _rotationLongitude;

    // Momentum/inertia state
    private double _velocityLatitude;
    private double _velocityLongitude;
    private DateTime _lastFrameTime;
    private const double Friction = 10.0; // Deceleration rate (stops in ~0.3s)

    // Selected location for clock display
    private double? _selectedLatitude;
    private double? _selectedLongitude;
    private string? _selectedLocationName;
    private DateTime? _selectionTime;
    private const double SelectionDisplaySeconds = 2.0;
    private const double FadeDurationSeconds = 0.5;

    // Zoom state
    private double _currentZoom = 1.0;
    private const double ZoomSensitivity = 0.1;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 2.0;

    // Time simulation
    private DateTime _simulatedTime;
    private DateTime _lastRealTime;

    public GlobeControl() : this(restoreSavedPosition: false)
    {
    }

    public GlobeControl(bool restoreSavedPosition)
    {
        InitializeComponent();

        _settings = ConfigurationManager.Current;

        // Initialize rotation - either from saved position or current sun position
        if (restoreSavedPosition && _settings.GlobeSavedLatitude.HasValue && _settings.GlobeSavedLongitude.HasValue)
        {
            _rotationLatitude = _settings.GlobeSavedLatitude.Value;
            _rotationLongitude = _settings.GlobeSavedLongitude.Value;
            _currentZoom = _settings.GlobeSavedZoom;
        }
        else
        {
            var sunPos = SolarCalculator.GetSunPosition(DateTime.UtcNow);
            _rotationLatitude = sunPos.SubSolarPoint.Latitude;
            _rotationLongitude = sunPos.SubSolarPoint.Longitude;
            _currentZoom = 1.0;
        }
        _lastFrameTime = DateTime.UtcNow;

        // Initialize simulated time
        _simulatedTime = DateTime.UtcNow;
        _lastRealTime = DateTime.UtcNow;

        InitializeRenderer(_settings);

        // Set up update timer (updates sun position/texture)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_settings.UpdateIntervalMs)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        // Set up fast animation timer for momentum (60 fps)
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animationTimer.Tick += AnimationTimer_Tick;

        Loaded += GlobeControl_Loaded;
        SizeChanged += GlobeControl_SizeChanged;

        // Mouse events for rotation
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;

        // Right-click to select location
        MouseRightButtonDown += OnMouseRightButtonDown;

        // Mouse wheel for zoom
        MouseWheel += OnMouseWheel;
    }

    private void GlobeControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Size changed - no action needed
    }

    /// <summary>
    /// Updates the control to use new settings.
    /// </summary>
    public void UpdateSettings(Settings settings)
    {
        _settings = settings;

        // Reset simulated time when speed changes
        _simulatedTime = DateTime.UtcNow;
        _lastRealTime = DateTime.UtcNow;

        if (_renderer != null)
        {
            _renderer.UpdateSettings(settings);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs);
            UpdateRender();
        }
        else
        {
            InitializeRenderer(settings);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs);
            UpdateRender();
        }
    }

    private void InitializeRenderer(Settings settings)
    {
        _renderer = new GlobeRenderer(settings);

        // Apply saved zoom level
        _renderer.SetCameraZoom(_currentZoom);

        var viewport = _renderer.GetViewport();
        ViewportContainer.Children.Clear();
        ViewportContainer.Children.Add(viewport);

        // Perform initial render
        _renderer.Render(DateTime.UtcNow);
    }

    private void GlobeControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateRender();
        _updateTimer.Start();
        _animationTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Force texture update on slow timer
        if (_renderer != null)
        {
            _renderer.Render(DateTime.UtcNow);
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        // Handle momentum animation
        bool hasMomentum = !_isDragging && (Math.Abs(_velocityLatitude) > 0.01 || Math.Abs(_velocityLongitude) > 0.01);
        // Also update during selection fade animation
        bool hasActiveSelection = _selectionTime.HasValue;
        // Always update when time is accelerated
        bool isTimeAccelerated = _settings.TimeSpeedMultiplier > 1.0;

        if (hasMomentum || hasActiveSelection || isTimeAccelerated)
        {
            UpdateRender();
        }
    }

    private void UpdateRender()
    {
        if (_disposed || _renderer == null) return;

        var now = DateTime.UtcNow;
        var realDeltaTime = (now - _lastRealTime).TotalSeconds;
        _lastRealTime = now;

        // Advance simulated time based on speed multiplier
        var simulatedDelta = realDeltaTime * _settings.TimeSpeedMultiplier;
        _simulatedTime = _simulatedTime.AddSeconds(simulatedDelta);

        var utcTime = _simulatedTime;
        var deltaTime = (now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = now;

        // Apply momentum when not dragging
        if (!_isDragging && (Math.Abs(_velocityLatitude) > 0.01 || Math.Abs(_velocityLongitude) > 0.01))
        {
            // Apply velocity to rotation
            _rotationLatitude += _velocityLatitude * deltaTime;
            _rotationLongitude += _velocityLongitude * deltaTime;

            // Apply friction (exponential decay for smooth stop)
            var frictionFactor = Math.Exp(-Friction * deltaTime);
            _velocityLatitude *= frictionFactor;
            _velocityLongitude *= frictionFactor;

            // Clamp latitude
            _rotationLatitude = Math.Clamp(_rotationLatitude, -90, 90);

            // Wrap longitude
            while (_rotationLongitude > 180) _rotationLongitude -= 360;
            while (_rotationLongitude < -180) _rotationLongitude += 360;
        }

        // Update globe rotation
        _renderer.SetManualRotation(_rotationLatitude, _rotationLongitude);
        _renderer.Render(utcTime);

        // Update info overlay
        UpdateInfoOverlay(utcTime);
    }

    private void UpdateInfoOverlay(DateTime utcTime)
    {
        // Always show user's timezone time (just the time, no label)
        var userTzTime = GetUserTimezoneTime(utcTime);
        InfoText.Text = $"{userTzTime:HH:mm:ss}";

        // Handle selected location display with fade
        if (_selectedLatitude.HasValue && _selectedLongitude.HasValue && _selectionTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - _selectionTime.Value).TotalSeconds;

            if (elapsed < SelectionDisplaySeconds)
            {
                // Full opacity during display period
                var localTime = GetLocalTimeAtLongitude(utcTime, _selectedLongitude.Value);
                var locationName = _selectedLocationName ?? "Selected";
                SelectedLocationText.Text = $"{locationName}: {localTime:HH:mm:ss}";
                SelectedLocationText.Opacity = 1.0;
            }
            else if (elapsed < SelectionDisplaySeconds + FadeDurationSeconds)
            {
                // Fading out
                var fadeProgress = (elapsed - SelectionDisplaySeconds) / FadeDurationSeconds;
                SelectedLocationText.Opacity = 1.0 - fadeProgress;
            }
            else
            {
                // Fade complete, clear selection
                SelectedLocationText.Opacity = 0;
                _selectedLatitude = null;
                _selectedLongitude = null;
                _selectedLocationName = null;
                _selectionTime = null;
            }
        }
        else
        {
            SelectedLocationText.Opacity = 0;
        }
    }

    private DateTime GetUserTimezoneTime(DateTime utcTime)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(_settings.UserTimezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
        catch
        {
            // Fallback to local time if timezone not found
            return utcTime.ToLocalTime();
        }
    }

    private DateTime GetLocalTimeAtLongitude(DateTime utcTime, double longitude)
    {
        // Simple timezone calculation based on longitude (solar time)
        // Each 15° of longitude = 1 hour offset from UTC
        var offsetHours = longitude / 15.0;
        return utcTime.AddHours(offsetHours);
    }

    #region Mouse Rotation

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastMousePosition = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPosition = e.GetPosition(this);
        var deltaX = currentPosition.X - _lastMousePosition.X;
        var deltaY = currentPosition.Y - _lastMousePosition.Y;

        // Calculate velocity based on mouse movement
        const double sensitivity = 0.3;
        var deltaLon = -deltaX * sensitivity;
        var deltaLat = -deltaY * sensitivity;

        // Apply rotation
        _rotationLongitude += deltaLon;
        _rotationLatitude += deltaLat;

        // Update velocity (for momentum when released)
        // Velocity is degrees per second - estimate from frame time
        var now = DateTime.UtcNow;
        var dt = (now - _lastFrameTime).TotalSeconds;
        if (dt > 0.001) // Avoid division by zero
        {
            // Smooth velocity update (weighted average)
            _velocityLongitude = _velocityLongitude * 0.5 + (deltaLon / dt) * 0.5;
            _velocityLatitude = _velocityLatitude * 0.5 + (deltaLat / dt) * 0.5;
        }

        // Clamp latitude to valid range
        _rotationLatitude = Math.Clamp(_rotationLatitude, -90, 90);

        // Wrap longitude
        while (_rotationLongitude > 180) _rotationLongitude -= 360;
        while (_rotationLongitude < -180) _rotationLongitude += 360;

        _lastMousePosition = currentPosition;
        _lastFrameTime = now;
        UpdateRender();
    }

    private void OnMouseLeave(object sender, WpfMouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Get click position relative to this control
        var clickPos = e.GetPosition(this);

        // Convert to normalized coordinates (-1 to 1)
        var normalizedX = (clickPos.X / ActualWidth) * 2 - 1;
        var normalizedY = 1 - (clickPos.Y / ActualHeight) * 2; // Flip Y

        // Check if click is within the globe circle (approximate)
        var distFromCenter = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
        if (distFromCenter > 0.8) // Outside globe
        {
            // Clear selection
            _selectedLatitude = null;
            _selectedLongitude = null;
            _selectedLocationName = null;
            _selectionTime = null;
            return;
        }

        // Calculate latitude and longitude from click position
        // This is a simplified projection - assumes orthographic view
        var clickLat = Math.Asin(Math.Clamp(normalizedY / 0.8, -1, 1)) * 180 / Math.PI;
        var cosLat = Math.Cos(clickLat * Math.PI / 180);
        var clickLon = cosLat > 0.01
            ? Math.Asin(Math.Clamp(normalizedX / 0.8 / cosLat, -1, 1)) * 180 / Math.PI
            : 0;

        // Adjust for current globe rotation
        _selectedLatitude = clickLat + _rotationLatitude;
        _selectedLongitude = clickLon + _rotationLongitude;

        // Clamp latitude
        _selectedLatitude = Math.Clamp(_selectedLatitude.Value, -90, 90);

        // Wrap longitude
        while (_selectedLongitude > 180) _selectedLongitude -= 360;
        while (_selectedLongitude < -180) _selectedLongitude += 360;

        // Try to find a nearby city name
        _selectedLocationName = FindNearestCityName(_selectedLatitude.Value, _selectedLongitude.Value);

        // Set selection time for fade animation
        _selectionTime = DateTime.UtcNow;

        e.Handled = true;
        UpdateRender();
    }

    private string? FindNearestCityName(double lat, double lon)
    {
        var cities = City.GetDefaultCities();
        City? nearest = null;
        double minDistance = 10; // Only match within 10 degrees

        foreach (var city in cities)
        {
            var dLat = city.Location.Latitude - lat;
            var dLon = city.Location.Longitude - lon;
            var distance = Math.Sqrt(dLat * dLat + dLon * dLon);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = city;
            }
        }

        return nearest?.Name;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Delta is typically +/-120 for one wheel notch
        var zoomDelta = -e.Delta / 120.0 * ZoomSensitivity;
        _currentZoom = Math.Clamp(_currentZoom + zoomDelta, MinZoom, MaxZoom);

        _renderer?.SetCameraZoom(_currentZoom);
        e.Handled = true;
        UpdateRender();
    }

    #endregion

    /// <summary>
    /// Starts the animation timers.
    /// </summary>
    public void Start()
    {
        if (!_disposed)
        {
            _updateTimer.Start();
            _animationTimer.Start();
        }
    }

    /// <summary>
    /// Stops the animation timers.
    /// </summary>
    public void Stop()
    {
        _updateTimer.Stop();
        _animationTimer.Stop();
    }

    /// <summary>
    /// Saves the current globe position and zoom to settings.
    /// </summary>
    public void SaveCurrentPosition()
    {
        var settings = ConfigurationManager.Current;
        settings.GlobeSavedLatitude = _rotationLatitude;
        settings.GlobeSavedLongitude = _rotationLongitude;
        settings.GlobeSavedZoom = _currentZoom;
        ConfigurationManager.UpdateSettings(settings, saveImmediately: true);
    }

    /// <summary>
    /// Gets the current globe rotation position.
    /// </summary>
    public (double Latitude, double Longitude) GetCurrentPosition()
    {
        return (_rotationLatitude, _rotationLongitude);
    }

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public double GetCurrentZoom() => _currentZoom;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
                _animationTimer.Stop();
                _animationTimer.Tick -= AnimationTimer_Tick;
                Loaded -= GlobeControl_Loaded;
                SizeChanged -= GlobeControl_SizeChanged;
                MouseLeftButtonDown -= OnMouseLeftButtonDown;
                MouseLeftButtonUp -= OnMouseLeftButtonUp;
                MouseMove -= OnMouseMove;
                MouseLeave -= OnMouseLeave;
                MouseRightButtonDown -= OnMouseRightButtonDown;
                MouseWheel -= OnMouseWheel;

                if (_renderer != null)
                {
                    var viewport = _renderer.GetViewport();
                    if (ViewportContainer.Children.Contains(viewport))
                    {
                        ViewportContainer.Children.Remove(viewport);
                    }
                }

                _renderer = null;
            }

            _disposed = true;
        }
    }
}
