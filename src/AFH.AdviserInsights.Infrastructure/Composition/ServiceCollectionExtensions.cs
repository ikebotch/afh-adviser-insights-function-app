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
        services.AddHttpClient<IIdentityClient, IdentityClient>();
        services.AddDbContextFactory<AdviserInsightsSnowflakeDbContext>((serviceProvider, options) =>
        {
            var snowflakeOptions = serviceProvider
                .GetRequiredService<IOptions<AdviserInsightsOptions>>()
                .Value
                .Snowflake;

            if (string.IsNullOrWhiteSpace(snowflakeOptions.ConnectionString))
                throw new InvalidOperationException("Missing AdviserInsights:Snowflake:ConnectionString configuration.");

            options.UseSnowflake(snowflakeOptions.ConnectionString);
        });
        services.AddScoped<IAdviserInsightsRepository, SnowflakeAdviserInsightsRepository>();
        return services;
    }
}
