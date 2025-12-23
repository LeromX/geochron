using System;
using System.Collections.Generic;
using System.Windows;
using GeochronScreensaver.UI;
using WinFormsScreen = System.Windows.Forms.Screen;

namespace GeochronScreensaver.Infrastructure;

/// <summary>
/// Manages display of screensaver across multiple monitors.
/// </summary>
public static class DisplayManager
{
    /// <summary>
    /// Creates and shows screensaver windows for all monitors.
    /// </summary>
    public static List<ScreensaverWindow> CreateFullscreenWindows()
    {
        var windows = new List<ScreensaverWindow>();

        foreach (var screen in WinFormsScreen.AllScreens)
        {
            var window = new ScreensaverWindow();

            // Position window to cover this screen
            window.Left = screen.Bounds.Left;
            window.Top = screen.Bounds.Top;
            window.Width = screen.Bounds.Width;
            window.Height = screen.Bounds.Height;

            // Ensure it's on top and fullscreen
            window.WindowStyle = WindowStyle.None;
            window.WindowState = WindowState.Normal; // Use Normal with explicit bounds instead of Maximized
            window.Topmost = true;
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;

            windows.Add(window);
        }

        return windows;
    }

    /// <summary>
    /// Shows all windows. The first window is shown with Show(), others with Show() to avoid blocking.
    /// </summary>
    public static void ShowAllWindows(List<ScreensaverWindow> windows)
    {
        if (windows.Count == 0) return;

        // Show all windows
        foreach (var window in windows)
        {
            window.Show();
        }
    }
}
