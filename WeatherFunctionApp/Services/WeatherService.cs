using WeatherFunctionApp.Models;

namespace WeatherFunctionApp.Services;

public sealed class WeatherService : IWeatherService
{
    private static readonly string[] Conditions = ["Sunny", "Cloudy", "Rain", "Storm", "Windy", "Snow"];

    public Task<WeatherResponse> GetWeatherAsync(WeatherMessage message, CancellationToken cancellationToken = default)
    {
        // Mocked weather generation. Replace with an actual weather API client when needed.
        var seed = HashCode.Combine(message.Request.City, message.Request.CountryCode, DateTime.UtcNow.Hour);
        var random = new Random(seed);

        var response = new WeatherResponse
        {
            CorrelationId = message.CorrelationId,
            City = message.Request.City,
            Condition = Conditions[random.Next(0, Conditions.Length)],
            Temperature = Math.Round((decimal)(random.NextDouble() * 35), 1),
            Unit = message.Request.Unit,
            RetrievedAtUtc = DateTime.UtcNow
        };

        return Task.FromResult(response);
    }
}
