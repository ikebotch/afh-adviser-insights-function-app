using System.Text.Json;
using System.Text.Json.Serialization;
using AFH.AdviserInsights.Application.Composition;
using AFH.AdviserInsights.Infrastructure.Composition;
using AFH.Common.Errors.AzureFunctions.DependencyInjection;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((_, cfg) =>
    {
        cfg
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        AddFlattenedValuesSection(cfg);
    })
    .ConfigureServices((ctx, services) =>
    {
        if (!string.IsNullOrWhiteSpace(ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]) ||
            !string.IsNullOrWhiteSpace(ctx.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]))
        {
            services.AddApplicationInsightsTelemetryWorkerService();
        }

        services.AddAdviserInsightsApplication();
        services.AddAdviserInsightsInfrastructure(ctx.Configuration);
        services.AddAfhCommonErrorsAzureFunctions();
        ConfigureWorkerSerialization(services);
    })
    .Build();

host.Run();

static void AddFlattenedValuesSection(IConfigurationBuilder cfg)
{
    var built = cfg.Build();
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var child in built.GetSection("Values").GetChildren())
        values[child.Key] = child.Value;

    if (values.Count > 0)
        cfg.AddInMemoryCollection(values);
}

static void ConfigureWorkerSerialization(IServiceCollection services)
{
    services.Configure<WorkerOptions>(options =>
    {
        options.Serializer = new JsonObjectSerializer(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });
    });
}
