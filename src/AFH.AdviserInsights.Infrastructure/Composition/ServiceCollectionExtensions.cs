using AFH.AdviserInsights.Application.Abstractions.Auth;
using AFH.AdviserInsights.Application.Abstractions.Persistence;
using AFH.AdviserInsights.Infrastructure.Auth;
using AFH.AdviserInsights.Infrastructure.Options;
using AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdviserInsightsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AdviserInsightsOptions>(configuration.GetSection(AdviserInsightsOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IInternalServiceAuthenticator, InternalBearerServiceAuthenticator>();
        services.AddHttpClient<IIdentityClient, IdentityClient>();
        services.AddDbContextFactory<AdviserInsightsSnowflakeDbContext>((serviceProvider, options) =>
        {
            var snowflakeOptions = serviceProvider
                .GetRequiredService<IOptions<AdviserInsightsOptions>>()
                .Value
                .Snowflake;

            if (string.IsNullOrWhiteSpace(snowflakeOptions.ConnectionString))
                throw new InvalidOperationException("Missing AdviserInsights:Snowflake:ConnectionString configuration.");

            options.UseSnowflake(BuildSnowflakeConnectionString(snowflakeOptions));
        });
        services.AddScoped<IAdviserInsightsRepository, SnowflakeAdviserInsightsRepository>();
        return services;
    }

    private static string BuildSnowflakeConnectionString(SnowflakeOptions options)
    {
        var parts = options.ConnectionString!
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var keys = parts
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .Select(pair => pair[0].Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!keys.Contains("db") && !keys.Contains("database") && !string.IsNullOrWhiteSpace(options.Database))
            parts.Add($"db={options.Database.Trim()}");

        if (!keys.Contains("schema") && !string.IsNullOrWhiteSpace(options.Schema))
            parts.Add($"schema={options.Schema.Trim()}");

        return string.Join(';', parts);
    }
}
