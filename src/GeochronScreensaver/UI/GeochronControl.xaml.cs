using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering;
using GeochronScreensaver.Services;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace GeochronScreensaver.UI;

public partial class GeochronControl : WpfUserControl, IDisposable
{
    private GeochronRenderer _renderer;
    private readonly DispatcherTimer _updateTimer;
    private RenderTargetBitmap? _renderTarget;
    private bool _disposed;

    public GeochronControl()
    {
        InitializeComponent();

        var settings = Infrastructure.ConfigurationManager.Current;
        _renderer = new GeochronRenderer(settings);

        // Set up animation timer using settings value
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        Loaded += GeochronControl_Loaded;
        SizeChanged += GeochronControl_SizeChanged;
    }

    /// <summary>
    /// Updates the control to use new settings.
    /// </summary>
    public void UpdateSettings(Settings settings)
    {
        _renderer = new GeochronRenderer(settings);
        _updateTimer.Interval = TimeSpan.FromMilliseconds(settings.UpdateIntervalMs);

        // Update weather manager when settings change
        WeatherManager.Instance.OnSettingsChanged();

        UpdateRender();
    }

    private void GeochronControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Start weather manager if enabled
        WeatherManager.Instance.Start();

        UpdateRender();
        _updateTimer.Start();
    }

    private void GeochronControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateRender();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRender();
    }

    private void UpdateRender()
    {
        if (_disposed || ActualWidth <= 0 || ActualHeight <= 0) return;

        int width = (int)Math.Ceiling(ActualWidth);
        int height = (int)Math.Ceiling(ActualHeight);

        // Create or recreate render target if needed
        if (_renderTarget == null ||
            _renderTarget.PixelWidth != width ||
            _renderTarget.PixelHeight != height)
        {
            // Clear old bitmap before creating new one
            _renderTarget?.Clear();
            _renderTarget = new RenderTargetBitmap(
                width, height, 96, 96, PixelFormats.Pbgra32);
        }

        // Create drawing visual
        var drawingVisual = new DrawingVisual();
        using (var dc = drawingVisual.RenderOpen())
        {
            // Render the Geochron display
            _renderer.Render(dc, width, height, DateTime.UtcNow);
        }

        // Render to bitmap
        _renderTarget.Render(drawingVisual);

        // Display the result
        RenderImage.Source = _renderTarget;
    }

    public void Stop()
    {
        _updateTimer.Stop();
    }

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
                // Stop timer and unsubscribe from events
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
                Loaded -= GeochronControl_Loaded;
                SizeChanged -= GeochronControl_SizeChanged;

                // Clear render target
                _renderTarget?.Clear();
                _renderTarget = null;
            }

            _disposed = true;
        }
    }
}
