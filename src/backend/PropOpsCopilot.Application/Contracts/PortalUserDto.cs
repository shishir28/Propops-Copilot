namespace PropOpsCopilot.Application.Contracts;

public sealed record PortalUserDto(
    string Id,
    string FullName,
    string Email,
    string Role);
