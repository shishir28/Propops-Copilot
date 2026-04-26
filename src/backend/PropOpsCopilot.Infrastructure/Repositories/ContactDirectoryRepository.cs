using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Infrastructure.Persistence;

namespace PropOpsCopilot.Infrastructure.Repositories;

public sealed class ContactDirectoryRepository(PropOpsDbContext dbContext) : IContactDirectoryRepository
{
    public Task<ContactDirectoryEntry?> FindByEmailAsync(string emailAddress, CancellationToken cancellationToken = default) =>
        dbContext.ContactDirectoryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.EmailAddress == emailAddress, cancellationToken);

    public Task<ContactDirectoryEntry?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        dbContext.ContactDirectoryEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.PhoneNumber == phoneNumber, cancellationToken);
}
