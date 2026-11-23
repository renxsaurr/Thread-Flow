using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Authentication;

public sealed class AccessTokenIssuer(
    SymmetricSecurityKey signingKey,
    string issuer,
    string audience,
    TimeSpan lifetime)
{
    public (string Token, DateTime ExpiresAtUtc) Issue(
        PlatformUser user,
        string? tenantCode,
        IReadOnlyCollection<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(lifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.UserName ?? user.Email ?? user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("is_platform_admin", user.IsPlatformAdmin ? "true" : "false")
        };

        if (user.TenantId is Guid tenantId)
        {
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
            claims.Add(new Claim("tenant_code", tenantCode ?? string.Empty));
        }

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            claims.Add(new Claim("role", role));

        if (user.IsPlatformAdmin && !roles.Contains(ThreadFlowRoles.PlatformAdmin, StringComparer.OrdinalIgnoreCase))
            claims.Add(new Claim("role", ThreadFlowRoles.PlatformAdmin));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
