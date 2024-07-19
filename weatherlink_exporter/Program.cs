using Prometheus;

Gauge WlWindSpeed = Metrics.CreateGauge("wl_wind_speed", "Wind speed in km/h");
Gauge WlWindDirection = Metrics.CreateGauge("wl_wind_direction", "Wind direction in degrees azimuth");
Gauge WlHumidity = Metrics.CreateGauge("wl_humidity", "Relative humidity 0 to 100 percent");
Gauge WlTemperature = Metrics.CreateGauge("wl_temperature", "Air temperature in degrees C");
Gauge WlWetBulb = Metrics.CreateGauge("wl_wet_bulb", "Wet bulb temperature in degrees C");
Gauge WlDewPoint = Metrics.CreateGauge("wl_dew_point", "Dew point temperature in degrees C");
Gauge WlRelativePressure = Metrics.CreateGauge("wl_rel_pressure", "Relative pressure in mbar");
Gauge WlAbsolutePressure = Metrics.CreateGauge("wl_abs_pressure", "Absolute pressure in mbar");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DataScraper>();
builder.Services.AddMetricServer(options =>
{
    options.Port = 9100;
});

var app = builder.Build();

Metrics.SuppressDefaultMetrics();
Metrics.DefaultRegistry.AddBeforeCollectCallback(async (cancel) =>
{
    var scraper = app.Services.GetRequiredService<DataScraper>();
    var rawData = await scraper.GetWeatherLinkData();
    var data = ConvertData(rawData);

    WlWindSpeed.Set(data.WindSpeedKph);
    WlWindDirection.Set(data.WindDirection);
    WlHumidity.Set(data.Humidity);
    WlTemperature.Set(data.TemperatureC);
    WlWetBulb.Set(data.WetBulbC);
    WlDewPoint.Set(data.DewPointC);
    WlRelativePressure.Set(data.RelativePressureMbar);
    WlAbsolutePressure.Set(data.AbsolutePressureMbar);
});

app.MapGet("/raw", async (HttpContext httpContext) =>
{
    var scraper = httpContext.RequestServices.GetRequiredService<DataScraper>();
    var data = await scraper.GetWeatherLinkData();
    return Results.Json(data);
});

app.MapGet("/weather", async (HttpContext httpContext) =>
{
    var scraper = httpContext.RequestServices.GetRequiredService<DataScraper>();
    var rawData = await scraper.GetWeatherLinkData();
    var weatherData = ConvertData(rawData);
    return Results.Json(weatherData);
});

app.Run();

static WeatherData ConvertData(WeatherLinkData data)
{
    return new WeatherData
    {
        TimeStampUtc = data.LastReceivedUtc.UtcDateTime,
        WindSpeedKph= GetItem("Wind Speed") is null ? 9999 : (GetItem("Wind Speed")!.Value * 1.60934),
        WindDirection = GetItem("Wind Direction")?.Value ?? 9999,
        Humidity = GetItem("Hum")?.Value ?? 9999,
        TemperatureC = GetItem("Temp") is null ? 9999 : ((GetItem("Temp")!.Value - 32) * 5.0/9.0),
        WetBulbC = GetItem("Wet Bulb") is null ? 9999 : ((GetItem("Wet Bulb")!.Value - 32) * 5.0 / 9.0),
        DewPointC = GetItem("Dew Point") is null ? 9999 : ((GetItem("Dew Point")!.Value - 32) * 5.0 / 9.0),
        RelativePressureMbar = GetItem("Barometer") is null ? 9999 : (GetItem("Barometer")!.Value * 33.8639),
        AbsolutePressureMbar = GetItem("Absolute Pressure") is null ? 9999 : (GetItem("Absolute Pressure")!.Value * 33.8639),
    };

    WeatherLinkItem? GetItem(string name) => data.CurrConditionValues.FirstOrDefault(x => x.SensorDataName == name);
}

class DataScraper
{
    private readonly string _url;
    private readonly HttpClient _client;

    public DataScraper(
        IConfiguration configuration,
        HttpClient client)
    {
        _url = configuration.GetValue<string>("WeatherLinkUrl") ?? throw new ArgumentNullException("WeatherLinkUrl not configured");
        _client = client;
    }

    public async Task<WeatherLinkData> GetWeatherLinkData()
    {
        var data = await _client.GetFromJsonAsync<WeatherLinkData>(_url);
        return data!;
    }
}

internal record WeatherLinkData
{
    public required string OwnerName { get; init; }
    public required long LastReceived { get; init; }
    public DateTimeOffset LastReceivedUtc => DateTimeOffset.FromUnixTimeMilliseconds(LastReceived);
    public List<WeatherLinkItem> CurrConditionValues { get; init; } = new();
}

internal record WeatherLinkItem
{
    public required int? SensorDataTypeId { get; init; }
    public required string SensorDataName { get; init; }
    public required string? DisplayName { get; init; }
    public required double ReportedValue { get; init; }
    public required double Value { get; init; }
    public required string ConvertedValue { get; init; }
    public required string? DepthLabel { get; init; }
    public required string? Category { get; init; }
    public required int? AssocSensorDataTypeId { get; init; }
    public required int? SortOrder { get; init; }
    public required string UnitLabel { get; init; }
}

internal record WeatherData
{
    public required DateTime TimeStampUtc { get; init; }
    public required double WindDirection { get; init; }
    public required double WindSpeedKph { get; init; }
    public required double Humidity { get; init; }
    public required double TemperatureC { get; init; }
    public required double WetBulbC { get; init; }
    public required double DewPointC { get; init; }
    public required double RelativePressureMbar { get; init; }
    public required double AbsolutePressureMbar { get; init; }
}