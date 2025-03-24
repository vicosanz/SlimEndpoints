namespace WebApplication1.Endpoints;

public record WeatherForecast(int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
