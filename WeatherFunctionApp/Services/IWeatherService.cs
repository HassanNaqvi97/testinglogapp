using WeatherFunctionApp.Models;

namespace WeatherFunctionApp.Services;

public interface IWeatherService
{
    Task<WeatherResponse> GetWeatherAsync(WeatherMessage message, CancellationToken cancellationToken = default);
}
