using PropOpsCopilot.Application.Contracts;

namespace PropOpsCopilot.Application.Abstractions;

public interface IPortalIdentityService
{
    Task<AuthenticationResultDto?> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<PortalUserDto?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}
