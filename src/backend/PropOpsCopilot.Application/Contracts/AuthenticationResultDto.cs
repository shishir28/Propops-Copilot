namespace PropOpsCopilot.Application.Contracts;

public sealed record AuthenticationResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    PortalUserDto User);
