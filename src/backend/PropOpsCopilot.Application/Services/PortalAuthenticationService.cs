using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;

namespace PropOpsCopilot.Application.Services;

public sealed class PortalAuthenticationService(IPortalIdentityService portalIdentityService)
{
    public Task<AuthenticationResultDto?> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default) =>
        portalIdentityService.AuthenticateAsync(request, cancellationToken);

    public Task<PortalUserDto?> GetCurrentUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        portalIdentityService.GetUserAsync(userId, cancellationToken);
}
