using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Infrastructure.Identity;
using PropOpsCopilot.Infrastructure.Persistence;
using PropOpsCopilot.Infrastructure.Repositories;

namespace PropOpsCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PropOpsDb")
            ?? throw new InvalidOperationException("Connection string 'PropOpsDb' was not found.");

        services.AddDbContext<PropOpsDbContext>(options => options.UseNpgsql(connectionString));
        var jwtSection = configuration.GetSection(PortalJwtOptions.SectionName);
        services.Configure<PortalJwtOptions>(options =>
        {
            options.Issuer = jwtSection["Issuer"] ?? string.Empty;
            options.Audience = jwtSection["Audience"] ?? string.Empty;
            options.SigningKey = jwtSection["SigningKey"] ?? string.Empty;
            options.ExpiryHours = int.TryParse(jwtSection["ExpiryHours"], out var expiryHours) ? expiryHours : 8;
        });
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PropOpsDbContext>();

        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
        services.AddScoped<IPortalIdentityService, PortalIdentityService>();
        services.AddScoped<PropOpsDataSeeder>();

        return services;
    }
}
