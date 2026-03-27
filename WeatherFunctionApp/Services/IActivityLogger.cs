namespace WeatherFunctionApp.Services;

public interface IActivityLogger
{
    Task LogMethodStartAsync(string correlationId, string methodName, string details, CancellationToken cancellationToken = default);
    Task LogMethodEndAsync(string correlationId, string methodName, string details, CancellationToken cancellationToken = default);
    Task LogMethodTransitionAsync(string correlationId, string fromMethod, string toMethod, string details, CancellationToken cancellationToken = default);
    Task LogErrorAsync(string correlationId, string methodName, string details, Exception exception, CancellationToken cancellationToken = default);
}
