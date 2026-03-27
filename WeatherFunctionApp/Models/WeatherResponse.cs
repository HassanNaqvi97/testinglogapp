namespace WeatherFunctionApp.Models;

public sealed class WeatherResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public string Unit { get; set; } = "C";
    public DateTime RetrievedAtUtc { get; set; }
}
