using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Infrastructure.Persistence;

public sealed class PropOpsDbContext(DbContextOptions<PropOpsDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropOpsDbContext).Assembly);
    }
}
