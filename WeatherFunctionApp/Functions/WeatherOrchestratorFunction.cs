using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherFunctionApp.Models;
using WeatherFunctionApp.Services;

namespace WeatherFunctionApp.Functions;

public sealed class WeatherOrchestratorFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IActivityLogger _activityLogger;
    private readonly IPayloadArchiveService _payloadArchiveService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherOrchestratorFunction> _logger;
    private readonly string _queueName;

    public WeatherOrchestratorFunction(
        ServiceBusClient serviceBusClient,
        IActivityLogger activityLogger,
        IPayloadArchiveService payloadArchiveService,
        IWeatherService weatherService,
        ILogger<WeatherOrchestratorFunction> logger)
    {
        _serviceBusClient = serviceBusClient;
        _activityLogger = activityLogger;
        _payloadArchiveService = payloadArchiveService;
        _weatherService = weatherService;
        _logger = logger;
        _queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName") ?? "weather-requests";
    }

    [Function("GetWeather")]
    public async Task<HttpResponseData> GetWeatherAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "weather/request")] HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        var correlationId = context.InvocationId;
        const string methodName = nameof(GetWeatherAsync);
        await _activityLogger.LogMethodStartAsync(correlationId, methodName, "HTTP request received.", cancellationToken);

        try
        {
            var payload = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            await _payloadArchiveService.ArchiveRequestAsync(correlationId, payload, cancellationToken);

            var weatherRequest = JsonSerializer.Deserialize<WeatherRequest>(payload, JsonOptions);
            if (weatherRequest is null || string.IsNullOrWhiteSpace(weatherRequest.City))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body must contain a valid city.", cancellationToken);
                await _activityLogger.LogMethodEndAsync(correlationId, methodName, "Bad request returned.", cancellationToken);
                return badRequest;
            }

            var message = new WeatherMessage
            {
                CorrelationId = correlationId,
                Request = weatherRequest,
                RequestedAtUtc = DateTime.UtcNow
            };

            await using var sender = _serviceBusClient.CreateSender(_queueName);
            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(message, JsonOptions))
            {
                CorrelationId = correlationId,
                MessageId = correlationId,
                Subject = "WeatherRequest"
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            await _activityLogger.LogMethodTransitionAsync(correlationId, methodName, nameof(ProcessWeatherQueueAsync), "Message sent to Service Bus.", cancellationToken);

            var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);
            await acceptedResponse.WriteStringAsync($"Request accepted. CorrelationId={correlationId}", cancellationToken);
            await _activityLogger.LogMethodEndAsync(correlationId, methodName, "HTTP request completed.", cancellationToken);
            return acceptedResponse;
        }
        catch (Exception ex)
        {
            await _activityLogger.LogErrorAsync(correlationId, methodName, "Unexpected error during HTTP handling.", ex, cancellationToken);
            _logger.LogError(ex, "Failed to process weather HTTP request. CorrelationId={CorrelationId}", correlationId);
            throw;
        }
    }

    [Function(nameof(ProcessWeatherQueueAsync))]
    [FixedDelayRetry(3, "00:00:10")]
    public async Task ProcessWeatherQueueAsync(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage serviceBusMessage,
        CancellationToken cancellationToken)
    {
        var correlationId = serviceBusMessage.CorrelationId ?? serviceBusMessage.MessageId ?? Guid.NewGuid().ToString("N");
        const string methodName = nameof(ProcessWeatherQueueAsync);

        await _activityLogger.LogMethodStartAsync(
            correlationId,
            methodName,
            $"Service Bus message received. DeliveryCount={serviceBusMessage.DeliveryCount}",
            cancellationToken);

        try
        {
            var messageBody = Encoding.UTF8.GetString(serviceBusMessage.Body);
            var message = JsonSerializer.Deserialize<WeatherMessage>(messageBody, JsonOptions)
                          ?? throw new InvalidOperationException("Unable to parse weather message.");

            await _activityLogger.LogMethodTransitionAsync(correlationId, methodName, nameof(IWeatherService.GetWeatherAsync), "Calling weather service.", cancellationToken);
            var weatherResponse = await _weatherService.GetWeatherAsync(message, cancellationToken);

            var responsePayload = JsonSerializer.Serialize(weatherResponse, JsonOptions);
            await _payloadArchiveService.ArchiveResponseAsync(correlationId, responsePayload, cancellationToken);

            await _activityLogger.LogMethodEndAsync(correlationId, methodName, "Weather request processed and response archived.", cancellationToken);
        }
        catch (Exception ex)
        {
            await _activityLogger.LogErrorAsync(correlationId, methodName, "Failure while processing Service Bus message.", ex, cancellationToken);
            _logger.LogError(ex, "Service Bus processing failed. CorrelationId={CorrelationId}", correlationId);
            throw;
        }
    }
}
