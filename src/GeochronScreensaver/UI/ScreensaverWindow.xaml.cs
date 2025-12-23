using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using GeochronScreensaver.Core;
using GeochronScreensaver.Infrastructure;
using WpfApplication = System.Windows.Application;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfPoint = System.Windows.Point;
using WpfMouse = System.Windows.Input.Mouse;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace GeochronScreensaver.UI;

public partial class ScreensaverWindow : Window
{
    private WpfPoint? _mousePosition;
    private readonly bool _isPreviewMode;
    private bool _isClosed = false;
    private GeochronControl? _geochronDisplay;
    private GlobeControl? _globeDisplay;
    private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "geochron_debug.log");

    public ScreensaverWindow(IntPtr previewHandle = default)
    {
        try
        {
            DebugLog("ScreensaverWindow: Constructor started");
            InitializeComponent();
            DebugLog("ScreensaverWindow: InitializeComponent completed");

            // Create the appropriate display control based on settings
            var settings = ConfigurationManager.Current;
            DebugLog($"ScreensaverWindow: Settings loaded, RenderMode={settings.RenderMode}");

            if (settings.RenderMode == RenderModeType.Globe3D)
            {
                DebugLog("ScreensaverWindow: Creating GlobeControl with saved position");
                _globeDisplay = new GlobeControl(restoreSavedPosition: true);
                DebugLog("ScreensaverWindow: GlobeControl created, setting as DisplayContainer.Content");
                DisplayContainer.Content = _globeDisplay;
                DebugLog($"ScreensaverWindow: DisplayContainer.Content set. HasContent={DisplayContainer.Content != null}");
            }
            else
            {
                DebugLog("ScreensaverWindow: Creating GeochronControl");
                _geochronDisplay = new GeochronControl();
                DisplayContainer.Content = _geochronDisplay;
            }

            if (previewHandle != IntPtr.Zero)
            {
                _isPreviewMode = true;
                DebugLog("ScreensaverWindow: Setting up preview mode");
                SetupPreviewMode(previewHandle);
            }
            else
            {
                DebugLog("ScreensaverWindow: Setting up screensaver mode");
                SetupScreensaverMode();
            }

            Loaded += ScreensaverWindow_Loaded;
            SizeChanged += ScreensaverWindow_SizeChanged;
            DebugLog("ScreensaverWindow: Constructor completed");
        }
        catch (Exception ex)
        {
            DebugLog($"EXCEPTION in ScreensaverWindow constructor: {ex}");
            throw;
        }
    }

    private void ScreensaverWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DebugLog($"ScreensaverWindow_Loaded: Window size={ActualWidth}x{ActualHeight}");
        DebugLog($"ScreensaverWindow_Loaded: DisplayContainer size={DisplayContainer.ActualWidth}x{DisplayContainer.ActualHeight}");
        DebugLog($"ScreensaverWindow_Loaded: DisplayContainer.Content={(DisplayContainer.Content != null ? DisplayContainer.Content.GetType().Name : "NULL")}");
    }

    private void ScreensaverWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DebugLog($"ScreensaverWindow size changed: {e.NewSize.Width}x{e.NewSize.Height}");
    }

    private static void DebugLog(string message)
    {
        try
        {
            File.AppendAllText(DebugLogPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    private void SetupPreviewMode(IntPtr previewHandle)
    {
        // Set up for preview in display settings
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Normal;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        Loaded += (s, e) =>
        {
            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;

            // Make this a child window of the preview pane
            NativeMethods.SetParent(hwnd, previewHandle);

            // Set window style to child
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE,
                new IntPtr(style | NativeMethods.WS_CHILD));

            // Get preview window size and position
            if (NativeMethods.GetClientRect(previewHandle, out var rect))
            {
                Left = 0;
                Top = 0;
                Width = rect.Right - rect.Left;
                Height = rect.Bottom - rect.Top;
            }
        };
    }

    private void SetupScreensaverMode()
    {
        // Full screen screensaver mode
        MouseMove += OnMouseMove;
        MouseDown += OnMouseActivity;
        KeyDown += OnKeyActivity;

        // Capture initial mouse position after a short delay
        Loaded += (s, e) =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _mousePosition = WpfMouse.GetPosition(this);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        };
    }

    private void OnMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_mousePosition.HasValue)
        {
            _mousePosition = e.GetPosition(this);
            return;
        }

        var currentPosition = e.GetPosition(this);
        var distance = Math.Abs(currentPosition.X - _mousePosition.Value.X) +
                      Math.Abs(currentPosition.Y - _mousePosition.Value.Y);

        // Exit if mouse moved more than 10 pixels
        if (distance > 10)
        {
            ExitScreensaver();
        }
    }

    private void OnMouseActivity(object sender, WpfMouseButtonEventArgs e)
    {
        ExitScreensaver();
    }

    private void OnKeyActivity(object sender, WpfKeyEventArgs e)
    {
        ExitScreensaver();
    }

    private void ExitScreensaver()
    {
        if (!_isPreviewMode)
        {
            _geochronDisplay?.Stop();
            _globeDisplay?.Stop();
            WpfApplication.Current.Shutdown();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosed = true;
        _geochronDisplay?.Stop();
        _globeDisplay?.Stop();
        base.OnClosed(e);
    }

    public bool IsClosed()
    {
        return _isClosed;
    }
}
