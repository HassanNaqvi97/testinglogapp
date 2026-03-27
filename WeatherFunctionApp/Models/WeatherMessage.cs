namespace WeatherFunctionApp.Models;

public sealed class WeatherMessage
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public WeatherRequest Request { get; set; } = new();
}
