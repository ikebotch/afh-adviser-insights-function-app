using AFH.AdviserInsights.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AFH.AdviserInsights.Application.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdviserInsightsApplication(this IServiceCollection services)
    {
        services.AddScoped<AdviserInsightsAccessService>();
        services.AddScoped<AdviserInsightsService>();
        return services;
    }
}
