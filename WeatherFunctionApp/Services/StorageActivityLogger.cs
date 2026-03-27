using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace WeatherFunctionApp.Services;

public sealed class StorageActivityLogger : IActivityLogger
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<StorageActivityLogger> _logger;
    private readonly string _logsContainer;
    private readonly string _logsTable;

    public StorageActivityLogger(
        BlobServiceClient blobServiceClient,
        TableServiceClient tableServiceClient,
        ILogger<StorageActivityLogger> logger)
    {
        _blobServiceClient = blobServiceClient;
        _tableServiceClient = tableServiceClient;
        _logger = logger;
        _logsContainer = Environment.GetEnvironmentVariable("ActivityLogsContainer") ?? "activity-logs";
        _logsTable = Environment.GetEnvironmentVariable("ActivityLogsTable") ?? "WeatherActivityLogs";
    }

    public Task LogMethodStartAsync(string correlationId, string methodName, string details, CancellationToken cancellationToken = default) =>
        LogAsync(correlationId, methodName, "Start", details, null, cancellationToken);

    public Task LogMethodEndAsync(string correlationId, string methodName, string details, CancellationToken cancellationToken = default) =>
        LogAsync(correlationId, methodName, "End", details, null, cancellationToken);

    public Task LogMethodTransitionAsync(string correlationId, string fromMethod, string toMethod, string details, CancellationToken cancellationToken = default) =>
        LogAsync(correlationId, $"{fromMethod} -> {toMethod}", "Transition", details, null, cancellationToken);

    public Task LogErrorAsync(string correlationId, string methodName, string details, Exception exception, CancellationToken cancellationToken = default) =>
        LogAsync(correlationId, methodName, "Error", details, exception, cancellationToken);

    private async Task LogAsync(
        string correlationId,
        string methodName,
        string stage,
        string details,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;

        var logLine = $"{utcNow:O} | CorrelationId={correlationId} | Stage={stage} | Method={methodName} | Details={details}";
        if (exception is not null)
        {
            logLine += $" | Exception={exception.GetType().Name}: {exception.Message}";
        }

        await WriteLogToBlobAsync(logLine, utcNow, cancellationToken);
        await WriteLogToTableAsync(correlationId, methodName, stage, details, exception, utcNow, cancellationToken);
        _logger.LogInformation("{LogLine}", logLine);
    }

    private async Task WriteLogToBlobAsync(string logLine, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_logsContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var appendBlob = container.GetAppendBlobClient($"{utcNow:yyyy}/{utcNow:MM}/{utcNow:dd}/activity.log");
        await appendBlob.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(logLine + Environment.NewLine);
        using var stream = new MemoryStream(bytes);
        await appendBlob.AppendBlockAsync(stream, cancellationToken: cancellationToken);
    }

    private async Task WriteLogToTableAsync(
        string correlationId,
        string methodName,
        string stage,
        string details,
        Exception? exception,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var tableClient = _tableServiceClient.GetTableClient(_logsTable);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

        var entity = new ActivityLogEntity
        {
            PartitionKey = utcNow.ToString("yyyyMMdd"),
            RowKey = $"{utcNow:HHmmssfff}-{Guid.NewGuid():N}",
            CorrelationId = correlationId,
            MethodName = methodName,
            Stage = stage,
            Details = details,
            Exception = exception?.ToString()
        };

        await tableClient.AddEntityAsync(entity, cancellationToken);
    }
}
