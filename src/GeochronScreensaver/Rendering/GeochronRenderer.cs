using System;
using System.Windows;
using System.Windows.Media;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering.Projections;
using GeochronScreensaver.Rendering.Sources;
using GeochronScreensaver.Services;
using WpfPen = System.Windows.Media.Pen;
using WpfBrush = System.Windows.Media.Brush;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace GeochronScreensaver.Rendering;

/// <summary>
/// Composites all rendering layers to create the final Geochron display.
/// </summary>
public class GeochronRenderer
{
    private readonly MapLayer _mapLayer;
    private readonly TerminatorLayer _terminatorLayer;
    private readonly SatelliteMapLayer? _satelliteMapLayer;
    private readonly IMapProjection _projection;
    private readonly Settings _settings;

    // Cached rendering resources to prevent per-frame allocations
    private readonly SolidColorBrush _cityDayBrush;
    private readonly SolidColorBrush _cityNightBrush;
    private readonly SolidColorBrush _textBrush;
    private readonly SolidColorBrush _infoBgBrush;
    private readonly SolidColorBrush _sunBrush;
    private readonly WpfPen _sunPen;
    private readonly Typeface _cityTypeface;
    private readonly Typeface _infoTypeface;
    private readonly City[] _cities;

    public GeochronRenderer(Settings settings)
    {
        _settings = settings;

        // Create projection based on settings
        _projection = settings.ProjectionType switch
        {
            MapProjectionType.Mercator => new MercatorProjection(),
            MapProjectionType.Equirectangular => new EquirectangularProjection(),
            _ => new EquirectangularProjection()
        };

        _mapLayer = new MapLayer(_projection, settings.ShowGridLines);
        _terminatorLayer = new TerminatorLayer(
            _projection,
            settings.TerminatorTransitionWidth,
            settings.NightOverlayOpacity);

        // Initialize satellite map layer if satellite mode is enabled
        if (settings.MapStyle == MapStyleType.Satellite)
        {
            var mapSource = new SatelliteMapSource();
            _satelliteMapLayer = new SatelliteMapLayer(
                mapSource,
                _projection,
                settings.TerminatorTransitionWidth);
        }

        // Initialize cached rendering resources
        _cityDayBrush = new SolidColorBrush(WpfColor.FromRgb(255, 215, 0)); // Gold
        _cityNightBrush = new SolidColorBrush(WpfColor.FromRgb(200, 200, 255)); // Light blue
        _textBrush = new SolidColorBrush(Colors.White);
        _infoBgBrush = new SolidColorBrush(WpfColor.FromArgb(180, 0, 0, 0));
        _sunBrush = new SolidColorBrush(WpfColor.FromRgb(255, 255, 0)); // Yellow
        _sunPen = new WpfPen(new SolidColorBrush(WpfColor.FromRgb(255, 165, 0)), 2); // Orange

        // Freeze brushes and pens for better performance and thread safety
        _cityDayBrush.Freeze();
        _cityNightBrush.Freeze();
        _textBrush.Freeze();
        _infoBgBrush.Freeze();
        _sunBrush.Freeze();
        _sunPen.Freeze();

        _cityTypeface = new Typeface("Segoe UI");
        _infoTypeface = new Typeface("Consolas");

        // Cache city list
        _cities = City.GetDefaultCities().ToArray();
    }

    /// <summary>
    /// Render the complete Geochron display.
    /// </summary>
    public void Render(DrawingContext dc, double width, double height, DateTime utcTime)
    {
        if (width <= 0 || height <= 0) return;

        // Calculate sun position
        var sunPosition = SolarCalculator.GetSunPosition(utcTime);

        // Render map based on style
        if (_settings.MapStyle == MapStyleType.Satellite && _satelliteMapLayer != null)
        {
            // Satellite mode: render blended day/night satellite imagery
            _satelliteMapLayer.Render(dc, width, height, sunPosition);
        }
        else
        {
            // Simplified mode: render base map with terminator overlay
            _mapLayer.Render(dc, width, height);
            _terminatorLayer.Render(dc, width, height, sunPosition);
        }

        // Draw cities if enabled
        if (_settings.ShowCities)
        {
            DrawCities(dc, width, height, sunPosition);
        }

        // Draw information overlay
        DrawInformationOverlay(dc, width, height, sunPosition);
    }

    private void DrawCities(DrawingContext dc, double width, double height, SunPosition sunPosition)
    {
        foreach (var city in _cities)
        {
            var screenPos = _projection.ProjectToScreen(city.Location, width, height);

            // Check if city is in daylight
            bool isDay = SolarCalculator.IsInDaylight(city.Location, sunPosition);
            var brush = isDay ? _cityDayBrush : _cityNightBrush;

            // Draw city marker
            dc.DrawEllipse(brush, null, screenPos, 3, 3);

            // Draw city name
            var formattedText = new FormattedText(
                city.Name,
                System.Globalization.CultureInfo.CurrentCulture,
                WpfFlowDirection.LeftToRight,
                _cityTypeface,
                10,
                _textBrush,
                1.0); // PixelsPerDip

            dc.DrawText(formattedText, new WpfPoint(screenPos.X + 5, screenPos.Y - 5));
        }
    }

    private void DrawInformationOverlay(DrawingContext dc, double width, double height, SunPosition sunPosition)
    {
        // Format information text
        var infoText = $"UTC: {sunPosition.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                      $"Sub-solar point: {sunPosition.SubSolarPoint}\n" +
                      $"Solar declination: {sunPosition.Declination:F2}°";

        var formattedText = new FormattedText(
            infoText,
            System.Globalization.CultureInfo.CurrentCulture,
            WpfFlowDirection.LeftToRight,
            _infoTypeface,
            12,
            _textBrush,
            1.0); // PixelsPerDip

        // Draw background
        var textRect = new Rect(10, height - formattedText.Height - 20,
            formattedText.Width + 20, formattedText.Height + 20);
        dc.DrawRectangle(_infoBgBrush, null, textRect);

        // Draw text
        dc.DrawText(formattedText, new WpfPoint(20, height - formattedText.Height - 10));

        // Draw sub-solar point marker
        var subSolarScreen = _projection.ProjectToScreen(sunPosition.SubSolarPoint, width, height);
        dc.DrawEllipse(_sunBrush, _sunPen, subSolarScreen, 8, 8);
    }
}
