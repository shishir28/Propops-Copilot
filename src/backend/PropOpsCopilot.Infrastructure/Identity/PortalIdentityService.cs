using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropOpsCopilot.Application.Abstractions;
using PropOpsCopilot.Application.Contracts;

namespace PropOpsCopilot.Infrastructure.Identity;

public sealed class PortalIdentityService(
    UserManager<AppUser> userManager,
    IOptions<PortalJwtOptions> jwtOptions) : IPortalIdentityService
{
    private readonly PortalJwtOptions options = jwtOptions.Value;

    public async Task<AuthenticationResultDto?> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return null;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return null;
        }

        var role = await ResolvePrimaryRoleAsync(user);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(options.ExpiryHours);

        return new AuthenticationResultDto(
            AccessToken: BuildToken(user, role, expiresAt),
            ExpiresAtUtc: expiresAt,
            User: Map(user, role));
    }

    public async Task<PortalUserDto?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var role = await ResolvePrimaryRoleAsync(user);
        return Map(user, role);
    }

    private async Task<string> ResolvePrimaryRoleAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? PortalRoles.PropertyManager;
    }

    private string BuildToken(AppUser user, string role, DateTimeOffset expiresAt)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static PortalUserDto Map(AppUser user, string role) =>
        new(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            role);
}
