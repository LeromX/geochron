using System;
using System.IO;
using System.Windows;
using GeochronScreensaver.Infrastructure;

namespace GeochronScreensaver;

public static class Program
{
    private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "geochron_debug.log");

    private static void DebugLog(string message)
    {
        try { File.AppendAllText(DebugLogPath, $"{DateTime.Now:HH:mm:ss.fff} - {message}\n"); } catch { }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        DebugLog($"=== Program.Main started === Args: {string.Join(", ", args)}");
        var mode = ScreensaverMode.Application; // Default to application mode
        IntPtr previewHandle = IntPtr.Zero;

        // Parse command line arguments
        if (args.Length > 0)
        {
            var command = args[0].ToLower().Trim();

            if (command.StartsWith("/s"))
            {
                mode = ScreensaverMode.Screensaver;
            }
            else if (command.StartsWith("/c"))
            {
                mode = ScreensaverMode.Configure;
            }
            else if (command.StartsWith("/p"))
            {
                mode = ScreensaverMode.Preview;
                if (args.Length > 1 && IntPtr.TryParse(args[1], out var handle))
                {
                    previewHandle = handle;
                }
            }
            else if (command.StartsWith("/app"))
            {
                mode = ScreensaverMode.Application;
            }
        }

        var app = new App();

        switch (mode)
        {
            case ScreensaverMode.Screensaver:
                RunMultiMonitorScreensaver(app);
                break;

            case ScreensaverMode.Preview:
                if (previewHandle != IntPtr.Zero)
                {
                    var previewWindow = new UI.ScreensaverWindow(previewHandle);
                    app.Run(previewWindow);
                }
                break;

            case ScreensaverMode.Configure:
                var configDialog = new UI.ConfigurationDialog();
                app.Run(configDialog);
                break;

            case ScreensaverMode.Application:
            default:
                // Run as standalone application with windowed UI
                app.Run(new UI.MainWindow());
                break;
        }
    }

    private static void RunMultiMonitorScreensaver(App app)
    {
        var windows = DisplayManager.CreateFullscreenWindows();

        if (windows.Count == 0)
        {
            // Fallback to single window if no screens detected
            app.Run(new UI.ScreensaverWindow());
            return;
        }

        // Set up cleanup handler for when the main window closes
        var mainWindow = windows[0];
        mainWindow.Closed += (s, e) =>
        {
            // Close all other windows from the main window's Closed event
            // This happens within the normal WPF lifecycle, avoiding race conditions
            foreach (var window in windows)
            {
                if (window != mainWindow && !window.IsClosed())
                {
                    window.Close();
                }
            }
        };

        // Show all windows
        DisplayManager.ShowAllWindows(windows);

        // Run the application with the first window as the main window
        app.Run(mainWindow);
    }
}

public enum ScreensaverMode
{
    Application,
    Screensaver,
    Configure,
    Preview
}
