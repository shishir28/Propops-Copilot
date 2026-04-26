using PropOpsCopilot.Domain.Entities;

namespace PropOpsCopilot.Application.Abstractions;

public interface IContactDirectoryRepository
{
    Task<ContactDirectoryEntry?> FindByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);

    Task<ContactDirectoryEntry?> FindByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
