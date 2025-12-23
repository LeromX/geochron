using System;
using System.Windows;
using System.Windows.Media;
using GeochronScreensaver.Core;
using GeochronScreensaver.Rendering.Projections;
using WpfBrush = System.Windows.Media.Brush;
using WpfPen = System.Windows.Media.Pen;
using WpfColor = System.Windows.Media.Color;

namespace GeochronScreensaver.Rendering;

/// <summary>
/// Renders the base world map with continents and oceans.
/// </summary>
public class MapLayer
{
    private readonly IMapProjection _projection;
    private readonly WpfBrush _landBrush;
    private readonly WpfBrush _oceanBrush;
    private readonly WpfPen _coastlinePen;
    private readonly bool _showGridLines;

    public MapLayer(IMapProjection projection, bool showGridLines = true)
    {
        _projection = projection;
        _showGridLines = showGridLines;
        _landBrush = new SolidColorBrush(WpfColor.FromRgb(34, 139, 34)); // Forest green
        _oceanBrush = new SolidColorBrush(WpfColor.FromRgb(25, 25, 112)); // Midnight blue
        _coastlinePen = new WpfPen(new SolidColorBrush(WpfColor.FromRgb(100, 100, 100)), 0.5);

        _landBrush.Freeze();
        _oceanBrush.Freeze();
        _coastlinePen.Freeze();
    }

    /// <summary>
    /// Render the world map to a drawing context.
    /// </summary>
    public void Render(DrawingContext dc, double width, double height)
    {
        // Fill ocean background
        dc.DrawRectangle(_oceanBrush, null, new Rect(0, 0, width, height));

        // Draw simplified continents
        DrawContinents(dc, width, height);

        // Draw grid lines if enabled
        if (_showGridLines)
        {
            DrawGridLines(dc, width, height);
        }
    }

