using AFH.AdviserInsights.Application.Abstractions;
using AFH.AdviserInsights.Infrastructure.Clients;
using AFH.AdviserInsights.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AFH.AdviserInsights.Infrastructure.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdviserInsightsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AdviserInsightsOptions>(configuration.GetSection(AdviserInsightsOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<IIdentityClient, IdentityClient>();
        services.AddHttpClient<ISnowflakeSqlClient, SnowflakeSqlApiClient>();
        services.AddScoped<IAdviserInsightsRepository, SnowflakeAdviserInsightsRepository>();
        return services;
    }
}
