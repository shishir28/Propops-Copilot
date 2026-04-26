using Microsoft.Extensions.DependencyInjection;
using PropOpsCopilot.Application.Services;

namespace PropOpsCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MaintenanceRequestService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<PortalAuthenticationService>();

        return services;
    }
}