    private void DrawContinents(DrawingContext dc, double width, double height)
    {
        // Simplified continent shapes with corrected geographic coordinates
        // GeoPoint(latitude, longitude) - lat: -90 to +90, lon: -180 to +180

        // Africa - corrected coordinates
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(37, -10),   // Morocco (Tangier)
            new GeoPoint(32, -5),    // Morocco coast
            new GeoPoint(36, 10),    // Tunisia
            new GeoPoint(32, 32),    // Egypt (Alexandria)
            new GeoPoint(12, 44),    // Horn of Africa (Djibouti)
            new GeoPoint(-12, 44),   // Tanzania coast
            new GeoPoint(-26, 33),   // South Africa (east coast)
            new GeoPoint(-35, 20),   // Cape of Good Hope
            new GeoPoint(-34, 18),   // Cape Town
            new GeoPoint(-17, 12),   // Angola coast
            new GeoPoint(5, 10),     // Gulf of Guinea
            new GeoPoint(5, -5),     // Ivory Coast
            new GeoPoint(15, -17),   // Senegal
            new GeoPoint(37, -10)    // Close polygon
        });

        // Europe (separate from Asia for better shape)
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(71, 28),    // Norway (North Cape)
            new GeoPoint(70, 25),    // Northern Norway
            new GeoPoint(64, 10),    // Norwegian coast (Bergen area)
            new GeoPoint(58, 6),     // Southern Norway
            new GeoPoint(55, 8),     // Denmark (Jutland)
            new GeoPoint(54, -8),    // Ireland
            new GeoPoint(50, -5),    // Cornwall
            new GeoPoint(44, -9),    // Portugal
            new GeoPoint(36, -6),    // Gibraltar
            new GeoPoint(36, -5),    // Southern Spain
            new GeoPoint(43, 5),     // Southern France
            new GeoPoint(44, 12),    // Italy (west)
            new GeoPoint(40, 18),    // Italy (heel)
            new GeoPoint(38, 24),    // Greece
            new GeoPoint(41, 29),    // Turkey (Istanbul)
            new GeoPoint(46, 30),    // Ukraine (Odessa)
            new GeoPoint(55, 20),    // Poland coast
            new GeoPoint(56, 10),    // Denmark
            new GeoPoint(60, 18),    // Sweden (Stockholm area)
            new GeoPoint(66, 24),    // Northern Sweden
            new GeoPoint(71, 28)     // Close polygon
        });

        // Asia (main continental mass)
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(77, 68),    // Siberia (Taymyr Peninsula)
            new GeoPoint(75, 100),   // Northern Siberia
            new GeoPoint(70, 140),   // Northeastern Siberia
            new GeoPoint(65, 170),   // Chukotka
            new GeoPoint(60, 165),   // Kamchatka area
            new GeoPoint(45, 140),   // Vladivostok area
            new GeoPoint(35, 130),   // Korea
            new GeoPoint(22, 120),   // Southern China
            new GeoPoint(10, 105),   // Vietnam
            new GeoPoint(1, 103),    // Singapore area
            new GeoPoint(8, 77),     // India (southern tip)
            new GeoPoint(22, 88),    // Bangladesh
            new GeoPoint(22, 70),    // India (west)
            new GeoPoint(25, 62),    // Pakistan
            new GeoPoint(30, 48),    // Iran
            new GeoPoint(42, 53),    // Kazakhstan
            new GeoPoint(55, 55),    // Ural region
            new GeoPoint(68, 66),    // Northern Russia
            new GeoPoint(77, 68)     // Close polygon
        });

        // North America - corrected coordinates
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(72, -170),  // Alaska (north)
            new GeoPoint(71, -155),  // Alaska (Barrow)
            new GeoPoint(70, -130),  // Northern Canada
            new GeoPoint(75, -95),   // Canadian Arctic
            new GeoPoint(70, -70),   // Baffin Island
            new GeoPoint(60, -65),   // Labrador
            new GeoPoint(47, -53),   // Newfoundland
            new GeoPoint(43, -70),   // New England
            new GeoPoint(25, -80),   // Florida
            new GeoPoint(20, -97),   // Gulf of Mexico
            new GeoPoint(18, -105),  // Mexico
            new GeoPoint(23, -110),  // Baja California
            new GeoPoint(32, -117),  // San Diego
            new GeoPoint(48, -125),  // Pacific Northwest
            new GeoPoint(60, -145),  // Alaska (south)
            new GeoPoint(63, -165),  // Alaska (west)
            new GeoPoint(72, -170)   // Close polygon
        });

        // Central America and Caribbean (simplified)
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(22, -98),   // Mexico (Yucatan)
            new GeoPoint(15, -88),   // Belize/Guatemala
            new GeoPoint(8, -77),    // Panama
            new GeoPoint(10, -84),   // Costa Rica
            new GeoPoint(14, -91),   // Guatemala
            new GeoPoint(22, -98)    // Close polygon
        });

        // South America - corrected coordinates
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(12, -72),   // Venezuela
            new GeoPoint(5, -60),    // Guyana coast
            new GeoPoint(-5, -35),   // Brazil (easternmost)
            new GeoPoint(-23, -43),  // Rio de Janeiro
            new GeoPoint(-34, -58),  // Buenos Aires
            new GeoPoint(-52, -70),  // Patagonia
            new GeoPoint(-56, -68),  // Tierra del Fuego
            new GeoPoint(-46, -75),  // Chile (south)
            new GeoPoint(-33, -72),  // Chile (Santiago)
            new GeoPoint(-18, -70),  // Chile/Peru
            new GeoPoint(-5, -81),   // Peru
            new GeoPoint(0, -80),    // Ecuador
            new GeoPoint(8, -77),    // Colombia
            new GeoPoint(12, -72)    // Close polygon
        });

        // Australia - corrected coordinates
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(-12, 130),  // Darwin area
            new GeoPoint(-12, 142),  // Gulf of Carpentaria
            new GeoPoint(-17, 146),  // Queensland
            new GeoPoint(-28, 154),  // Brisbane area
            new GeoPoint(-38, 145),  // Melbourne area
            new GeoPoint(-35, 138),  // Adelaide
            new GeoPoint(-32, 115),  // Perth
            new GeoPoint(-22, 114),  // Western Australia
            new GeoPoint(-12, 130)   // Close polygon
        });

        // Greenland
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(83, -34),   // Northern Greenland
            new GeoPoint(77, -18),   // Northeast
            new GeoPoint(70, -22),   // East coast
            new GeoPoint(60, -43),   // Southern tip
            new GeoPoint(65, -53),   // Southwest
            new GeoPoint(76, -68),   // Northwest
            new GeoPoint(83, -34)    // Close polygon
        });

        // Antarctica (simplified band)
        DrawSimpleContinent(dc, width, height, new[]
        {
            new GeoPoint(-65, -180),
            new GeoPoint(-65, 180),
            new GeoPoint(-90, 180),
            new GeoPoint(-90, -180),
            new GeoPoint(-65, -180)
        });
    }

    private void DrawSimpleContinent(DrawingContext dc, double width, double height, GeoPoint[] points)
    {
        if (points.Length < 3) return;

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();

        var startPoint = _projection.ProjectToScreen(points[0], width, height);
        pathFigure.StartPoint = startPoint;

        for (int i = 1; i < points.Length; i++)
        {
            var point = _projection.ProjectToScreen(points[i], width, height);
            pathFigure.Segments.Add(new LineSegment(point, true));
        }

        pathFigure.IsClosed = true;
        pathGeometry.Figures.Add(pathFigure);
        pathGeometry.Freeze();

        dc.DrawGeometry(_landBrush, _coastlinePen, pathGeometry);
    }

    private void DrawGridLines(DrawingContext dc, double width, double height)
    {
        var gridPen = new WpfPen(new SolidColorBrush(WpfColor.FromArgb(40, 255, 255, 255)), 0.5);
        gridPen.Freeze();

        // Draw latitude lines every 30 degrees
        for (double lat = -60; lat <= 60; lat += 30)
        {
            var p1 = _projection.ProjectToScreen(new GeoPoint(lat, -180), width, height);
            var p2 = _projection.ProjectToScreen(new GeoPoint(lat, 180), width, height);
            dc.DrawLine(gridPen, p1, p2);
        }

        // Draw longitude lines every 30 degrees
        for (double lon = -180; lon <= 150; lon += 30)
        {
            var p1 = _projection.ProjectToScreen(new GeoPoint(90, lon), width, height);
            var p2 = _projection.ProjectToScreen(new GeoPoint(-90, lon), width, height);
            dc.DrawLine(gridPen, p1, p2);
        }

        // Draw equator and prime meridian more prominently
        var prominentPen = new WpfPen(new SolidColorBrush(WpfColor.FromArgb(80, 255, 255, 255)), 1.0);
        prominentPen.Freeze();

        var eq1 = _projection.ProjectToScreen(new GeoPoint(0, -180), width, height);
        var eq2 = _projection.ProjectToScreen(new GeoPoint(0, 180), width, height);
        dc.DrawLine(prominentPen, eq1, eq2);

        var pm1 = _projection.ProjectToScreen(new GeoPoint(90, 0), width, height);
        var pm2 = _projection.ProjectToScreen(new GeoPoint(-90, 0), width, height);
        dc.DrawLine(prominentPen, pm1, pm2);
    }
}
