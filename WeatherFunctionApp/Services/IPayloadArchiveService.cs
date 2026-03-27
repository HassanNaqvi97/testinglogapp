namespace WeatherFunctionApp.Services;

public interface IPayloadArchiveService
{
    Task ArchiveRequestAsync(string correlationId, string payload, CancellationToken cancellationToken = default);
    Task ArchiveResponseAsync(string correlationId, string payload, CancellationToken cancellationToken = default);
}
