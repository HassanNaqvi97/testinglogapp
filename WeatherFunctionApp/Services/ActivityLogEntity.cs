using Azure;
using Azure.Data.Tables;

namespace WeatherFunctionApp.Services;

public sealed class ActivityLogEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
