using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherFunctionApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton(sp =>
            new BlobServiceClient(Environment.GetEnvironmentVariable("StorageConnectionString")));

        services.AddSingleton(sp =>
            new TableServiceClient(Environment.GetEnvironmentVariable("StorageConnectionString")));

        services.AddSingleton(sp =>
            new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection")));

        services.AddSingleton<IActivityLogger, StorageActivityLogger>();
        services.AddSingleton<IPayloadArchiveService, PayloadArchiveService>();
        services.AddSingleton<IWeatherService, WeatherService>();
    })
    .Build();

host.Run();
