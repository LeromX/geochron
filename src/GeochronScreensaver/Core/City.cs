namespace GeochronScreensaver.Core;

/// <summary>
/// Represents a city location for display on the map.
/// </summary>
public class City
{
    public string Name { get; set; }
    public GeoPoint Location { get; set; }
    public string TimeZone { get; set; }
    public int Population { get; set; }

    public City(string name, GeoPoint location, string timeZone, int population = 0)
    {
        Name = name;
        Location = location;
        TimeZone = timeZone;
        Population = population;
    }

    /// <summary>
    /// Get a default list of major world cities.
    /// </summary>
    public static List<City> GetDefaultCities()
    {
        return new List<City>
        {
            new City("London", new GeoPoint(51.5074, -0.1278), "Europe/London", 9000000),
            new City("New York", new GeoPoint(40.7128, -74.0060), "America/New_York", 8400000),
            new City("Tokyo", new GeoPoint(35.6762, 139.6503), "Asia/Tokyo", 14000000),
            new City("Sydney", new GeoPoint(-33.8688, 151.2093), "Australia/Sydney", 5300000),
            new City("Paris", new GeoPoint(48.8566, 2.3522), "Europe/Paris", 2200000),
            new City("Dubai", new GeoPoint(25.2048, 55.2708), "Asia/Dubai", 3400000),
            new City("Singapore", new GeoPoint(1.3521, 103.8198), "Asia/Singapore", 5700000),
            new City("Los Angeles", new GeoPoint(34.0522, -118.2437), "America/Los_Angeles", 4000000),
            new City("Moscow", new GeoPoint(55.7558, 37.6173), "Europe/Moscow", 12600000),
            new City("Beijing", new GeoPoint(39.9042, 116.4074), "Asia/Shanghai", 21500000),
            new City("Mumbai", new GeoPoint(19.0760, 72.8777), "Asia/Kolkata", 20400000),
            new City("São Paulo", new GeoPoint(-23.5505, -46.6333), "America/Sao_Paulo", 12300000),
            new City("Cairo", new GeoPoint(30.0444, 31.2357), "Africa/Cairo", 20900000),
            new City("Mexico City", new GeoPoint(19.4326, -99.1332), "America/Mexico_City", 21800000),
            new City("Hong Kong", new GeoPoint(22.3193, 114.1694), "Asia/Hong_Kong", 7500000),
            new City("Stockholm", new GeoPoint(59.3293, 18.0686), "Europe/Stockholm", 1600000),
        };
    }

    public override string ToString()
    {
        return $"{Name} ({Location})";
    }
}
