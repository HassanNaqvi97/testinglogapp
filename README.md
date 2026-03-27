# testinglogapp

A sample **.NET 8 isolated Azure Function App** that demonstrates end-to-end activity logging and payload archival for a weather workflow using:

- Azure Functions (HTTP trigger + Service Bus trigger)
- Azure Service Bus (async processing + retries)
- Azure Storage Blob (request/response payloads + activity log file)
- Azure Table Storage (structured activity logs)

## Solution layout

- `WeatherFunctionApp/Functions/WeatherOrchestratorFunction.cs`
  - `GetWeather` HTTP trigger receives the request.
  - Request payload is persisted to Blob Storage.
  - A message is pushed to Service Bus for asynchronous processing.
  - `ProcessWeatherQueueAsync` consumes the message with `[FixedDelayRetry(3, "00:00:10")]`.
- `WeatherFunctionApp/Services/StorageActivityLogger.cs`
  - Logs every method start/end/transition/error to Blob + Table Storage.
- `WeatherFunctionApp/Services/PayloadArchiveService.cs`
  - Persists request/response JSON payloads in their own containers.
- `WeatherFunctionApp/Services/WeatherService.cs`
  - Mock weather provider implementation.

## Solution file

- `testinglogapp.sln` includes the `WeatherFunctionApp` project for Visual Studio / solution-based workflows.

## Configuration

Set these values in `WeatherFunctionApp/local.settings.json` (or app settings in Azure):

- `StorageConnectionString`
- `ServiceBusConnection`
- `ServiceBusQueueName`
- `RequestPayloadContainer`
- `ResponsePayloadContainer`
- `ActivityLogsContainer`
- `ActivityLogsTable`

## Run locally

```bash
cd WeatherFunctionApp
dotnet restore
dotnet build
func start
```

Then send a request:

```bash
curl -X POST "http://localhost:7071/api/weather/request" \
  -H "Content-Type: application/json" \
  -d '{"city":"Seattle","countryCode":"US","unit":"C"}'
```

## Logging behavior

For each method execution cycle, the app logs:

1. Method start.
2. Method transition to downstream call/method.
3. Method end when processing completes.
4. Errors with exception details on failure.

The same activity is written to:

- Blob log file: `activity-logs/yyyy/MM/dd/activity.log`
- Table rows: `WeatherActivityLogs`

Request and response payloads are written to separate containers.
