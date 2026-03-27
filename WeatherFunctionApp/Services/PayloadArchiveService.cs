using Azure.Storage.Blobs;

namespace WeatherFunctionApp.Services;

public sealed class PayloadArchiveService : IPayloadArchiveService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _requestContainer;
    private readonly string _responseContainer;

    public PayloadArchiveService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
        _requestContainer = Environment.GetEnvironmentVariable("RequestPayloadContainer") ?? "weather-request-payloads";
        _responseContainer = Environment.GetEnvironmentVariable("ResponsePayloadContainer") ?? "weather-response-payloads";
    }

    public Task ArchiveRequestAsync(string correlationId, string payload, CancellationToken cancellationToken = default) =>
        ArchivePayloadAsync(_requestContainer, correlationId, payload, "request", cancellationToken);

    public Task ArchiveResponseAsync(string correlationId, string payload, CancellationToken cancellationToken = default) =>
        ArchivePayloadAsync(_responseContainer, correlationId, payload, "response", cancellationToken);

    private async Task ArchivePayloadAsync(
        string containerName,
        string correlationId,
        string payload,
        string suffix,
        CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{correlationId}-{suffix}.json";
        var blob = container.GetBlobClient(blobName);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
    }
}
