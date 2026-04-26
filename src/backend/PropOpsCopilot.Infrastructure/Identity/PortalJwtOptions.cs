namespace PropOpsCopilot.Infrastructure.Identity;

public sealed class PortalJwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryHours { get; set; } = 8;
}
