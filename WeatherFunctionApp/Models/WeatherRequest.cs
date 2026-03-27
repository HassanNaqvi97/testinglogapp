namespace WeatherFunctionApp.Models;

public sealed class WeatherRequest
{
    public string City { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string Unit { get; set; } = "C";
}
